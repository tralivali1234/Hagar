using System;
using System.Collections.Generic;
using System.Text;
using Hagar.Exceptions;
using Hagar.Orleans.Serialization;
using Hagar.WireProtocol;

namespace Hagar
{
    public class SerializationContext
    {
        private readonly Dictionary<uint, Type> wellKnownTypes;
        private readonly Dictionary<Type, uint> wellKnownTypeToIdMap = new Dictionary<Type, uint>();

        private readonly Dictionary<uint, Type> referencedTypes = new Dictionary<uint, Type>();
        private readonly Dictionary<Type, uint> referencedTypeToIdMap = new Dictionary<Type, uint>();

        public SerializationContext()
        {
            this.wellKnownTypes = new Dictionary<uint, Type>
            {
                [1] = typeof(int),
                [2] = typeof(string),
                [3] = typeof(bool),
                [4] = typeof(short),
                [5] = typeof(long),
                [6] = typeof(sbyte),
                [7] = typeof(uint),
                [8] = typeof(ushort),
                [9] = typeof(ulong),
                [10] = typeof(byte),
                [11] = typeof(float),
                [12] = typeof(double),
                [13] = typeof(decimal),
                [14] = typeof(char),
                [15] = typeof(Guid),
                [16] = typeof(DateTime),
                [17] = typeof(TimeSpan),
            };
            foreach (var item in this.wellKnownTypes)
            {
                this.wellKnownTypeToIdMap[item.Value] = item.Key;
            }
        }
        public TypeCodec TypeCodec { get; } = new TypeCodec();

        public Type GetWellKnownType(uint typeId) => this.wellKnownTypes[typeId];
        public bool TryGetWellKnownType(uint typeId, out Type type) => this.wellKnownTypes.TryGetValue(typeId, out type);
        public bool TryGetWellKnownTypeId(Type type, out uint typeId) => this.wellKnownTypeToIdMap.TryGetValue(type, out typeId);
        
        public Type GetReferencedType(uint reference) => this.referencedTypes[reference];
        public bool TryGetReferencedType(uint reference, out Type type) => this.referencedTypes.TryGetValue(reference, out type);
        public bool TryGetTypeReference(Type type, out uint reference) => this.referencedTypeToIdMap.TryGetValue(type, out reference);
    }

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

        public static void WriteEndBase(this Writer writer)
        {
            writer.Write(EndBaseFieldsTag);
        }

        public static Field ReadFieldHeader(this Reader reader, SerializationContext context)
        {
            var field = default(Field);
            field.Tag = reader.ReadByte();
            if (field.HasExtendedFieldId) field.FieldId = reader.ReadVarUInt32();

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

        public static void WriteType(this Writer writer, SerializationContext context, SchemaType schemaType, uint idOrReference, Type type)
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

        public static Type ReadType(this Reader reader, SerializationContext context, SchemaType schemaType, Type expectedType)
        {
            switch (schemaType)
            {
                case SchemaType.Expected:
                    return expectedType;
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

namespace Hagar.WireProtocol
{
    public static class VarIntWriterExtensions
    {
        public static void WriteVarInt(this Writer writer, sbyte value) => WriteVarInt(writer, ZigZagEncode(value));
        public static void WriteVarInt(this Writer writer, short value) => WriteVarInt(writer, ZigZagEncode(value));
        public static void WriteVarInt(this Writer writer, int value) => WriteVarInt(writer, ZigZagEncode(value));
        public static void WriteVarInt(this Writer writer, long value) => WriteVarInt(writer, ZigZagEncode(value));

        public static unsafe void WriteVarInt(this Writer writer, byte value)
        {
            byte* scratch = stackalloc byte[2];
            var count = 0;
            do
            {
                scratch[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            scratch[count - 1] &= 0x7F; // adjust the last byte.
            writer.Write(new Span<byte>(scratch, count));
        }

        public static unsafe void WriteVarInt(this Writer writer, ushort value)
        {
            byte* scratch = stackalloc byte[3];
            var count = 0;
            do
            {
                scratch[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            scratch[count - 1] &= 0x7F; // adjust the last byte.
            writer.Write(new Span<byte>(scratch, count));
        }

        public static unsafe void WriteVarInt(this Writer writer, uint value)
        {
            byte* scratch = stackalloc byte[5];
            var count = 0;
            do
            {
                scratch[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            scratch[count - 1] &= 0x7F; // adjust the last byte.
            writer.Write(new Span<byte>(scratch, count));
        }

        public static unsafe void WriteVarInt(this Writer writer, ulong value)
        {
            byte* scratch = stackalloc byte[10];
            var count = 0;
            do
            {
                scratch[count++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            scratch[count - 1] &= 0x7F; // adjust the last byte.
            writer.Write(new Span<byte>(scratch, count));
        }

        private static byte ZigZagEncode(sbyte value)
        {
            return (byte)((value << 1) ^ (value >> 7));
        }

        private static ushort ZigZagEncode(short value)
        {
            return (ushort)((value << 1) ^ (value >> 15));
        }

        private static uint ZigZagEncode(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        private static ulong ZigZagEncode(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }
    }

    public static class VarIntReaderExtensions
    {
        public static byte ReadVarUInt8(this Reader reader)
        {
            var next = reader.ReadByte();
            if ((next & 0x80) == 0) return next;
            var result = (byte) (next & 0x7F);

            next = reader.ReadByte();
            result |= (byte)((next & 0x7F) << 7);

            // Consume extra bytes.
            while ((next & 0x80) != 0) next = reader.ReadByte(); 

            return result;
        }

        public static ushort ReadVarUInt16(this Reader reader)
        {
            var next = reader.ReadByte();
            var result = (ushort)(next & 0x7F);
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (ushort)((next & 0x7F) << 7);
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (ushort)((next & 0x7F) << 14);

            // Consume extra bytes.
            while ((next & 0x80) != 0) next = reader.ReadByte();

            return result;
        }

        public static uint ReadVarUInt32(this Reader reader)
        {
            var next = reader.ReadByte();
            var result = (uint)(next & 0x7F);
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (uint)((next & 0x7F) << 7);
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (uint)((next & 0x7F) << 14);
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (uint)((next & 0x7F) << 21);
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (uint)((next & 0x7F) << 28);
            if ((next & 0x80) == 0) return result;

            // Consume extra bytes.
            while ((next & 0x80) != 0) next = reader.ReadByte();

            return result;
        }

        public static ulong ReadVarUInt64(this Reader reader)
        {
            ulong next = reader.ReadByte();
            var result = next & 0x7F;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 7;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 14;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 21;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 28;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 35;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 42;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 49;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 56;
            if ((next & 0x80) == 0) return result;

            next = reader.ReadByte();
            result |= (next & 0x7F) << 63;
            if ((next & 0x80) == 0) return result;

            // Consume extra bytes.
            while ((next & 0x80) != 0) next = reader.ReadByte();

            return result;
        }

        public static byte ReadUInt8(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ReadVarUInt8(reader);
                case WireType.Fixed32:
                    return (byte) reader.ReadUInt();
                case WireType.Fixed64:
                    return (byte) reader.ReadULong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<byte>(nameof(wireType));
            }
        }

        public static ushort ReadUInt16(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ReadVarUInt16(reader);
                case WireType.Fixed32:
                    return (ushort) reader.ReadUInt();
                case WireType.Fixed64:
                    return (ushort) reader.ReadULong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<ushort>(nameof(wireType));
            }
        }

        public static uint ReadUInt32(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ReadVarUInt32(reader);
                case WireType.Fixed32:
                    return reader.ReadUInt();
                case WireType.Fixed64:
                    return (uint) reader.ReadULong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<uint>(nameof(wireType));
            }
        }

        public static ulong ReadUInt64(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ReadVarUInt64(reader);
                case WireType.Fixed32:
                    return reader.ReadUInt();
                case WireType.Fixed64:
                    return reader.ReadULong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<ulong>(nameof(wireType));
            }
        }

        public static sbyte ReadInt8(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ZigZagDecode(ReadVarUInt8(reader));
                case WireType.Fixed32:
                    return (sbyte) reader.ReadInt();
                case WireType.Fixed64:
                    return (sbyte) reader.ReadLong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<sbyte>(nameof(wireType));
            }
        }

        public static short ReadInt16(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ZigZagDecode(ReadVarUInt16(reader));
                case WireType.Fixed32:
                    return (short) reader.ReadInt();
                case WireType.Fixed64:
                    return (short) reader.ReadLong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<short>(nameof(wireType));
            }
        }

        public static int ReadInt32(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ZigZagDecode(ReadVarUInt32(reader));
                case WireType.Fixed32:
                    return reader.ReadInt();
                case WireType.Fixed64:
                    return (int) reader.ReadLong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<int>(nameof(wireType));
            }
        }

        public static long ReadInt64(this Reader reader, WireType wireType)
        {
            switch (wireType)
            {
                case WireType.VarInt:
                    return ZigZagDecode(ReadVarUInt64(reader));
                case WireType.Fixed32:
                    return reader.ReadInt();
                case WireType.Fixed64:
                    return reader.ReadLong();
                default:
                    return ExceptionHelper.ThrowArgumentOutOfRange<long>(nameof(wireType));
            }
        }

        private const sbyte Int8Msb = unchecked((sbyte) 0x80);
        private const short Int16Msb = unchecked((short) 0x8000);
        private const int Int32Msb = unchecked((int) 0x80000000);
        private const long Int64Msb = unchecked((long) 0x8000000000000000);

        private static sbyte ZigZagDecode(byte encoded)
        {
            var value = (sbyte) encoded;
            return (sbyte) (-(value & 0x01) ^ ((sbyte) (value >> 1) & ~Int8Msb));
        }

        private static short ZigZagDecode(ushort encoded)
        {
            var value = (short) encoded;
            return (short) (-(value & 0x01) ^ ((short) (value >> 1) & ~Int16Msb));
        }

        private static int ZigZagDecode(uint encoded)
        {
            var value = (int) encoded;
            return -(value & 0x01) ^ ((value >> 1) & ~Int32Msb);
        }

        private static long ZigZagDecode(ulong encoded)
        {
            var value = (long) encoded;
            return -(value & 0x01L) ^ ((value >> 1) & ~Int64Msb);
        }
    }

    public struct Field
    {
        private uint fieldId;
        public Tag Tag;

        public static readonly Field EndObject = new Field
        {
            WireType = WireType.Extended,
            ExtendedWireType = ExtendedWireType.EndTagDelimited
        };

        public Field(Tag tag)
        {
            this.Tag = tag;
            this.fieldId = 0;
            if (!this.HasFieldId) ThrowFieldIdInvalid();
        }

        public uint FieldId
        {
            // If the embedded field id is valid, return it, otherwise return the extended field id.
            // The extended field id might not be valid if this field has the Extended wire type.
            get => this.Tag.IsFieldIdValid ? this.Tag.FieldId : this.HasFieldId ? this.fieldId : ThrowFieldIdInvalid();
            set
            {
                // I the field id can fit into the tag, embed it there, otherwise invalidate the embedded field id and set the full field id.
                if (value < 7)
                {
                    this.Tag.FieldId = value;
                    this.fieldId = 0;
                }
                else
                {
                    this.Tag.SetFieldIdInvalid();
                    this.fieldId = value;
                }
            }
        }
        public bool HasFieldId => this.Tag.WireType != WireType.Extended;
        public bool HasExtendedFieldId => this.Tag.HasExtendedFieldId;

        public WireType WireType
        {
            get => this.Tag.WireType;
            set => this.Tag.WireType = value;
        }

        public SchemaType SchemaType
        {
            get => this.IsSchemaTypeValid ? this.Tag.SchemaType : ThrowSchemaTypeInvalid();
            set => this.Tag.SchemaType = value;
        }

        public ExtendedWireType ExtendedWireType
        {
            get => this.WireType == WireType.Extended ? this.Tag.ExtendedWireType : ThrowExtendedWireTypeInvalid();
            set => this.Tag.ExtendedWireType = value;
        }

        public bool IsSchemaTypeValid => this.Tag.IsSchemaTypeValid;
        public bool HasExtendedSchemaType => this.IsSchemaTypeValid && this.SchemaType != SchemaType.Expected;

        public bool IsStartObject => this.WireType == WireType.TagDelimited;
        public bool IsEndBaseFields => this.Tag.HasExtendedWireType && this.Tag.ExtendedWireType == ExtendedWireType.EndBaseFields;
        public bool IsEndObject => this.Tag.HasExtendedWireType && this.Tag.ExtendedWireType == ExtendedWireType.EndTagDelimited;

        public bool IsEndBaseOrEndObject => this.Tag.HasExtendedWireType &&
                                            (this.Tag.ExtendedWireType == ExtendedWireType.EndTagDelimited ||
                                             this.Tag.ExtendedWireType == ExtendedWireType.EndBaseFields);

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append('[').Append(this.WireType.ToString());
            if (this.HasFieldId) builder.Append($", Id:{this.FieldId}");
            if (this.IsSchemaTypeValid) builder.Append($", Type:{this.SchemaType}");
            if (this.WireType == WireType.Extended) builder.Append($": {this.ExtendedWireType}");
            builder.Append(']');
            return builder.ToString();
        }

        private static uint ThrowFieldIdInvalid() => throw new FieldIdNotPresentException();
        private static SchemaType ThrowSchemaTypeInvalid() => throw new SchemaTypeInvalidException();
        private static ExtendedWireType ThrowExtendedWireTypeInvalid() => throw new ExtendedWireTypeInvalidException();
    }

    public struct Tag
    {
        // [W W W] [S S] [F F F]
        public const byte WireTypeMask = 0b1110_0000; // The first 3 bits are dedicated to the wire type.
        public const byte SchemaTypeMask = 0b0001_1000; // The next 2 bits are dedicated to the schema type specifier, if the schema type is expected.
        public const byte FieldIdMask = 0b000_0111; // The final 3 bits are used for the field id, if the field id is expected.
        public const byte FieldIdCompleteMask = 0b0000_0111;
        public const byte ExtendedWireTypeMask = 0b0001_1000;

        private byte tag;

        public Tag(byte tag)
        {
            this.tag = tag;
        }

        public static implicit operator Tag(byte tag) => new Tag(tag);
        public static implicit operator byte(Tag tag) => tag.tag;

        /// <summary>
        /// Returns the wire type of the data following this tag.
        /// </summary>
        public WireType WireType
        {
            get => (WireType)(this.tag & WireTypeMask);
            set => this.tag = (byte)((this.tag & ~WireTypeMask) | ((byte)value & WireTypeMask));
        }

        public bool HasExtendedWireType => this.WireType == WireType.Extended;

        /// <summary>
        /// Returns the wire type of the data following this tag.
        /// </summary>
        public ExtendedWireType ExtendedWireType
        {
            get => (ExtendedWireType)(this.tag & ExtendedWireTypeMask);
            set => this.tag = (byte)((this.tag & ~ExtendedWireTypeMask) | ((byte)value & ExtendedWireTypeMask));
        }

        /// <summary>
        /// Returns <see langword="true"/> if this field represents a value of the expected type, <see langword="false"/> otherwise.
        /// </summary>
        /// <remarks>
        /// If this value is <see langword="false"/>, this tag and field id must be followed by a type specification.
        /// </remarks>
        public SchemaType SchemaType
        {
            get => (SchemaType)(this.tag & SchemaTypeMask);
            set => this.tag = (byte)((this.tag & ~SchemaTypeMask) | ((byte)value & SchemaTypeMask));
        }

        /// <summary>
        /// Returns <see langword="true"/> if the <see cref="SchemaType"/> is valid, <see langword="false"/> otherwise.
        /// </summary>
        public bool IsSchemaTypeValid => this.WireType != WireType.Extended;

        /// <summary>
        /// Returns the <see cref="FieldId"/> of the field represented by this tag.
        /// </summary>
        /// <remarks>
        /// If <see cref="IsFieldIdValid"/> is <see langword="false"/>, this value is not a complete field id.
        /// </remarks>
        public uint FieldId
        {
            get => (uint)(this.tag & FieldIdMask);
            set => this.tag = (byte)((this.tag & ~FieldIdMask) | ((byte)value & FieldIdMask));
        }

        /// <summary>
        /// Clears the <see cref="FieldId"/>.
        /// </summary>
        public void ClearFieldId() => this.tag = (byte)(this.tag & ~FieldIdMask);

        /// <summary>
        /// Invalidates <see cref="FieldId"/>.
        /// </summary>
        public void SetFieldIdInvalid() => this.tag |= FieldIdCompleteMask;

        /// <summary>
        /// Returns <see langword="true"/> if the <see cref="FieldId"/> represents a complete id, <see langword="false"/> if more data is required.
        /// </summary>
        /// <remarks>
        /// If all bits are set in the field id portion of the tag, this field id is not valid and this tag must be followed by a field id.
        /// Therefore, field ids 0-7 can be represented without additional bytes.
        /// </remarks>
        public bool IsFieldIdValid => (this.tag & FieldIdCompleteMask) != FieldIdCompleteMask && this.WireType != WireType.Extended;

        /// <summary>
        /// Returns <see langword="true"/> if this tag must be followed by a field id.
        /// </summary>
        public bool HasExtendedFieldId => (this.tag & FieldIdCompleteMask) == FieldIdCompleteMask && this.WireType != WireType.Extended;
    }

    /// <summary>
    /// Represents a 3-bit wire type, shifted into position 
    /// </summary>
    public enum WireType : byte
    {
        VarInt = 0b000 << 5, // Followed by a VarInt
        TagDelimited = 0b001 << 5, // Followed by field specifiers, then an Extended tag with EndTagDelimited as the extended wire type.
        LengthPrefixed = 0b010 << 5, // Followed by VarInt length representing the number of bytes which follow.
        Fixed32 = 0b011 << 5, // Followed by 4 bytes
        Fixed64 = 0b100 << 5, // Followed by 8 bytes
        Fixed128 = 0b101 << 5, // Followed by 16 bytes
        Reference = 0b110 << 5, // Followed by a VarInt reference to a previously defined object. Note that the SchemaType and type specification must still be included.
        Extended = 0b111 << 5, // This is a control tag. The schema type and embedded field id are invalid. The remaining 5 bits are used for control information.
    }

    public enum SchemaType : byte
    {
        Expected = 0b00 << 3, // This value has the type expected by the current schema.
        WellKnown = 0b01 << 3, // This value is an instance of a well-known type. Followed by a VarInt type id.
        Encoded = 0b10 << 3, // This value is of a named type. Followed by an encoded type name.
        Referenced = 0b11 << 3, // This value is of a type which was previously specified. Followed by a VarInt indicating which previous type is being reused.
    }

    public enum ExtendedWireType : byte
    {
        EndTagDelimited = 0b00 << 3, // This tag marks the end of a tag-delimited object.
        EndBaseFields = 0b01 << 3, // This tag marks the end of a base object in a tag-delimited object.
    }
}
