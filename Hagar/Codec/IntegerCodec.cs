using System;
using Hagar.Session;
using Hagar.Utilities;
using Hagar.Utilities.Orleans.Serialization;
using Hagar.WireProtocol;

namespace Hagar.Codec
{
    public class IntegerCodec : IValueCodec<byte>, IValueCodec<sbyte>, IValueCodec<int>, IValueCodec<uint>,
        IValueCodec<short>, IValueCodec<ushort>, IValueCodec<long>, IValueCodec<ulong>, IValueCodec<char>
    {
        void IValueCodec<char>.WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType,
            char value)
        {
            writer.WriteFieldHeader(context, fieldId, expectedType, typeof(char), WireType.VarInt);
            writer.WriteVarInt(value);
        }

        char IValueCodec<char>.ReadValue(Reader reader, SerializationContext context, Field field)
        {
            return (char)reader.ReadUInt8(field.WireType);
        }

        void IValueCodec<byte>.WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType,
            byte value)
        {
            writer.WriteFieldHeader(context, fieldId, expectedType, typeof(byte), WireType.VarInt);
            writer.WriteVarInt(value);
        }

        byte IValueCodec<byte>.ReadValue(Reader reader, SerializationContext context, Field field)
        {
            return reader.ReadUInt8(field.WireType);
        }

        void IValueCodec<sbyte>.WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType,
            sbyte value)
        {
            writer.WriteFieldHeader(context, fieldId, expectedType, typeof(sbyte), WireType.VarInt);
            writer.WriteVarInt(value);
        }

        sbyte IValueCodec<sbyte>.ReadValue(Reader reader, SerializationContext context, Field field)
        {
            return reader.ReadInt8(field.WireType);
        }

        void IValueCodec<short>.WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType,
            short value)
        {
            writer.WriteFieldHeader(context, fieldId, expectedType, typeof(short), WireType.VarInt);
            writer.WriteVarInt(value);
        }

        ushort IValueCodec<ushort>.ReadValue(Reader reader, SerializationContext context, Field field)
        {
            return reader.ReadUInt16(field.WireType);
        }

        void IValueCodec<ushort>.WriteField(Writer writer, SerializationContext context, uint fieldId,
            Type expectedType, ushort value)
        {
            writer.WriteFieldHeader(context, fieldId, expectedType, typeof(ushort), WireType.VarInt);
            writer.WriteVarInt(value);
        }

        short IValueCodec<short>.ReadValue(Reader reader, SerializationContext context, Field field)
        {
            return reader.ReadInt16(field.WireType);
        }

        void IValueCodec<uint>.WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType,
            uint value)
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

        uint IValueCodec<uint>.ReadValue(Reader reader, SerializationContext context, Field field)
        {
            return reader.ReadUInt32(field.WireType);
        }

        void IValueCodec<int>.WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType,
            int value)
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

        int IValueCodec<int>.ReadValue(Reader reader, SerializationContext context, Field field)
        {
            return reader.ReadInt32(field.WireType);
        }

        void IValueCodec<long>.WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType,
            long value)
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

        long IValueCodec<long>.ReadValue(Reader reader, SerializationContext context, Field field)
        {
            return reader.ReadInt64(field.WireType);
        }

        void IValueCodec<ulong>.WriteField(Writer writer, SerializationContext context, uint fieldId, Type expectedType,
            ulong value)
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

        ulong IValueCodec<ulong>.ReadValue(Reader reader, SerializationContext context, Field field)
        {
            return reader.ReadUInt64(field.WireType);
        }
    }
}