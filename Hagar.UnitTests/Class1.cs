using System;

namespace Hagar.UnitTests
{/*
    public class Serializer
    {
        private class NullSerializer : IHagarSerializer
        {
            public void Serialize(Writer writer, SerializationContext context, uint fieldId, object instance, Type expectedType)
            {
                writer.WriteFieldHeader(context, fieldId, expectedType, typeof(void), WireType.TagDelimited);
                writer.WriteEndObject();
            }

            public object Deserialize(Reader reader, SerializationContext context, Field field)
            {
                var end = reader.ReadFieldHeader(context);
                if (!end.IsEndObject) ThrowInvalidEnd(field, end);
                return null;
            }

            private static void ThrowInvalidEnd(Field field, Field next) => throw new HagarException(
                $"Field {field} must be followed immediately by an {ExtendedWireType.EndTagDelimited}, but it was followed by {next}.");
        }

        public void Serialize(Writer writer, SerializationContext context, object instance, Type expectedType)
        {
            if (instance == null)
            {
                writer.WriteNull();
                return;
            }
        }

        public object Deserialize(Reader reader, SerializationContext context, Type expectedType)
        {
            var header = reader.ReadFieldHeader(context);
            if (header.Tag.IsNull)
            {
                return null;
            }

            // Deserialize TagDelimited with field id = 0
            // Deserialize end object
        }

        public void SerializeReferenceField<T>(Writer writer, SerializationContext context, uint fieldId, T instance) where T : class
        {
            // If null, no serialization is neccessary.
            if (instance == default(T)) return;
            // If already serialized, serialize reference
            // Find most appropriate serializer
            // Call serializer with
        }

        public void SerializeValueField<T>(Writer writer, SerializationContext context, uint fieldId, ref T instance) where T : struct
        {
            // Get the serializer for T
            writer.writeIn
        }
    }*/
}
