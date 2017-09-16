using System.Threading;
using Hagar.Analyzers.Helpers.Collection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Hagar.Analyzers.Helpers.SymbolHelpers
{
    internal static class FieldSymbolExt
    {
        internal static bool TryGetAssignedValue(this IFieldSymbol field, CancellationToken cancellationToken, out ExpressionSyntax value)
        {
            value = null;
            if (field == null)
            {
                return false;
            }

            if (field.DeclaringSyntaxReferences.TryGetLast(out SyntaxReference reference))
            {
                var declarator = reference.GetSyntax(cancellationToken) as VariableDeclaratorSyntax;
                value = declarator?.Initializer?.Value;
                return value != null;
            }

            return false;
        }
    }
}