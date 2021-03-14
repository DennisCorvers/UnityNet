using System.Runtime.CompilerServices;

namespace UnityNet.Compression
{
    internal static class ZigZag
    {
        private const long Int64Msb = ((long)1) << 63;
        private const int Int32Msb = 1 << 31;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Zig(int value)
        {
            return (uint)((value << 1) ^ (value >> 31));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Zig(long value)
        {
            return (ulong)((value << 1) ^ (value >> 63));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Zag(uint ziggedValue)
        {
            int value = (int)ziggedValue;
            return (-(value & 0x01)) ^ ((value >> 1) & ~Int32Msb);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Zag(ulong ziggedValue)
        {
            long value = (long)ziggedValue;
            return (-(value & 0x01L)) ^ ((value >> 1) & ~Int64Msb);
        }
    }
}
