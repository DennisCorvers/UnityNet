using System;
using System.Net;
using System.Runtime.InteropServices;

namespace UnityNet
{
    [StructLayout(LayoutKind.Explicit)]
    public struct UNetIp : IComparable<UNetIp>, IEquatable<UNetIp>
    {
        [FieldOffset(0)]
        readonly byte _octet1;
        [FieldOffset(1)]
        readonly byte _octet2;
        [FieldOffset(2)]
        readonly byte _octet3;
        [FieldOffset(3)]
        readonly byte _octet4;

        [FieldOffset(0)]
        readonly uint _ipValue;

        public UNetIp(byte _1, byte _2, byte _3, byte _4)
        {
            _ipValue = 0;
            _octet1 = _1;
            _octet2 = _2;
            _octet3 = _3;
            _octet4 = _4;
        }

        public IPAddress ToIPAddress()
        {
            return ParseIP(_octet4, _octet3, _octet2, _octet1);
        }

        public static IPAddress ParseIP(byte _1, byte _2, byte _3, byte _4)
        {
            long m_Address = ((_4 << 24 | _3 << 16 | _2 << 8 | _1) & 0x0FFFFFFFF);
            return new IPAddress(m_Address);
        }

        public static IPEndPoint ParseIPEndpoint(byte _1, byte _2, byte _3, byte _4, ushort port)
        {
            return new IPEndPoint(ParseIP(_1, _2, _3, _4), port);
        }

        public bool Equals(UNetIp other)
        {
            return _ipValue == other._ipValue;
        }

        public int CompareTo(UNetIp other)
        {
            return _ipValue.CompareTo(other._ipValue);

        }

        public override int GetHashCode()
        {
            return (int)_ipValue;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UNetIp))
                return false;

            UNetIp ip = (UNetIp)obj;

            return Equals(ip);
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder(15);
            sb.Append(_octet1).Append('.');
            sb.Append(_octet2).Append('.');
            sb.Append(_octet3).Append('.');
            sb.Append(_octet4);

            return sb.ToString();
        }

        public static bool operator ==(UNetIp left, UNetIp right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UNetIp left, UNetIp right)
        {
            return !(left == right);
        }
    }
}
