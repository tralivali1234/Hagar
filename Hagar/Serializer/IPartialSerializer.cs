using System.Diagnostics.CodeAnalysis;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.Utilities.Orleans.Serialization;

namespace Hagar.Serializer
{
    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant")]
    public interface IPartialSerializer<T> where T : class
    {
        void Serialize(Writer writer, SerializationContext context, T value);
        void Deserialize(Reader reader, SerializationContext context, T value);
    }
}
