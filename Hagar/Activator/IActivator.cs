using System.Diagnostics.CodeAnalysis;

namespace Hagar.Activator
{
    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant")]
    public interface IActivator<T>
    {
        T Create();
    }
}
