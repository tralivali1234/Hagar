using System;
using System.Text;
using Hagar.Serializer;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codec
{
    public class StringCodec : FieldCodecBase<string, StringCodec>, IFieldCodec<string>
    {
        private readonly ICodecProvider codecProvider;
        public StringCodec(ICodecProvider codecProvider)
        {
            this.codecProvider = codecProvider;
        }

        string IFieldCodec<string>.ReadValue(Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType == WireType.Reference)
                return ReferenceCodec.ReadReference<string>(reader, session, field, this.codecProvider);
            if (field.WireType != WireType.LengthPrefixed) ThrowUnsupportedWireTypeException(field);
            var length = reader.ReadVarUInt32();
            var bytes = reader.ReadBytes((int) length);
            var result = Encoding.UTF8.GetString(bytes);
            ReferenceCodec.RecordObject(session, result);
            return result;
        }

        void IFieldCodec<string>.WriteField(Writer writer, SerializerSession session, uint fieldId, Type expectedType, string value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, session, fieldId, expectedType, value)) return;

            writer.WriteFieldHeader(session, fieldId, expectedType, typeof(string), WireType.LengthPrefixed);
            // TODO: use Span<byte>
            var bytes = Encoding.UTF8.GetBytes(value);
            writer.WriteVarInt((uint)bytes.Length);
            writer.Write(bytes);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.LengthPrefixed} is supported for string fields. {field}");
    }
}