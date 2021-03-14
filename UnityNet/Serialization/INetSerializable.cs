namespace UnityNet.Serialization
{
    interface INetSerializable
    {
        void Serialize(ref NetPacket packet);
    }
}
