using System;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.Utilities.Orleans.Serialization;
using Hagar.WireProtocol;

namespace Hagar.Codec
{
    public class FloatCodec : IValueCodec<float>, IValueCodec<double>, IValueCodec<decimal>
    {
        void IValueCodec<float>.WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType,
            float value)
        {
            writer.WriteFieldHeader(context, fieldId, expectedType, typeof(float), WireType.Fixed32);
            writer.Write(value);
        }

        float IValueCodec<float>.ReadValue(Reader reader, SerializationContext context, Field field)
        {
            return reader.ReadFloat();
        }

        void IValueCodec<double>.WriteField(Writer writer, SerializationContext context, uint fieldId,
            Type expectedType, double value)
        {
            writer.WriteFieldHeader(context, fieldId, expectedType, typeof(double), WireType.Fixed64);
            writer.Write(value);
        }

        double IValueCodec<double>.ReadValue(Reader reader, SerializationContext context, Field field)
        {
            return reader.ReadDouble();
        }

        void IValueCodec<decimal>.WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType, decimal value)
        {
            writer.WriteFieldHeader(context, fieldId, expectedType, typeof(decimal), WireType.Fixed128);
            writer.Write(value);
        }

        decimal IValueCodec<decimal>.ReadValue(Reader reader, SerializationContext context, Field field)
        {
            return reader.ReadDecimal();
        }
    }
}