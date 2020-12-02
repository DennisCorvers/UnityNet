using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityNet.Utils;

namespace UnityNet.Tcp
{
    public sealed class TcpSocket
    {
        public const int BUFFER_SIZE = 1024;

#pragma warning disable IDE0032, IDE0044
        private bool m_isDisposed = false;
        private Socket m_socket;
        private byte[] m_buffer;
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
        {
            get
            { return m_socket.Connected; }
        }

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
        /// Creates a new TcpSocket with an internal buffer.
        /// </summary>
        public TcpSocket()
        {
            m_socket = CreateSocket();
            m_buffer = new byte[BUFFER_SIZE];
        }

        /// <summary>
        /// Creates a new TcpSocket with a user-defined buffer.
        /// </summary>
        /// <param name="buffer">The Send/Receive buffer.</param>
        public TcpSocket(byte[] buffer)
            : this()
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            UNetDebug.Assert(buffer.Length >= BUFFER_SIZE);

            m_buffer = buffer;
        }

        internal TcpSocket(Socket socket, ushort bufferSize)
            : this(socket, new byte[bufferSize])
        { }

        internal TcpSocket(Socket socket, byte[] buffer)
        {
            m_socket = ConfigureSocket(socket);
            m_buffer = buffer;
        }

        ~TcpSocket()
        {
            Dispose();
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
            return ConfigureSocket(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
        }

        /// <summary>
        /// Establishes a connection to the remote host.
        /// </summary>
        /// <param name="address">The Ip Address of the remote host</param>
        /// <param name="port">The port of the remote host</param>
        public SocketStatus Connect(IPAddress address, ushort port)
        {
            return Connect(new IPEndPoint(address, port), 0);
        }

        /// <summary>
        /// Establishes a connection to the remote host.
        /// </summary>
        /// <param name="endpoint">The endpoint of the remote host</param>
        public SocketStatus Connect(IPEndPoint endpoint)
        {
            return Connect(endpoint, 0);
        }

        /// <summary>
        /// Establishes a connection to the remote host.
        /// </summary>
        /// <param name="address">The Ip Address of the remote host</param>
        /// <param name="port">The port of the remote host</param>
        /// <param name="timeout">The timeout in seconds to wait for a connection</param>
        public SocketStatus Connect(IPAddress address, ushort port, int timeout)
        {
            return Connect(new IPEndPoint(address, port), timeout);
        }

        /// <summary>
        /// Establishes a connection to the remote host.
        /// </summary>
        /// <param name="hostname">The hostname of the remote host</param>
        /// <param name="port">The port of the remote host</param>
        /// <returns></returns>
        public SocketStatus Connect(string hostname, ushort port)
        {
            return Connect(hostname, port, 0);
        }

        /// <summary>
        /// Establishes a connection to the remote host.
        /// </summary>
        /// <param name="hostname">The hostname of the remote host</param>
        /// <param name="port">The port of the remote host</param>
        /// <returns></returns>
        public SocketStatus Connect(string hostname, ushort port, int timeout)
        {
            if (hostname == null)
                throw new ArgumentNullException("hostname");

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
        /// <param name="endpoint">The endpoint of the remote host</param>
        /// <param name="timeout">The timeout in seconds to wait for a connection</param>
        public SocketStatus Connect(IPEndPoint endpoint, int timeout)
        {
            if (endpoint == null)
                throw new ArgumentNullException("endpoint");

            UNetDebug.Assert(!m_socket.Connected, "Socket is already connected.");

            if (m_isDisposed)
                throw new InvalidOperationException("TcpSocket was disposed.");

            if (timeout <= 0)
            {
                return InnerConnect(endpoint);
            }
            else
            {
                //Same previous socket information
                bool blockState = Blocking;

                try
                {
                    var connectResult = m_socket.BeginConnect(endpoint, null, null);
                    var success = connectResult.AsyncWaitHandle.WaitOne(timeout * 1000);

                    if (m_socket.Connected)
                        return SocketStatus.Done;
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
            }
            catch (SocketException ex)
            {
                Logger.Error(ex);
                return SocketStatus.Error;
            }

            if (m_socket.Connected)
                return SocketStatus.Done;

            return SocketStatus.Disconnected;
        }

        /// <summary>
        /// Sends data over the socket.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="size">The size of the payload.</param>
        /// <param name="bytesSent">The amount of bytes that have been sent.</param>
        public unsafe SocketStatus Send(IntPtr data, int size, out int sent)
        {
            sent = 0;

            if (data == IntPtr.Zero || size == 0)
            {
                Logger.Error("Cannot send data over the network. No data to send.");
                return SocketStatus.Error;
            }

            int result = 0;
            for (sent = 0; sent < size; sent += result)
            {
                int len = Unsafe.CopyToBuffer(m_buffer, (byte*)data, size - sent);
                var socketError = Send(m_buffer, len, out result);
            }

            return SocketStatus.Done;
        }

        /// <summary>
        /// Sends data over the socket.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        public SocketStatus Send(byte[] data)
        {
            return Send(data, out int bytesSent);
        }

        /// <summary>
        /// Sends data over the socket.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="sent">The amount of bytes that have been sent.</param>
        public SocketStatus Send(byte[] data, out int sent)
        {
            return Send(data, data.Length, out sent);
        }

        /// <summary>
        /// Sends data over the socket.
        /// </summary>
        /// <param name="data">The payload to send.</param>
        /// <param name="size">The amount of data to send.</param>
        /// <param name="sent">The amount of bytes that have been sent.</param>
        public SocketStatus Send(byte[] data, int size, out int sent)
        {
            sent = 0;

            if (data == null || data.Length == 0)
            {
                Logger.Error("Cannot send data over the network. No data to send.");
                return SocketStatus.Error;
            }

            UNetDebug.Assert(data.Length >= size);

            int result = 0;
            for (sent = 0; sent < size; sent += result)
            {
                result = m_socket.Send(data, sent, size - sent, SocketFlags.None, out SocketError error);

                if (result == 0) //No data was sent, why?
                {
                    SocketStatus status = SocketStatusMapper.Map(error);
                    if (status == SocketStatus.NotReady && sent > 0)
                        return SocketStatus.Partial;

                    return status;
                }
            }

            return SocketStatus.Done;
        }

        //public SocketStatus Send(MyPacket)
        //{
        //https://github.com/SFML/SFML/blob/master/src/SFML/Network/TcpSocket.cpp#L301
        //}

        public SocketStatus Receive(IntPtr data, int size, ref int received)
        {
            received = 0;

            if (data == IntPtr.Zero)
                throw new ArgumentNullException("data");

            //https://github.com/SFML/SFML/blob/master/src/SFML/Network/TcpSocket.cpp#L268
            //Poll socket to check disconnect?
            return SocketStatus.Error;
        }

        public SocketStatus Receive(byte[] data, ref int received)
        {
            //TODO Use STACKALLOC BUFFER INSTEAD?? NO GC!
            throw new NotImplementedException();
        }

        //public SocketStatus Receive(MyPacket)
        //{
        //https://github.com/SFML/SFML/blob/master/src/SFML/Network/TcpSocket.cpp#L345
        //}

        /// <summary>
        /// Closes the network connection
        /// </summary>
        /// <param name="reuseSocket">TRUE to create a new underlying socket.</param>
        public void Close(bool reuseSocket)
        {
            if (Connected)
            {
                m_socket.Close();
                if (reuseSocket)
                    m_socket = CreateSocket();
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (!m_isDisposed)
            {
                m_socket.Close();
                m_isDisposed = true;
            }
        }
    }
}
