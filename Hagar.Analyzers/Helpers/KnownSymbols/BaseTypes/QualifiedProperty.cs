using Microsoft.CodeAnalysis;

namespace Hagar.Analyzers.Helpers.KnownSymbols.BaseTypes
{
    internal class QualifiedProperty : QualifiedMember<IPropertySymbol>
    {
        public QualifiedProperty(QualifiedType containingType, string name)
            : base(containingType, name)
        {
        }
    }
}