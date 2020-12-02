using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace UnityNet.Utils
{
    internal static class Unsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe int CopyToBuffer(byte[] destination, byte* source, int length)
        {
            fixed(byte* pinnedBuffer = destination)
            {
                length = Math.Min(destination.Length, length);
                Buffer.MemoryCopy(source, pinnedBuffer, length, length);
            }

            return length;
        }
    }
}
