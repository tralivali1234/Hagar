using Hagar.Analyzers.Helpers.KnownSymbols.BaseTypes;

namespace Hagar.Analyzers.Helpers.KnownSymbols
{
    internal class StringType : QualifiedType
    {
        internal readonly QualifiedMethod Format;

        internal StringType()
            : base("System.String")
        {
            this.Format = new QualifiedMethod(this, nameof(this.Format));
        }
    }
}