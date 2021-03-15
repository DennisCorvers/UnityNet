using System;
using System.Net.Sockets;

namespace UnityNet.Udp
{
    public class UdpSocket : IDisposable
    {
        /// <summary>
        /// Maximum size of an UDP datagram.
        /// </summary>
        public const int MaxDatagramSize = 65507;

#pragma warning disable IDE0032, IDE0044
        private Socket m_socket;
        private readonly byte[] m_buffer;

        private bool m_isActive;
        private bool m_isClearedUp;
        private bool m_hasSharedBuffer;
#pragma warning restore IDE0032, IDE0044
        private AddressFamily m_addressFamily;

        /// <summary>
        /// Creates a new <see cref="TcpSocket"/> with an internal buffer.
        /// </summary>
        public UdpSocket()
            : this(new byte[MaxDatagramSize], AddressFamily.InterNetwork)
        { }

        /// <summary>
        /// Creates a new <see cref="TcpSocket"/> with an internal buffer.
        /// </summary>
        /// <param name="family">One of the System.Net.Sockets.AddressFamily values that specifies the addressing scheme of the socket.</param>
        public UdpSocket(AddressFamily family)
            : this(new byte[MaxDatagramSize], family)
        { }

        /// <summary>
        /// Creates a new <see cref="TcpSocket"/> with a user-defined buffer.
        /// </summary>
        /// <param name="buffer">The Send/Receive buffer.</param>
        public UdpSocket(byte[] buffer)
            : this(buffer, AddressFamily.InterNetwork)
        { }

        /// <summary>
        /// Creates a new <see cref="TcpSocket"/> with a user-defined buffer.
        /// </summary>
        /// <param name="buffer">The Send/Receive buffer.</param>
        /// <param name="family">One of the System.Net.Sockets.AddressFamily values that specifies the addressing scheme of the socket.</param>
        public UdpSocket(byte[] buffer, AddressFamily family)
        {
            if (family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6)
                throw new ArgumentException(("Client can only accept InterNetwork or InterNetworkV6 addresses."), nameof(family));

            m_addressFamily = family;
        }

        /// <summary>
        /// Configures a socket.
        /// </summary>
        private static Socket ConfigureSocket(Socket socket)
        {
            socket.Blocking = false;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, false);
            socket.SendBufferSize = ushort.MaxValue;
            socket.EnableBroadcast = true;

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
        /// Binds the <see cref="UdpSocket"/> to a specified port.
        /// </summary>
        /// <param name="port">The port to bind the <see cref="UdpSocket"/> to.</param>
        public SocketStatus Bind(int port)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
