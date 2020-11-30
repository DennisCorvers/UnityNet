using System;
using System.Net;
using System.Net.Sockets;

namespace UnityNet
{
    public abstract class UNetSocket : IDisposable
    {
        private readonly SocketType m_type;
        private bool m_isBlocking;

        protected Socket Socket
        { get; set; }

        protected IPEndPoint m_endpoint;

        public bool Blocking
        {
            get { return m_isBlocking; }
            set
            {
                if (Socket.Handle != IntPtr.Zero)
                    Socket.Blocking = value;

                m_isBlocking = value;
            }
        }

        public UNetSocket(SocketType type)
        {
            m_type = type;

            if (type == SocketType.TCP)
                Socket = CreateTcpSocket();
        }

        internal UNetSocket(Socket socket)
        {
            if (socket.ProtocolType == ProtocolType.Tcp)
            {
                m_type = SocketType.TCP;
                ConfigureTcpSocket(socket);
            }

            Socket = socket;
        }

        protected static Socket CreateTcpSocket()
        {
            //Will IPv6 always work???
            var socket = new Socket(AddressFamily.InterNetworkV6, System.Net.Sockets.SocketType.Stream, ProtocolType.Tcp);
            ConfigureTcpSocket(socket);
            return socket;
        }

        private static void ConfigureTcpSocket(Socket sock)
        {
            sock.Blocking = true;
            sock.NoDelay = true;
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, false);
            sock.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);
        }

        ~UNetSocket()
        {
            Dispose(false);
        }

        /// <summary>
        /// Close is actually Disconnect, since that allows re-use of the socket.
        /// </summary>
        public virtual void Close()
        {
            m_endpoint = null;
            Socket.Disconnect(true);
        }

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    Socket.Close();

                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
