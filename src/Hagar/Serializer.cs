using System;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Serializers;
using Hagar.Session;

namespace Hagar
{
    public class Serializer<T>
    {
        private readonly IFieldCodec<T> codec;
        private readonly Type expectedType = typeof(T);

        public Serializer(ITypedCodecProvider codecProvider)
        {
            this.codec = codecProvider.GetCodec<T>();
        }

        public void Serialize(T value, SerializerSession session, Writer writer)
        {
            this.codec.WriteField(writer, session, 0, this.expectedType, value);
        }

        public T Deserialize(SerializerSession session, Reader reader)
        {
            var field = reader.ReadFieldHeader(session);
            return this.codec.ReadValue(reader, session, field);
        }
    }

    public class Serializer : Serializer<object>
    {
        public Serializer(ITypedCodecProvider codecProvider) : base(codecProvider)
        {
        }
    }
}