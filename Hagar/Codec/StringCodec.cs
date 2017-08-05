using System;
using System.Text;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codec
{
    public class StringCodec : IFieldCodec<string>
    {
        public string ReadValue(Reader reader, SerializationContext context, Field field)
        {
            if (field.WireType == WireType.Reference) return ReferenceCodec.ReadReference<string>(reader, context);
            if (field.WireType != WireType.LengthPrefixed) ThrowUnsupportedWireTypeException(field);
            var length = reader.ReadVarUInt32();
            var bytes = reader.ReadBytes((int)length);
            var result = Encoding.UTF8.GetString(bytes);
            ReferenceCodec.RecordObject(context, result);
            return result;
        }

        public void WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType, string value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, context, fieldId, expectedType, value)) return;

            writer.WriteFieldHeader(context, fieldId, expectedType, typeof(string), WireType.LengthPrefixed);
            // TODO: use Span<byte>
            var bytes = Encoding.UTF8.GetBytes(value);
            writer.WriteVarInt((uint)bytes.Length);
            writer.Write(bytes);
        }

        private static void ThrowUnsupportedWireTypeException(Field field) => throw new UnsupportedWireTypeException(
            $"Only a {nameof(WireType)} value of {WireType.LengthPrefixed} is supported for string fields. {field}");
    }
}