using System;
using System.Collections.Generic;
using Hagar.Activator;
using Hagar.Codec;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Serializer
{
    public class ObjectSerializer<TField, TActivator, TSerializer> : IFieldCodec<TField>
        where TField : class
        where TActivator : IActivator<TField>
        where TSerializer : IPartialSerializer<TField>
    {
        private readonly TActivator activator;
        private readonly TSerializer serializer;
        private readonly ISerializerCatalog serializerCatalog;

        public ObjectSerializer(TActivator activator, TSerializer serializer, ISerializerCatalog serializerCatalog)
        {
            this.activator = activator;
            this.serializer = serializer;
            this.serializerCatalog = serializerCatalog;
        }

        public void WriteField(Writer writer, SerializerSession context, uint fieldId, Type expectedType, TField value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, context, fieldId, expectedType, value)) return;
            var fieldType = value.GetType();
            if (fieldType == typeof(TField))
            {
                writer.WriteStartObject(context, fieldId, expectedType, fieldType);
                this.serializer.Serialize(writer, context, value);
                writer.WriteEndObject();
            }
            else
            {
                var specificSerializer = this.serializerCatalog.GetSerializer(fieldType);
                if (specificSerializer != null)
                {
                    specificSerializer.WriteField(writer, context, fieldId, expectedType, value);
                }
                else
                {
                    ThrowSerializerNotFoundException(fieldType);
                }
            }
        }

        public TField ReadValue(Reader reader, SerializerSession context, Field field)
        {
            if (field.WireType == WireType.Reference) return ReferenceCodec.ReadReference<TField>(reader, context);
            var fieldType = field.FieldType;
            if (fieldType == null || fieldType == typeof(TField))
            {
                var result = this.activator.Create(reader, context);
                ReferenceCodec.RecordObject(context, result);
                this.serializer.Deserialize(reader, context, result);
                return result;
            }

            // The type is a descendant, not an exact match, so get the specific serializer for it.
            var specificSerializer = this.serializerCatalog.GetSerializer(fieldType);
            if (specificSerializer != null)
            {
                return (TField) specificSerializer.ReadValue(reader, context, field);
            }

            ThrowSerializerNotFoundException(fieldType);
            return null;
        }

        private static void ThrowSerializerNotFoundException(Type type)
        {
            throw new KeyNotFoundException($"Could not find a serializer of type {type}.");
        }
    }
}