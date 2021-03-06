using System;
using Hagar.Buffers;
using Hagar.Session;
using Hagar.WireProtocol;

namespace Hagar.Codecs
{
    public class VoidCodec : IFieldCodec<object>
    {
        public void WriteField(ref Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, object value)
        {
            if (!ReferenceCodec.TryWriteReferenceField(ref writer, session, fieldIdDelta, expectedType, value))
            {
                ThrowNotNullException(value);
            }
        }

        public object ReadValue(ref Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType != WireType.Reference)
            {
                ThrowInvalidWireType(field);
            }

            return ReferenceCodec.ReadReference<object>(ref reader, session, field);
        }

        private static void ThrowInvalidWireType(Field field)
        {
            throw new UnsupportedWireTypeException($"Expected a reference, but encountered wire type of '{field.WireType}'.");
        }

        private static void ThrowNotNullException(object value) => throw new InvalidOperationException(
            $"Expected a value of null, but encountered a value of '{value}'.");
    }
}