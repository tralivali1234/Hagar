using System.Diagnostics.CodeAnalysis;
using Hagar.Session;
using Hagar.Utilities;

namespace Hagar.Activator
{
    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant")]
    public interface IActivator<T>
    {
        T Create(Reader reader, SerializationContext context);
    }
}
