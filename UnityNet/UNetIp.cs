using System.Net;

namespace UnityNet
{
    public struct UNetIp
    {
        public byte Octet1
        { get; }
        public byte Octet2
        { get; }
        public byte Octet3
        { get; }
        public byte Octet4
        { get; }

        public UNetIp(byte _1, byte _2, byte _3, byte _4)
        {
            Octet1 = _1;
            Octet2 = _2;
            Octet3 = _3;
            Octet4 = _4;
        }

        public IPAddress ToIPAddress()
        {
            long m_Address = ((Octet4 << 24 | Octet3 << 16 | Octet2 << 8 | Octet1) & 0x0FFFFFFFF);
            return new IPAddress(m_Address);
        }

        public static IPAddress ParseIP(byte _1, byte _2, byte _3, byte _4)
        {
            long m_Address = ((_4 << 24 | _3 << 16 | _2 << 8 | _1) & 0x0FFFFFFFF);
            return new IPAddress(m_Address);
        }
    }
}
