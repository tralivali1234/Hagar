using Hagar.Analyzers.Helpers.KnownSymbols.BaseTypes;

namespace Hagar.Analyzers.Helpers.KnownSymbols
{
    internal class XunitAssertType : QualifiedType
    {
        internal readonly QualifiedMethod Equal;

        internal XunitAssertType()
            : base("Xunit.Assert")
        {
            this.Equal = new QualifiedMethod(this, nameof(this.Equal));
        }
    }
}