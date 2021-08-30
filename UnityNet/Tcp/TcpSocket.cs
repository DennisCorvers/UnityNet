﻿using System;
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
        private const int HeaderSize = sizeof(int);

#pragma warning disable IDE0032, IDE0044
        private Socket m_socket;
        private PendingPacket m_pendingPacket;
        private AddressFamily m_family;

        private bool m_isActive;
        private bool m_isClearedUp;

        private int m_maxPacketSize = int.MaxValue;
#pragma warning restore IDE0032, IDE0044

        /// <summary>
        /// Indicates whether the underlying socket is connected.
        /// </summary>
        public bool Connected
            => m_socket.Connected;
        /// <summary>
        /// Gets or sets a value that indicates whether the <see cref="TcpSocket"/> is in blocking mode.
        /// </summary>
        public bool Blocking
        {
            get => m_socket.Blocking;
            set => m_socket.Blocking = value;
        }

        private bool ExclusiveAddressUse
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
        /// Creates a new <see cref="TcpSocket"/> with an internal buffer.
        /// </summary>
        public TcpSocket()
            : this(AddressFamily.Unknown)
        { }

        /// <summary>
        /// Creates a new <see cref="TcpSocket"/> with an internal buffer.
        /// </summary>
        /// <param name="family">The AddressFamily of the IP.</param>
        public TcpSocket(AddressFamily family)
        {
            ValidateAddressFamily(family);
            m_family = family;

            InitializeClientSocket();
        }

        /// <summary>
        /// Creates a new <see cref="TcpSocket"/> from an accepted Socket.
        /// Creates its own internal buffer.
        /// </summary>
        internal TcpSocket(Socket socket, int maxPacketSize)
        {
            m_isActive = true;
            m_socket = ConfigureSocket(socket);
            m_maxPacketSize = maxPacketSize;
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
            ThrowIfDisposed();

            ThrowIfActive();

            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

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
                        m_family = m_socket.AddressFamily;
                        m_isActive = true;
                        return SocketStatus.Done;
                    }

                    return SocketStatus.Disconnected;
                }
                catch (Exception e)
                {
                    Logger.Error(e);
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

            ThrowIfActive();

            var tcs = new TaskCompletionSource<SocketStatus>();

            var t = m_socket.BeginConnect(hostname, port, (asyncResult) =>
            {
                var innerTcs = (TaskCompletionSource<SocketStatus>)asyncResult.AsyncState;

                try
                {
                    m_socket.EndConnect(asyncResult);
                    m_isActive = true;
                    m_family = m_socket.AddressFamily;
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
        public Task<SocketStatus> ConnectAsync(IPEndPoint endpoint)
        {
            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            if (m_isClearedUp)
                throw new ObjectDisposedException(GetType().FullName);

            ThrowIfActive();

            var tcs = new TaskCompletionSource<SocketStatus>();

            m_socket.BeginConnect(endpoint, (asyncResult) =>
            {
                var innerTcs = (TaskCompletionSource<SocketStatus>)asyncResult.AsyncState;

                try
                {
                    m_socket.EndConnect(asyncResult);
                    m_isActive = true;
                    m_family = m_socket.AddressFamily;
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
        public SocketStatus Send(IntPtr data, int size, out int bytesSent)
        {
            bytesSent = 0;

            if (data == IntPtr.Zero || size == 0)
            {
                ExceptionHelper.ThrowNoData();
                return SocketStatus.Error;
            }

            return Send(new ReadOnlySpan<byte>((byte*)data, size), out bytesSent);
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
        public SocketStatus Send(NetPacket packet)
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

            return Receive(new Span<byte>((void*)data, size), out receivedBytes);
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
        public SocketStatus Receive(ref RawPacket packet)
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
        private SocketStatus InnerConnect(EndPoint endpoint)
        {
            try
            {
                m_socket.Connect(endpoint);
                m_family = m_socket.AddressFamily;
                m_isActive = true;
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == (int)SocketError.WouldBlock)
                    return SocketStatus.NotReady;

                Logger.Error(e);
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
                    m_socket.Close();
                    return SocketStatus.Disconnected;
                }
            }

            // Receive packet data.
            int dataReceived = pendingPacket.SizeReceived - HeaderSize;
            byte* buffer = stackalloc byte[1024];
            while (dataReceived < pendingPacket.Size)
            {
                // Receive into buffer.
                int amountToReceive = Math.Min(1024, pendingPacket.Size - dataReceived);
                var status = Receive(new Span<byte>(buffer, amountToReceive), out received);

                // Received greater than 0 can only occur with a SocketStatus of Done
                if (received > 0)
                {
                    pendingPacket.Resize(dataReceived + received);
                    Memory.MemCpy(buffer, pendingPacket.Data + dataReceived, received);
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
                result = m_socket.Send(data, SocketFlags.None, out SocketError error);

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
            received = m_socket.Receive(buffer, SocketFlags.None, out SocketError error);

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

            m_pendingPacket.Dispose();

            m_isClearedUp = true;
            m_isActive = false;
        }

        public void Dispose()
            => Dispose(true);

        ~TcpSocket()
            => Dispose(false);

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
        /// Gets or sets the value of the connection's linger option.
        /// </summary>
        public LingerOption LingerState
        {
            get { return m_socket.LingerState; }
            set { m_socket.LingerState = value; }
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

        private void InitializeClientSocket()
        {
            if (m_socket != null)
                return;

            if (m_family == AddressFamily.Unknown)
            {
                m_socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

                if (m_socket.AddressFamily == AddressFamily.InterNetwork)
                    m_family = AddressFamily.InterNetwork;
            }
            else
            {
                m_socket = new Socket(m_family, SocketType.Stream, ProtocolType.Tcp);
            }

            // Apply TCP-specific configuration.
            ConfigureSocket(m_socket);
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

        private void ValidateAddressFamily(AddressFamily family)
        {
            if (family != AddressFamily.InterNetwork &&
                family != AddressFamily.InterNetworkV6 &&
                family != AddressFamily.Unknown)
            {
                throw new ArgumentException("Invalid AddressFamily for TCP protocol.", nameof(family));
            }
        }
    }
}
