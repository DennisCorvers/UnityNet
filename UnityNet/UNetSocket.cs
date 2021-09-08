using System;
using System.Net;
using System.Net.Sockets;

namespace UnityNet
{
    public abstract class UNetSocket : IDisposable
    {
        private Socket m_socket;
        private bool m_isDisposed;

        /// <summary>
        /// Indicates whether the underlying socket is connected.
        /// </summary>
        public bool Connected
            => m_socket.Connected;
        /// <summary>
        /// Gets or sets a value that indicates whether the underlying socket is in blocking mode.
        /// </summary>
        public bool Blocking
        {
            get => m_socket.Blocking;
            set => m_socket.Blocking = value;
        }
        /// <summary>
        /// Gets or sets a <see cref="bool"/> value that specifies whether the underlying socket
        /// allows only one client to use a port.
        /// </summary>
        public bool ExclusiveAddressUse
        {
            get { return m_socket?.ExclusiveAddressUse ?? false; }
            set
            {
                if (m_socket != null)
                {
                    m_socket.ExclusiveAddressUse = value;
                }
            }
        }

        /// <summary>
        /// Get the port to which the socket is remotely connected.
        /// If the socket is not connected, this property returns 0.
        /// </summary>
        public ushort RemotePort
        {
            get
            {
                if (Socket.RemoteEndPoint == null)
                    return 0;
                return (ushort)((IPEndPoint)Socket.RemoteEndPoint).Port;
            }
        }
        /// <summary>
        /// The local port of the socket.
        /// </summary>
        public ushort LocalPort
        {
            get
            {
                if (Socket.LocalEndPoint == null)
                    return 0;
                return (ushort)((IPEndPoint)Socket.LocalEndPoint).Port;
            }
        }
        /// <summary>
        /// Get the the address to which the socket is remotely connected.
        /// </summary>
        public IPAddress RemoteAddress
        {
            get
            {
                if (Socket.RemoteEndPoint == null)
                    return IPAddress.None;
                return ((IPEndPoint)Socket.RemoteEndPoint).Address;
            }
        }
        /// <summary>
        /// Gets the local endpoint.
        /// </summary>
        public EndPoint LocalEndpoint
            => m_socket.LocalEndPoint;

        protected Socket Socket
            => m_socket;

        public UNetSocket(Socket socket)
        {
            m_socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_isDisposed)
                return;

            if (!m_isDisposed)
            {
                if (disposing)
                {
                    if (m_socket != null)
                    {
                        try
                        {
                            if (m_socket.Connected)
                                m_socket.Shutdown(SocketShutdown.Both);
                        }
                        finally
                        {
                            m_socket.Close();
                            m_socket = null;
                        }
                    }

                    GC.SuppressFinalize(this);
                }

                m_isDisposed = true;
            }
        }

        public void Dispose()
            => Dispose(true);

        ~UNetSocket()
            => Dispose(false);
    }
}
