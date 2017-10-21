using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Hagar.CodeGenerator.SyntaxGeneration
{
    internal static class SymbolExtensions
    {
        public static TypeSyntax ToTypeSyntax(this ITypeSymbol typeSymbol)
        {
            return ParseTypeName(typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }
    }
}
