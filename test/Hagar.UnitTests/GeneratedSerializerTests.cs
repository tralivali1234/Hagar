using System;
using Hagar.Buffers;
using Hagar.Codecs;
using Hagar.Serializers;
using Hagar.Session;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Hagar.UnitTests
{
    [Trait("Category", "BVT")]
    public class GeneratedSerializerTests : IDisposable
    {
        private readonly ServiceProvider serviceProvider;
        private readonly ITypedCodecProvider codecProvider;
        private readonly SessionPool sessionPool;

        public GeneratedSerializerTests()
        {
            this.serviceProvider = new ServiceCollection()
                .AddHagar()
                .AddSerializers(typeof(GeneratedSerializerTests).Assembly)
                .BuildServiceProvider();
            this.codecProvider = this.serviceProvider.GetRequiredService<ITypedCodecProvider>();
            this.sessionPool = this.serviceProvider.GetRequiredService<SessionPool>();
        }

        [Fact]
        public void GeneratedSerializersRoundTripThroughCodec()
        {
            var original = new SomeClassWithSerialzers { IntField = 2, IntProperty = 30 };
            var result = this.RoundTripThroughCodec(original);

            Assert.Equal(original.IntField, result.IntField);
            Assert.Equal(original.IntProperty, result.IntProperty);
        }

        [Fact]
        public void GeneratedSerializersRoundTripThroughSerializer()
        {
            var original = new SomeClassWithSerialzers { IntField = 2, IntProperty = 30 };
            var result = (SomeClassWithSerialzers) this.RoundTripThroughUntypedSerializer(original);

            Assert.Equal(original.IntField, result.IntField);
            Assert.Equal(original.IntProperty, result.IntProperty);
        }

        [Fact]
        public void UnmarkedFieldsAreNotSerialized()
        {
            var original = new SomeClassWithSerialzers { IntField = 2, IntProperty = 30, UnmarkedField = 12, UnmarkedProperty = 47 };
            var result = this.RoundTripThroughCodec(original);

            Assert.NotEqual(original.UnmarkedField, result.UnmarkedField);
            Assert.NotEqual(original.UnmarkedProperty, result.UnmarkedProperty);
        }
/*
        [Fact]
        public void UnmarkedFieldsAreNotSerialized()
        {
            var original = new GenericPoco<string>() { }
            var result = this.RoundTripThroughCodec(original);

            Assert.NotEqual(original.UnmarkedField, result.UnmarkedField);
            Assert.NotEqual(original.UnmarkedProperty, result.UnmarkedProperty);
        }*/

        public void Dispose()
        {
            this.serviceProvider?.Dispose();
        }

        private T RoundTripThroughCodec<T>(T original)
        {
            T result;
            var writer = new Writer();
            using (var readerSession = this.sessionPool.GetSession())
            using (var writeSession = this.sessionPool.GetSession())
            {
                var codec = this.codecProvider.GetCodec<T>();
                codec.WriteField(
                    writer,
                    writeSession,
                    0,
                    null,
                    original);

                var reader = new Reader(writer.ToBytes());

                var initialHeader = reader.ReadFieldHeader(readerSession);
                result = codec.ReadValue(reader, readerSession, initialHeader);
            }
            return result;
        }

        private object RoundTripThroughUntypedSerializer(object original)
        {
            object result;
            var writer = new Writer();
            using (var readerSession = this.sessionPool.GetSession())
            using (var writeSession = this.sessionPool.GetSession())
            {
                var serializer = this.serviceProvider.GetService<Serializer>();
                serializer.Serialize(original, writeSession, writer);
                var reader = new Reader(writer.ToBytes());

                result = serializer.Deserialize(readerSession, reader);
            }

            return result;
        }
    }
}
