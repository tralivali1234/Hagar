using System;
using System.Collections.Generic;
using Hagar.Activator;
using Hagar.Codec;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Serializer
{
    /// <summary>
    /// Serializer for reference types which can be instantiated.
    /// </summary>
    /// <typeparam name="TField">The type.</typeparam>
    /// <typeparam name="TActivator">The activator used to create new instances of the specified type.</typeparam>
    /// <typeparam name="TSerializer">The partial serializer for the specified type.</typeparam>
    public class ConcreteTypeSerializer<TField, TActivator, TSerializer> : IFieldCodec<TField>
        where TField : class
        where TActivator : IActivator<TField>
        where TSerializer : IPartialSerializer<TField>
    {
        private readonly TActivator activator;
        private readonly TSerializer serializer;
        private readonly ICodecProvider codecProvider;

        public ConcreteTypeSerializer(TActivator activator, TSerializer serializer, ICodecProvider codecProvider)
        {
            this.activator = activator;
            this.serializer = serializer;
            this.codecProvider = codecProvider;
        }

        public void WriteField(Writer writer, SerializerSession session, uint fieldIdDelta, Type expectedType, TField value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, session, fieldIdDelta, expectedType, value)) return;
            var fieldType = value.GetType();
            if (fieldType == typeof(TField))
            {
                writer.WriteStartObject(session, fieldIdDelta, expectedType, fieldType);
                this.serializer.Serialize(writer, session, value);
                writer.WriteEndObject();
            }
            else
            {
                var specificSerializer = this.codecProvider.GetCodec(fieldType);
                if (specificSerializer != null)
                {
                    specificSerializer.WriteField(writer, session, fieldIdDelta, expectedType, value);
                }
                else
                {
                    ThrowSerializerNotFoundException(fieldType);
                }
            }
        }

        public TField ReadValue(Reader reader, SerializerSession session, Field field)
        {
            if (field.WireType == WireType.Reference) return ReferenceCodec.ReadReference<TField>(reader, session, field, this.codecProvider);
            var fieldType = field.FieldType;
            if (fieldType == null || fieldType == typeof(TField))
            {
                var result = this.activator.Create();
                ReferenceCodec.RecordObject(session, result);
                this.serializer.Deserialize(reader, session, result);
                return result;
            }

            // The type is a descendant, not an exact match, so get the specific serializer for it.
            var specificSerializer = this.codecProvider.GetCodec(fieldType);
            if (specificSerializer != null)
            {
                return (TField) specificSerializer.ReadValue(reader, session, field);
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