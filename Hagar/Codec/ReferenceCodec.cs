using System;
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

        public static T ReadReference<T>(Reader reader, SerializerSession context)
        {
            var reference = reader.ReadVarUInt32();
            if (context.ReferencedObjects.TryGetReferencedObject(reference, out object value)) return (T) value;

            ThrowReferenceNotFound<T>(reference, context);
            return default(T);
        }
        
        public static void RecordObject(SerializerSession context, object value) => context.ReferencedObjects.RecordReferenceField(value);

        private static void ThrowReferenceNotFound<T>(uint reference, SerializerSession context)
        {
            throw new ReferenceNotFoundException(typeof(T), reference, context.ReferencedObjects.CopyReferenceTable());
        }
    }
}