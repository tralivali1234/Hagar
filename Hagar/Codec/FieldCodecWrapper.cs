using System;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codec
{
    public class FieldCodecWrapper<TField, TCodec> : IFieldCodec<object> where TCodec : IFieldCodec<TField>
    {
        private readonly TCodec codec;

        public FieldCodecWrapper(TCodec codec)
        {
            this.codec = codec;
        }

        public void WriteField(Writer writer, SerializerSession session, uint fieldId, Type expectedType, object value)
        {
            this.codec.WriteField(writer, session, fieldId, expectedType, (TField)value);
        }

        public object ReadValue(Reader reader, SerializerSession session, Field field)
        {
            return this.codec.ReadValue(reader, session, field);
        }
    }

    public class FieldCodecBase<TField, TCodec> : IFieldCodec<object> where TCodec : class, IFieldCodec<TField>
    {
        private readonly TCodec codec;

        public FieldCodecBase()
        {
            this.codec = this as TCodec;
            if (this.codec == null) ThrowInvalidSubclass();
        }

        public void WriteField(Writer writer, SerializerSession session, uint fieldId, Type expectedType, object value)
        {
            this.codec.WriteField(writer, session, fieldId, expectedType, (TField)value);
        }

        public object ReadValue(Reader reader, SerializerSession session, Field field)
        {
            return this.codec.ReadValue(reader, session, field);
        }

        private static void ThrowInvalidSubclass()
        {
            throw new InvalidCastException($"Subclasses of {typeof(FieldCodecBase<TField, TCodec>)} must implement/derive from {typeof(TCodec)}.");
        }
    }
}