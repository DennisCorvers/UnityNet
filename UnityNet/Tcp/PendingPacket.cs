using System;
using System.Collections.Generic;
using System.Text;

namespace UnityNet.Tcp
{
    internal unsafe struct PendingPacket
    {
        internal uint Size;
        internal int SizeReceived;
        internal byte* Data;
    }
}
