using System;
using System.Diagnostics.CodeAnalysis;
using Hagar.Activator;
using Hagar.Codec;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Serializer
{
    [SuppressMessage("ReSharper", "TypeParameterCanBeVariant")]
    public interface IPartialSerializer<T> where T : class
    {
        void Serialize(Writer writer, SerializationContext context, T value);
        void Deserialize(Reader reader, SerializationContext context, T value);
    }

    public interface IPartialValueSerializer<T> where T : struct
    {
        void Serialize(Writer writer, SerializationContext context, ref T value);
        void Deserialize(Reader reader, SerializationContext context, ref T value);
    }

    public interface IFieldSerializer<T>
    {
        void Serialize(Writer writer, SerializationContext context, T value);
        T Deserialize(Reader reader, SerializationContext context, Field field);
    }

    public class ObjectFieldSerializer<TField, TActivator, TSerializer> : IFieldCodec<TField> where TField : class
        where TActivator : IActivator<TField>
        where TSerializer : IPartialSerializer<TField>
    {
        private readonly TActivator activator;
        private readonly TSerializer serializer;

        public ObjectFieldSerializer(TActivator activator, TSerializer serializer)
        {
            this.activator = activator;
            this.serializer = serializer;
        }

        public void WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType,
            TField value)
        {
            if (ReferenceCodec.TryWriteReferenceField(writer, context, fieldId, expectedType, value)) return;
            writer.WriteStartObject(context, fieldId, expectedType, value.GetType());
            this.serializer.Serialize(writer, context, value);
            writer.WriteEndObject();
        }

        public TField ReadValue(Reader reader, SerializationContext context, Field field)
        {
            if (field.WireType == WireType.Reference) return ReferenceCodec.ReadReference<TField>(reader, context);
            var result = this.activator.Create(reader, context);
            ReferenceCodec.RecordObject(context, result);
            this.serializer.Deserialize(reader, context, result);
            return result;
        }
    }
}