using System;
using System.Runtime.Serialization;
using System.Text;
using Hagar.Codec;
using Hagar.Serializer;
using Hagar.Session;
using Hagar.TypeSystem;
using Hagar;
using Hagar.Buffers;
using Hagar.ISerializable;
using Hagar.Json;
using Hagar.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NodaTime;

namespace TestApp
{
    internal class HandCraftedProvider : IMetadataProvider<CodecMetadata>
    {
        public void PopulateMetadata(CodecMetadata metadata)
        {
            metadata.PartialSerializers.Add(typeof(SubTypeSerializer));
            metadata.PartialSerializers.Add(typeof(BaseTypeSerializer));
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddCryoBuf();
            serviceCollection.AddSingleton<IGeneralizedCodec, DotNetSerializableCodec>();
            serviceCollection.AddSingleton<IMetadataProvider<CodecMetadata>, HandCraftedProvider>();
            serviceCollection.AddSingleton<IGeneralizedCodec, JsonCodec>();
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var codecProvider = serviceProvider.GetRequiredService<CodecProvider>();
            var serializer = codecProvider.GetCodec<SubType>();
            var testString = "hello, hagar";
            Test(
                serializer,
                new SubType
                {
                    BaseTypeString = "base",
                    String = "sub",
                    Int = 2,
                });
            Test(
                serializer,
                new SubType
                {
                    BaseTypeString = "base",
                    String = "sub",
                    Int = int.MinValue,
                });

            // Tests for duplicates
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
            Test(serializer, self);

            self.Ref = Guid.NewGuid();
            Test(serializer, self);

            Test(
                new AbstractTypeSerializer<object>(codecProvider),
                new WhackyJsonType
                {
                    Number = 7,
                    String = "bananas!"
                });

            var mySerializable = new MySerializableClass
            {
                String = "yolooo",
                Integer = 38,
                Self = null,
            };
            Test(
                new AbstractTypeSerializer<object>(codecProvider),
                mySerializable
            );

            mySerializable.Self = mySerializable;
            Test(
                new AbstractTypeSerializer<object>(codecProvider),
                mySerializable
            );
            Exception exception = null;
            try
            {
                throw new ReferenceNotFoundException(typeof(int), 2401);
            }
            catch (Exception e)
            {
                exception = e;
            }

            Test(
                new AbstractTypeSerializer<object>(codecProvider),
                exception
            );

            Test(new AbstractTypeSerializer<object>(codecProvider), new LocalDate());

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        [Serializable]
        public class MySerializableClass : ISerializable
        {
            public string String { get; set; }
            public int Integer { get; set; }
            public MySerializableClass Self { get; set; }

            public MySerializableClass()
            {
            }

            protected MySerializableClass(SerializationInfo info, StreamingContext context)
            {
                this.String = info.GetString(nameof(this.String));
                this.Integer = info.GetInt32(nameof(this.Integer));
                this.Self = (MySerializableClass) info.GetValue(nameof(this.Self), typeof(MySerializableClass));
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                info.AddValue(nameof(this.String), this.String);
                info.AddValue(nameof(this.Integer), this.Integer);
                info.AddValue(nameof(this.Self), this.Self);
            }

            public override string ToString()
            {
                string refString = this.Self == this ? "[this]" : $"[{this.Self?.ToString() ?? "null"}]";
                return $"{base.ToString()}, {nameof(this.String)}: {this.String}, {nameof(this.Integer)}: {this.Integer}, Self: {refString}";
            }

        }

        static void Test<T>(IFieldCodec<T> serializer, T expected)
        {
            var session = new SerializerSession(new TypeCodec(), new WellKnownTypeCollection(), new ReferencedTypeCollection(), new ReferencedObjectCollection());
            var writer = new Writer();

            serializer.WriteField(writer, session, 0, typeof(T), expected);

            Console.WriteLine($"Size: {writer.CurrentOffset} bytes.");
            Console.WriteLine($"Wrote References:\n{GetWriteReferenceTable(session)}");
            var reader = new Reader(writer.ToBytes());
            var deserializationContext = new SerializerSession(new TypeCodec(), new WellKnownTypeCollection(), new ReferencedTypeCollection(), new ReferencedObjectCollection());
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
            var session = new SerializerSession(new TypeCodec(), new WellKnownTypeCollection(), new ReferencedTypeCollection(), new ReferencedObjectCollection());
            var writer = new Writer();

            serializer.WriteField(writer, session, 0, typeof(SubType), expected);
            
            var reader = new Reader(writer.ToBytes());
            var deserializationContext = new SerializerSession(new TypeCodec(), new WellKnownTypeCollection(), new ReferencedTypeCollection(), new ReferencedObjectCollection());
            var initialHeader = reader.ReadFieldHeader(session);
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

    public class WhackyJsonType
    {
        public int Number { get; set; }
        public string String { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}