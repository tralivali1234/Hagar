using System;
using System.Text;
using Hagar.Buffers;
using Hagar.Utilities;

namespace Hagar.TypeSystem
{
    public class TypeCodec
    {
        private readonly CachedReadConcurrentDictionary<Type, byte[]> typeToEncodingMapping = new CachedReadConcurrentDictionary<Type, byte[]>();
        //private readonly CachedReadConcurrentDictionary<byte[], Type> encodingToTypeMapping = new CachedReadConcurrentDictionary<byte[], Type>();

        public void Write(Writer writer, Type type)
        {
            var encoded = this.typeToEncodingMapping.GetOrAdd(type, t => Encoding.UTF8.GetBytes(RuntimeTypeNameFormatter.Format(t)));
            writer.Write((uint)encoded.Length);
            writer.Write(encoded);
        }

        public bool TryRead(Reader reader, out Type type)
        {
            var length = reader.ReadUInt();
            var bytes = reader.ReadBytes((int)length);
            var typeName = Encoding.UTF8.GetString(bytes);
            type = Type.GetType(typeName, throwOnError: false);
            return type != null;
        }

        public Type Read(Reader reader)
        {
            var length = reader.ReadUInt();
            var bytes = reader.ReadBytes((int)length);
            var typeName = Encoding.UTF8.GetString(bytes);
            return Type.GetType(typeName);
        }
    }
}
