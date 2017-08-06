using System;
using System.Reflection;
using System.Runtime.Serialization;
using Hagar.Serializer;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.ISerializable
{
    public class DotNetSerializableCodec : IGenericCodec
    {
        private static readonly TypeInfo SerializableType = typeof(System.Runtime.Serialization.ISerializable).GetTypeInfo();

        public void WriteField(Writer writer, SerializerSession session, uint fieldId, Type expectedType, object value)
        {
            var serializableValue = value as System.Runtime.Serialization.ISerializable;
            /*var info = new SerializationInfo(value.GetType(), new FormatterConverter());
            serializableValue.GetObjectData(info, new StreamingContext());*/
        }

        public object ReadValue(Reader reader, SerializerSession session, Field field)
        {
            throw new NotImplementedException();
        }

        public bool IsSupportedType(Type type) => SerializableType.IsAssignableFrom(type);
    }
}
