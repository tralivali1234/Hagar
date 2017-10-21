using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator.SyntaxGeneration
{
    internal static class SyntaxFactoryUtility
    {
        /// <summary>
        /// Returns <see cref="TypeSyntax"/> for the provided <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="includeNamespace">
        /// A value indicating whether or not to include the namespace name.
        /// </param>
        /// <param name="includeGenericParameters">
        /// Whether or not to include the names of generic parameters in the result.
        /// </param>
        /// <param name="getNameFunc">The delegate used to get the unadorned, simple type name of <paramref name="type"/>.</param>
        /// <returns>
        /// <see cref="TypeSyntax"/> for the provided <paramref name="type"/>.
        /// </returns>
        public static TypeSyntax GetTypeSyntax(
            this Type type,
            bool includeNamespace = true,
            bool includeGenericParameters = true,
            Func<Type, string> getNameFunc = null)
        {
            if (type == typeof(void))
            {
                return PredefinedType(Token(SyntaxKind.VoidKeyword));
            }

            return
                ParseTypeName(
                    type.GetParseableName(
                        new TypeUtility.TypeFormattingOptions(
                            includeNamespace: includeNamespace,
                            includeGenericParameters: includeGenericParameters),
                        getNameFunc));
        }

        /// <summary>
        /// Returns <see cref="NameSyntax"/> specified <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="includeNamespace">
        /// A value indicating whether or not to include the namespace name.
        /// </param>
        /// <returns>
        /// <see cref="NameSyntax"/> specified <paramref name="type"/>.
        /// </returns>
        public static NameSyntax GetNameSyntax(this Type type, bool includeNamespace = true)
        {
            return ParseName(type.GetParseableName(new TypeUtility.TypeFormattingOptions(includeNamespace: includeNamespace)));
        }

        /// <summary>
        /// Returns <see cref="NameSyntax"/> specified <paramref name="method"/>.
        /// </summary>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <returns>
        /// <see cref="NameSyntax"/> specified <paramref name="method"/>.
        /// </returns>
        public static SimpleNameSyntax GetNameSyntax(this MethodInfo method)
        {
            var plainName = method.GetUnadornedMethodName();
            if (!method.IsGenericMethod) return plainName.ToIdentifierName();
            var args = method.GetGenericArguments().Select(arg => arg.GetTypeSyntax());
            return plainName.ToGenericName().AddTypeArgumentListArguments(args.ToArray());
        }

        /// <summary>
        /// Returns <see cref="ParenthesizedExpressionSyntax"/>  representing parenthesized binary expression of  <paramref name="bindingFlags"/>.
        /// </summary>
        /// <param name="operationKind">
        /// The kind of the binary expression.
        /// </param> 
        /// <param name="bindingFlags">
        /// The binding flags.
        /// </param>
        /// <returns>
        /// <see cref="ParenthesizedExpressionSyntax"/> representing parenthesized binary expression of <paramref name="bindingFlags"/>.
        /// </returns>
        public static ParenthesizedExpressionSyntax GetBindingFlagsParenthesizedExpressionSyntax(SyntaxKind operationKind, params BindingFlags[] bindingFlags)
        {
            if (bindingFlags.Length < 2)
            {
                throw new ArgumentOutOfRangeException(
                    "bindingFlags",
                    string.Format("Can't create parenthesized binary expression with {0} arguments", bindingFlags.Length));
            }

            var flags = IdentifierName("System").Member("Reflection").Member("BindingFlags");
            var bindingFlagsBinaryExpression = BinaryExpression(
                operationKind,
                flags.Member(bindingFlags[0].ToString()),
                flags.Member(bindingFlags[1].ToString()));
            for (var i = 2; i < bindingFlags.Length; i++)
            {
                bindingFlagsBinaryExpression = BinaryExpression(
                    operationKind,
                    bindingFlagsBinaryExpression,
                    flags.Member(bindingFlags[i].ToString()));
            }

            return ParenthesizedExpression(bindingFlagsBinaryExpression);
        }

        /// <summary>
        /// Returns <see cref="ArrayTypeSyntax"/> representing the array form of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="includeNamespace">
        /// A value indicating whether or not to include the namespace name.
        /// </param>
        /// <returns>
        /// <see cref="ArrayTypeSyntax"/> representing the array form of <paramref name="type"/>.
        /// </returns>
        public static ArrayTypeSyntax GetArrayTypeSyntax(this Type type, bool includeNamespace = true)
        {
            return ArrayType(ParseTypeName(type.GetParseableName(new TypeUtility.TypeFormattingOptions(includeNamespace: includeNamespace))))
                .AddRankSpecifiers(ArrayRankSpecifier().AddSizes(OmittedArraySizeExpression()));
        }

        /// <summary>
        /// Returns the method declaration syntax for the provided method.
        /// </summary>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <returns>
        /// The method declaration syntax for the provided method.
        /// </returns>
        public static MethodDeclarationSyntax GetDeclarationSyntax(this MethodInfo method)
        {
            var syntax =
                MethodDeclaration(method.ReturnType.GetTypeSyntax(), method.Name.ToIdentifier())
                    .WithParameterList(ParameterList().AddParameters(method.GetParameterListSyntax()));
            if (method.IsGenericMethodDefinition)
            {
                syntax = syntax.WithTypeParameterList(TypeParameterList().AddParameters(method.GetTypeParameterListSyntax()));

                // Handle type constraints on type parameters.
                var typeParameters = method.GetGenericArguments();
                var typeParameterConstraints = new List<TypeParameterConstraintClauseSyntax>();
                foreach (var arg in typeParameters)
                {
                    typeParameterConstraints.AddRange(GetTypeParameterConstraints(arg));
                }

                if (typeParameterConstraints.Count > 0)
                {
                    syntax = syntax.AddConstraintClauses(typeParameterConstraints.ToArray());
                }
            }

            if (method.IsPublic)
            {
                syntax = syntax.AddModifiers(Token(SyntaxKind.PublicKeyword));
            }
            else if (method.IsPrivate)
            {
                syntax = syntax.AddModifiers(Token(SyntaxKind.PrivateKeyword));
            }
            else if (method.IsFamily)
            {
                syntax = syntax.AddModifiers(Token(SyntaxKind.ProtectedKeyword));
            }

            return syntax;
        }

        /// <summary>
        /// Returns the method declaration syntax for the provided constructor.
        /// </summary>
        /// <param name="constructor">
        /// The constructor.
        /// </param>
        /// <param name="typeName">
        /// The name of the type which the constructor will reside on.
        /// </param>
        /// <returns>
        /// The method declaration syntax for the provided constructor.
        /// </returns>
        public static ConstructorDeclarationSyntax GetDeclarationSyntax(this ConstructorInfo constructor, string typeName)
        {
            var syntax =
                ConstructorDeclaration(typeName.ToIdentifier())
                    .WithParameterList(ParameterList().AddParameters(constructor.GetParameterListSyntax()));
            if (constructor.IsPublic)
            {
                syntax = syntax.AddModifiers(Token(SyntaxKind.PublicKeyword));
            }
            else if (constructor.IsPrivate)
            {
                syntax = syntax.AddModifiers(Token(SyntaxKind.PrivateKeyword));
            }
            else if (constructor.IsFamily)
            {
                syntax = syntax.AddModifiers(Token(SyntaxKind.ProtectedKeyword));
            }

            return syntax;
        }

        /// <summary>
        /// Returns the name of the provided parameter.
        /// If the parameter has no name (possible in F#),
        /// it returns a name computed by suffixing "arg" with the parameter's index
        /// </summary>
        /// <param name="parameter"> The parameter. </param>
        /// <param name="parameterIndex"> The parameter index in the list of parameters. </param>
        /// <returns> The parameter name. </returns>
        public static string GetOrCreateName(this ParameterInfo parameter, int parameterIndex)
        {
            var argName = parameter.Name;
            if (string.IsNullOrWhiteSpace(argName))
            {
                argName = string.Format(CultureInfo.InvariantCulture, "arg{0:G}", parameterIndex);
            }
            return argName;
        }

        /// <summary>
        /// Returns the parameter list syntax for the provided method.
        /// </summary>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <returns>
        /// The parameter list syntax for the provided method.
        /// </returns>
        public static ParameterSyntax[] GetParameterListSyntax(this MethodInfo method)
        {
            return
                method.GetParameters()
                      .Select(
                          (parameter, parameterIndex) =>
                              Parameter(parameter.GetOrCreateName(parameterIndex).ToIdentifier())
                                  .WithType(parameter.ParameterType.GetTypeSyntax()))
                      .ToArray();
        }

        /// <summary>
        /// Returns the parameter list syntax for the provided method.
        /// </summary>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <returns>
        /// The parameter list syntax for the provided method.
        /// </returns>
        public static TypeParameterSyntax[] GetTypeParameterListSyntax(this MethodInfo method)
        {
            return
                method.GetGenericArguments()
                      .Select(parameter => TypeParameter(parameter.Name))
                      .ToArray();
        }

        /// <summary>
        /// Returns the parameter list syntax for the provided constructor
        /// </summary>
        /// <param name="constructor">
        /// The constructor.
        /// </param>
        /// <returns>
        /// The parameter list syntax for the provided constructor.
        /// </returns>
        public static ParameterSyntax[] GetParameterListSyntax(this ConstructorInfo constructor)
        {
            return
                constructor.GetParameters()
                           .Select(parameter => Parameter(parameter.Name.ToIdentifier()).WithType(parameter.ParameterType.GetTypeSyntax()))
                           .ToArray();
        }

        /// <summary>
        /// Returns type constraint syntax for the provided generic type argument.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// Type constraint syntax for the provided generic type argument.
        /// </returns>
        public static TypeParameterConstraintClauseSyntax[] GetTypeConstraintSyntax(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsGenericTypeDefinition) return new TypeParameterConstraintClauseSyntax[0];
            var constraints = new List<TypeParameterConstraintClauseSyntax>();
            foreach (var genericParameter in typeInfo.GetGenericArguments())
            {
                constraints.AddRange(GetTypeParameterConstraints(genericParameter));
            }

            return constraints.ToArray();
        }

        private static TypeParameterConstraintClauseSyntax[] GetTypeParameterConstraints(Type genericParameter)
        {
            var results = new List<TypeParameterConstraintClauseSyntax>();
            var parameterConstraints = new List<TypeParameterConstraintSyntax>();
            var attributes = genericParameter.GetTypeInfo().GenericParameterAttributes;

            // The "class" or "struct" constraints must come first.
            if (attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
            {
                parameterConstraints.Add(ClassOrStructConstraint(SyntaxKind.ClassConstraint));
            }
            else if (attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
            {
                parameterConstraints.Add(ClassOrStructConstraint(SyntaxKind.StructConstraint));
            }

            // Follow with the base class or interface constraints.
            foreach (var genericType in genericParameter.GetTypeInfo().GetGenericParameterConstraints())
            {
                // If the "struct" constraint was specified, skip the corresponding "ValueType" constraint.
                if (genericType == typeof(ValueType))
                {
                    continue;
                }

                parameterConstraints.Add(TypeConstraint(genericType.GetTypeSyntax()));
            }

            // The "new()" constraint must be the last constraint in the sequence.
            if (attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint)
                && !attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
            {
                parameterConstraints.Add(ConstructorConstraint());
            }

            if (parameterConstraints.Count > 0)
            {
                results.Add(
                    TypeParameterConstraintClause(genericParameter.Name)
                        .AddConstraints(parameterConstraints.ToArray()));
            }

            return results.ToArray();
        }

        /// <summary>
        /// Returns member access syntax.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <returns>
        /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
        /// </returns>
        public static MemberAccessExpressionSyntax Member(this ExpressionSyntax instance, string member)
        {
            return instance.Member(member.ToIdentifierName());
        }

        /// <summary>
        /// Returns qualified name syntax.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <returns>
        /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
        /// </returns>
        public static QualifiedNameSyntax Qualify(this NameSyntax instance, string member)
        {
            return instance.Qualify(member.ToIdentifierName());
        }

        /// <summary>
        /// Returns member access syntax.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <param name="genericTypes">
        /// The generic type parameters.
        /// </param>
        /// <returns>
        /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
        /// </returns>
        public static MemberAccessExpressionSyntax Member(
            this ExpressionSyntax instance,
            string member,
            params Type[] genericTypes)
        {
            return
                instance.Member(
                    member.ToGenericName()
                          .AddTypeArgumentListArguments(genericTypes.Select(_ => _.GetTypeSyntax()).ToArray()));
        }

        /// <summary>
        /// Returns member access syntax.
        /// </summary>
        /// <typeparam name="TInstance">
        /// The class type.
        /// </typeparam>
        /// <typeparam name="T">
        /// The member return type.
        /// </typeparam>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <param name="genericTypes">
        /// The generic type parameters.
        /// </param>
        /// <returns>
        /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
        /// </returns>
        public static MemberAccessExpressionSyntax Member<TInstance, T>(
            this ExpressionSyntax instance,
            Expression<Func<TInstance, T>> member,
            params Type[] genericTypes)
        {
            switch (member.Body)
            {
                case MethodCallExpression methodCall:
                    if (genericTypes != null && genericTypes.Length > 0)
                    {
                        return instance.Member(methodCall.Method.Name, genericTypes);
                    }
                    return instance.Member(methodCall.Method.Name.ToIdentifierName());
                case MemberExpression memberAccess:
                    if (genericTypes != null && genericTypes.Length > 0)
                    {
                        return instance.Member(memberAccess.Member.Name, genericTypes);
                    }
                    return instance.Member(memberAccess.Member.Name.ToIdentifierName());
            }

            throw new ArgumentException("Expression type unsupported.");
        }

        /// <summary>
        /// Returns method invocation syntax.
        /// </summary>
        /// <typeparam name="T">
        /// The method return type.
        /// </typeparam>
        /// <param name="expression">
        /// The invocation expression.
        /// </param>
        /// <returns>
        /// The resulting <see cref="InvocationExpressionSyntax"/>.
        /// </returns>
        public static InvocationExpressionSyntax Invoke<T>(this Expression<Func<T>> expression)
        {
            if (expression.Body is MethodCallExpression methodCall)
            {
                var decl = methodCall.Method.DeclaringType;
                return
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            decl.GetNameSyntax(),
                            methodCall.Method.GetNameSyntax()));
            }

            throw new ArgumentException("Expression type unsupported.");
        }

        /// <summary>
        /// Returns method invocation syntax.
        /// </summary>
        /// <param name="expression">
        /// The invocation expression.
        /// </param>
        /// <param name="instance">
        /// The instance to invoke this method on, or <see langword="null"/> for static invocation.
        /// </param>
        /// <returns>
        /// The resulting <see cref="InvocationExpressionSyntax"/>.
        /// </returns>
        public static InvocationExpressionSyntax Invoke(this Expression<Action> expression, ExpressionSyntax instance = null)
        {
            if (expression.Body is MethodCallExpression methodCall)
            {
                if (instance == null && methodCall.Method.IsStatic)
                {
                    instance = methodCall.Method.DeclaringType.GetNameSyntax();
                }

                return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        instance,
                        methodCall.Method.Name.ToIdentifierName()));
            }

            throw new ArgumentException("Expression type unsupported.");
        }

        /// <summary>
        /// Returns method invocation syntax.
        /// </summary>
        /// <typeparam name="T">The argument type of <paramref name="expression"/>.</typeparam>
        /// <param name="expression">
        /// The invocation expression.
        /// </param>
        /// <param name="instance">
        /// The instance to invoke this method on, or <see langword="null"/> for static invocation.
        /// </param>
        /// <returns>
        /// The resulting <see cref="InvocationExpressionSyntax"/>.
        /// </returns>
        public static InvocationExpressionSyntax Invoke<T>(this Expression<Action<T>> expression, ExpressionSyntax instance = null)
        {
            if (expression.Body is MethodCallExpression methodCall)
            {
                var decl = methodCall.Method.DeclaringType;
                return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        instance ?? decl.GetNameSyntax(),
                        methodCall.Method.Name.ToIdentifierName()));
            }

            throw new ArgumentException("Expression type unsupported.");
        }

        /// <summary>
        /// Returns member access syntax.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <returns>
        /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
        /// </returns>
        public static MemberAccessExpressionSyntax Member(this ExpressionSyntax instance, IdentifierNameSyntax member)
        {
            return MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, instance, member);
        }

        /// <summary>
        /// Returns member access syntax.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <returns>
        /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
        /// </returns>
        public static MemberAccessExpressionSyntax Member(this ExpressionSyntax instance, GenericNameSyntax member)
        {
            return MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, instance, member);
        }

        /// <summary>
        /// Returns member access syntax.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="member">
        /// The member.
        /// </param>
        /// <returns>
        /// The resulting <see cref="MemberAccessExpressionSyntax"/>.
        /// </returns>
        public static QualifiedNameSyntax Qualify(this NameSyntax instance, IdentifierNameSyntax member)
        {
            return QualifiedName(instance, member).WithDotToken(Token(SyntaxKind.DotToken));
        }
    }
}