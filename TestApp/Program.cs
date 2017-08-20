using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Hagar.Activator;
using Hagar.Codec;
using Hagar.Json;
using Hagar.Serializer;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.ISerializable;
using Hagar.TypeSystem;
using Hagar;
using Newtonsoft.Json;

namespace TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var codecs = new Dictionary<Type, IFieldCodec<object>>
            {
                [typeof(bool)] = new BoolCodec(),
                [typeof(char)] = new CharCodec(),
                [typeof(byte)] = new ByteCodec(),
                [typeof(sbyte)] = new SByteCodec(),
                [typeof(ushort)] = new UInt16Codec(),
                [typeof(short)] = new Int16Codec(),
                [typeof(uint)] = new UInt32Codec(),
                [typeof(int)] = new Int32Codec(),
                [typeof(ulong)] = new UInt64Codec(),
                [typeof(long)] = new Int64Codec(),
                [typeof(Guid)] = FieldCodecWrapper.Create<Guid, GuidCodec>(new GuidCodec()),
            };
            var genericCodecs = new List<IGenericCodec>();
            var codecProvider = new CodecProvider(codecs, genericCodecs);

            var typeSerializerCodec = new TypeSerializerCodec(codecProvider);
            var typeCodec = FieldCodecWrapper.Create<Type, TypeSerializerCodec>(typeSerializerCodec);
            codecs[typeof(Type)] = typeCodec;
            // ReSharper disable once PossibleMistakenCallToGetType.2
            codecs[typeof(Type).GetType()] = typeCodec;

            // TODO: construct using DI
            var dotNetSerializableCodec = new DotNetSerializableCodec(
                typeSerializerCodec,
                new StringCodec(codecProvider),
                new ObjectCodec(codecProvider),
                new DefaultTypeFilter(),
                codecProvider);
            genericCodecs.Add(dotNetSerializableCodec);
            genericCodecs.Add(new JsonCodec(codecProvider));

            var stringCodec = new StringCodec(codecProvider);
            codecs[typeof(string)] = stringCodec;
            codecs[typeof(object)] = new ObjectCodec(codecProvider);

            var intCodec = new Int32Codec();
            var baseTypeSerializer = new BaseTypeSerializer<StringCodec>(stringCodec);
            var activator = new DefaultActivator<SubType>();
            var objectCodec = new ObjectCodec(codecProvider);

            var partialSerializer = new SubTypeSerializer<BaseTypeSerializer<StringCodec>, StringCodec, Int32Codec>(
                baseTypeSerializer,
                stringCodec,
                intCodec,
                objectCodec);
            var serializer =
                new ConcreteTypeSerializer<SubType, DefaultActivator<SubType>, SubTypeSerializer<
                    BaseTypeSerializer<StringCodec>, StringCodec, Int32Codec>>(
                    activator,
                    partialSerializer,
                    codecProvider);
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

            Test(
                new AbstractTypeSerializer<object>(codecProvider),
                new MySerializableClass
                {
                    String = "yolooo",
                    Integer = 38,
                    Self = null,
                }
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
        }

        static void Test<T>(IFieldCodec<T> serializer, T expected)
        {
            var session = new SerializerSession();
            var writer = new Writer();

            serializer.WriteField(writer, session, 0, typeof(T), expected);

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
            var session = new SerializerSession();
            var writer = new Writer();

            serializer.WriteField(writer, session, 0, typeof(SubType), expected);
            
            var reader = new Reader(writer.ToBytes());
            var deserializationContext = new SerializerSession();
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