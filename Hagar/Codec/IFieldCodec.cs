using System;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codec
{
    public interface IFieldCodec<T>
    {
        void WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType, T value);
        T ReadValue(Reader reader, SerializationContext context, Field field);
    }
}