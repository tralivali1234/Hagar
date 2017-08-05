using System;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.WireProtocol;

namespace Hagar.Codec
{
    public class FloatCodec : IFieldCodec<float>, IFieldCodec<double>, IFieldCodec<decimal>
    {
        void IFieldCodec<float>.WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType,
            float value)
        {
            writer.WriteFieldHeader(context, fieldId, expectedType, typeof(float), WireType.Fixed32);
            writer.Write(value);
        }

        float IFieldCodec<float>.ReadValue(Reader reader, SerializationContext context, Field field)
        {
            return reader.ReadFloat();
        }

        void IFieldCodec<double>.WriteField(Writer writer, SerializationContext context, uint fieldId,
            Type expectedType, double value)
        {
            writer.WriteFieldHeader(context, fieldId, expectedType, typeof(double), WireType.Fixed64);
            writer.Write(value);
        }

        double IFieldCodec<double>.ReadValue(Reader reader, SerializationContext context, Field field)
        {
            return reader.ReadDouble();
        }

        void IFieldCodec<decimal>.WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType, decimal value)
        {
            writer.WriteFieldHeader(context, fieldId, expectedType, typeof(decimal), WireType.Fixed128);
            writer.Write(value);
        }

        decimal IFieldCodec<decimal>.ReadValue(Reader reader, SerializationContext context, Field field)
        {
            return reader.ReadDecimal();
        }
    }
}