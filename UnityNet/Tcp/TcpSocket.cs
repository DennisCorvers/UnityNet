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
        internal const int BUFFER_SIZE = 1024;

#pragma warning disable IDE0032, IDE0044
        private Socket m_socket;
        private byte[] m_buffer;

        private PendingPacket m_pendingPacket;

        private bool m_isActive = false;
        private bool m_isClearedUp = false;
        private bool m_hasSharedBuffer = false;
#pragma warning restore IDE0032, IDE0044

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
        /// Gets or sets the receive time out value of the connection in seconds.
        /// </summary>
        public int ReceiveTimeout
        {
            get { return m_socket.ReceiveTimeout; }
            set { m_socket.ReceiveTimeout = value; }
        }
        /// <summary>
        /// Gets or sets the send time out value of the connection in seconds.
        /// </summary>
        public int SendTimeout
        {
            get { return m_socket.SendTimeout; }
            set { m_socket.SendTimeout = value; }
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
            m_socket = CreateSocket();
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

        ~TcpSocket()
        {
            Dispose(false);
        }


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
        private Socket CreateSocket()
        {
            m_isActive = false;
            return ConfigureSocket(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
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
                throw new ObjectDisposedException(this.GetType().FullName);

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
            if (data == IntPtr.Zero || size == 0)
            {
                bytesSent = 0;
                Logger.Error(ExceptionHelper.NO_DATA);
                return SocketStatus.Error;
            }

            int result = 0;
            for (bytesSent = 0; bytesSent < size; bytesSent += result)
            {
                int len = Math.Min(m_buffer.Length, size - bytesSent);
                Memory.MemCpy(((byte*)data) + bytesSent, m_buffer, 0, len);
                var stat = InnerSend(m_buffer, len, 0, out result);

                // If the returned status is anything but Done, 
                // stop sending because something went wrong.
                if (stat != SocketStatus.Done)
                    return stat;
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
        /// <returns></returns>
        public SocketStatus Send(byte[] data, int length, int offset, out int bytesSent)
        {
            bytesSent = 0;

            if (data == null || data.Length == 0)
            {
                Logger.Error(ExceptionHelper.NO_DATA);
                return SocketStatus.Error;
            }

            if ((uint)length + (uint)offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(length), length, $"{nameof(length)} needs smaller or equal to the length of {nameof(data)}.");

            return InnerSend(data, length, offset, out bytesSent);
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

        public SocketStatus Send(ref NetPacket packet)
        {
            //https://github.com/SFML/SFML/blob/master/src/SFML/Network/TcpSocket.cpp#L301

            return SocketStatus.Done;
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
                var status = InnerReceive((byte*)data, size - receivedBytes, receivedBytes, out int result);

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
            receivedBytes = 0;

            if (data == null || data.Length == 0)
            {
                Logger.Error(ExceptionHelper.INVALID_BUFFER);
                return SocketStatus.Error;
            }

            if ((uint)size + (uint)offset > data.Length)
                throw new ArgumentOutOfRangeException($"{nameof(size)} + {nameof(offset)} needs to be smaller than, or equal to {nameof(data.Length)}.");

            receivedBytes = m_socket.Receive(data, offset, size, SocketFlags.None, out SocketError error);

            if (receivedBytes > 0)
                return SocketStatus.Done;

            // If the socket returns 0 immediately with a SocketError.Success
            // The connection has been shutdown by the remote endpoint.
            if (error == SocketError.Success)
                return SocketStatus.Disconnected;

            return SocketStatusMapper.Map(error);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe SocketStatus InnerReceive(byte* data, int size, int offset, out int receivedBytes)
        {
            int maxBytes = Math.Min(size, m_buffer.Length);
            receivedBytes = m_socket.Receive(m_buffer, 0, maxBytes, SocketFlags.None, out SocketError error);

            if (receivedBytes > 0)
            {
                Memory.MemCpy(m_buffer, 0, data + offset, receivedBytes);
                return SocketStatus.Done;
            }

            if (error == SocketError.Success)
                return SocketStatus.Disconnected;

            return SocketStatusMapper.Map(error);
        }

        public SocketStatus ReceiveUnsafe(ref NetPacket packet)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Receives a packet from the <see cref="TcpSocket"/> and stores it in the provided <see cref="RawPacket"/>.
        /// </summary>
        /// <param name="packet">Packet that contains unmanaged memory as its data.</param>
        public SocketStatus ReceiveUnsafe(ref RawPacket packet)
        {
            // packet.clear

            // Store this temporarily so we can read bytes into this value.
            uint pendingPackSize = m_pendingPacket.Size;

            uint packetSize = 0;
            int received = 0;

            // Try to get the size of the packet.
            while (m_pendingPacket.SizeReceived < sizeof(uint))
            {
                byte* data = ((byte*)&pendingPackSize) + m_pendingPacket.SizeReceived;
                var status = InnerReceive(data, sizeof(uint) - m_pendingPacket.SizeReceived, 0, out received);
                m_pendingPacket.SizeReceived += received;

                if (status != SocketStatus.Done)
                    return status;
            }

            // Write back the packet size.
            packetSize = m_pendingPacket.Size = pendingPackSize;

            // Receive packet here...
            // https://github.com/SFML/SFML/blob/498d7ee79c700bba767ca42f68ced8ca116b5ada/src/SFML/Network/TcpSocket.cpp#L377

            return SocketStatus.Error;
        }

        /// <summary>
        /// Disposes the TCP Connection.
        /// </summary>
        /// <param name="reuseSocket">TRUE to create a new underlying socket.</param>
        public void Close(bool reuseSocket = false)
        {
            if (m_isActive)
            {
                // Shuts down and closes the connection.
                Dispose();

                if (reuseSocket)
                {
                    m_socket = CreateSocket();
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
                        // Shutdown the connection if there's one in progress.
                        if (m_socket.Connected)
                            m_socket.Shutdown(SocketShutdown.Both);
                    }
                    finally
                    {
                        m_socket.Close();
                        m_socket = null;
                    }
                }
            }

            if (m_pendingPacket.Data != null)
                Memory.Free(m_pendingPacket.Data);
            m_pendingPacket.Data = null;

            m_isClearedUp = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
