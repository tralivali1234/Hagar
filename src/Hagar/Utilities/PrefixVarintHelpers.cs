using System.Runtime.CompilerServices;

namespace Hagar.Utilities
{
    public static class PrefixVarIntHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint CountLeadingOnes(uint x) => CountLeadingZeros(~x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint CountSetBits(uint x)
        {
            x -= ((x >> 1) & 0x55555555);
            x = (((x >> 2) & 0x33333333) + (x & 0x33333333));
            x = (((x >> 4) + x) & 0x0f0f0f0f);
            x += (x >> 8);
            x += (x >> 16);
            return x & 0x0000003f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint CountLeadingZeros(uint x)
        {
            x |= (x >> 1);
            x |= (x >> 2);
            x |= (x >> 4);
            x |= (x >> 8);
            x |= (x >> 16);
            return 32 - CountSetBits(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int CountRequiredBytes(uint x)
        {
            var a = x > 0b01111111;
            var b = x > 0b00111111_11111111;
            var c = x > 0b00011111_11111111_11111111;
            var d = x > 0b00001111_11111111_11111111_11111111;
            return 1 + *((byte*)&a) + *((byte*)&b) + *((byte*)&c) + *((byte*)&d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int WriteShuntForFiveByteValues(uint x)
        {
            var d = x > 0b00001111_11111111_11111111_11111111;
            return *((byte*)&d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int ReadShuntForFiveByteValues(byte x)
        {
            var d = (x & 0b11110000) == 0b11110000;
            return *((byte*)&d);
        }
    }
}