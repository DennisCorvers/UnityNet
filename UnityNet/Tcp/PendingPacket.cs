using System;
using System.Runtime.CompilerServices;
using UnityNet.Utils;

namespace UnityNet.Tcp
{
    internal unsafe struct PendingPacket
    {
        internal int Capacity
        { get; private set; }
        internal int Size;
        internal int SizeReceived;
        internal byte* Data
        { get; private set; }

        internal void Resize(int newSize)
        {
            if (newSize > Capacity)
            {
                if (Data == null)
                {
                    Data = (byte*)Memory.Alloc(newSize);
                }
                else
                {
                    Data = (byte*)Memory.Realloc((IntPtr)Data, Size, newSize);
                }

                Capacity = newSize;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear()
        {
            Size = 0;
            SizeReceived = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Free()
        {
            Memory.Free(Data);

            Data = null;
            Capacity = 0;
            Size = 0;
            SizeReceived = 0;
        }
    }
}
