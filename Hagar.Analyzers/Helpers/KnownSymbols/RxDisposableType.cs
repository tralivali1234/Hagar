using Hagar.Analyzers.Helpers.KnownSymbols.BaseTypes;

namespace Hagar.Analyzers.Helpers.KnownSymbols
{
    internal class RxDisposableType : QualifiedType
    {
        internal readonly QualifiedMethod Create;

        internal RxDisposableType()
            : base("System.Reactive.Disposables.Disposable")
        {
            this.Create = new QualifiedMethod(this, nameof(this.Create));
        }
    }
}