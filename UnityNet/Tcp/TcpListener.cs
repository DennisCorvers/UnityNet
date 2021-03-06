using System;
using System.Net;
using System.Net.Sockets;
using UnityNet.Utils;

namespace UnityNet.Tcp
{
    public sealed class TcpListener : IDisposable
    {
        public const int SOMAXCONN = ushort.MaxValue;

#pragma warning disable IDE0032, IDE0044
        private byte[] m_sharedBuffer = null;
        private Socket m_socket;

        private bool m_isDisposed = false;
        private bool m_isActive = false;
        private bool m_shareBuffer;
#pragma warning restore IDE0032, IDE0044

        /// <summary>
        /// Indicates if the listener is listening on a port.
        /// </summary>
        public bool IsActive
            => m_isActive;
        /// <summary>
        /// Gets/Sets the blocking state of the underlying socket.
        /// </summary>
        public bool Blocking
        {
            get { return m_socket.Blocking; }
            set
            {
                if (m_socket.Handle != IntPtr.Zero)
                    m_socket.Blocking = value;
            }
        }
        /// <summary>
        /// Get the port to which the socket is bound locally.
        /// If the socket is not listening to a port, this property returns 0.
        /// </summary>
        public ushort LocalPort
        {
            get
            {
                if (m_socket.LocalEndPoint == null)
                    return 0;
                return (ushort)((IPEndPoint)m_socket.LocalEndPoint).Port;
            }
        }
        public IPAddress BoundAddress
        {
            get
            {
                if (m_socket.LocalEndPoint == null)
                    return IPAddress.None;
                return ((IPEndPoint)m_socket.LocalEndPoint).Address;
            }
        }
        /// <summary>
        /// Indicates if the listener shares a buffer among the connected TcpSockets
        /// </summary>
        public bool IsSharingBuffer
            => m_shareBuffer;

        /// <summary>
        /// Creates a <see cref="TcpListener"/>.
        /// </summary>
        public TcpListener()
            : this(false)
        { }

        /// <summary>
        /// Creates a <see cref="TcpListener"/>.
        /// </summary>
        /// <param name="shareBuffer">If TRUE, all accepted TCPSockets will share the same buffer.</param>
        public TcpListener(bool shareBuffer)
        {
            m_socket = CreateListener();
            m_shareBuffer = shareBuffer;

            if (shareBuffer)
                m_sharedBuffer = new byte[TcpSocket.BUFFER_SIZE];
        }

        ~TcpListener()
        {
            Dispose();
        }

        private Socket CreateListener()
        {
            var sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp)
            {
                DualMode = true,
                Blocking = false,
                NoDelay = true
            };
            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, false);

            return sock;
        }

        public void AllowNatTraversal(bool isallowed)
        {
            if (IsActive)
                Logger.Error(new InvalidOperationException("Tcp listener must be stopped."));

            else
            {
                if (isallowed)
                    m_socket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
                else
                    m_socket.SetIPProtectionLevel(IPProtectionLevel.EdgeRestricted);
            }
        }

        /// <summary>
        /// Start listening for incoming connection attempts
        ///
        /// This function makes the socket start listening on the
        /// specified port, waiting for incoming connection attempts.
        /// </summary>
        /// <param name="port">Port to listen on for incoming connection attempts</param>
        public SocketStatus Listen(ushort port)
        {
            return Listen(new IPEndPoint(IPAddress.IPv6Any, port));
        }

        /// <summary>
        /// Start listening for incoming connection attempts
        ///
        /// This function makes the socket start listening on the
        /// specified port, waiting for incoming connection attempts.
        /// </summary>
        /// <param name="port">Port to listen on for incoming connection attempts</param>
        /// <param name="address">Address of the interface to listen on</param>
        public SocketStatus Listen(IPAddress address, ushort port)
        {
            if ((address == IPAddress.None) || (address == IPAddress.Broadcast))
                throw new InvalidOperationException("IPAddress can't be IPAddress.None or IPAddress.Broadcast.");

            return Listen(new IPEndPoint(address, port));

        }

        /// <summary>
        /// Start listening for incoming connection attempts
        ///
        /// This function makes the socket start listening on the
        /// specified port, waiting for incoming connection attempts.
        /// </summary>
        /// <param name="iPEndPoint">Endpoint of the interface to listen on</param>
        public SocketStatus Listen(IPEndPoint endpoint)
        {
            if (m_isActive)
            {
                ExceptionHelper.ThrowAlreadyBound();
                return SocketStatus.Error;
            }

            if (endpoint == null)
                throw new ArgumentNullException();

            m_socket.Bind(endpoint);
            try
            {
                m_socket.Listen(SOMAXCONN);
            }
            catch (Exception ex)
            {
                Close(true);
                Logger.Error(ex);
                return SocketStatus.Error;
            }

            m_isActive = true;
            return SocketStatus.Done;
        }

        /// <summary>
        /// Accept a new connection
        ///
        /// If the socket is in blocking mode, this function will
        /// not return until a connection is actually received.
        /// </summary>
        /// <param name="socket">Socket that will hold the new connection</param>
        public SocketStatus Accept(out TcpSocket socket)
        {
            if (!m_socket.IsBound)
            {
                ExceptionHelper.ThrowNotListening();
                socket = null;
                return SocketStatus.Error;
            }

            if (!m_socket.Poll(0, SelectMode.SelectRead))
            {
                socket = null;
                return SocketStatus.Error;
            }

            var acceptedSocket = m_socket.Accept();

            if (m_shareBuffer)
                socket = new TcpSocket(acceptedSocket, m_sharedBuffer);
            else
                socket = new TcpSocket(acceptedSocket);

            return SocketStatus.Done;
        }

        /// <summary>
        /// Closes the network connection.
        /// </summary>
        public void Close(bool reuseListener = false)
        {
            // TODO Rewrite this
            if (m_socket.IsBound)
            {
                m_socket.Close();
                m_socket = null;

                if (reuseListener)
                    m_socket = CreateListener();
            }
            m_isActive = false;
        }

        /// <summary>
        /// Disposes the TcpListener.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (!m_isDisposed)
            {
                m_socket.Close();
                m_isDisposed = true;
                m_isActive = false;
            }
        }
    }
}
