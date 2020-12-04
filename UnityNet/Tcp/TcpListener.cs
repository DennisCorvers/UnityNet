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
        private bool m_isDisposed = false;
        private bool m_isActive = false;
        private byte[] m_sharedBuffer = null;
        private BufferOptions m_bufferOptions;
        private Socket m_socket;
#pragma warning restore IDE0032, IDE0044

        /// <summary>
        /// Indicates if the listener is listening on a port.
        /// </summary>
        public bool IsActive
        {
            get { return m_isActive; }
        }
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
        /// Creates a TcpListener.
        /// </summary>
        public TcpListener()
        {
            m_socket = CreateListener();

            m_sharedBuffer = new byte[TcpSocket.BUFFER_SIZE];
            m_bufferOptions = new BufferOptions(TcpSocket.BUFFER_SIZE, true);
        }

        /// <summary>
        /// Creates a TcpListener with a user-defined buffer.
        /// </summary>
        /// <param name="sharedBuffer">The Read/Write buffer that the connected sockets will use.</param>
        public TcpListener(byte[] sharedBuffer)
        {
            if (sharedBuffer == null)
                throw new ArgumentNullException();

            if (sharedBuffer.Length < 1024)
                throw new ArgumentException("Buffer needs to have a minimum size of 1024.");

            m_socket = CreateListener();

            m_sharedBuffer = sharedBuffer;
            m_bufferOptions = new BufferOptions((ushort)sharedBuffer.Length, true);
        }

        /// <summary>
        /// Creates a TcpListener.
        /// </summary>
        /// <param name="bufferOptions">The options for the buffer that the connected sockets will use.</param>
        public TcpListener(BufferOptions bufferOptions)
        {
            m_socket = CreateListener();

            m_sharedBuffer = null;
            m_bufferOptions = bufferOptions;

            if (bufferOptions.UseSharedBuffer)
                m_sharedBuffer = new byte[bufferOptions.BufferSize];
        }

        ~TcpListener()
        {
            Dispose();
        }

        private Socket CreateListener()
        {
            var sock = new Socket(AddressFamily.InterNetworkV6, System.Net.Sockets.SocketType.Stream, ProtocolType.Tcp)
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
        ///
        /// If the socket is already listening on a port when this
        /// function is called, it will stop listening on the old
        /// port before starting to listen on the new port.
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
        ///
        /// If the socket is already listening on a port when this
        /// function is called, it will stop listening on the old
        /// port before starting to listen on the new port.
        /// </summary>
        /// <param name="port">Port to listen on for incoming connection attempts</param>
        /// <param name="address">Address of the interface to listen on</param>
        public SocketStatus Listen(ushort port, IPAddress address)
        {
            if ((address == IPAddress.None) || (address == IPAddress.Broadcast))
                return SocketStatus.Error;

            return Listen(new IPEndPoint(address, port));

        }

        /// <summary>
        /// Start listening for incoming connection attempts
        ///
        /// This function makes the socket start listening on the
        /// specified port, waiting for incoming connection attempts.
        ///
        /// If the socket is already listening on a port when this
        /// function is called, it will stop listening on the old
        /// port before starting to listen on the new port.
        /// </summary>
        /// <param name="iPEndPoint">Endpoint of the interface to listen on</param>
        public SocketStatus Listen(IPEndPoint endpoint)
        {
            if (m_isActive)
            {
                Logger.Error(ExceptionMessages.LISTENER_BOUND);
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
                Logger.Error(ExceptionMessages.NOT_LISTENING);
                socket = null;
                return SocketStatus.Error;
            }

            if (!m_socket.Poll(0, SelectMode.SelectRead))
            {
                socket = null;
                return SocketStatus.Disconnected;
            }

            socket = CreateTcpSocket(m_socket.Accept());
            return SocketStatus.Done;
        }

        private TcpSocket CreateTcpSocket(Socket socket)
        {
            if (m_bufferOptions.UseSharedBuffer)
                return new TcpSocket(socket, m_sharedBuffer);
            else
                return new TcpSocket(socket, m_bufferOptions.BufferSize);
        }

        /// <summary>
        /// Closes the network connection.
        /// </summary>
        public void Close(bool reuseListener)
        {
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
