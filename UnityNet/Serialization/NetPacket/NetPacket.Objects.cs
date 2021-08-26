using System;

namespace UnityNet.Serialization
{
    public unsafe partial class NetPacket
    {
        public void Serialize<T>(ref T serializable)
            where T : INetSerializable
        {
            serializable.Serialize(this);
        }
    }
}
