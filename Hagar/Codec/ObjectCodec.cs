using System;
using Hagar.Serializer;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codec
{
    public class ObjectCodec : IFieldCodec<object>
    {
        private readonly ISerializerCatalog serializerCatalog;

        public ObjectCodec(ISerializerCatalog serializerCatalog)
        {
            this.serializerCatalog = serializerCatalog;
        }

        public object ReadValue(Reader reader, SerializerSession context, Field field)
        {
            if (field.WireType == WireType.Reference) return ReferenceCodec.ReadReference<object>(reader, context, field, this.serializerCatalog);
            reader.SkipField(context, field);
            return new object();
        }

        public void WriteField(Writer writer, SerializerSession context, uint fieldId, Type expectedType, object value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, context, fieldId, expectedType, value)) return;
            writer.WriteFieldHeader(context, fieldId, expectedType, typeof(object), WireType.LengthPrefixed);
            writer.WriteVarInt(0);
        }
    }
}