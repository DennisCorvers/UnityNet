using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityNet.Serialization;
using UnityNet.Utils;

namespace UnityNet.Tcp
{
    public sealed class TcpSocket : UNetSocket
    {
        private const int HeaderSize = sizeof(int);

#pragma warning disable IDE0032, IDE0044
        private PendingPacket m_pendingPacket;
        private AddressFamily m_family;

        private bool m_isActive;
        private bool m_isClearedUp;

        private int m_maxPacketSize = int.MaxValue;
#pragma warning restore IDE0032, IDE0044

        /// <summary>
        /// Creates a new <see cref="TcpSocket"/>.
        /// </summary>
        public TcpSocket()
            : this(AddressFamily.Unknown)
        { }

        /// <summary>
        /// Creates a new <see cref="TcpSocket"/>.
        /// </summary>
        /// <param name="family">The AddressFamily of the IP.</param>
        public TcpSocket(AddressFamily family)
            : base(CreateSocket(ref family))
        {
            m_family = family;
        }

        /// <summary>
        /// Creates a new <see cref="TcpSocket"/> from an accepted Socket.
        /// </summary>
        internal TcpSocket(Socket socket, int maxPacketSize)
            : base(ConfigureSocket(socket))
        {
            m_isActive = true;
            m_maxPacketSize = maxPacketSize;
        }


        /// <summary>
        /// Starts connecting to the remote host.
        /// </summary>
        /// <param name="address">The Ip Address of the remote host.</param>
        /// <param name="port">The port of the remote host.</param>
        public Task<SocketStatus> ConnectAsync(UNetIp address, ushort port)
        {
            return ConnectAsync(new IPEndPoint(address.ToIPAddress(), port));
        }

        /// <summary>
        /// Starts connecting to the remote host.
        /// </summary>
        /// <param name="address">The Ip Address of the remote host.</param>
        /// <param name="port">The port of the remote host.</param>
        public Task<SocketStatus> ConnectAsync(IPAddress address, ushort port)
        {
            return ConnectAsync(new IPEndPoint(address, port));
        }

        /// <summary>
        /// Starts connecting to the remote host.
        /// </summary>
        /// <param name="hostname">The hostname of the remote host.</param>
        /// <param name="port">The port of the remote host.</param>
        public async Task<SocketStatus> ConnectAsync(string hostname, ushort port)
        {
            ThrowIfDisposed();

            ThrowIfActive();

            try
            {
                await Socket.ConnectAsync(hostname, port);
                m_family = Socket.AddressFamily;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SocketStatus.Error;
            }

            if (Socket.Connected)
            {
                m_isActive = true;
                return SocketStatus.Done;
            }

            return SocketStatus.Disconnected;
        }

        /// <summary>
        /// Starts connecting to the remote host.
        /// </summary>
        /// <param name="endpoint">The endpoint of the remote host.</param>
        public async Task<SocketStatus> ConnectAsync(IPEndPoint endpoint)
        {
            ThrowIfDisposed();

            ThrowIfActive();

            try
            {
                await Socket.ConnectAsync(endpoint);
                m_family = Socket.AddressFamily;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return SocketStatus.Error;
            }

            if (Socket.Connected)
            {
                m_isActive = true;
                return SocketStatus.Done;
            }

            return SocketStatus.Disconnected;
        }


        /// <summary>
        /// Sends data over the <see cref="TcpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="size">The size of the payload.</param>
        /// <param name="bytesSent">The amount of bytes that have been sent.</param>
        public SocketStatus Send(IntPtr data, int size, out int bytesSent)
        {
            bytesSent = 0;

            if (data == IntPtr.Zero || size == 0)
            {
                ExceptionHelper.ThrowNoData();
                return SocketStatus.Error;
            }

            return Send(data.ToReadOnlySpan<byte>(size), out bytesSent);
        }

        /// <summary>
        /// Sends data over the <see cref="TcpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        public SocketStatus Send(byte[] data)
        {
            return Send(data, data.Length, 0, out _);
        }

        /// <summary>
        /// Sends data over the <see cref="TcpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="bytesSent">The amount of bytes that have been sent.</param>
        public SocketStatus Send(byte[] data, out int bytesSent)
        {
            return Send(data, data.Length, 0, out bytesSent);
        }

        /// <summary>
        /// Sends data over the <see cref="TcpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="length">The amount of data to send.</param>
        /// <param name="bytesSent">The amount of bytes that have been sent.</param>
        public SocketStatus Send(byte[] data, int length, out int bytesSent)
        {
            return Send(data, length, 0, out bytesSent);
        }

        /// <summary>
        /// Sends data over the <see cref="TcpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="length">The amount of data to sent.</param>
        /// <param name="offset">The offset at which to start sending.</param>
        /// <param name="bytesSent">The amount of bytes that have been sent.</param>
        public SocketStatus Send(byte[] data, int length, int offset, out int bytesSent)
        {
            if (data == null)
                ExceptionHelper.ThrowNoData();

            if ((uint)(length - offset) > data.Length)
                ExceptionHelper.ThrowArgumentOutOfRange(nameof(data));

            return Send(new ReadOnlySpan<byte>(data, offset, length), out bytesSent);
        }

        /// <summary>
        /// Sends a <see cref="NetPacket"/> over the TcpSocket.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public unsafe SocketStatus Send(NetPacket packet)
        {
            if (packet.Data == null)
                ExceptionHelper.ThrowNoData();

            int bytesSent = packet.SendPosition;
            int packetSize = packet.Size;

            // Send packet header
            if (bytesSent < HeaderSize)
            {
                byte* sendPosPtr = (byte*)&packetSize + bytesSent;
                var status = Send(new ReadOnlySpan<byte>(sendPosPtr, HeaderSize - bytesSent), out bytesSent);
                packet.SendPosition = bytesSent;

                if (status != SocketStatus.Done)
                    return status;
            }

            // Send packet body.
            {
                int sendOffset = bytesSent - HeaderSize;

                byte* data = ((byte*)packet.Data) + sendOffset;
                var status = Send(new ReadOnlySpan<byte>(data, packetSize - sendOffset), out int chunk);

                if (status != SocketStatus.Done)
                {
                    packet.SendPosition += chunk;
                    return status;
                }

                packet.SendPosition = 0;
                return SocketStatus.Done;
            }
        }


        /// <summary>
        /// Receives raw data from the <see cref="TcpSocket"/>.
        /// </summary>
        /// <param name="data">The buffer where the received data is copied to.</param>
        /// <param name="size">The size of the buffer.</param>
        /// <param name="receivedBytes">The amount of copied to the buffer.</param>
        public SocketStatus Receive(IntPtr data, int size, out int receivedBytes)
        {
            receivedBytes = 0;

            if (data == null)
            {
                ExceptionHelper.ThrowArgumentNull(nameof(data));
                return SocketStatus.Error;
            }

            return Receive(data.ToSpan<byte>(size), out receivedBytes);
        }

        /// <summary>
        /// Receives raw data from the <see cref="TcpSocket"/>.
        /// </summary>
        /// <param name="data">The buffer where the received data is copied to.</param>
        /// <param name="receivedBytes">The amount of copied to the buffer.</param>
        public SocketStatus Receive(byte[] data, out int receivedBytes)
        {
            return Receive(data, data.Length, 0, out receivedBytes);
        }

        /// <summary>
        /// Receives raw data from the <see cref="TcpSocket"/>.
        /// </summary>
        /// <param name="data">The buffer where the received data is copied to.</param>
        /// <param name="size">The amount of bytes to copy.</param>
        /// <param name="receivedBytes">The amount of copied to the buffer.</param>
        /// <param name="offset">The offset where to start receiving.</param>
        public SocketStatus Receive(byte[] data, int size, int offset, out int receivedBytes)
        {
            if (data == null)
                ExceptionHelper.ThrowArgumentNull(nameof(data));

            if ((uint)(size - offset) > data.Length)
                ExceptionHelper.ThrowArgumentOutOfRange(nameof(data));

            return Receive(new Span<byte>(data, offset, size), out receivedBytes);
        }

        /// <summary>
        /// Copies received data into the supplied NetPacket.
        /// Must be disposed after use.
        /// </summary>
        /// <param name="packet">Packet to copy the data into.</param>
        public SocketStatus Receive(NetPacket packet)
        {
            var status = ReceivePacket();
            if (status == SocketStatus.Done)
            {
                packet.OnReceive(ref m_pendingPacket);

                // PendingPacket buffer is completely passed to NetPacket. 
                m_pendingPacket = new PendingPacket();
            }

            return status;
        }

        /// <summary>
        /// Receives a <see cref="RawPacket"/> from the <see cref="TcpSocket"/>.
        /// Must be disposed after use.
        /// </summary>
        /// <param name="packet">Packet that contains unmanaged memory as its data.</param>
        public unsafe SocketStatus Receive(ref RawPacket packet)
        {
            if (packet.Data != IntPtr.Zero)
                ThrowNonEmptyBuffer();

            var status = ReceivePacket();
            if (status == SocketStatus.Done)
            {
                packet.ReceiveInto((IntPtr)m_pendingPacket.Data, m_pendingPacket.Size);

                // Reset Pending packet completely, as we've passed on its internal buffer.
                // PendingPacket.Resize will allocate a new buffer.
                m_pendingPacket = new PendingPacket();
            }
            else
            {
                // No complete packet received.
                packet = default;
            }
            return status;

            void ThrowNonEmptyBuffer()
                => throw new InvalidOperationException("Packet must be empty.");
        }


        #region Internal Methods
        private unsafe SocketStatus ReceivePacket()
        {
            PendingPacket pendingPacket = m_pendingPacket;

            int received;
            if (pendingPacket.SizeReceived < HeaderSize)
            {
                // Receive packet size.
                while (pendingPacket.SizeReceived < HeaderSize)
                {
                    byte* data = (byte*)&pendingPacket.Size + pendingPacket.SizeReceived;

                    var status = Receive(new Span<byte>(data, HeaderSize - pendingPacket.SizeReceived), out received);
                    pendingPacket.SizeReceived += received;

                    if (status != SocketStatus.Done)
                    {
                        m_pendingPacket = pendingPacket;
                        return status;
                    }
                }

                // If the received packet size exceeds the maximum allowed packet size, reject the client connection.
                // This prevents clients from abusively sending huge packets and consuming a lot of server memory.
                if (pendingPacket.Size > m_maxPacketSize)
                {
                    Socket.Close();
                    return SocketStatus.Disconnected;
                }
                // Pre-allocate packet buffer.
                else
                {
                    pendingPacket.Resize(pendingPacket.Size);
                }
            }

            // Receive packet data.
            int dataReceived = pendingPacket.SizeReceived - HeaderSize;
            while (dataReceived < pendingPacket.Size)
            {
                // Receive into buffer.
                int amountToReceive = pendingPacket.Size - dataReceived;
                var status = Receive(new Span<byte>(pendingPacket.Data + dataReceived, amountToReceive), out received);

                // Received greater than 0 can only occur with a SocketStatus of Done
                if (received > 0)
                {
                    dataReceived += received;
                }
                else
                {
                    pendingPacket.SizeReceived += dataReceived;
                    m_pendingPacket = pendingPacket;

                    return status;
                }
            }

            pendingPacket.SizeReceived += dataReceived;
            m_pendingPacket = pendingPacket;

            return SocketStatus.Done;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SocketStatus Send(ReadOnlySpan<byte> data, out int sent)
        {
            int size = data.Length;
            int result;
            for (sent = 0; sent < size; sent += result)
            {
                result = Socket.Send(data, SocketFlags.None, out SocketError error);

                // No data was sent, why?
                if (result == 0)
                {
                    SocketStatus status = SocketStatusMapper.Map(error);
                    if (status == SocketStatus.NotReady && sent > 0)
                        return SocketStatus.Partial;

                    return status;
                }
            }

            return SocketStatus.Done;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SocketStatus Receive(Span<byte> buffer, out int received)
        {
            received = Socket.Receive(buffer, SocketFlags.None, out SocketError error);

            if (received > 0)
                return SocketStatus.Done;

            if (error == SocketError.Success)
                return SocketStatus.Disconnected;

            return SocketStatusMapper.Map(error);
        }
        #endregion

        /// <summary>
        /// Disposes the TCP Connection.
        /// </summary>
        public void Close()
        {
            if (m_isActive)
                Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (m_isClearedUp)
                return;

            m_pendingPacket.Dispose();

            m_isClearedUp = true;
            m_isActive = false;
        }

        /// <summary>
        /// Gets or sets the size of the receive buffer in bytes.
        /// </summary>
        public int ReceiveBufferSize
        {
            get { return Socket.ReceiveBufferSize; }
            set { Socket.ReceiveBufferSize = value; }
        }
        /// <summary>
        /// Gets or sets the size of the send buffer in bytes.
        /// </summary>
        public int SendBufferSize
        {
            get { return Socket.SendBufferSize; }
            set { Socket.SendBufferSize = value; }
        }
        /// <summary>
        /// Gets or sets the receive time out value of the connection in milliseconds.
        /// Only has an effect when the <see cref="TcpSocket"/> is in blocking mode.
        /// </summary>
        public int ReceiveTimeout
        {
            get { return Socket.ReceiveTimeout; }
            set { Socket.ReceiveTimeout = value; }
        }
        /// <summary>
        /// Gets or sets the send time out value of the connection in milliseconds.
        /// Only has an effect when the <see cref="TcpSocket"/> is in blocking mode.
        /// </summary>
        public int SendTimeout
        {
            get { return Socket.SendTimeout; }
            set { Socket.SendTimeout = value; }
        }

        /// <summary>
        /// Gets or sets the value of the connection's linger option.
        /// </summary>
        public LingerOption LingerState
        {
            get { return Socket.LingerState; }
            set { Socket.LingerState = value; }
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

        private static Socket CreateSocket(ref AddressFamily family)
        {
            ValidateAddressFamily(family);

            Socket socket;

            if (family == AddressFamily.Unknown)
            {
                socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                if (socket.AddressFamily == AddressFamily.InterNetwork)
                    family = AddressFamily.InterNetwork;
            }
            else
            {
                socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp);
            }

            // Apply TCP-specific configuration.
            return ConfigureSocket(socket);
        }

        private static void ValidateAddressFamily(AddressFamily family)
        {
            if (family != AddressFamily.InterNetwork &&
                family != AddressFamily.InterNetworkV6 &&
                family != AddressFamily.Unknown)
            {
                throw new ArgumentException("Invalid AddressFamily for TCP protocol.", nameof(family));
            }
        }

        private static Socket ConfigureSocket(Socket socket)
        {
            socket.Blocking = false;
            socket.NoDelay = true;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, false);
            socket.SendBufferSize = ushort.MaxValue;

            return socket;
        }


        private void ThrowIfDisposed()
        {
            if (m_isClearedUp)
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
