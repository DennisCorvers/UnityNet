using System;
using System.Net;
using System.Net.Sockets;
using UnityNet.Utils;

namespace UnityNet.Udp
{
    public unsafe sealed class UdpSocket : IDisposable
    {
        /// <summary>
        /// Maximum size of an UDP datagram.
        /// </summary>
        public const int MaxDatagramSize = 65507;

#pragma warning disable IDE0032, IDE0044
        private Socket m_socket;
        private bool m_isActive;
        private readonly byte[] m_buffer;
        private AddressFamily m_addressFamily = AddressFamily.InterNetwork;

        private bool m_isCleanedUp;
        private bool m_hasSharedBuffer;
#pragma warning restore IDE0032, IDE0044

        /// <summary>
        /// Indicates whether the underlying socket is connected.
        /// </summary>
        public bool Connected
            => m_socket.Connected;
        /// <summary>
        /// Returns false if the <see cref="UdpSocket"/> is not using an internal buffer.
        /// </summary>
        public bool HasSharedBuffer
            => m_hasSharedBuffer;


        /// <summary>
        /// Creates a new <see cref="UdpSocket"/> with an internal buffer.
        /// </summary>
        /// <param name="family">One of the System.Net.Sockets.AddressFamily values that specifies the addressing scheme of the socket.</param>
        public UdpSocket(AddressFamily family = AddressFamily.InterNetwork)
        {
            ValidateAddressFamily(family);

            m_addressFamily = family;
            m_buffer = new byte[MaxDatagramSize];

            CreateNewSocketIfNeeded();
        }

        /// <summary>
        /// Creates a new <see cref="TcpSocket"/> with a user-defined buffer.
        /// </summary>
        /// <param name="buffer">The Send/Receive buffer.</param>
        /// <param name="family">One of the System.Net.Sockets.AddressFamily values that specifies the addressing scheme of the socket.</param>
        public UdpSocket(byte[] buffer, AddressFamily family = AddressFamily.InterNetwork)
        {
            ValidateAddressFamily(family);

            if (m_buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (buffer.Length < MaxDatagramSize)
                throw new ArgumentOutOfRangeException(nameof(buffer), $"Buffer needs to have a size of at least {MaxDatagramSize}");

            m_addressFamily = family;
            m_buffer = buffer;
            m_hasSharedBuffer = true;

            CreateNewSocketIfNeeded();
        }


        /// <summary>
        /// Establishes a connection to the remote host.
        /// </summary>
        /// <param name="address">The Ip Address of the remote host.</param>
        /// <param name="port">The port of the remote host.</param>
        public SocketStatus Connect(UNetIp address, ushort port)
        {
            return Connect(new IPEndPoint(address.ToIPAddress(), port));
        }

        /// <summary>
        /// Establishes a connection to the remote host.
        /// </summary>
        /// <param name="address">The Ip Address of the remote host.</param>
        /// <param name="port">The port of the remote host.</param>
        public SocketStatus Connect(IPAddress address, ushort port)
        {
            return Connect(new IPEndPoint(address, port));
        }

        /// <summary>
        /// Establishes a connection to the remote host.
        /// </summary>
        /// <param name="hostname">The hostname of the remote host.</param>
        /// <param name="port">The port of the remote host.</param>
        public SocketStatus Connect(string hostname, ushort port)
        {
            ThrowIfDisposed();

            ThrowIfActive();

            if (hostname == null)
                throw new ArgumentNullException(nameof(hostname));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Establishes a connection to the remote host.
        /// </summary>
        /// <param name="endpoint">The endpoint of the remote host.</param>
        public SocketStatus Connect(IPEndPoint endpoint)
        {
            ThrowIfDisposed();

            ThrowIfActive();

            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            throw new NotImplementedException();
        }

        /// <summary>
        /// Binds the <see cref="UdpSocket"/> to a specified port.
        /// </summary>
        /// <param name="port">The port to bind the <see cref="UdpSocket"/> to.</param>
        public void Bind(int port)
        {
            IPEndPoint tempEP;

            if (m_addressFamily == AddressFamily.InterNetwork)
                tempEP = new IPEndPoint(IPAddress.Any, port);
            else
                tempEP = new IPEndPoint(IPAddress.IPv6Any, port);

            m_socket.Bind(tempEP);
        }

        /// <summary>
        /// Binds the <see cref="UdpSocket"/> to a specified endpoint.
        /// </summary>
        public void Bind(IPEndPoint localEP)
        {
            if (localEP == null)
                throw new ArgumentNullException(nameof(localEP));

            if (localEP.AddressFamily != m_addressFamily)
                throw new ArgumentException("localEP AddressFamily is not compatible with Sockets AddressFamily.");

            if (m_socket.IsBound)
                ExceptionHelper.ThrowAlreadyBound();

            m_socket.Bind(localEP);
        }

        /// <summary>
        /// Closes the UDP connection.
        /// </summary>
        /// <param name="reuseSocket">TRUE to create a new underlying socket. Resets all previously set socket options.</param>
        public void Close(bool reuseSocket = false)
        {
            if (m_isActive || m_socket.IsBound)
            {
                Dispose();

                if (reuseSocket)
                {
                    m_isCleanedUp = false;
                    CreateNewSocketIfNeeded();
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (m_isCleanedUp)
                return;

            if (disposing)
            {
                if (m_socket != null)
                {
                    m_socket.Close();
                    m_socket = null;
                }

                GC.SuppressFinalize(this);
            }

            // Free unmanaged resources.

            m_isCleanedUp = true;
            m_isActive = false;
        }

        public void Dispose()
            => Dispose(true);

        ~UdpSocket()
            => Dispose(false);

        /// <summary>
        /// Gets or sets a value that specifies the Time to Live (TTL) value of Internet
        /// Protocol (IP) packets sent by the <see cref="UdpSocket"/>.
        /// </summary>
        public short Ttl
        {
            get { return m_socket.Ttl; }
            set { m_socket.Ttl = value; }
        }
        /// <summary>
        /// Gets or sets a <see cref="bool"/> value that specifies whether the <see cref="UdpSocket"/>
        /// allows Internet Protocol (IP) datagrams to be fragmented.
        /// </summary>
        public bool DontFragment
        {
            get { return m_socket.DontFragment; }
            set { m_socket.DontFragment = value; }
        }
        /// <summary>
        /// Gets or sets a <see cref="bool"/> value that specifies whether outgoing multicast
        /// packets are delivered to the sending application.
        /// </summary>
        public bool MulticastLoopback
        {
            get { return m_socket.MulticastLoopback; }
            set { m_socket.MulticastLoopback = value; }
        }
        /// <summary>
        /// Gets or sets a <see cref="bool"/> value that specifies whether the <see cref="UdpSocket"/>
        /// may send or receive broadcast packets.
        /// </summary>
        public bool EnableBroadcast
        {
            get { return m_socket.EnableBroadcast; }
            set { m_socket.EnableBroadcast = value; }
        }
        /// <summary>
        /// Gets or sets a <see cref="bool"/> value that specifies whether the <see cref="UdpSocket"/>
        /// allows only one client to use a port.
        /// </summary>
        public bool ExclusiveAddressUse
        {
            get { return m_socket.ExclusiveAddressUse; }
            set { m_socket.ExclusiveAddressUse = value; }
        }
        /// <summary>
        /// Enables or disables Network Address Translation (NAT) traversal on a <see cref="UdpSocket"/>
        /// instance.
        /// </summary>
        public void AllowNatTraversal(bool allowed)
        {
            m_socket.SetIPProtectionLevel(allowed ? IPProtectionLevel.Unrestricted : IPProtectionLevel.EdgeRestricted);
        }


        private void CreateNewSocketIfNeeded()
        {
            // Don't allow recreation of the socket when this object is disposed.
            if (m_isCleanedUp)
                throw new ObjectDisposedException(GetType().Name);

            if (m_socket != null)
                return;

            m_socket = new Socket(m_addressFamily, SocketType.Dgram, ProtocolType.Udp)
            {
                Blocking = false,
                SendBufferSize = ushort.MaxValue,
                EnableBroadcast = true
            };
        }

        private void ValidateAddressFamily(AddressFamily family)
        {
            if (family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6)
                throw new ArgumentException(("Client can only accept InterNetwork or InterNetworkV6 addresses."), nameof(family));
        }

        private void ThrowIfDisposed()
        {
            if (m_isCleanedUp)
                ThrowObjectDisposedException();

            void ThrowObjectDisposedException() => throw new ObjectDisposedException(GetType().FullName);
        }

        private void ThrowIfActive()
        {
            if (m_isActive)
                ExceptionHelper.ThrowAlreadyActive();
        }
    }
}
