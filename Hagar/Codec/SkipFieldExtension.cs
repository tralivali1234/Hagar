using System;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codec
{
    public class SkipFieldCodec : IFieldCodec<object>
    {
        public void WriteField(Writer writer, SerializerSession session, uint fieldId, Type expectedType, object value)
        {
            ReferenceCodec.MarkValueField(session);
            throw new NotImplementedException();
        }

        public object ReadValue(Reader reader, SerializerSession session, Field field)
        {
            ReferenceCodec.MarkValueField(session);
            reader.SkipField(session, field);
            return null;
        }
    }

    public static class SkipFieldExtension
    {
        public static void SkipField(this Reader reader, SerializerSession session, Field field)
        {
            switch (field.WireType)
            {
                case WireType.Reference:
                case WireType.VarInt:
                    reader.ReadVarUInt64();
                    break;
                case WireType.TagDelimited:
                    SkipTagDelimitedField(reader, session);
                    break;
                case WireType.LengthPrefixed:
                    SkipLengthPrefixedField(reader);
                    break;
                case WireType.Fixed32:
                    reader.ReadUInt();
                    break;
                case WireType.Fixed64:
                    reader.ReadULong();
                    break;
                case WireType.Fixed128:
                    reader.ReadULong();
                    reader.ReadULong();
                    break;
                case WireType.Extended:
                    if (!field.IsEndBaseOrEndObject)
                        ThrowUnexpectedExtendedWireType(field);
                    break;
                default:
                    ThrowUnexpectedWireType(field);
                    break;
            }
        }

        private static void ThrowUnexpectedExtendedWireType(Field field)
        {
            throw new ArgumentOutOfRangeException(
                $"Unexpected {nameof(ExtendedWireType)} value [{field.ExtendedWireType}] in field {field} while skipping field.");
        }

        private static void ThrowUnexpectedWireType(Field field)
        {
            throw new ArgumentOutOfRangeException(
                $"Unexpected {nameof(WireType)} value [{field.WireType}] in field {field} while skipping field.");
        }

        private static void SkipLengthPrefixedField(Reader reader)
        {
            var length = reader.ReadVarUInt32();
            while (length > sizeof(ulong))
            {
                reader.ReadULong();
                length -= sizeof(ulong);
            }
            while (length > sizeof(uint))
            {
                reader.ReadUInt();
                length -= sizeof(int);
            }

            while (length > 0)
            {
                reader.ReadByte();
                length--;
            }
        }

        private static void SkipTagDelimitedField(Reader reader, SerializerSession session)
        {
            while (true)
            {
                var field = reader.ReadFieldHeader(session);
                if (field.IsEndObject) break;
                reader.SkipField(session, field);
            }
        }
    }
}