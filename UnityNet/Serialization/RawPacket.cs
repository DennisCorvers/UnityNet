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
        /// Gets a value that indicates if the RawPacket has unmanaged memory allocated and Dispose should be called.
        /// </summary>
        public bool IsAllocated
            => Data != IntPtr.Zero;

        /// <summary>
        /// The total size of Data in bytes.
        /// </summary>
        public int Size
        {
            get;
            private set;
        }
        /// <summary>
        /// The data contained in this packet.
        /// </summary>
        public IntPtr Data
        {
            get;
            private set;
        }

        internal void ReceiveInto(IntPtr data, int dataSize)
        {
            Data = data;
            Size = dataSize;
        }

        public void Dispose()
        {
            Memory.Free(Data);
            Data = IntPtr.Zero;
            Size = 0;
        }
    }
}
