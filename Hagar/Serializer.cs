using System;
using System.IO.Pipelines;
using Hagar.Orleans.Serialization;

namespace Hagar
{
    public interface ISerializationContext { }
    public interface ICopyContext { }
    public interface IDeserializationContext { }

    public class Ser
    {
        public void Write()
        {
            var factory = new PipeFactory();
            var pipe = factory.Create();
            var writer = pipe.Writer;
            var buffer = writer.Alloc(0);
            buffer.Ensure(5);
        }
    }
    
    public static class WireProtocol
    {
        public struct Field
        {
            private int fieldId;
            public Tag Tag;
            public Span<byte> FieldData;
        }

        public struct Tag
        {
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
            public WireType WireType => (WireType) (this.tag & WireTypeMask);

            /// <summary>
            /// Returns <see langword="true"/> if this field represents a value of the expected type, <see langword="false"/> otherwise.
            /// </summary>
            /// <remarks>
            /// If this value is <see langword="false"/>, this tag and field id must be followed by a type specification.
            /// </remarks>
            public SchemaType SchemaType
            {
                get => (SchemaType) (this.tag & SchemaTypeMask);
                set => this.tag |= (byte)((byte) value & SchemaTypeMask);
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
            public uint FieldId => (uint) (this.tag & FieldIdMask);

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

        // [W W W] [S S] [F F F]
        public const byte WireTypeMask = 0b1110_0000; // The first 3 bits are dedicated to the wire type.
        public const byte SchemaTypeMask = 0b0001_1000; // The next 2 bits are dedicated to the schema type specifier, if the schema type is expected.
        public const byte FieldIdMask = 0b000_0111; // The final 3 bits are used for the field id, if the field id is expected.
        public const byte FieldIdCompleteMask = 0b0000_0111;

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
            EndTagDelimited = 0b00 << 3, // This tag marks the end of a tag-delimited object. Field id is invalid.
        }
    }

    /// <summary>
    /// Codec for operating with primitive types using the <see cref="WireCodec"/>.
    /// Operates on types such as int, bool, string, objects, and fields.
    /// </summary>
    public class PrimitiveTypeCodec
    {

    }

    /// <summary>
    /// Codec for operating with the wire format.
    /// Operates on format-specific types, such as variable-length integers, length-prefixed data, fixed-width data, and tag-delimited data.
    /// </summary>
    public static class WireEncoder
    {
        public static void Write(this Writer writer, int fieldId, int val)
        {
            byte tag = 0;
            tag |= (byte) WireProtocol.WireType.Fixed32;

        }
    }
}
