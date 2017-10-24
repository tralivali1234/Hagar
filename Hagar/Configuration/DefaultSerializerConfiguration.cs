using Hagar.Codec;
using Hagar.Metadata;

namespace Hagar.Configuration
{
    internal class DefaultSerializerConfiguration : IConfigurationProvider<SerializerConfiguration>
    {
        public void Configure(SerializerConfiguration configuration)
        {
            var codecs = configuration.FieldCodecs;
            codecs.Add(typeof(BoolCodec));
            codecs.Add(typeof(CharCodec));
            codecs.Add(typeof(ByteCodec));
            codecs.Add(typeof(SByteCodec));
            codecs.Add(typeof(UInt16Codec));
            codecs.Add(typeof(Int16Codec));
            codecs.Add(typeof(UInt32Codec));
            codecs.Add(typeof(Int32Codec));
            codecs.Add(typeof(UInt64Codec));
            codecs.Add(typeof(Int64Codec));
            codecs.Add(typeof(GuidCodec));
            codecs.Add(typeof(StringCodec));

#warning need to also handle RuntimeType, not just Type
            codecs.Add(typeof(TypeSerializerCodec));
        }
    }
}
