using System;
using Hagar.Codec;
using Hagar.Serializer;
using Hagar.Session;
using Hagar.Utilities;

namespace TestApp
{
    public class BaseTypeSerializer<TStringCodec> : IPartialSerializer<BaseType>
        where TStringCodec : IFieldCodec<string>
    {
        private readonly TStringCodec stringCodec;

        public BaseTypeSerializer(TStringCodec stringCodec)
        {
            this.stringCodec = stringCodec;
        }

        public void Serialize(Writer writer, SerializationContext context, BaseType obj)
        {
            this.stringCodec.WriteField(writer, context, 0, typeof(string), obj.BaseTypeString);
        }

        public void Deserialize(Reader reader, SerializationContext context, BaseType obj)
        {
            uint fieldId = 0;
            while (true)
            {
                var header = reader.ReadFieldHeader(context);
                //Console.WriteLine(header);
                if (header.IsEndBaseOrEndObject) break;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                    {
                        obj.BaseTypeString = this.stringCodec.ReadValue(reader, context, header);
                        /*var type = header.FieldType ?? typeof(string);
                            Console.WriteLine(
                            $"\tReading field {fieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");*/
                        break;
                    }
                    default:
                    {
                        /*var type = header.FieldType;
                        Console.WriteLine(
                            $"\tReading UNKNOWN field {fieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");*/
                        break;
                    }
                }
            }
        }
    }
}