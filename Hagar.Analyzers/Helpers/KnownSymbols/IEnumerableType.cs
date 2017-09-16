using Hagar.Analyzers.Helpers.KnownSymbols.BaseTypes;

namespace Hagar.Analyzers.Helpers.KnownSymbols
{
    // ReSharper disable once InconsistentNaming
    internal class IEnumerableType : QualifiedType
    {
        internal readonly QualifiedMethod GetEnumerator;

        public IEnumerableType()
            : base("System.Collections.IEnumerable")
        {
            this.GetEnumerator = new QualifiedMethod(this, nameof(this.GetEnumerator));
        }
    }
}