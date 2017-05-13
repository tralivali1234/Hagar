using System;
using System.Security.Cryptography.X509Certificates;
using Hagar;
using Hagar.Orleans.Serialization;
using Hagar.WireProtocol;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var context = new SerializationContext();
            var writer = new Writer();

            var expected = new SubType
            {
                BaseTypeString = "base",
                String = "sub",
                Int = 2,
            };

            writer.WriteFieldHeader(context, 0, typeof(SubType), typeof(SubType), WireType.TagDelimited);
            SerializeMyType(writer, context, expected);
            writer.WriteEndObject();

            Console.WriteLine($"Size: {writer.CurrentOffset} bytes");
            var reader = new Reader(writer.ToBytes());
            var deserializationContext = new SerializationContext();
            var initialHeader = reader.ReadFieldHeader(context);
            Console.WriteLine(initialHeader);
            var actual = new SubType();
            DeserializeMyType(reader, deserializationContext, actual);
        }

        static void SerializeBaseType(Writer writer, SerializationContext context, BaseType obj)
        {
            writer.WriteFieldHeader(context, 0, typeof(string), obj.BaseTypeString.GetType(), WireType.LengthPrefixed);
            writer.WriteFieldHeader(context, 2, typeof(float), typeof(decimal), WireType.Fixed32);
            // write the feild data
        }

        static void SerializeMyType(Writer writer, SerializationContext context, SubType obj)
        {
            SerializeBaseType(writer, context, obj);
            writer.WriteEndBase(); // the base object is complete.
            writer.WriteFieldHeader(context, 0, typeof(string), obj.String.GetType(), WireType.LengthPrefixed);
            writer.WriteFieldHeader(context, 1, typeof(int), obj.Int.GetType(), WireType.VarInt);
            writer.WriteFieldHeader(context, 2, typeof(Guid), Guid.Empty.GetType(), WireType.Fixed128);
        }

        static void DeserializeBaseType(Reader reader, SerializationContext context, BaseType obj)
        {
            while (true)
            {
                var header = reader.ReadFieldHeader(context);
                Console.WriteLine(header);
                if (header.IsEndBaseOrEndObject) break;
                switch (header.FieldId)
                {
                    case 0:
                        var type = reader.ReadType(context, header.SchemaType, typeof(string));
                        Console.WriteLine($"\tReading field {header.FieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");
                        break;
                    default:
                        type = reader.ReadType(context, header.SchemaType, typeof(long));
                        Console.WriteLine($"\tReading UNKNOWN field {header.FieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");
                        break;
                }
            }
        }
    

        static void DeserializeMyType(Reader reader, SerializationContext context, SubType obj)
        {
            DeserializeBaseType(reader, context, obj);
            while (true)
            {
                var header = reader.ReadFieldHeader(context);
                Console.WriteLine(header);
                if (header.IsEndBaseOrEndObject) break;
                Type type;
                switch (header.FieldId)
                {
                    case 0:
                        type = reader.ReadType(context, header.SchemaType, typeof(string));
                        Console.WriteLine($"\tReading field {header.FieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");
                        break;
                    case 1:
                        type = reader.ReadType(context, header.SchemaType, typeof(long));
                        Console.WriteLine($"\tReading field {header.FieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");
                        break;
                    default:
                        type = reader.ReadType(context, header.SchemaType, null);
                        Console.WriteLine($"\tReading UNKNOWN field {header.FieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");
                        break;
                }
            }
        }
    }

    public class BaseType
    {
        public string BaseTypeString { get; set; }
    }

    public class SubType : BaseType
    {
        // 0
        public string String { get; set; }

        // 1
        public int Int { get; set; }
    }
}