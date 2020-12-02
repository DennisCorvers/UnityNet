using System.Net.Sockets;

namespace UnityNet.Utils
{
    public static class SocketStatusMapper
    {
        public static SocketStatus Map(SocketError error)
        {
            //See https://referencesource.microsoft.com/#System/net/System/Net/Sockets/SocketErrors.cs,cb4675d5a1a2c847

            if (error == SocketError.TryAgain || error == SocketError.InProgress)
                return SocketStatus.NotReady;

            switch (error)
            {
                case SocketError.WouldBlock: return SocketStatus.NotReady;
                case SocketError.ConnectionAborted: return SocketStatus.Disconnected;
                case SocketError.ConnectionReset: return SocketStatus.Disconnected;
                case SocketError.TimedOut: return SocketStatus.Disconnected;
                case SocketError.NetworkReset: return SocketStatus.Disconnected;
                case SocketError.NotConnected: return SocketStatus.Disconnected;
                case SocketError.Disconnecting: return SocketStatus.Disconnected;
                default: return SocketStatus.Error;
            }
        }
    }
}
