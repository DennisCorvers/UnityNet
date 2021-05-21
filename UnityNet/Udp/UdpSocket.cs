using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using UnityNet.Serialization;
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
        private AddressFamily m_family = AddressFamily.InterNetwork;

        private bool m_isCleanedUp;
        private bool m_hasSharedBuffer;
#pragma warning restore IDE0032, IDE0044

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

            m_family = family;
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

            m_family = family;
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

            IPAddress[] addresses = null;
            try
            {
                addresses = Dns.GetHostAddresses(hostname);
            }
            catch (SocketException)
            {
                Logger.Error("Unable to resolve hostname " + hostname);
                return SocketStatus.Error;
            }

            foreach (var address in addresses)
            {
                if (address.AddressFamily == m_family || m_family == AddressFamily.Unknown)
                    return Connect(new IPEndPoint(address, port));
            }

            return SocketStatus.Error;
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

            if (endpoint.Address.Equals(IPAddress.Broadcast))
                m_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

            m_socket.Connect(endpoint);
            m_isActive = true;

            return SocketStatus.Done;
        }

        /// <summary>
        /// Binds the <see cref="UdpSocket"/> to a specified port.
        /// </summary>
        /// <param name="port">The port to bind the <see cref="UdpSocket"/> to.</param>
        public void Bind(int port)
        {
            IPEndPoint tempEP;

            if (m_family == AddressFamily.InterNetwork)
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

            if (localEP.AddressFamily != m_family)
                throw new ArgumentException("localEP AddressFamily is not compatible with Sockets AddressFamily.");

            if (m_socket.IsBound)
                ExceptionHelper.ThrowAlreadyBound();

            m_socket.Bind(localEP);
        }


        /// <summary>
        /// Sends data over the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="size">The size of the payload.</param>
        /// <param name="bytesSent">The amount of bytes that have been sent.</param>
        public SocketStatus Send(IntPtr data, int size, out int bytesSent)
        {
            if (data == IntPtr.Zero)
                ExceptionHelper.ThrowNoData();

            bytesSent = 0;
            return InnerSend((void*)data, size, ref bytesSent);
        }

        /// <summary>
        /// Sends data over the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        public SocketStatus Send(byte[] data)
        {
            return Send(data, data.Length, 0, out _);
        }

        /// <summary>
        /// Sends data over the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="bytesSent">The amount of bytes that have been sent.</param>
        public SocketStatus Send(byte[] data, out int bytesSent)
        {
            return Send(data, data.Length, 0, out bytesSent);
        }

        /// <summary>
        /// Sends data over the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="length">The amount of data to send.</param>
        /// <param name="bytesSent">The amount of bytes that have been sent.</param>
        public SocketStatus Send(byte[] data, int length, out int bytesSent)
        {
            return Send(data, length, 0, out bytesSent);
        }

        /// <summary>
        /// Sends data over the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="length">The amount of data to sent.</param>
        /// <param name="offset">The offset at which to start sending.</param>
        /// <param name="bytesSent">The amount of bytes that have been sent.</param>
        public SocketStatus Send(byte[] data, int length, int offset, out int bytesSent)
        {
            return InnerSend(data, length, offset, out bytesSent);
        }

        /// <summary>
        /// Sends a <see cref="RawPacket"/> over the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public SocketStatus Send(ref RawPacket packet)
        {
            if (packet.Data == IntPtr.Zero)
                ExceptionHelper.ThrowNoData();

            return InnerSend((void*)packet.Data, packet.Size, ref packet.SendPosition);
        }

        /// <summary>
        /// Sends a <see cref="NetPacket"/> over the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public SocketStatus Send(ref NetPacket packet)
        {
            if (packet.Data == null)
                ExceptionHelper.ThrowNoData();

            return InnerSend(packet.Data, packet.Size, ref packet.SendPosition);
        }


        /// <summary>
        /// Receives raw data from the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The buffer where the received data is copied to.</param>
        /// <param name="size">The size of the buffer.</param>
        /// <param name="receivedBytes">The amount of copied to the buffer.</param>
        public SocketStatus Receive(IntPtr data, int size, out int receivedBytes, ref IPEndPoint remoteEP)
        {
            receivedBytes = 0;

            if (data == null)
            {
                ExceptionHelper.ThrowNoData();
                return SocketStatus.Error;
            }

            return InnerReceive((void*)data, size, out receivedBytes, ref remoteEP);
        }

        /// <summary>
        /// Receives raw data from the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The buffer where the received data is copied to.</param>
        /// <param name="receivedBytes">The amount of copied to the buffer.</param>
        public SocketStatus Receive(byte[] data, out int receivedBytes, ref IPEndPoint remoteEP)
        {
            return Receive(data, data.Length, 0, out receivedBytes, ref remoteEP);
        }

        /// <summary>
        /// Receives raw data from the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The buffer where the received data is copied to.</param>
        /// <param name="size">The amount of bytes to copy.</param>
        /// <param name="receivedBytes">The amount of copied to the buffer.</param>
        public SocketStatus Receive(byte[] data, int size, int offset, out int receivedBytes, ref IPEndPoint remoteEP)
        {
            if (data == null)
                ExceptionHelper.ThrowArgumentNull(nameof(data));

            if ((uint)(size - offset) > data.Length)
                ExceptionHelper.ThrowArgumentOutOfRange(nameof(data));

            return InnerReceive(data, size, offset, out receivedBytes, ref remoteEP);
        }

        /// <summary>
        /// Copies received data into the supplied NetPacket.
        /// Must be disposed after use.
        /// </summary>
        /// <param name="packet">Packet to copy the data into.</param>
        public SocketStatus Receive(ref NetPacket packet, ref IPEndPoint remoteEP)
        {
            InnerReceive(m_buffer, MaxDatagramSize, 0, out int receivedBytes, ref remoteEP);

            if (receivedBytes > 0)
            {
                fixed (byte* buf = m_buffer)
                {
                    packet.OnReceive(buf, receivedBytes);
                }
            }

            return SocketStatus.Done;
        }

        /// <summary>
        /// Receives a <see cref="RawPacket"/> from the <see cref="UdpSocket"/>.
        /// Must be disposed after use.
        /// </summary>
        /// <param name="packet">Packet that contains unmanaged memory as its data.</param>
        public SocketStatus Receive(out RawPacket packet, ref IPEndPoint remoteEP)
        {
            InnerReceive(m_buffer, MaxDatagramSize, 0, out int receivedBytes, ref remoteEP);

            if (receivedBytes == 0)
            {
                packet = default;
            }
            else
            {
                IntPtr packetDat = Memory.Alloc(receivedBytes);
                Memory.MemCpy(m_buffer, 0, (void*)packetDat, receivedBytes);

                packet = new RawPacket(packetDat, receivedBytes);
            }

            return SocketStatus.Done;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SocketStatus InnerReceive(void* data, int size, out int receivedBytes, ref IPEndPoint remoteEP)
        {
            InnerReceive(m_buffer, size, 0, out receivedBytes, ref remoteEP);
            Memory.MemCpy(m_buffer, 0, data, size);

            return SocketStatus.Done;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SocketStatus InnerReceive(byte[] data, int size, int offset, out int receivedBytes, ref IPEndPoint remoteEP)
        {
            EndPoint endpoint;

            if (m_family == AddressFamily.InterNetwork)
                endpoint = IpEndpointStatics.Any;
            else
                endpoint = IpEndpointStatics.IPv6Any;

            receivedBytes = m_socket.ReceiveFrom(data, offset, size, SocketFlags.None, ref endpoint);
            remoteEP = (IPEndPoint)endpoint;

            return SocketStatus.Done;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SocketStatus InnerSend(byte[] data, int length, int offset, out int bytesSent, IPEndPoint endpoint = null)
        {
            if (m_isActive && endpoint != null)
                ExceptionHelper.ThrowAlreadyActive();

            if (endpoint == null)
                bytesSent = m_socket.Send(data, offset, length, SocketFlags.None);
            else
                bytesSent = m_socket.SendTo(data, offset, length, SocketFlags.None, endpoint);

            return SocketStatus.Done;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SocketStatus InnerSend(void* data, int packetSize, ref int bytesSent, IPEndPoint endpoint = null)
        {
            if ((uint)packetSize > MaxDatagramSize)
                ExceptionHelper.ThrowPacketSizeExceeded();

            // Copy memory to managed buffer.
            Memory.MemCpy(data, m_buffer, 0, packetSize);
            return InnerSend(m_buffer, packetSize, 0, out bytesSent, endpoint);
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

            m_socket = new Socket(m_family, SocketType.Dgram, ProtocolType.Udp)
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
