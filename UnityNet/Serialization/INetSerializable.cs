namespace UnityNet.Serialization
{
    public interface INetSerializable
    {
        void Serialize(NetPacket packet);
    }
}
