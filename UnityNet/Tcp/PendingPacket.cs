using System;
using System.Runtime.CompilerServices;
using UnityNet.Utils;

namespace UnityNet.Tcp
{
    internal unsafe struct PendingPacket
    {
        internal int Capacity
        { get; private set; }
        internal byte* Data
        { get; private set; }

        internal int Size;
        internal int SizeReceived;

        internal void Resize(int newSize)
        {
            if (newSize > Capacity)
            {
                var alignedSize = MathUtils.GetNextMultipleOf8(newSize);

                if (Data == null)
                {
                    Data = (byte*)Memory.Alloc(alignedSize);
                }
                else
                {
                    Data = (byte*)Memory.Realloc((IntPtr)Data, Capacity, alignedSize);
                }

                Capacity = alignedSize;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear()
        {
            Size = 0;
            SizeReceived = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Dispose()
        {
            if (Data != null)
                Memory.Free(Data);

            Data = null;
            Capacity = 0;
            Size = 0;
            SizeReceived = 0;
        }
    }
}
