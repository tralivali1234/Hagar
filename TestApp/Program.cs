using System;
using Hagar.Activator;
using Hagar.Codec;
using Hagar.Serializer;
using Hagar.Session;
using Hagar.Utilities;

namespace TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var stringCodec = new StringCodec();
            var intCodec = new IntegerCodec();
            var baseTypeSerializer = new BaseTypeSerializer<StringCodec>(stringCodec);
            var activator = new DefaultActivator<SubType>();
            var partialSerializer = new SubTypeSerializer<BaseTypeSerializer<StringCodec>, StringCodec, IntegerCodec>(
                baseTypeSerializer,
                stringCodec, intCodec);
            var serializer =
                new ConcreteTypeSerializer<SubType, DefaultActivator<SubType>, SubTypeSerializer<
                    BaseTypeSerializer<StringCodec>, StringCodec, IntegerCodec>>(
                    activator,
                    partialSerializer, new SerializerCatalog(null));

            Test(serializer, new SubType
            {
                BaseTypeString = "base",
                String = "sub",
                Int = 2,
            });
            Test(serializer, new SubType
            {
                BaseTypeString = "base",
                String = "sub",
                Int = int.MinValue,
            });

            // Tests for duplicates
            var testString = new string('*', 10);
            Test(
                serializer,
                new SubType
                {
                    BaseTypeString = testString,
                    String = testString,
                    Int = 10
                });
            Test(
                serializer,
                new SubType
                {
                    BaseTypeString = "hello, hagar",
                    String = null,
                    Int = 1
                });
            Test(
                serializer,
                new SubType
                {
                    BaseTypeString = "hello, hagar",
                    String = null,
                    Int = 1
                });
            TestSkip(
                serializer,
                new SubType
                {
                    BaseTypeString = "hello, hagar",
                    String = null,
                    Int = 1
                });
        }

        static void Test(IFieldCodec<SubType> serializer, SubType expected)
        {
            var session = new SerializerSession();
            var writer = new Writer();

            serializer.WriteField(writer, session, 0, typeof(SubType), expected);

            Console.WriteLine($"Size: {writer.CurrentOffset} bytes");
            var reader = new Reader(writer.ToBytes());
            var deserializationContext = new SerializerSession();
            var initialHeader = reader.ReadFieldHeader(session);
            //Console.WriteLine(initialHeader);
            var actual = serializer.ReadValue(reader, deserializationContext, initialHeader);

            Console.WriteLine($"Expect: {expected}\nActual: {actual}");
        }

        static void TestSkip(IFieldCodec<SubType> serializer, SubType expected)
        {
            var context = new SerializerSession();
            var writer = new Writer();

            serializer.WriteField(writer, context, 0, typeof(SubType), expected);
            
            var reader = new Reader(writer.ToBytes());
            var deserializationContext = new SerializerSession();
            var initialHeader = reader.ReadFieldHeader(context);
            var skipCodec = new SkipFieldCodec();
            skipCodec.ReadValue(reader, deserializationContext, initialHeader);
            
            Console.WriteLine($"Skipped {reader.CurrentPosition}/{reader.Length} bytes.");
        }
    }

    public class BaseType
    {
        public string BaseTypeString { get; set; }

        public override string ToString()
        {
            return $"{nameof(this.BaseTypeString)}: {this.BaseTypeString}";
        }
    }

    public class SubType : BaseType
    {
        // 0
        public string String { get; set; }

        // 1
        public int Int { get; set; }
        
        // 3
        public BaseType Ref { get; set; }

        public override string ToString()
        {
            string refString = this.Ref == this ? "[this]" : $"[{this.Ref?.ToString() ?? "null"}]";
            return $"{base.ToString()}, {nameof(this.String)}: {this.String}, {nameof(this.Int)}: {this.Int}, Ref: {refString}";
        }
    }
}