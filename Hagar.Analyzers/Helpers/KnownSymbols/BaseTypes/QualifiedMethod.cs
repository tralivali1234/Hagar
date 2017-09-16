using Microsoft.CodeAnalysis;

namespace Hagar.Analyzers.Helpers.KnownSymbols.BaseTypes
{
    internal class QualifiedMethod : QualifiedMember<IMethodSymbol>
    {
        public QualifiedMethod(QualifiedType containingType, string name)
            : base(containingType, name)
        {
        }
    }
}