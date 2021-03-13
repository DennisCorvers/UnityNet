using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityNet.Serialization;
using UnityNet.Utils;

namespace UnityNet.Tcp
{
    public unsafe sealed class TcpSocket : IDisposable
    {
        // 1380 is a conservative MTU size.
        internal const int BUFFER_SIZE = 3;// 1380;
        internal const int MAX_PACKET_SIZE = ushort.MaxValue;
        private const int HEADER_SIZE = sizeof(ushort);

#pragma warning disable IDE0032, IDE0044
        private PendingPacket m_pendingPacket;

        private Socket m_socket;
        private byte[] m_buffer;

        private bool m_isActive;
        private bool m_isClearedUp;
        private bool m_hasSharedBuffer;
#pragma warning restore IDE0032, IDE0044

        /// <summary>
        /// Get the port to which the socket is remotely connected.
        /// If the socket is not connected, this property returns 0.
        /// </summary>
        public ushort RemotePort
        {
            get
            {
                if (m_socket.RemoteEndPoint == null)
                    return 0;
                return (ushort)((IPEndPoint)m_socket.RemoteEndPoint).Port;
            }
        }
        /// <summary>
        /// The local port of the socket.
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
        /// <summary>
        /// Get the the address to which the socket is remotely connected.
        /// </summary>
        public IPAddress RemoteAddress
        {
            get
            {
                if (m_socket.RemoteEndPoint == null)
                    return IPAddress.None;
                return ((IPEndPoint)m_socket.RemoteEndPoint).Address;
            }
        }
        /// <summary>
        /// Gets or sets a value that indicates whether the <see cref="TcpSocket"/> is in blocking mode.
        /// </summary>
        public bool Blocking
        {
            get { return m_socket.Blocking; }
            set { m_socket.Blocking = value; }
        }

        /// <summary>
        /// Gets or sets the size of the receive buffer in bytes.
        /// </summary>
        public int ReceiveBufferSize
        {
            get { return m_socket.ReceiveBufferSize; }
            set { m_socket.ReceiveBufferSize = value; }
        }
        /// <summary>
        /// Gets or sets the size of the send buffer in bytes.
        /// </summary>
        public int SendBufferSize
        {
            get { return m_socket.SendBufferSize; }
            set { m_socket.SendBufferSize = value; }
        }
        /// <summary>
        /// Gets or sets the receive time out value of the connection in milliseconds.
        /// Only has an effect when the <see cref="TcpSocket"/> is in blocking mode.
        /// </summary>
        public int ReceiveTimeout
        {
            get { return m_socket.ReceiveTimeout; }
            set { m_socket.ReceiveTimeout = value; }
        }
        /// <summary>
        /// Gets or sets the send time out value of the connection in milliseconds.
        /// Only has an effect when the <see cref="TcpSocket"/> is in blocking mode.
        /// </summary>
        public int SendTimeout
        {
            get { return m_socket.SendTimeout; }
            set { m_socket.SendTimeout = value; }
        }

        /// <summary>
        /// Incidates the maximum size of a packet that can be processed.
        /// Packets larger than this will be dropped.
        /// </summary>
        public int MaxPacketSize
        {
            get => ushort.MaxValue;
        }

        /// <summary>
        /// Indicates whether the underlying socket is connected.
        /// </summary>
        public bool Connected
            => m_socket.Connected;
        /// <summary>
        /// Indication that a connection has been made.
        /// </summary>
        public bool IsActive
            => m_isActive;
        /// <summary>
        /// Indicates if the TcpSocket is using a buffer that is shared between other TcpSocket instances.
        /// </summary>
        public bool HasSharedBuffer
            => m_hasSharedBuffer;

        private bool ExclusiveAddressUse
        {
            get
            {
                return m_socket.ExclusiveAddressUse;
            }
            set
            {
                m_socket.ExclusiveAddressUse = value;
            }
        }


        /// <summary>
        /// Creates a new <see cref="TcpSocket"/> with an internal buffer.
        /// </summary>
        public TcpSocket()
        {
            CreateSocket();
            m_buffer = new byte[BUFFER_SIZE];
        }

        /// <summary>
        /// Creates a new <see cref="TcpSocket"/> with a user-defined buffer.
        /// </summary>
        /// <param name="buffer">The Send/Receive buffer.</param>
        public TcpSocket(byte[] buffer)
            : this()
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (buffer.Length < BUFFER_SIZE)
                throw new ArgumentOutOfRangeException(nameof(buffer), buffer.Length, $"Buffer needs to have a size of at least {BUFFER_SIZE}.");

            m_buffer = buffer;
        }

        /// <summary>
        /// Creates a new <see cref="TcpSocket"/> from an accepted Socket.
        /// Creates its own internal buffer.
        /// </summary>
        internal TcpSocket(Socket socket)
            : this(socket, new byte[BUFFER_SIZE])
        {
            m_isActive = true;
            m_socket = ConfigureSocket(socket);
            m_buffer = new byte[BUFFER_SIZE];
        }

        /// <summary>
        /// Creates a new <see cref="TcpSocket"/> from an accepted Socket.
        /// Uses a shared buffer.
        /// </summary>
        internal TcpSocket(Socket socket, byte[] buffer)
        {
            m_isActive = true;
            m_hasSharedBuffer = true;
            m_socket = ConfigureSocket(socket);
            m_buffer = buffer;
        }

        ~TcpSocket() => Dispose(false);


        /// <summary>
        /// Configures a socket.
        /// </summary>
        private static Socket ConfigureSocket(Socket socket)
        {
            socket.Blocking = false;
            socket.NoDelay = true;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, false);

            return socket;
        }

        /// <summary>
        /// Creates and configures a new socket.
        /// </summary>
        private void CreateSocket()
        {
            if (m_socket == null)
                m_socket = ConfigureSocket(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
        }


        /// <summary>
        /// Establishes a connection to the remote host.
        /// </summary>
        /// <param name="address">The Ip Address of the remote host.</param>
        /// <param name="port">The port of the remote host.</param>
        /// <param name="timeout">The timeout in seconds to wait for a connection.</param>
        public SocketStatus Connect(UNetIp address, ushort port, int timeout = 0)
        {
            return Connect(new IPEndPoint(address.ToIPAddress(), port), 0);
        }

        /// <summary>
        /// Establishes a connection to the remote host.
        /// </summary>
        /// <param name="address">The Ip Address of the remote host.</param>
        /// <param name="port">The port of the remote host.</param>
        /// <param name="timeout">The timeout in seconds to wait for a connection.</param>
        public SocketStatus Connect(IPAddress address, ushort port, int timeout = 0)
        {
            return Connect(new IPEndPoint(address, port), timeout);
        }

        /// <summary>
        /// Establishes a connection to the remote host.
        /// </summary>
        /// <param name="hostname">The hostname of the remote host.</param>
        /// <param name="port">The port of the remote host.</param>
        public SocketStatus Connect(string hostname, ushort port, int timeout = 0)
        {
            if (hostname == null)
                throw new ArgumentNullException(nameof(hostname));

            if (m_isClearedUp)
                throw new ObjectDisposedException(GetType().FullName);

            if (m_isActive)
                throw new InvalidOperationException("Socket is already connected.");

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
                if (address.AddressFamily == AddressFamily.InterNetwork)
                    return Connect(new IPEndPoint(address, port), timeout);
            }

            return SocketStatus.Error;
        }

        /// <summary>
        /// Establishes a connection to the remote host.
        /// </summary>
        /// <param name="endpoint">The endpoint of the remote host.</param>
        /// <param name="timeout">The timeout in seconds to wait for a connection.</param>
        public SocketStatus Connect(IPEndPoint endpoint, int timeout = 0)
        {
            if (endpoint == null)
                throw new ArgumentNullException("endpoint");

            if (m_isClearedUp)
                throw new ObjectDisposedException(GetType().FullName);

            if (m_isActive)
                throw new InvalidOperationException("Socket is already connected.");

            if (timeout <= 0)
            {
                return InnerConnect(endpoint);
            }
            else
            {
                try
                {
                    var connectResult = m_socket.BeginConnect(endpoint, null, null);
                    var success = connectResult.AsyncWaitHandle.WaitOne(timeout * 1000);

                    if (m_socket.Connected)
                    {
                        m_isActive = true;
                        return SocketStatus.Done;
                    }

                    return SocketStatus.Disconnected;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    return SocketStatus.Error;
                }
            }
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
        public Task<SocketStatus> ConnectAsync(string hostname, ushort port)
        {
            if (hostname == null)
                throw new ArgumentNullException(nameof(hostname));

            if (m_isClearedUp)
                throw new ObjectDisposedException(GetType().FullName);

            var tcs = new TaskCompletionSource<SocketStatus>();

            m_socket.BeginConnect(hostname, port, (asyncResult) =>
            {
                var innerTcs = (TaskCompletionSource<SocketStatus>)asyncResult.AsyncState;

                try
                {
                    m_socket.EndConnect(asyncResult);
                    m_isActive = true;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    innerTcs.TrySetResult(SocketStatus.Error);
                    return;
                }

                if (m_socket.Connected)
                {
                    innerTcs.TrySetResult(SocketStatus.Done);
                    return;
                }

                innerTcs.TrySetResult(SocketStatus.Disconnected);

            }, tcs);

            return tcs.Task;
        }

        /// <summary>
        /// Starts connecting to the remote host.
        /// </summary>
        /// <param name="endpoint">The endpoint of the remote host.</param>
        /// <param name="callback">The callback that receives the connection result.</param>
        public Task<SocketStatus> ConnectAsync(IPEndPoint endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            if (m_isClearedUp)
                throw new ObjectDisposedException(GetType().FullName);

            var tcs = new TaskCompletionSource<SocketStatus>();

            m_socket.BeginConnect(endpoint, (asyncResult) =>
            {
                var innerTcs = (TaskCompletionSource<SocketStatus>)asyncResult.AsyncState;

                try
                {
                    m_socket.EndConnect(asyncResult);
                    m_isActive = true;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    innerTcs.TrySetResult(SocketStatus.Error);
                    return;
                }

                if (m_socket.Connected)
                {
                    innerTcs.TrySetResult(SocketStatus.Done);
                    return;
                }

                innerTcs.TrySetResult(SocketStatus.Disconnected);

            }, tcs);

            return tcs.Task;
        }


        /// <summary>
        /// Sends data over the <see cref="TcpSocket"/>.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="size">The size of the payload.</param>
        /// <param name="bytesSent">The amount of bytes that have been sent.</param>
        public unsafe SocketStatus Send(IntPtr data, int size, out int bytesSent)
        {
            bytesSent = 0;

            if (data == IntPtr.Zero || size == 0)
            {
                Logger.Error(ExceptionHelper.NO_DATA);
                return SocketStatus.Error;
            }

            int result = 0;
            for (; bytesSent < size; bytesSent += result)
            {
                // Copy unmanaged data to managed buffer.
                int toSend = Math.Min(BUFFER_SIZE, size - bytesSent);
                Memory.MemCpy((byte*)data + bytesSent, m_buffer, 0, toSend);

                // Send managed buffer.
                var status = InnerSend(m_buffer, toSend, 0, out result);

                // If the returned status is anything but Done, 
                // stop sending because something went wrong.
                if (status != SocketStatus.Done)
                    return status;
            }

            return SocketStatus.Done;
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
            return InnerSend(data, length, offset, out bytesSent);
        }

        /// <summary>
        /// Sends a <see cref="RawPacket"/> over the TcpSocket.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public SocketStatus Send(ref RawPacket packet)
        {
            return InnerSend((void*)packet.Data, packet.Size, ref packet.SendPosition);
        }

        /// <summary>
        /// Sends a <see cref="NetPacket"/> over the TcpSocket.
        /// </summary>
        /// <param name="packet">The packet to send.</param>
        public SocketStatus Send(ref NetPacket packet)
        {
            return InnerSend(packet.Data, packet.ByteSize, ref packet.SendPosition);
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
                Logger.Error(ExceptionHelper.INVALID_BUFFER);
                return SocketStatus.Error;
            }

            while (receivedBytes < size)
            {
                // Allow a maximum receive of buffer.Length at a time.
                var status = InnerReceive((byte*)data + receivedBytes, size - receivedBytes, out int result);

                receivedBytes += result;

                if (status != SocketStatus.Done)
                    return status;
            }

            return SocketStatus.Done;
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
        public SocketStatus Receive(byte[] data, int size, int offset, out int receivedBytes)
        {
            return InnerReceive(data, size, offset, out receivedBytes);
        }

        /// <summary>
        /// Copies received data into the supplied NetPacket.
        /// Must be disposed after use.
        /// </summary>
        /// <param name="packet">Packet to copy the data into.</param>
        public SocketStatus Receive(ref NetPacket packet)
        {
            var status = ReceivePacket();
            if (status == SocketStatus.Done)
            {
                packet.OnReceive(m_pendingPacket.Data, m_pendingPacket.Size);

                // Reset the PendingPacket, but keep the internal buffer since we only made a copy.
                m_pendingPacket.Clear();
            }

            return status;
        }

        /// <summary>
        /// Receives a <see cref="RawPacket"/> from the <see cref="TcpSocket"/>.
        /// Must be disposed after use.
        /// </summary>
        /// <param name="packet">Packet that contains unmanaged memory as its data.</param>
        public SocketStatus Receive(out RawPacket packet)
        {
            var status = ReceivePacket();
            if (status == SocketStatus.Done)
            {
                packet = new RawPacket((IntPtr)m_pendingPacket.Data, m_pendingPacket.Size);

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
        }


        #region Internal Methods
        private SocketStatus InnerConnect(EndPoint endpoint)
        {
            try
            {
                m_socket.Connect(endpoint);
                m_isActive = true;
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == (int)SocketError.WouldBlock)
                    return SocketStatus.NotReady;

                Logger.Error(ex);
                return SocketStatus.Error;
            }

            if (m_socket.Connected)
                return SocketStatus.Done;

            return SocketStatus.Disconnected;
        }

        private SocketStatus ReceivePacket()
        {
            PendingPacket pendingPacket = m_pendingPacket;

            int received = 0;
            if (pendingPacket.SizeReceived < HEADER_SIZE)
            {
                // Receive packet size.
                while (pendingPacket.SizeReceived < HEADER_SIZE)
                {
                    byte* data = (byte*)&pendingPacket.Size + pendingPacket.SizeReceived;

                    var status = InnerReceive(data, HEADER_SIZE - pendingPacket.SizeReceived, out received);
                    pendingPacket.SizeReceived += received;

                    if (status != SocketStatus.Done)
                    {
                        m_pendingPacket = pendingPacket;
                        return status;
                    }
                }

                pendingPacket.Resize(pendingPacket.Size);
            }

            // Receive packet data.
            int dataReceived = pendingPacket.SizeReceived - HEADER_SIZE;
            while (dataReceived < pendingPacket.Size)
            {
                // Receive into buffer.
                int amountToReceive = Math.Min(BUFFER_SIZE, pendingPacket.Size - dataReceived);
                var status = InnerReceive(m_buffer, amountToReceive, 0, out received);

                // Received greater than 0 can only occur with a SocketStatus of Done
                if (received > 0)
                {
                    Memory.MemCpy(m_buffer, 0, pendingPacket.Data + dataReceived, received);
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
        private SocketStatus InnerReceive(void* data, int size, out int receivedBytes)
        {
            int maxBytes = Math.Min(size, BUFFER_SIZE);
            receivedBytes = m_socket.Receive(m_buffer, 0, maxBytes, SocketFlags.None, out SocketError error);

            if (receivedBytes > 0)
            {
                Memory.MemCpy(m_buffer, 0, data, receivedBytes);
                return SocketStatus.Done;
            }

            if (error == SocketError.Success)
                return SocketStatus.Disconnected;

            return SocketStatusMapper.Map(error);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SocketStatus InnerReceive(byte[] data, int size, int offset, out int receivedBytes)
        {
            receivedBytes = m_socket.Receive(data, offset, size, SocketFlags.None, out SocketError error);

            if (receivedBytes > 0)
                return SocketStatus.Done;

            if (error == SocketError.Success)
                return SocketStatus.Disconnected;

            return SocketStatusMapper.Map(error);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SocketStatus InnerSend(byte[] data, int length, int offset, out int bytesSent)
        {
            int result = 0;
            for (bytesSent = 0; bytesSent < length; bytesSent += result)
            {
                result = m_socket.Send(data, bytesSent + offset, length - bytesSent, SocketFlags.None, out SocketError error);

                // No data was sent, why?
                if (result == 0)
                {
                    SocketStatus status = SocketStatusMapper.Map(error);
                    if (status == SocketStatus.NotReady && bytesSent > 0)
                        return SocketStatus.Partial;

                    return status;
                }
            }

            return SocketStatus.Done;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SocketStatus InnerSend(void* data, int packetSize, ref int sendPosition)
        {
            if (packetSize > MAX_PACKET_SIZE)
                ExceptionHelper.ThrowPacketSizeExceeded();

            // Send packet header (and remaining data that fits within the buffer)
            if (sendPosition < HEADER_SIZE)
            {
                byte* sendPosPtr = (byte*)&packetSize + sendPosition;
                int sizeToCopy = Math.Min(BUFFER_SIZE - HEADER_SIZE, packetSize);

                fixed (byte* bufPtr = &m_buffer[sendPosition])
                {
                    Memory.MemCpy(sendPosPtr, bufPtr, HEADER_SIZE - sendPosition);
                    Memory.MemCpy(data, bufPtr + 2, sizeToCopy);
                }

                var status = InnerSend(m_buffer, sizeToCopy + HEADER_SIZE, 0, out sendPosition);

                if (status != SocketStatus.Done)
                    return status;
            }

            // Send packet body.
            int dataOffset = sendPosition - HEADER_SIZE;
            while (dataOffset < packetSize)
            {
                int toSend = Math.Min(BUFFER_SIZE, packetSize - dataOffset);
                Memory.MemCpy((byte*)data + dataOffset, m_buffer, 0, toSend);

                var status = InnerSend(m_buffer, toSend, 0, out int bytesSent);
                dataOffset += bytesSent;

                if (status != SocketStatus.Done)
                {
                    sendPosition = dataOffset + HEADER_SIZE;
                    return status;
                }
            }

            // All sends completed at this point.
            sendPosition = 0;

            return SocketStatus.Done;
        }
        #endregion

        /// <summary>
        /// Disposes the TCP Connection.
        /// </summary>
        /// <param name="reuseSocket">TRUE to create a new underlying socket. Resets all previously set socket options.</param>
        public void Close(bool reuseSocket = false)
        {
            if (m_isActive)
            {
                // Shuts down and closes the connection.
                Dispose();

                if (reuseSocket)
                {
                    CreateSocket();
                    m_isClearedUp = false;
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (m_isClearedUp)
                return;

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

            if (m_pendingPacket.Data != null)
                m_pendingPacket.Free();

            m_isClearedUp = true;
            m_isActive = false;
        }

        public void Dispose()
            => Dispose(true);
    }
}
