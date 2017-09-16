using Hagar.Analyzers.Helpers.KnownSymbols.BaseTypes;

namespace Hagar.Analyzers.Helpers.KnownSymbols
{
    internal class SerialDisposableType : QualifiedType
    {
        internal readonly QualifiedProperty Disposable;

        internal SerialDisposableType()
            : base("System.Reactive.Disposables.SerialDisposable")
        {
            this.Disposable = new QualifiedProperty(this, nameof(this.Disposable));
        }
    }
}