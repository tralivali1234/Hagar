using System;
using System.Text;
using Hagar.Exceptions;

namespace Hagar.WireProtocol
{
    public struct Field
    {
        private uint fieldId;
        private Type fieldType;
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
            this.fieldType = null;
            if (!this.HasFieldId) ThrowFieldIdInvalid();
        }

        public uint FieldId
        {
            // If the embedded field id is valid, return it, otherwise return the extended field id.
            // The extended field id might not be valid if this field has the Extended wire type.
            get => this.Tag.IsFieldIdValid ? this.Tag.FieldId : this.HasFieldId ? this.fieldId : ThrowFieldIdInvalid();
            set
            {
                // If the field id can fit into the tag, embed it there, otherwise invalidate the embedded field id and set the full field id.
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

        public Type FieldType
        {
            get => this.IsSchemaTypeValid ? this.fieldType : ThrowFieldTypeInvalid();
            set
            {
                if (this.IsSchemaTypeValid) this.fieldType = value;
                else ThrowFieldTypeInvalid();
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
            builder.Append('[').Append((string) this.WireType.ToString());
            if (this.HasFieldId) builder.Append($", Id:{this.FieldId}");
            if (this.IsSchemaTypeValid) builder.Append($", Type:{this.SchemaType}");
            if (this.WireType == WireType.Extended) builder.Append($": {this.ExtendedWireType}");
            builder.Append(']');
            return builder.ToString();
        }

        private static uint ThrowFieldIdInvalid() => throw new FieldIdNotPresentException();
        private static SchemaType ThrowSchemaTypeInvalid() => throw new SchemaTypeInvalidException();
        private static Type ThrowFieldTypeInvalid() => throw new FieldTypeInvalidException();
        private static ExtendedWireType ThrowExtendedWireTypeInvalid() => throw new ExtendedWireTypeInvalidException();
    }
}