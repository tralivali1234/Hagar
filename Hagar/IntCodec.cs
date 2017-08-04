using System;
using Hagar.Session;
using Hagar.Utilities.Orleans.Serialization;
using Hagar.WireProtocol;

namespace Hagar
{
    public static class IntCodec
    {
        public static void WriteInt32Field(this Writer writer, SerializationContext context, uint fieldId, Type expectedType, int value)
        {
            if (value > 1 << 20 || -value > 1 << 20)
            {
                writer.WriteFieldHeader(context, fieldId, expectedType, typeof(int), WireType.Fixed32);
                writer.Write(value);
            }
            else
            {
                writer.WriteFieldHeader(context, fieldId, expectedType, typeof(int), WireType.VarInt);
                writer.WriteVarInt(value);
            }
        }

        public static void WriteUInt32Field(this Writer writer, SerializationContext context, uint fieldId, Type expectedType, uint value)
        {
            if (value > 1 << 20)
            {
                writer.WriteFieldHeader(context, fieldId, expectedType, typeof(uint), WireType.Fixed32);
                writer.Write(value);
            }
            else
            {
                writer.WriteFieldHeader(context, fieldId, expectedType, typeof(uint), WireType.VarInt);
                writer.WriteVarInt(value);
            }
        }

        public static void WriteInt64Field(this Writer writer, SerializationContext context, uint fieldId, Type expectedType, long value)
        {
            if (value <= int.MaxValue && value >= int.MinValue)
            {
                if (value > 1 << 20 || -value > 1 << 20)
                {
                    writer.WriteFieldHeader(context, fieldId, expectedType, typeof(long), WireType.Fixed32);
                    writer.Write(value);
                }
                else
                {
                    writer.WriteFieldHeader(context, fieldId, expectedType, typeof(long), WireType.VarInt);
                    writer.WriteVarInt(value);
                }
            }
            else if (value > 1 << 41 || -value > 1 << 41)
            {
                writer.WriteFieldHeader(context, fieldId, expectedType, typeof(long), WireType.Fixed64);
                writer.Write(value);
            }
            else
            {
                writer.WriteFieldHeader(context, fieldId, expectedType, typeof(long), WireType.VarInt);
                writer.WriteVarInt(value);
            }
        }

        public static void WriteUInt64Field(this Writer writer, SerializationContext context, uint fieldId, Type expectedType, ulong value)
        {
            if (value <= int.MaxValue)
            {
                if (value > 1 << 20)
                {
                    writer.WriteFieldHeader(context, fieldId, expectedType, typeof(ulong), WireType.Fixed32);
                    writer.Write(value);
                }
                else
                {
                    writer.WriteFieldHeader(context, fieldId, expectedType, typeof(ulong), WireType.VarInt);
                    writer.WriteVarInt(value);
                }
            }
            else if (value > 1 << 41)
            {
                writer.WriteFieldHeader(context, fieldId, expectedType, typeof(ulong), WireType.Fixed64);
                writer.Write(value);
            }
            else
            {
                writer.WriteFieldHeader(context, fieldId, expectedType, typeof(ulong), WireType.VarInt);
                writer.WriteVarInt(value);
            }
        }
    }
}
