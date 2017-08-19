using System;
using System.Reflection;
using System.Runtime.Serialization;
using Hagar.Codec;
using Hagar.Serializer;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.ISerializable
{
    public class TypeSerializerCodec : IFieldCodec<Type>
    {
        private static readonly Type SchemaTypeType = typeof(SchemaType);
        private static readonly Type TypeType = typeof(Type);
        private static readonly Type ByteArrayType = typeof(byte[]);
        private static readonly Type UIntType = typeof(uint);

        public void WriteField(Writer writer, SerializerSession session, uint fieldId, Type expectedType, Type value)
        {
            writer.WriteFieldHeader(session, fieldId, TypeType, TypeType, WireType.TagDelimited);
            var (schemaType, id) = GetSchemaType(session, value);

            // Write the encoding type
            writer.WriteFieldHeader(session, 0, SchemaTypeType, SchemaTypeType, WireType.VarInt);
            writer.WriteVarInt((uint)schemaType);

            if (schemaType == SchemaType.Encoded)
            {
                // If the type is encoded, write the length-prefixed bytes.
                writer.WriteFieldHeader(session, 1, ByteArrayType, ByteArrayType, WireType.LengthPrefixed);
                session.TypeCodec.Write(writer, value);
            }
            else
            {
                // If the type is referenced or well-known, write it as a varint.
                writer.WriteFieldHeader(session, 2, UIntType, UIntType, WireType.VarInt);
                writer.WriteVarInt(id);
            }

            writer.WriteEndObject();
        }

        public Type ReadValue(Reader reader, SerializerSession session, Field field)
        {
            throw new NotImplementedException();
        }

        private static (SchemaType, uint) GetSchemaType(SerializerSession session, Type actualType)
        {
            if (session.WellKnownTypes.TryGetWellKnownTypeId(actualType, out uint typeId))
            {
                return (SchemaType.WellKnown, typeId);
            }

            if (session.ReferencedTypes.TryGetTypeReference(actualType, out uint reference))
            {
                return (SchemaType.Referenced, reference);
            }

            return (SchemaType.Encoded, 0);
        }
    }

    public class DotNetSerializableCodec : IGenericCodec
    {
        private static readonly TypeInfo SerializableType = typeof(System.Runtime.Serialization.ISerializable).GetTypeInfo();
        private static readonly FormatterConverter FormatterConverter = new FormatterConverter();
        private readonly StreamingContext streamingContext = new StreamingContext();
        private static readonly Type CodecType = typeof(DotNetSerializableCodec);

        public void WriteField(Writer writer, SerializerSession session, uint fieldId, Type expectedType, object value)
        {
            var serializableValue = (System.Runtime.Serialization.ISerializable) value;
            var type = value.GetType();
            var info = new SerializationInfo(type, FormatterConverter);
            serializableValue.GetObjectData(info, streamingContext);
            writer.WriteFieldHeader(session, fieldId, expectedType, CodecType, WireType.TagDelimited);
            writer.WriteFieldHeader(session, 0, typeof(Type), typeof(Type), );
            foreach (var field in info)
            {
            }
        }

        public object ReadValue(Reader reader, SerializerSession session, Field field)
        {
            throw new NotImplementedException();
        }

        public bool IsSupportedType(Type type) => SerializableType.IsAssignableFrom(type);
    }
}
