using System.Net;

namespace UnityNet.Utils
{
    internal static class IpEndpointStatics
    {
        internal const int AnyPort = IPEndPoint.MinPort;
        internal static readonly IPEndPoint Any = new IPEndPoint(IPAddress.Any, AnyPort);
        internal static readonly IPEndPoint IPv6Any = new IPEndPoint(IPAddress.IPv6Any, AnyPort);
    }
}
