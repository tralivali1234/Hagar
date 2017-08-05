using System;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codec
{
    /// <summary>
    /// Codec for operating with the wire format.
    /// </summary>
    public static class FieldHeaderCodec
    {
        public static void WriteFieldHeader(this Writer writer, SerializationContext context, uint fieldId, Type expectedType, Type actualType, WireType wireType)
        {
            var (schemaType, idOrReference) = GetSchemaTypeWithEncoding(context, expectedType, actualType);
            var field = default(Field);
            field.FieldIdDelta = fieldId;
            field.SchemaType = schemaType;
            field.WireType = wireType;

            writer.Write(field.Tag);
            if (field.HasExtendedFieldId) writer.WriteVarInt(field.FieldIdDelta);
            if (field.HasExtendedSchemaType) writer.WriteType(context, schemaType, idOrReference, actualType);
        }

        public static Field ReadFieldHeader(this Reader reader, SerializationContext context)
        {
            var field = default(Field);
            field.Tag = reader.ReadByte();
            if (field.HasExtendedFieldId) field.FieldIdDelta = reader.ReadVarUInt32();
            if (field.IsSchemaTypeValid) field.FieldType = reader.ReadType(context, field.SchemaType);
            
            return field;
        }

        private static (SchemaType, uint) GetSchemaTypeWithEncoding(SerializationContext context, Type expectedType, Type actualType)
        {
            if (actualType == expectedType)
            {
                return (SchemaType.Expected, 0);
            }

            if (context.WellKnownTypes.TryGetWellKnownTypeId(actualType, out uint typeId))
            {
                return (SchemaType.WellKnown, typeId);
            }

            if (context.ReferencedTypes.TryGetTypeReference(actualType, out uint reference))
            {
                return (SchemaType.Referenced, reference);
            }

            return (SchemaType.Encoded, 0);
        }

        private static void WriteType(this Writer writer, SerializationContext context, SchemaType schemaType, uint idOrReference, Type type)
        {
            switch (schemaType)
            {
                case SchemaType.Expected:
                    break;
                case SchemaType.WellKnown:
                case SchemaType.Referenced:
                    writer.WriteVarInt(idOrReference);
                    break;
                case SchemaType.Encoded:
                    context.TypeCodec.Write(writer, type);
                    break;
                default:
                    ExceptionHelper.ThrowArgumentOutOfRange(nameof(schemaType));
                    break;
            }
        }

        private static Type ReadType(this Reader reader, SerializationContext context, SchemaType schemaType)
        {
            switch (schemaType)
            {
                case SchemaType.Expected:
                    return null;
                case SchemaType.WellKnown:
                    var typeId = reader.ReadVarUInt32();
                    return context.WellKnownTypes.GetWellKnownType(typeId);
                case SchemaType.Encoded:
                    context.TypeCodec.TryRead(reader, out Type encoded);
                    return encoded;
                case SchemaType.Referenced:
                    var reference = reader.ReadVarUInt32();
                    return context.ReferencedTypes.GetReferencedType(reference);
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<Type>(nameof(SchemaType));
            }
        }
    }
}
