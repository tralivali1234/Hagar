using System.Runtime.CompilerServices;
using Hagar.Buffers;

namespace Hagar.Utilities
{
    public static class VarIntWriterExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVarInt(ref this Writer writer, sbyte value) => writer.WriteVarInt(ZigZagEncode(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVarInt(ref this Writer writer, short value) => writer.WriteVarInt(ZigZagEncode(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteVarInt(ref this Writer writer, int value) => writer.WriteVarInt(ZigZagEncode(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ZigZagEncode(sbyte value)
        {
            return (byte)((value << 1) ^ (value >> 7));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort ZigZagEncode(short value)
        {
            return (ushort)((value << 1) ^ (value >> 15));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ZigZagEncode(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }
    }
}