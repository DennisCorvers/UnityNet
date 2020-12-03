using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityNet.Utils;

namespace UnityNet.Unsafe
{
    internal static unsafe class Memory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int CopyToBuffer(byte[] destination, byte* source, int length)
        {
            fixed (byte* pinnedBuffer = destination)
            {
                length = Math.Min(destination.Length, length);
                MemCopy((IntPtr)source, (IntPtr)pinnedBuffer, length);
            }

            return length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IntPtr Malloc(int size)
        {
#if UNITY
            return (IntPtr)UnsafeUtility.Malloc(size, 8, Unity.Collections.Allocator.Persistent);
#else
            return Marshal.AllocHGlobal(size);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IntPtr MallocZeroed(int size)
        {
            var memory = Malloc(size);

            ZeroMemory(memory, size);

            return memory;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Free(IntPtr mem)
        {
#if UNITY
            UnsafeUtility.Free((void*)mem, Unity.Collections.Allocator.Persistent);
#else
            Marshal.FreeHGlobal(mem);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void MemCopy(IntPtr source, IntPtr destination, int size)
        {
#if UNITY
            UnsafeUtility.MemCpy((void*)destination, (void*)source, size);
#else
            Buffer.MemoryCopy((void*)source, (void*)destination, size, size);
#endif
        }

        internal static void ZeroMemory(IntPtr ptr, int size)
        {
#if UNITY
            UnsafeUtility.MemClear((void*)ptr, size);
#else
            int c = size / 8; //longs
            int b = size % 8; //bytes

            for (int i = 0; i < c; i++)
                *((ulong*)ptr + i) = 0;

            for (int i = 0; i < b; i++)
                *((byte*)ptr + i) = 0;
#endif
        }

        internal static IntPtr Resize(IntPtr buffer, int currentSize, int newSize)
        {
            UNetDebug.Assert(newSize > currentSize);

#if UNITY
            var oldBuffer = buffer;
            var newBuffer = MallocZeroed(newSize);

            MemCpy(oldBuffer, newBuffer, currentSize);
            Free(oldBuffer);

            return newBuffer;
#else
            return Marshal.ReAllocHGlobal(buffer, (IntPtr)newSize);
#endif
        }

    }
}
