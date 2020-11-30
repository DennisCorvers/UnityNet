using System;
using System.Net;
using System.Net.Sockets;
using UnityNet.Utils;

namespace UnityNet.Tcp
{
    public sealed class TcpListener : UNetSocket
    {
        public const int SOMAXCONN = ushort.MaxValue;
#pragma warning disable IDE0032, IDE0044
        private bool m_isActive = false;
#pragma warning restore IDE0032, IDE0044

        /// <summary>
        /// Indicates if the listener is listening on a port.
        /// </summary>
        private bool IsActive
        {
            get { return m_isActive; }
        }


        /// <summary>
        /// Get the port to which the socket is bound locally.
        /// If the socket is not listening to a port, this property returns 0.
        /// </summary>
        public ushort LocalPort
        {
            get
            {
                if (m_endpoint == null)
                    return 0;
                return (ushort)m_endpoint.Port;
            }
        }
        public IPAddress BoundAddress
        {
            get
            {
                if (m_endpoint == null)
                    return IPAddress.None;
                return m_endpoint.Address;
            }
        }

        public TcpListener()
            : base(SocketType.TCP)
        { }

        private TcpListener(IPAddress address, ushort port)
            : base(SocketType.TCP)
        {
            m_endpoint = new IPEndPoint(address, port);
        }

        /// <summary>
        /// Creates a TcpListener that listens on both IPv4 and IPv6 on the given port.
        /// </summary>
        /// <param name="port">The port to listen on.</param>
        public static TcpListener Create(ushort port)
        {
            var listener = new TcpListener(IPAddress.IPv6Any, port);
            listener.Socket.DualMode = true;
            return listener;
        }

        public void AllowNatTraversal(bool isallowed)
        {
            if (IsActive)
                Logger.Error(new InvalidOperationException("Tcp listener must be stopped."));

            else
            {
                if (isallowed)
                    Socket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
                else
                    Socket.SetIPProtectionLevel(IPProtectionLevel.EdgeRestricted);
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
            return Listen(new IPEndPoint(IPAddress.Any, port));
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
                Logger.Error("TcpListener is already bound. Call Close() first.");
                return SocketStatus.Error;
            }

            m_endpoint = endpoint ?? throw new ArgumentNullException("endpoint");

            Socket.Bind(m_endpoint);
            try
            {
                Socket.Listen(SOMAXCONN);
            }
            catch (Exception ex)
            {
                Close();
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
            if (!Socket.Poll(0, SelectMode.SelectRead))
            {
                Logger.Error("Failed to accept a new connection, the socket is not listening.");

                socket = null;
                return SocketStatus.Error;
            }

            socket = new TcpSocket(Socket.Accept());
            return SocketStatus.Done;
        }

        public override void Close()
        {
            Socket.Close();
            Socket = null;

            m_isActive = false;
            Socket = CreateTcpSocket();
        }
    }
}
