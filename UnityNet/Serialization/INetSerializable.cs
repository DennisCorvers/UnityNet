namespace UnityNet.Serialization
{
    public interface INetSerializable
    {
        void Serialize(ref NetPacket packet);
    }
}
