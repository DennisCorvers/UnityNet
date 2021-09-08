using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using UnityNet.Serialization;
using UnityNet.Utils;

namespace UnityNet.Udp
{
    public sealed class UdpSocket : UNetSocket
    {
        /// <summary>
        /// Maximum size of an UDP datagram.
        /// </summary>
        public const int MaxDatagramSize = 65507;

#pragma warning disable IDE0032, IDE0044
        private byte[] m_buffer;
        private bool m_isActive;
        private AddressFamily m_family = AddressFamily.InterNetwork;

        private bool m_isCleanedUp;
#pragma warning restore IDE0032, IDE0044

        /// <summary>
        /// Creates a new <see cref="UdpSocket"/> with a user-defined buffer.
        /// </summary>
        /// <param name="family">One of the System.Net.Sockets.AddressFamily values that specifies the addressing scheme of the socket.</param>
        public UdpSocket(AddressFamily family = AddressFamily.InterNetwork)
            : base(CreateSocket(family))
        {
            ValidateAddressFamily(family);

            m_buffer = new byte[MaxDatagramSize];
            m_family = family;
        }

        /// <summary>
        /// Gets the amount of data that has been received from the network and is available to be read.
        /// </summary>
        public int Available()
            => Socket.Available;

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
        /// <param name="endpoint">The endpoint of the remote host.</param>
        public SocketStatus Connect(IPEndPoint endpoint)
        {
            ThrowIfDisposed();

            ThrowIfActive();

            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            if (endpoint.Address.Equals(IPAddress.Broadcast))
                Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);

            Socket.Connect(endpoint);
            m_isActive = true;

            return SocketStatus.Done;
        }

        /// <summary>
        /// Establishes a connection to the remote host.
        /// </summary>
        /// <param name="hostname">The hostname of the remote host.</param>
        /// <param name="port">The port of the remote host.</param>
        public ValueTask<SocketStatus> ConnectAsync(string hostname, ushort port)
        {
            ThrowIfDisposed();

            ThrowIfActive();

            if (hostname == null)
                throw new ArgumentNullException(nameof(hostname));

            return Core();

            async ValueTask<SocketStatus> Core()
            {
                try
                {
                    await Socket.ConnectAsync(hostname, port).ConfigureAwait(false);
                }
                catch
                {
                    return SocketStatus.Error;
                }

                if (Socket.Connected)
                {
                    m_family = Socket.RemoteEndPoint.AddressFamily;
                    return SocketStatus.Done;
                }

                return SocketStatus.Error;
            }
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

            Socket.Bind(tempEP);
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

            if (Socket.IsBound)
                ExceptionHelper.ThrowAlreadyBound();

            Socket.Bind(localEP);
        }

        /// <summary>
        /// Sends data over the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="size">The size of the payload.</param>
        /// <param name="bytesSent">The amount of bytes that have been sent.</param>
        /// <param name="remoteEP">An System.Net.IPEndPoint that represents the host and port to which to send the datagram.</param>
        public SocketStatus Send(IntPtr data, int size, out int bytesSent, IPEndPoint remoteEP = null)
        {
            if (data == IntPtr.Zero)
                ExceptionHelper.ThrowNoData();

            ReadOnlySpan<byte> sData;

            unsafe
            {
                sData = new ReadOnlySpan<byte>(data.ToPointer(), size);
            }

            return InnerSend(sData, out bytesSent, remoteEP);
        }

        /// <summary>
        /// Sends data over the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="remoteEP">An System.Net.IPEndPoint that represents the host and port to which to send the datagram.</param>
        public SocketStatus Send(byte[] data, IPEndPoint remoteEP = null)
            => Send(data, data.Length, 0, out _, remoteEP);

        /// <summary>
        /// Sends data over the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="bytesSent">The amount of bytes that have been sent.</param>
        /// <param name="remoteEP">An System.Net.IPEndPoint that represents the host and port to which to send the datagram.</param>
        public SocketStatus Send(byte[] data, out int bytesSent, IPEndPoint remoteEP = null)
            => Send(data, data.Length, 0, out bytesSent, remoteEP);

        /// <summary>
        /// Sends data over the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="length">The amount of data to send.</param>
        /// <param name="bytesSent">The amount of bytes that have been sent.</param>
        /// <param name="remoteEP">An System.Net.IPEndPoint that represents the host and port to which to send the datagram.</param>
        public SocketStatus Send(byte[] data, int length, out int bytesSent, IPEndPoint remoteEP = null)
            => Send(data, length, 0, out bytesSent, remoteEP);

        /// <summary>
        /// Sends data over the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="length">The amount of data to sent.</param>
        /// <param name="offset">The offset at which to start sending.</param>
        /// <param name="bytesSent">The amount of bytes that have been sent.</param>
        /// <param name="endpoint">An System.Net.IPEndPoint that represents the host and port to which to send the datagram.</param>
        public SocketStatus Send(byte[] data, int length, int offset, out int bytesSent, IPEndPoint endpoint = null)
        {
            ThrowIfDisposed();

            if (length > MaxDatagramSize)
                ExceptionHelper.ThrowPacketSizeExceeded();

            if ((uint)(length - offset) > data.Length)
                ExceptionHelper.ThrowArgumentOutOfRange(nameof(data));

            if (endpoint == null)
            {
                bytesSent = Socket.Send(data, offset, length, SocketFlags.None, out SocketError errorCode);
                return SocketStatus.Done;
            }

            if (m_isActive)
            {
                ExceptionHelper.ThrowAlreadyConnected();
            }

            bytesSent = Socket.SendTo(data, offset, length, SocketFlags.None, endpoint);
            return SocketStatus.Done;
        }

        /// <summary>
        /// Sends a <see cref="NetPacket"/> over the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        /// <param name="remoteEP">An System.Net.IPEndPoint that represents the host and port to which to send the datagram.</param>
        public unsafe SocketStatus Send(NetPacket packet, IPEndPoint remoteEP = null)
        {
            if (packet.Data == null)
                ExceptionHelper.ThrowNoData();

            return InnerSend(packet.Buffer, out _, remoteEP);
        }


        /// <summary>
        /// Receives raw data from the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The buffer where the received data is copied to.</param>
        /// <param name="size">The size of the buffer.</param>
        /// <param name="receivedBytes">The amount of copied to the buffer.</param>
        public SocketStatus Receive(IntPtr data, int size, out int receivedBytes, ref IPEndPoint remoteEP)
        {
            if (data == null)
                ExceptionHelper.ThrowNoData();

            InnerReceive(m_buffer, MaxDatagramSize, 0, out receivedBytes, ref remoteEP);

            unsafe
            {
                var sBuf = new ReadOnlySpan<byte>(m_buffer);
                var sData = new Span<byte>(data.ToPointer(), size);

                sBuf.CopyTo(sData);
            }

            return SocketStatus.Done;
        }

        /// <summary>
        /// Receives raw data from the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The buffer where the received data is copied to.</param>
        /// <param name="receivedBytes">The amount of copied to the buffer.</param>
        public SocketStatus Receive(byte[] data, out int receivedBytes, ref IPEndPoint remoteEP)
            => Receive(data, data.Length, 0, out receivedBytes, ref remoteEP);

        /// <summary>
        /// Receives raw data from the <see cref="UdpSocket"/>.
        /// </summary>
        /// <param name="data">The buffer where the received data is copied to.</param>
        /// <param name="size">The amount of bytes to copy.</param>
        /// <param name="offset">The offset at which to start receiving.</param>
        /// <param name="receivedBytes">The amount of copied to the buffer.</param>
        public SocketStatus Receive(byte[] data, int size, int offset, out int receivedBytes, ref IPEndPoint remoteEP)
        {
            if ((uint)(size - offset) > data.Length)
                ExceptionHelper.ThrowArgumentOutOfRange(nameof(data));

            return InnerReceive(data, size, offset, out receivedBytes, ref remoteEP);
        }

        /// <summary>
        /// Copies received data into the supplied NetPacket.
        /// Must be disposed after use.
        /// </summary>
        /// <param name="packet">Packet to copy the data into.</param>
        public SocketStatus Receive(NetPacket packet, ref IPEndPoint remoteEP)
        {
            InnerReceive(m_buffer, MaxDatagramSize, 0, out int receivedBytes, ref remoteEP);

            if (receivedBytes > 0)
                packet.OnReceive(new ReadOnlySpan<byte>(m_buffer, 0, receivedBytes));

            return SocketStatus.Done;
        }

        /// <summary>
        /// Receives a <see cref="RawPacket"/> from the <see cref="UdpSocket"/>.
        /// Must be disposed after use.
        /// </summary>
        /// <param name="packet">Packet that contains unmanaged memory as its data.</param>
        [Obsolete("No longer works.", true)]
        public SocketStatus Receive(ref RawPacket packet, ref IPEndPoint remoteEP)
        {
            if (packet.Data != IntPtr.Zero)
                ThrowNonEmptyBuffer();

            InnerReceive(m_buffer, MaxDatagramSize, 0, out int receivedBytes, ref remoteEP);

            if (receivedBytes == 0)
            {
                packet = default;
            }
            else
            {
                IntPtr packetDat = Memory.Alloc(receivedBytes);
                //Memory.MemCpy(m_buffer, 0, (void*)packetDat, receivedBytes);

                packet.ReceiveInto(packetDat, receivedBytes);
            }

            return SocketStatus.Done;

            void ThrowNonEmptyBuffer()
                => throw new InvalidOperationException("Packet must be empty.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe SocketStatus InnerSend(ReadOnlySpan<byte> data, out int bytesSent, IPEndPoint endPoint = null)
        {
            ThrowIfDisposed();

            if (data.Length > MaxDatagramSize)
                ExceptionHelper.ThrowPacketSizeExceeded();

            if (endPoint == null)
            {
                bytesSent = Socket.Send((data), SocketFlags.None, out _);
                return SocketStatus.Done;
            }
            if (m_isActive)
            {
                ExceptionHelper.ThrowAlreadyConnected();
            }

            bytesSent = InnerSendSlow(data, endPoint);
            return SocketStatus.Done;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe int InnerSendSlow(ReadOnlySpan<byte> data, IPEndPoint endpoint)
        {
            // Workaround for missing Send call with ReadOnlySpan
            var sBuf = new Span<byte>(m_buffer);
            data.CopyTo(sBuf);

            return Socket.SendTo(m_buffer, 0, data.Length, SocketFlags.None, endpoint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SocketStatus InnerReceive(byte[] data, int size, int offset, out int receivedBytes, ref IPEndPoint remoteEP)
        {
            EndPoint endpoint;

            if (m_family == AddressFamily.InterNetwork)
                endpoint = IpEndpointStatics.Any;
            else
                endpoint = IpEndpointStatics.IPv6Any;

            receivedBytes = Socket.ReceiveFrom(data, offset, size, SocketFlags.None, ref endpoint);
            remoteEP = (IPEndPoint)endpoint;

            return SocketStatus.Done;
        }

        /// <summary>
        /// Closes the UDP connection.
        /// </summary>
        public void Close()
        {
            if (m_isActive || Socket.IsBound)
            {
                Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (m_isCleanedUp)
                return;

            m_isCleanedUp = true;
            m_isActive = false;
        }

        /// <summary>
        /// Gets or sets a value that specifies the Time to Live (TTL) value of Internet
        /// Protocol (IP) packets sent by the <see cref="UdpSocket"/>.
        /// </summary>
        public short Ttl
        {
            get { return Socket.Ttl; }
            set { Socket.Ttl = value; }
        }
        /// <summary>
        /// Gets or sets a <see cref="bool"/> value that specifies whether the <see cref="UdpSocket"/>
        /// allows Internet Protocol (IP) datagrams to be fragmented.
        /// </summary>
        public bool DontFragment
        {
            get { return Socket.DontFragment; }
            set { Socket.DontFragment = value; }
        }
        /// <summary>
        /// Gets or sets a <see cref="bool"/> value that specifies whether outgoing multicast
        /// packets are delivered to the sending application.
        /// </summary>
        public bool MulticastLoopback
        {
            get { return Socket.MulticastLoopback; }
            set { Socket.MulticastLoopback = value; }
        }
        /// <summary>
        /// Gets or sets a <see cref="bool"/> value that specifies whether the <see cref="UdpSocket"/>
        /// may send or receive broadcast packets.
        /// </summary>
        public bool EnableBroadcast
        {
            get { return Socket.EnableBroadcast; }
            set { Socket.EnableBroadcast = value; }
        }
        /// <summary>
        /// Enables or disables Network Address Translation (NAT) traversal on a <see cref="UdpSocket"/>
        /// instance.
        /// </summary>
        public void AllowNatTraversal(bool allowed)
        {
            Socket.SetIPProtectionLevel(allowed ? IPProtectionLevel.Unrestricted : IPProtectionLevel.EdgeRestricted);
        }


        private static Socket CreateSocket(AddressFamily family)
        {
            return new Socket(family, SocketType.Dgram, ProtocolType.Udp)
            {
                Blocking = false,
                SendBufferSize = ushort.MaxValue,
                EnableBroadcast = true
            };
        }

        private static void ValidateAddressFamily(AddressFamily family)
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
