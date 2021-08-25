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
        private Socket m_socket;

        private int m_maxPacketSize = int.MaxValue;
        private bool m_isActive;
        private bool m_exclusiveAddressUse;

        private bool? m_allowNatTraversal;

        private bool m_isDisposed;
#pragma warning restore IDE0032, IDE0044

        /// <summary>
        /// Defines the maximum packet size the client is allowed to send to this listener.
        /// Client gets disconnected if the packet size exceeds this value.
        /// </summary>
        public int MaximumPacketSize
        {
            get => m_maxPacketSize;
            set => m_maxPacketSize = value < 1 ? throw new ArgumentOutOfRangeException(nameof(value), "MaximumPacketSize must be at least 1.") : value;
        }

        /// <summary>
        /// Indicates if the listener is listening on a port.
        /// </summary>
        public bool IsActive
            => m_isActive;
        /// <summary>
        /// Get the port to which the socket is bound locally.
        /// If the socket is not listening to a port, this property returns 0.
        /// </summary>
        public ushort LocalPort
        {
            get
            {
                if (m_socket == null || m_socket.LocalEndPoint == null)
                    return 0;
                return (ushort)((IPEndPoint)m_socket.LocalEndPoint).Port;
            }
        }
        /// <summary>
        /// Gets or sets a value that indicates whether the <see cref="TcpListener"/> is in blocking mode.
        /// </summary>
        public bool Blocking
        {
            get => m_socket.Blocking;
            set => m_socket.Blocking = value;
        }

        public IPAddress BoundAddress
        {
            get
            {
                if (m_socket == null || m_socket.LocalEndPoint == null)
                    return IPAddress.None;
                return ((IPEndPoint)m_socket.LocalEndPoint).Address;
            }
        }

        /// <summary>
        /// Creates a <see cref="TcpListener"/>.
        /// </summary>
        public TcpListener()
        {
            m_socket = CreateListener();
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
            if (m_isActive)
                throw new InvalidOperationException("Tcp listener must be stopped.");

            if (m_socket != null)
                SetIPProtectionLevel(isallowed);
            else
                m_allowNatTraversal = isallowed;
        }

        public bool ExclusiveAddressUse
        {
            get
            {
                return m_socket != null ? m_socket.ExclusiveAddressUse : m_exclusiveAddressUse;
            }
            set
            {
                if (m_isActive)
                    throw new InvalidOperationException("Tcp listener must be stopped.");

                if (m_socket != null)
                    m_socket.ExclusiveAddressUse = value;

                m_exclusiveAddressUse = value;
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

            CreateNewSocketIfNeeded();

            m_socket.Bind(endpoint);

            try
            {
                m_socket.Listen(SOMAXCONN);
            }
            catch (Exception e)
            {
                Stop();
                Logger.Error(e);
                return SocketStatus.Error;
            }

            m_isActive = true;
            return SocketStatus.Done;
        }

        /// <summary>
        /// Determines if there are pending connection requests.
        /// </summary>
        /// <param name="microSeconds">The timeout in microseconds.</param>
        public bool ConnectionPending(int microSeconds = 0)
        {
            if (!m_isActive)
                ExceptionHelper.ThrowNotListening();

            return m_socket.Poll(microSeconds, SelectMode.SelectRead);
        }

        /// <summary>
        /// Accept a new connection
        /// </summary>
        /// <param name="socket">Socket that will hold the new connection</param>
        public SocketStatus Accept(out TcpSocket socket)
        {
            return Accept(out socket, 0);
        }

        /// <summary>
        /// Accept a new connection.
        /// </summary>
        /// <param name="socket">Socket that will hold the new connection</param>
        /// <param name="microSeconds">The time in microseconds that this function blocks until a connection is available.</param>
        public SocketStatus Accept(out TcpSocket socket, int microSeconds)
        {
            if (!m_isActive)
            {
                ExceptionHelper.ThrowNotListening();
                socket = null;
                return SocketStatus.Error;
            }

            if (!m_socket.Poll(microSeconds, SelectMode.SelectRead))
            {
                socket = null;
                return SocketStatus.NotReady;
            }

            var acceptedSocket = m_socket.Accept();
            socket = new TcpSocket(acceptedSocket, m_maxPacketSize);

            return SocketStatus.Done;
        }

        /// <summary>
        /// Closes the network connection.
        /// </summary>
        public void Stop()
        {
            if (!m_isActive)
                return;

            m_socket.Close();
            m_isActive = false;
            m_socket = null;
        }

        private void SetIPProtectionLevel(bool allowed)
        {
            m_socket.SetIPProtectionLevel(allowed ? IPProtectionLevel.Unrestricted : IPProtectionLevel.EdgeRestricted);
        }

        private void CreateNewSocketIfNeeded()
        {
            // Don't allow recreation of the socket when this object is disposed.
            if (m_isDisposed)
                throw new ObjectDisposedException(GetType().Name);

            if (m_socket != null)
                return;

            m_socket = CreateListener();

            if (m_exclusiveAddressUse)
                m_socket.ExclusiveAddressUse = true;

            if (m_allowNatTraversal != null)
            {
                SetIPProtectionLevel(m_allowNatTraversal.GetValueOrDefault());
                m_allowNatTraversal = null;
            }
        }

        /// <summary>
        /// Disposes the TcpListener.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (!m_isDisposed)
            {
                Stop();
                m_isDisposed = true;
            }
        }
    }
}
