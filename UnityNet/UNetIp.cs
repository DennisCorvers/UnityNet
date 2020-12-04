using System.Net;

namespace UnityNet
{
    public static class UNetIp
    {
        public static IPAddress ParseIP(byte _1, byte _2, byte _3, byte _4)
        {
            long m_Address = ((_4 << 24 | _3 << 16 | _2 << 8 | _1) & 0x0FFFFFFFF);
            return new IPAddress(m_Address);
        }
    }
}
