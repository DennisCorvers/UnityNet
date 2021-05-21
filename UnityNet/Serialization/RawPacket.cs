using System;
using System.Collections.Generic;
using System.Text;
using UnityNet.Utils;

namespace UnityNet.Serialization
{
    /// <summary>
    /// A struct that contains a netpacket with its length.
    /// </summary>
    public struct RawPacket : IDisposable
    {
        /// <summary>
        /// Used for keeping track of partial packet sends.
        /// </summary>
        internal int SendPosition;

        /// <summary>
        /// The total size of Data in bytes.
        /// </summary>
        public int Size
        {
            get;
            internal set;
        }
        /// <summary>
        /// The data contained in this packet.
        /// </summary>
        public IntPtr Data
        {
            get;
            internal set;
        }

        public RawPacket(IntPtr data, int sizeInBytes)
        {
            Size = sizeInBytes;
            Data = data;
            SendPosition = 0;
        }

        internal unsafe RawPacket(byte[] data, int size)
        {
            Data = Memory.Alloc(size);
            Size = size;
            SendPosition = 0;

            Memory.MemCpy(data, 0, (void*)Data, size);
        }

        public void Dispose()
        {
            Memory.Free(Data);
            Data = IntPtr.Zero;
            Size = 0;
        }
    }
}
