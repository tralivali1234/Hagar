using System;
using Hagar.Analyzers.Helpers.Collection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hagar.Analyzers.Helpers.SyntaxtTreeHelpers
{
    internal static class FieldDeclarationSyntaxExt
    {
        internal static string Name(this FieldDeclarationSyntax declaration)
        {
            VariableDeclaratorSyntax variable = null;
            if (declaration?.Declaration?.Variables.TryGetSingle(out variable) == true)
            {
                return variable.Identifier.ValueText;
            }

            throw new InvalidOperationException($"Could not get name of field {declaration}");
        }

        internal static SyntaxToken Identifier(this FieldDeclarationSyntax declaration)
        {
            VariableDeclaratorSyntax variable = null;
            if (declaration?.Declaration?.Variables.TryGetSingle(out variable) == true)
            {
                return variable.Identifier;
            }

            throw new InvalidOperationException($"Could not get name of field {declaration}");
        }
    }
}