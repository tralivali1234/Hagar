using System;
using Hagar.Serializer;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codec
{
    public static class ReferenceCodec
    {
        /// <summary>
        /// Indicates that the field being serialized or deserialized is a value type.
        /// </summary>
        /// <param name="session">The serializer session.</param>
        public static void MarkValueField(SerializerSession session)
        {
            session.ReferencedObjects.MarkValueField();
        }

        public static bool TryWriteReferenceField(
            Writer writer,
            SerializerSession context,
            uint fieldId,
            Type expectedType,
            object value)
        {
            if (!context.ReferencedObjects.GetOrAddReference(value, out uint reference))
            {
                return false;
            }

            writer.WriteFieldHeader(context, fieldId, expectedType, value?.GetType(), WireType.Reference);
            writer.WriteVarInt(reference);
            return true;
        }

        public static T ReadReference<T>(Reader reader, SerializerSession context, Field field, ISerializerCatalog serializers)
        {
            var reference = reader.ReadVarUInt32();
            if (!context.ReferencedObjects.TryGetReferencedObject(reference, out object value))
            {
                ThrowReferenceNotFound<T>(reference, context);
            }

            if (value is UnknownFieldMarker marker)
            {
                return DeserializeFromMarker<T>(reader, context, field, serializers, marker, reference);
            }

            if (value is T) return (T)value;
            return default(T);
        }

        private static T DeserializeFromMarker<T>(
            Reader reader,
            SerializerSession context,
            Field field,
            ISerializerCatalog serializers,
            UnknownFieldMarker marker,
            uint reference)
        {
            // Create a reader at the position specified by the marker.
            var referencedReader = new Reader(reader.GetBuffers());
            referencedReader.Advance(marker.Offset);

            // Determine the correct type for the field.
            var fieldType = field.FieldType ?? marker.Field.FieldType ?? typeof(T);

            // Get a serializer for that type.
            var specificSerializer = serializers.GetSerializer(fieldType);

            // Reset the session's reference id so that the deserialized object overwrites the marker.
            var originalCurrentReferenceId = context.ReferencedObjects.CurrentReferenceId;
            context.ReferencedObjects.CurrentReferenceId = reference - 1;

            // Deserialize the object, replacing the marker in the session.
            var result = (T) specificSerializer.ReadValue(referencedReader, context, marker.Field);

            // Revert the reference id.
            context.ReferencedObjects.CurrentReferenceId = originalCurrentReferenceId;
            return result;
        }

        public static void RecordObject(SerializerSession context, object value) => context.ReferencedObjects.RecordReferenceField(value);

        private static void ThrowReferenceNotFound<T>(uint reference, SerializerSession context)
        {
            throw new ReferenceNotFoundException(typeof(T), reference, context.ReferencedObjects.CopyReferenceTable());
        }
    }
}