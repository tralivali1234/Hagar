using System;
using System.Collections.Generic;
using System.Text;
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
            var serializerCatalog = new SerializerCatalog(null);
            var stringCodec = new StringCodec(serializerCatalog);
            var intCodec = new IntegerCodec();
            var baseTypeSerializer = new BaseTypeSerializer<StringCodec>(stringCodec);
            var activator = new DefaultActivator<SubType>();
            var objectCodec = new ObjectCodec(serializerCatalog);

            serializerCatalog.SetSerializer(
                new Dictionary<Type, IFieldCodec<object>>
                {
                    [typeof(string)] = stringCodec,
                });


            var partialSerializer = new SubTypeSerializer<BaseTypeSerializer<StringCodec>, StringCodec, IntegerCodec>(
                baseTypeSerializer,
                stringCodec,
                intCodec,
                objectCodec);
            var serializer =
                new ConcreteTypeSerializer<SubType, DefaultActivator<SubType>, SubTypeSerializer<
                    BaseTypeSerializer<StringCodec>, StringCodec, IntegerCodec>>(
                    activator,
                    partialSerializer, serializerCatalog);

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
            var testString = "hello, hagar";
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
                    BaseTypeString = testString,
                    String = null,
                    Int = 1
                });
            Test(
                serializer,
                new SubType
                {
                    BaseTypeString = testString,
                    String = null,
                    Int = 1
                });
            TestSkip(
                serializer,
                new SubType
                {
                    BaseTypeString = testString,
                    String = null,
                    Int = 1
                });
            Test(
                serializer,
                new SubType
                {
                    BaseTypeString = "HOHOHO",
                    AddedLaterString = testString,
                    String = null,
                    Int = 1,
                    Ref = testString
                });

            var self = new SubType
            {
                BaseTypeString = "HOHOHO",
                AddedLaterString = testString,
                String = null,
                Int = 1
            };
            self.Ref = self;
            Test(serializer, self );
        }

        static void Test(IFieldCodec<SubType> serializer, SubType expected)
        {
            var session = new SerializerSession();
            var writer = new Writer();

            serializer.WriteField(writer, session, 0, typeof(SubType), expected);

            Console.WriteLine($"Size: {writer.CurrentOffset} bytes.");
            Console.WriteLine($"Wrote References:\n{GetWriteReferenceTable(session)}");
            var reader = new Reader(writer.ToBytes());
            var deserializationContext = new SerializerSession();
            var initialHeader = reader.ReadFieldHeader(session);
            //Console.WriteLine(initialHeader);
            var actual = serializer.ReadValue(reader, deserializationContext, initialHeader);

            Console.WriteLine($"Expect: {expected}\nActual: {actual}");
            var references = GetReadReferenceTable(deserializationContext);
            Console.WriteLine($"Read references:\n{references}");
        }

        private static StringBuilder GetReadReferenceTable(SerializerSession session)
        {
            var table = session.ReferencedObjects.CopyReferenceTable();
            var references = new StringBuilder();
            foreach (var entry in table)
            {
                references.AppendLine($"\t[{entry.Key}] {entry.Value}");
            }
            return references;
        }
        private static StringBuilder GetWriteReferenceTable(SerializerSession session)
        {
            var table = session.ReferencedObjects.CopyIdTable();
            var references = new StringBuilder();
            foreach (var entry in table)
            {
                references.AppendLine($"\t[{entry.Value}] {entry.Key}");
            }
            return references;
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
        public string AddedLaterString { get; set; }

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
        public object Ref { get; set; }

        public override string ToString()
        {
            string refString = this.Ref == this ? "[this]" : $"[{this.Ref?.ToString() ?? "null"}]";
            return $"{base.ToString()}, {nameof(this.String)}: {this.String}, {nameof(this.Int)}: {this.Int}, Ref: {refString}";
        }
    }
}