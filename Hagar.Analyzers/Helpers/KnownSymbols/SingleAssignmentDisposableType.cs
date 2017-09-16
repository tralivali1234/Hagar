using Hagar.Analyzers.Helpers.KnownSymbols.BaseTypes;

namespace Hagar.Analyzers.Helpers.KnownSymbols
{
    internal class SingleAssignmentDisposableType : QualifiedType
    {
        internal readonly QualifiedProperty Disposable;

        internal SingleAssignmentDisposableType()
            : base("System.Reactive.Disposables.SingleAssignmentDisposable")
        {
            this.Disposable = new QualifiedProperty(this, nameof(this.Disposable));
        }
    }
}