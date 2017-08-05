using System;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.Utilities.Orleans.Serialization;
using Hagar.WireProtocol;

namespace Hagar.Codec
{
    public static class ReferenceCodec
    {
        public static bool TryWriteReferenceField(
            Writer writer,
            SerializationContext context,
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

        public static T ReadReference<T>(Reader reader, SerializationContext context)
        {
            var reference = reader.ReadVarUInt32();
            if (context.ReferencedObjects.TryGetReferencedType(reference, out object value)) return (T) value;

            ThrowReferenceNotFound<T>(reference, context);
            return default(T);
        }
        
        public static void RecordObject(SerializationContext context, object value) => context.ReferencedObjects.AddReference(value);

        private static void ThrowReferenceNotFound<T>(uint reference, SerializationContext context)
        {
            throw new ReferenceNotFoundException(typeof(T), reference, context.ReferencedObjects.CopyReferenceTable());
        }
    }
}