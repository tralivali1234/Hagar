using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hagar.Analyzers.Helpers.SyntaxtTreeHelpers
{
    internal static class PropertyDeclarationSyntaxExt
    {
        internal static bool IsAutoProperty(this PropertyDeclarationSyntax property)
        {
            AccessorDeclarationSyntax setter = null;
            if (property.TryGetGetAccessorDeclaration(out AccessorDeclarationSyntax getter) ||
                property.TryGetSetAccessorDeclaration(out setter))
            {
                return getter?.Body == null && setter?.Body == null;
            }

            return false;
        }
    }
}