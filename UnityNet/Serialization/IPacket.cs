using System;

namespace UnityNet.Serialization
{
    public interface IPacket
    {
        void Receive(ReadOnlySpan<byte> data);
    }
}
