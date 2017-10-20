using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator
{
    internal class FieldDescription
    {
        public FieldDescription(int fieldId)
        {
            this.FieldId = fieldId;
        }

        public int FieldId { get; }
    }

    internal class TypeDescription
    {
        public TypeDescription(INamedTypeSymbol type, IEnumerable<FieldDescription>fields)
        {
            this.Type = type;
            this.Fields = fields.ToList();
        }

        public INamedTypeSymbol Type { get; }

        public List<FieldDescription> Fields { get; }
    }

    public static class CodeGenerator
    {
        public static async Task<CompilationUnitSyntax> GenerateCode(string projectFilePath, CancellationToken cancellationToken)
        {
            var manager = new AnalyzerManager();
            var analyzer = manager.GetProject(projectFilePath);
            var workspace = analyzer.GetWorkspace();
            var project = workspace.CurrentSolution.Projects.Single();
            var compilation = await project.GetCompilationAsync(cancellationToken);
            
            return new Generator(compilation).GenerateCode(cancellationToken);
        }
    }

    public class Generator
    {
        private readonly Compilation compilation;
        private readonly INamedTypeSymbol generateSerializerAttribute;
        private readonly INamedTypeSymbol fieldIdAttribute;

        public Generator(Compilation compilation)
        {
            this.compilation = compilation;

            this.generateSerializerAttribute = compilation.GetTypeByMetadataName("Hagar.GenerateSerializerAttribute");
            this.fieldIdAttribute = compilation.GetTypeByMetadataName("Hagar.FieldIdAttribute");
        }

        public CompilationUnitSyntax GenerateCode(CancellationToken cancellationToken)
        {
            // Collect metadata from the compilation.
            var serializableTypes = GetSerializableTypes(cancellationToken);

            // Generate code.
            foreach (var type in serializableTypes)
            {
                Console.WriteLine($"Will generate serializer for: {type}");

                {
                    var members = type.Type.GetMembers();
                    foreach (var member in members)
                    {
                        Console.WriteLine($"\t{member.Name} ({member.GetType()}) with attrs {string.Join(", ", member.GetAttributes().Select(attr => attr.AttributeClass.Name))}");
                    }
                }

                var baseType = type.Type.BaseType;
                if (baseType != null)
                {
                    var members = baseType.GetMembers();
                    foreach (var member in members)
                    {
                        Console.WriteLine($"\tBase {member.Name} ({member.GetType()}) with attrs {string.Join(", ", member.GetAttributes().Select(attr => attr.AttributeClass.Name))}");
                    }
                }
            }

            return CompilationUnit();
        }

        private List<TypeDescription> GetSerializableTypes(CancellationToken cancellationToken)
        {
            var results = new List<TypeDescription>();
            foreach (var syntaxTree in this.compilation.SyntaxTrees)
            {
                var semanticModel = this.compilation.GetSemanticModel(syntaxTree, ignoreAccessibility: false);
                var nodes = syntaxTree.GetRoot(cancellationToken).DescendantNodesAndSelf();
                foreach (var node in nodes)
                {
                    if (!(node is TypeDeclarationSyntax decl)) continue;
                    if (!this.HasGenerateSerializerAttribute(decl, semanticModel)) break;
                    var typeDescription = this.CreateTypeDescription(semanticModel, decl);
                    results.Add(typeDescription);
                }
            }

            return results;
        }

        private TypeDescription CreateTypeDescription(SemanticModel semanticModel, TypeDeclarationSyntax typeDecl)
        {
            var declared = semanticModel.GetDeclaredSymbol(typeDecl);
            var typeDescription = new TypeDescription(declared, this.GetSerializableFields(declared));
            return typeDescription;
        }

        // Returns descriptions of all fields 
        private IEnumerable<FieldDescription> GetSerializableFields(INamedTypeSymbol symbol)
        {
            foreach (var member in symbol.GetMembers())
            {
                var fieldIdAttr = member.GetAttributes().SingleOrDefault(attr => attr.AttributeClass.Equals(this.fieldIdAttribute));
                if (fieldIdAttr == null) continue;

                var id = (int) fieldIdAttr.ConstructorArguments.First().Value;
                yield return new FieldDescription(id);
            }
        }

        // Returns true if the type declaration has the [GenerateSerializer] attribute.
        private bool HasGenerateSerializerAttribute(TypeDeclarationSyntax node, SemanticModel model)
        {
            switch (node)
            {
                case ClassDeclarationSyntax classDecl:
                    return HasAttribute(classDecl.AttributeLists);
                case StructDeclarationSyntax structDecl:
                    return HasAttribute(structDecl.AttributeLists);
                default:
                    return false;
            }

            bool HasAttribute(SyntaxList<AttributeListSyntax> attributeLists)
            {
                return attributeLists
                    .SelectMany(list => list.Attributes)
                    .Select(attr => model.GetTypeInfo(attr).ConvertedType)
                    .Any(attrType => attrType.Equals(this.generateSerializerAttribute));
            }
        }
    }
}