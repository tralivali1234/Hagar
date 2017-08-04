using System;
using Hagar.Codec;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.Utilities.Orleans.Serialization;
using Hagar.WireProtocol;

namespace Hagar.Codec
{
    public class GuidCodec : IValueCodec<Guid>
    {
        public void WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType, Guid value)
        {
            writer.WriteFieldHeader(context, fieldId, expectedType, typeof(Guid), WireType.Fixed128);
            writer.Write(value);
        }

        public Guid ReadValue(Reader reader, SerializationContext context, Field field)
        {
            var bytes = new byte[16];
            reader.ReadByteArray(bytes, 0, 16);
            return new Guid(bytes);
        }
    }
}