using System;
using Hagar.Codec;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.Utilities.Orleans.Serialization;
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

            writer.WriteStartObject(context, 0, typeof(SubType), typeof(SubType));
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
            writer.WriteFieldHeader(context, 2, typeof(float), typeof(decimal), WireType.Fixed128);
            // write the field data
        }

        static void SerializeMyType(Writer writer, SerializationContext context, SubType obj)
        {
            SerializeBaseType(writer, context, obj);
            writer.WriteEndBase(); // the base object is complete.
            writer.WriteFieldHeader(context, 0, typeof(string), obj.String.GetType(), WireType.LengthPrefixed);
            writer.WriteFieldHeader(context, 1, typeof(int), obj.Int.GetType(), WireType.VarInt);
            writer.WriteFieldHeader(context, 1025, typeof(Guid), Guid.Empty.GetType(), WireType.Fixed128);
            writer.WriteFieldHeader(context, 1020, typeof(object), typeof(Program), WireType.Reference);
        }

        public interface IHagarSerializer
        {
            void Serialize(Writer writer, SerializationContext context, uint fieldId, object instance, Type expectedType);
            object Deserialize(Reader reader, SerializationContext context, Field field);
        }

        public interface IReferenceSerializer<T>
        {

            void Serialize(Writer writer, SerializationContext context);
        }

        public interface IObjectSerializer<T>
        {
            // Serialize each field defined within T. Do not serialize base fields.
            void Serialize(Writer writer, SerializationContext context, T instance);

            void Deserialize(Reader reader, SerializationContext context, ref T instance);
        }

        public interface IValueSerializer<T>
        {
            void Serialize(Writer writer, SerializationContext context, ref T instance);
            void Deserialize(Reader reader, SerializationContext context, ref T instance);
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
                    {
                        var type = header.FieldType ?? typeof(string);
                        Console.WriteLine(
                            $"\tReading field {header.FieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");
                        break;
                    }
                    default:
                    {
                        var type = header.FieldType;
                        Console.WriteLine(
                            $"\tReading UNKNOWN field {header.FieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");
                        break;
                    }
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
                        type = header.FieldType ?? typeof(string);
                        Console.WriteLine($"\tReading field {header.FieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");
                        break;
                    case 1:
                        type = header.FieldType ?? typeof(long);
                        Console.WriteLine($"\tReading field {header.FieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");
                        break;
                    default:
                        type = header.FieldType;
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