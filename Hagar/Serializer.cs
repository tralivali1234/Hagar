using System;
using Hagar.Exceptions;
using Hagar.Orleans.Serialization;
using Hagar.WireProtocol;

namespace Hagar
{
    /// <summary>
    /// Codec for operating with the wire format.
    /// Operates on format-specific types, such as variable-length integers, length-prefixed data, fixed-width data, and tag-delimited data.
    /// </summary>
    public static class WireCodec
    {
        private static readonly byte EndObjectTag = new Tag
        {
            WireType = WireType.Extended,
            ExtendedWireType = ExtendedWireType.EndTagDelimited
        };

        private static readonly byte EndBaseFieldsTag = new Tag
        {
            WireType = WireType.Extended,
            ExtendedWireType = ExtendedWireType.EndBaseFields
        };

/*
        private static readonly byte NullObjectTag = new Tag
        {
            WireType = WireType.Extended,
            ExtendedWireType = ExtendedWireType.Null
        };
*/

        public static void WriteFieldHeader(this Writer writer, SerializationContext context, uint fieldId, Type expectedType, Type actualType, WireType wireType)
        {
            var (schemaType, idOrReference) = GetSchemaTypeWithEncoding(context, expectedType, actualType);
            var field = default(Field);
            field.FieldId = fieldId;
            field.SchemaType = schemaType;
            field.WireType = wireType;

            writer.Write(field.Tag);
            if (field.HasExtendedFieldId) writer.WriteVarInt(field.FieldId);
            if (field.HasExtendedSchemaType) writer.WriteType(context, schemaType, idOrReference, actualType);
        }

        public static void WriteEndObject(this Writer writer)
        {
            writer.Write(EndObjectTag);
        }
/*
        public static void WriteNull(this Writer writer)
        {
            writer.Write(NullObjectTag);
        }*/

        public static void WriteEndBase(this Writer writer)
        {
            writer.Write(EndBaseFieldsTag);
        }

        public static Field ReadFieldHeader(this Reader reader, SerializationContext context)
        {
            var field = default(Field);
            field.Tag = reader.ReadByte();
            if (field.HasExtendedFieldId) field.FieldId = reader.ReadVarUInt32();
            if (field.IsSchemaTypeValid) field.FieldType = reader.ReadType(context, field.SchemaType);
            
            return field;
        }

        public static (SchemaType, uint) GetSchemaTypeWithEncoding(SerializationContext context, Type expectedType, Type actualType)
        {
            if (actualType == expectedType)
            {
                return (SchemaType.Expected, 0);
            }

            if (context.TryGetWellKnownTypeId(actualType, out uint typeId))
            {
                return (SchemaType.WellKnown, typeId);
            }

            if (context.TryGetTypeReference(actualType, out uint reference))
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
                    return context.GetWellKnownType(typeId);
                case SchemaType.Encoded:
                    context.TypeCodec.TryRead(reader, out Type encoded);
                    return encoded;
                case SchemaType.Referenced:
                    var reference = reader.ReadVarUInt32();
                    return context.GetReferencedType(reference);
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<Type>(nameof(SchemaType));
            }
        }
        
        public static void WriteInt32Field(this Writer writer, uint fieldId, int value)
        {
            if (value > 1 << 20 || -value > 1 << 20)
            {
                var field = new Field
                {
                    FieldId = fieldId,
                    SchemaType = SchemaType.Expected,
                    WireType = WireType.Fixed32
                };

                writer.Write(field.Tag);
                if (field.HasExtendedFieldId) writer.WriteVarInt(field.FieldId);

                writer.Write(value);
            }
            else
            {
                var field = new Field
                {
                    FieldId = fieldId,
                    SchemaType = SchemaType.Expected,
                    WireType = WireType.VarInt
                };

                writer.Write(field.Tag);
                if (field.HasExtendedFieldId) writer.WriteVarInt(field.FieldId);

                writer.WriteVarInt(value);
            }
        }

        public static void WriteUInt32Field(this Writer writer, uint fieldId, uint value)
        {
            if (value > 1 << 20)
            {
                var field = new Field
                {
                    FieldId = fieldId,
                    SchemaType = SchemaType.Expected,
                    WireType = WireType.Fixed32
                };

                writer.Write(field.Tag);
                if (field.HasExtendedFieldId) writer.WriteVarInt(field.FieldId);

                writer.Write(value);
            }
            else
            {
                var field = new Field
                {
                    FieldId = fieldId,
                    SchemaType = SchemaType.Expected,
                    WireType = WireType.VarInt
                };

                writer.Write(field.Tag);
                if (field.HasExtendedFieldId) writer.WriteVarInt(field.FieldId);

                writer.WriteVarInt(value);
            }
        }

        public static void WriteInt64Field(this Writer writer, uint fieldId, long value)
        {
            if (value <= int.MaxValue && value >= int.MinValue)
            {
                writer.WriteInt32Field(fieldId, (int) value);
            }
            else if (value > 1 << 41 || -value > 1 << 41)
            {
                var field = new Field
                {
                    FieldId = fieldId,
                    SchemaType = SchemaType.Expected,
                    WireType = WireType.Fixed64
                };

                writer.Write(field.Tag);
                if (field.HasExtendedFieldId) writer.WriteVarInt(field.FieldId);

                writer.Write(value);
            }
            else
            {
                var field = new Field
                {
                    FieldId = fieldId,
                    SchemaType = SchemaType.Expected,
                    WireType = WireType.VarInt
                };

                writer.Write(field.Tag);
                if (field.HasExtendedFieldId) writer.WriteVarInt(field.FieldId);

                writer.WriteVarInt(value);
            }
        }

        public static void WriteUInt64Field(this Writer writer, uint fieldId, ulong value)
        {
            if (value <= int.MaxValue)
            {
                writer.WriteUInt32Field(fieldId, (uint)value);
            }
            else if (value > 1 << 41)
            {
                var field = new Field
                {
                    FieldId = fieldId,
                    SchemaType = SchemaType.Expected,
                    WireType = WireType.Fixed64
                };

                writer.Write(field.Tag);
                if (field.HasExtendedFieldId) writer.WriteVarInt(field.FieldId);

                writer.Write(value);
            }
            else
            {
                var field = new Field
                {
                    FieldId = fieldId,
                    SchemaType = SchemaType.Expected,
                    WireType = WireType.VarInt
                };

                writer.Write(field.Tag);
                if (field.HasExtendedFieldId) writer.WriteVarInt(field.FieldId);

                writer.WriteVarInt(value);
            }
        }
    }
}
