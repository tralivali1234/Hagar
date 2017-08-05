using System;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codec
{
    public static class ObjectCodec
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

        public static void WriteStartObject(
            this Writer writer,
            SerializationContext context,
            uint fieldId,
            Type expectedType,
            Type actualType)
        {
            writer.WriteFieldHeader(context, fieldId, expectedType, actualType, WireType.TagDelimited);
        }

        public static void WriteEndObject(this Writer writer)
        {
            writer.Write((byte) EndObjectTag);
        }

        public static void WriteEndBase(this Writer writer)
        {
            writer.Write((byte) EndBaseFieldsTag);
        }
    }
}