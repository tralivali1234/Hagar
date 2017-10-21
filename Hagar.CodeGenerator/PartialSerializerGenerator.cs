using System;
using System.Collections.Generic;
using System.Linq;
using Hagar.CodeGenerator.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    internal class LibraryTypes
    {
        public static LibraryTypes FromCompilation(Compilation compilation)
        {
            return new LibraryTypes
            {
                PartialSerializer = compilation.GetTypeByMetadataName("Hagar.Serializer.IPartialSerializer`1"),
                FieldCodec = compilation.GetTypeByMetadataName("Hagar.Codec.IFieldCodec`1"),
                Writer = compilation.GetTypeByMetadataName("Hagar.Buffers.Writer"),
                Reader = compilation.GetTypeByMetadataName("Hagar.Buffers.Reader"),
                SerializerSession = compilation.GetTypeByMetadataName("Hagar.Session.SerializerSession"),
                ObjectType = compilation.GetSpecialType(SpecialType.System_Object),
            };
        }

        public INamedTypeSymbol ObjectType { get; set; }

        public INamedTypeSymbol SerializerSession { get; set; }

        public INamedTypeSymbol Reader { get; set; }

        public INamedTypeSymbol Writer { get; set; }

        public INamedTypeSymbol FieldCodec { get; set; }

        public INamedTypeSymbol PartialSerializer { get; set; }
    }

    internal class SerializerFieldDescription
    {
        public SerializerFieldDescription(ITypeSymbol fieldType, string fieldName)
        {
            this.FieldType = fieldType;
            this.FieldName = fieldName;
        }

        public ITypeSymbol FieldType { get; }
        public string FieldName { get; }
    }

    internal class CodecFieldDescription : SerializerFieldDescription
    {
        public CodecFieldDescription(ITypeSymbol fieldType, string fieldName, ITypeSymbol underlyingType) : base(fieldType, fieldName)
        {
            this.UnderlyingType = underlyingType;
        }

        public ITypeSymbol UnderlyingType { get; }
    }

    internal static class PartialSerializerGenerator
    {
        private const string ClassPrefix = CodeGenerator.CodeGeneratorName;
        private const string BaseTypeSerializerFieldName = "baseTypeSerializer";
        private const string SerializeMethodName = "Serialize";
        private const string DeserializeMethodName = "Deserialize";

        public static ClassDeclarationSyntax GenerateSerializer(Compilation compilation, TypeDescription typeDescription)
        {
            var type = typeDescription.Type;
            var simpleClassName = $"{ClassPrefix}_{type.Name}";

            var libraryTypes = LibraryTypes.FromCompilation(compilation);
            var partialSerializerInterface = libraryTypes.PartialSerializer.Construct(type).ToTypeSyntax();

            var fieldDescriptions = GetFieldDescriptions(typeDescription, libraryTypes);
            var fields = GetFieldDeclarations(fieldDescriptions);
            var ctor = GenerateConstructor(simpleClassName, fieldDescriptions);

            var serializeMethod = GenerateSerializeMethod(typeDescription, fieldDescriptions, libraryTypes);
            var deserializeMethod = GenerateDeserializeMethod(typeDescription, fieldDescriptions, libraryTypes);

            var classDeclaration = ClassDeclaration(simpleClassName)
                .AddBaseListTypes(SimpleBaseType(partialSerializerInterface))
                .AddModifiers(Token(SyntaxKind.InternalKeyword), Token(SyntaxKind.SealedKeyword))
                .AddAttributeLists(AttributeList(SingletonSeparatedList(CodeGenerator.GetGeneratedCodeAttributeSyntax())))
                .AddMembers(fields)
                .AddMembers(ctor, serializeMethod, deserializeMethod);
            if (type.IsUnboundGenericType)
            {
#warning handle generic type constraints & variance?
                classDeclaration = classDeclaration.WithTypeParameterList(TypeParameterList(SeparatedList(type.TypeParameters.Select(tp => TypeParameter(tp.Name)))));
            }

            return classDeclaration;
        }

        private static MemberDeclarationSyntax[] GetFieldDeclarations(List<SerializerFieldDescription> fieldDescriptions)
        {
            return fieldDescriptions.Select(
                                        f => (MemberDeclarationSyntax) FieldDeclaration(VariableDeclaration(f.FieldType.ToTypeSyntax(), SingletonSeparatedList(VariableDeclarator(f.FieldName))))
                                            .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword)))
                                    .ToArray();
        }

        private static ConstructorDeclarationSyntax GenerateConstructor(string simpleClassName, List<SerializerFieldDescription> fields)
        {
            var parameters = fields.Select(f => Parameter(f.FieldName.ToIdentifier()).WithType(f.FieldType.ToTypeSyntax()));
            var body = fields.Select(
                f => (StatementSyntax) ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, ThisExpression().Member(f.FieldName.ToIdentifierName()), f.FieldName.ToIdentifierName())));
            return ConstructorDeclaration(simpleClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(parameters.ToArray())
                .AddBodyStatements(body.ToArray());
        }

        private static List<SerializerFieldDescription> GetFieldDescriptions(TypeDescription typeDescription, LibraryTypes libraryTypes)
        {
            var type = typeDescription.Type;
            var fields = new List<SerializerFieldDescription>();
            if (HasComplexBaseType(type))
            {
                fields.Add(new SerializerFieldDescription(libraryTypes.PartialSerializer.Construct(type.BaseType), BaseTypeSerializerFieldName));
            }

            fields.AddRange(typeDescription.Members.Select(m => GetCodecType(m.Type, libraryTypes)).Distinct().Select(GetDescription));
            return fields;

            CodecFieldDescription GetDescription(ITypeSymbol t)
            {
                var codecType = libraryTypes.FieldCodec.Construct(t);
                var fieldName = ToLowerCamelCase(t.Name) + "Codec";
                return new CodecFieldDescription(codecType, fieldName, t);
            }

            string ToLowerCamelCase(string input) => char.IsLower(input, 0) ? input : char.ToLowerInvariant(input[0]) + input.Substring(1);
        }

        private static ITypeSymbol GetCodecType(ITypeSymbol type, LibraryTypes libraryTypes)
        {
            if (type is IArrayTypeSymbol)
                return libraryTypes.ObjectType;
            if (type is IPointerTypeSymbol pointerType)
                throw new NotSupportedException($"Cannot serialize pointer type {pointerType.Name}");
            return type;
        }

        private static bool HasComplexBaseType(INamedTypeSymbol type)
        {
            return type.BaseType != null && type.BaseType.SpecialType != SpecialType.System_Object;
        }

        private static MemberDeclarationSyntax GenerateSerializeMethod(TypeDescription typeDescription, List<SerializerFieldDescription> fieldDescriptions, LibraryTypes libraryTypes)
        {
            var returnType = PredefinedType(Token(SyntaxKind.VoidKeyword));

            var writerParam = "writer".ToIdentifierName();
            var sessionParam = "session".ToIdentifierName();
            var instanceParam = "instance".ToIdentifierName();

            var body = new List<StatementSyntax>();
            if (HasComplexBaseType(typeDescription.Type))
            {
                body.Add(
                    ExpressionStatement(
                        InvocationExpression(
                            ThisExpression().Member(BaseTypeSerializerFieldName.ToIdentifierName()).Member(SerializeMethodName),
                            ArgumentList(SeparatedList(new[] {Argument(writerParam), Argument(sessionParam), Argument(instanceParam)})))));
                body.Add(ExpressionStatement(InvocationExpression(writerParam.Member("WriteEndBase"), ArgumentList())));
            }

            foreach (var member in typeDescription.Members)
            {
                var codec = fieldDescriptions.OfType<CodecFieldDescription>().First(f => f.UnderlyingType.Equals(GetCodecType(member.Type, libraryTypes)));
                body.Add(
                    ExpressionStatement(
                        InvocationExpression(
                            ThisExpression().Member(codec.FieldName).Member("WriteField"),
                            ArgumentList(
                                SeparatedList(
                                    new[]
                                    {
                                        Argument(writerParam),
                                        Argument(sessionParam),
                                        Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(member.FieldId))),
                                        Argument(instanceParam.Member(member.Member.Name))
                                    })))));
            }

            return MethodDeclaration(returnType, SerializeMethodName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(
                    Parameter("writer".ToIdentifier()).WithType(libraryTypes.Writer.ToTypeSyntax()),
                    Parameter("session".ToIdentifier()).WithType(libraryTypes.SerializerSession.ToTypeSyntax()),
                    Parameter("instance".ToIdentifier()).WithType(typeDescription.Type.ToTypeSyntax()))
                .AddBodyStatements(body.ToArray());
        }

        private static MemberDeclarationSyntax GenerateDeserializeMethod(TypeDescription typeDescription, List<SerializerFieldDescription> fieldDescriptions, LibraryTypes libraryTypes)
        {
            var returnType = PredefinedType(Token(SyntaxKind.VoidKeyword));

            var readerParam = "reader".ToIdentifierName();
            var sessionParam = "session".ToIdentifierName();
            var instanceParam = "instance".ToIdentifierName();
            var fieldIdVar = "fieldId".ToIdentifierName();
            var headerVar = "header".ToIdentifierName();

            var body = new List<StatementSyntax>();

            // C#: uint fieldId = 0;
            body.Add(
                LocalDeclarationStatement(
                    VariableDeclaration(
                        PredefinedType(Token(SyntaxKind.UIntKeyword)),
                        SingletonSeparatedList(VariableDeclarator(fieldIdVar.Identifier).WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))))))));


            if (HasComplexBaseType(typeDescription.Type))
            {
                // C#: this.baseTypeSerializer.Deserialize(reader, session, instance);
                body.Add(
                    ExpressionStatement(
                        InvocationExpression(
                            ThisExpression().Member(BaseTypeSerializerFieldName.ToIdentifierName()).Member(DeserializeMethodName),
                            ArgumentList(SeparatedList(new[] {Argument(readerParam), Argument(sessionParam), Argument(instanceParam)})))));
            }

            body.Add(WhileStatement(LiteralExpression(SyntaxKind.TrueLiteralExpression), Block(GetDeserializerLoopBody())));

            return MethodDeclaration(returnType, DeserializeMethodName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(
                    Parameter(readerParam.Identifier).WithType(libraryTypes.Writer.ToTypeSyntax()),
                    Parameter(sessionParam.Identifier).WithType(libraryTypes.SerializerSession.ToTypeSyntax()),
                    Parameter(instanceParam.Identifier).WithType(typeDescription.Type.ToTypeSyntax()))
                .AddBodyStatements(body.ToArray());

            List<StatementSyntax> GetDeserializerLoopBody()
            {
                // Create the loop body.
                var result = new List<StatementSyntax>();

                // C#: var header = reader.ReadFieldHeader(session);
                result.Add(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            IdentifierName("var"),
                            SingletonSeparatedList(
                                VariableDeclarator(headerVar.Identifier)
                                    .WithInitializer(EqualsValueClause(InvocationExpression(readerParam.Member("ReadFieldHeader"), ArgumentList(SingletonSeparatedList(Argument(sessionParam))))))))));

                // C#: if (header.IsEndBaseOrEndObject) break;
                result.Add(
                    IfStatement(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, headerVar, IdentifierName("IsEndBaseOrEndObject")), BreakStatement()));

                // C#: fieldId += header.FieldIdDelta;
                result.Add(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.AddAssignmentExpression,
                            fieldIdVar,
                            Token(SyntaxKind.PlusEqualsToken),
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, headerVar, IdentifierName("FieldIdDelta")))));

                // C#: switch (fieldId) { ... }
                result.Add(SwitchStatement(fieldIdVar, List(GetSwitchSections())));
                return result;
            }

            // Creates switch sections for each member.
            List<SwitchSectionSyntax> GetSwitchSections()
            {
                var switchSections = new List<SwitchSectionSyntax>();
                foreach (var member in typeDescription.Members)
                {
                    // C#: case <fieldId>:
                    var label = CaseSwitchLabel(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(member.FieldId)));

                    // C#: instance.<member> = this.<codec>.ReadValue(reader, session, header);
                    var codec = fieldDescriptions.OfType<CodecFieldDescription>().First(f => f.UnderlyingType.Equals(GetCodecType(member.Type, libraryTypes)));
                    ExpressionSyntax readValueExpression = InvocationExpression(
                        ThisExpression().Member(codec.FieldName).Member("ReadValue"),
                        ArgumentList(SeparatedList(new[] {Argument(readerParam), Argument(sessionParam), Argument(headerVar)})));
                    if (!codec.UnderlyingType.Equals(member.Type))
                    {
                        readValueExpression = CastExpression(member.Type.ToTypeSyntax(), readValueExpression);
                    }

                    var memberAssignment = ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, instanceParam.Member(member.Member.Name), readValueExpression));
                    var caseBody = List(new StatementSyntax[] {memberAssignment, BreakStatement()});

                    // Create the switch section with a break at the end.
                    // C#: break;
                    switchSections.Add(SwitchSection(SingletonList<SwitchLabelSyntax>(label), caseBody));
                }

                // Add the default switch section.
                var consumeUnknown = ExpressionStatement(InvocationExpression(readerParam.Member("ConsumeUnknownField"), ArgumentList(SeparatedList(new[] {Argument(sessionParam), Argument(headerVar)}))));
                switchSections.Add(SwitchSection(SingletonList<SwitchLabelSyntax>(DefaultSwitchLabel()), List(new StatementSyntax[] {consumeUnknown, BreakStatement()})));

                return switchSections;
            }
        }
    }
}
