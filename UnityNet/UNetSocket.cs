using System;
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
