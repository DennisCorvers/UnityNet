using System;

namespace UnityNet.Serialization
{
    public unsafe partial struct NetPacket
    {
        public void Serialize<T>(ref T serializable)
            where T : INetSerializable
        {
            serializable.Serialize(ref this);
        }
    }
}
