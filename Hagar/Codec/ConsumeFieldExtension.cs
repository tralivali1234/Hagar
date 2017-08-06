using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codec
{
    public class UnknownFieldMarker
    {
        public UnknownFieldMarker(Field field, int offset)
        {
            this.Field = field;
            this.Offset = offset;
        }

        public int Offset { get; }
        public Field Field { get; }

        public override string ToString()
        {
            return $"[{nameof(UnknownFieldMarker)}] {nameof(this.Offset)}: {this.Offset}, {nameof(this.Field)}: {this.Field}";
        }
    }

    public static class ConsumeFieldExtension
    {
/*        public static Reader GetReaderForReference(this Reader reader, SerializerSession session, uint reference)
        {
            if (!session.ReferencedObjects.TryGetReferencedObject(reference, out object referencedObject)) ThrowReferenceNotFound();
            if (referencedObject is UnknownFieldMarker marker)
            {
                var result = new Reader(reader.GetBuffers());
                result.Advance(marker.Offset);
                return result;
            }
        }*/

        public static void ConsumeUnknownField(this Reader reader, SerializerSession session, Field field)
        {
            // References cannot themselves be referenced.
            if (field.WireType == WireType.Reference)
            {
                reader.ReadVarUInt32();
                return;
            }

            // Record a placeholder so that this field can later be correctly deserialized if it is referenced.
            ReferenceCodec.RecordObject(session, new UnknownFieldMarker(field, reader.CurrentPosition));

            // TODO: Advance the reader without actually reading bytes / allocating.
            switch (field.WireType)
            {
                case WireType.VarInt:
                    reader.ReadVarUInt64();
                    break;
                case WireType.TagDelimited:
                    // Since tag delimited fields can be comprised of other fields, recursively consume those, too.
                    ConsumeTagDelimitedField(reader, session);
                    break;
                case WireType.LengthPrefixed:
                    SkipFieldExtension.SkipLengthPrefixedField(reader);
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
                    SkipFieldExtension.ThrowUnexpectedExtendedWireType(field);
                    break;
                default:
                    SkipFieldExtension.ThrowUnexpectedWireType(field);
                    break;
            }
        }

        private static void ConsumeTagDelimitedField(Reader reader, SerializerSession session)
        {
            while (true)
            {
                var field = reader.ReadFieldHeader(session);
                if (field.IsEndObject) break;
                if (field.IsEndBaseFields) continue;
                reader.ConsumeUnknownField(session, field);
            }
        }
    }
}