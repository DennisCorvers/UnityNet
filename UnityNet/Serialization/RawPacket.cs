using System;
using System.Collections.Generic;
using System.Text;

namespace UnityNet.Serialization
{
    /// <summary>
    /// A struct that contains a netpacket with its length.
    /// </summary>
    public struct RawPacket
    {
        public int Size
        { get; internal set; }
        public IntPtr Data
        { get; internal set; }
    }
}
