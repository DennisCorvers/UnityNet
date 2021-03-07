using System;
using System.Runtime.CompilerServices;
using UnityNet.Utils;

namespace UnityNet.Tcp
{
    internal unsafe struct PendingPacket
    {
        internal int DataSize;
        internal int Size;
        internal int SizeReceived;
        internal byte* Data;

        internal void Resize(int newSize)
        {
            if (newSize > DataSize)
            {
                if (Data == null)
                {
                    Data = (byte*)Memory.Alloc(newSize);
                }
                else
                {
                    Data = (byte*)Memory.Realloc((IntPtr)Data, Size, newSize);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Free()
        {
            Memory.Free(Data);

            Data = null;
            DataSize = 0;
        }
    }
}
