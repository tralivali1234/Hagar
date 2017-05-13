using System;
using System.Text;
using Hagar.Orleans.Serialization;

namespace Hagar
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
            type = Type.GetType(Encoding.UTF8.GetString(bytes), throwOnError: false);
            return type != null;
        }
    }
}
