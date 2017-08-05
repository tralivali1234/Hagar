using System;
using Hagar.Codec;
using Hagar.Serializer;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.Utilities.Orleans.Serialization;

namespace TestApp
{
    partial class Program
    {
        static void Main(string[] args)
        {
            var stringCodec = new StringCodec();
            var intCodec = new IntegerCodec();
            var baseTypeSerializer = new BaseTypeSerializer<StringCodec>(stringCodec);
            var serializer =
                new SubTypeSerializer<BaseTypeSerializer<StringCodec>, StringCodec, IntegerCodec>(baseTypeSerializer,
                    stringCodec, intCodec);

            Test(serializer, new SubType
            {
                BaseTypeString = "base",
                String = "sub",
                Int = 2,
            });

            var testString = new string('*', 100);
            Test(
                serializer,
                new SubType
                {
                    BaseTypeString = testString,
                    String = testString,
                    Int = 109
                });
        }

        static void Test(IPartialSerializer<SubType> serializer, SubType expected)
        {
            var context = new SerializationContext();
            var writer = new Writer();

            writer.WriteStartObject(context, 0, typeof(SubType), typeof(SubType));
            serializer.Serialize(writer, context, expected);
            writer.WriteEndObject();

            Console.WriteLine($"Size: {writer.CurrentOffset} bytes");
            var reader = new Reader(writer.ToBytes());
            var deserializationContext = new SerializationContext();
            var initialHeader = reader.ReadFieldHeader(context);
            Console.WriteLine(initialHeader);
            var actual = new SubType();
            serializer.Deserialize(reader, deserializationContext, actual);

            Console.WriteLine($"Expect: {expected}\nActual: {actual}");
        }
    }

    public class BaseType
    {
        public string BaseTypeString { get; set; }

        public override string ToString()
        {
            return $"{nameof(BaseTypeString)}: {BaseTypeString}";
        }
    }

    public class SubType : BaseType
    {
        // 0
        public string String { get; set; }

        // 1
        public int Int { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(String)}: {String}, {nameof(Int)}: {Int}";
        }
    }
}