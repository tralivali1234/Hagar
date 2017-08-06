using System;
using Hagar.Codec;
using Hagar.Serializer;
using Hagar.Session;
using Hagar.Utilities;

namespace TestApp
{
    public class SubTypeSerializer<TBaseSerializer, TStringCodec, TIntCodec> : IPartialSerializer<SubType>
        where TBaseSerializer : IPartialSerializer<BaseType>
        where TStringCodec : IFieldCodec<string>
        where TIntCodec : IFieldCodec<int>
    {
        private readonly TBaseSerializer baseTypeSerializer;
        private readonly TStringCodec stringCodec;
        private readonly TIntCodec intCodec;
        private readonly IFieldCodec<object> objectCodec;

        public SubTypeSerializer(TBaseSerializer baseTypeSerializer, TStringCodec stringCodec, TIntCodec intCodec, IFieldCodec<object> objectCodec)
        {
            this.baseTypeSerializer = baseTypeSerializer;
            this.stringCodec = stringCodec;
            this.intCodec = intCodec;
            this.objectCodec = objectCodec;
        }

        public void Serialize(Writer writer, SerializerSession context, SubType obj)
        {
            this.baseTypeSerializer.Serialize(writer, context, obj);
            writer.WriteEndBase(); // the base object is complete.
            this.stringCodec.WriteField(writer, context, 0, typeof(string), obj.String);
            this.intCodec.WriteField(writer, context, 1, typeof(int), obj.Int);
            this.objectCodec.WriteField(writer, context, 1, typeof(object), obj.Ref);
            this.intCodec.WriteField(writer, context, 1, typeof(int), obj.Int);
            this.intCodec.WriteField(writer, context, 409, typeof(int), obj.Int);
            /*writer.WriteFieldHeader(context, 1025, typeof(Guid), Guid.Empty.GetType(), WireType.Fixed128);
            writer.WriteFieldHeader(context, 1020, typeof(object), typeof(Program), WireType.Reference);*/
        }

        public void Deserialize(Reader reader, SerializerSession context, SubType obj)
        {
            uint fieldId = 0;
            this.baseTypeSerializer.Deserialize(reader, context, obj);
            while (true)
            {
                var header = reader.ReadFieldHeader(context);
                //Console.WriteLine(header);
                if (header.IsEndBaseOrEndObject) break;
                Type type;
                fieldId += header.FieldIdDelta;
                switch (fieldId)
                {
                    case 0:
                        obj.String = this.stringCodec.ReadValue(reader, context, header);
                        /*type = header.FieldType ?? typeof(string);
                        Console.WriteLine($"\tReading field {fieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");*/
                        break;
                    case 1:
                        obj.Int = this.intCodec.ReadValue(reader, context, header);
                        break;
                    case 2:
                        obj.Ref = this.objectCodec.ReadValue(reader, context, header);
                        /*type = header.FieldType ?? typeof(long);
                        Console.WriteLine($"\tReading field {fieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");*/
                        break;
                    /*case 2:
                        obj.Ref = this.refSerializer.ReadValue(reader, context, header);
                        /*type = header.FieldType ?? typeof(long);
                        Console.WriteLine($"\tReading field {fieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");#1#
                        break;*/
                    default:
                        reader.ConsumeUnknownField(context, header);
                        /*type = header.FieldType;
                        Console.WriteLine(
                            $"\tReading UNKNOWN field {fieldId} with type = {type?.ToString() ?? "UNKNOWN"} and wireType = {header.WireType}");*/
                        break;
                }
            }
        }
    }
}