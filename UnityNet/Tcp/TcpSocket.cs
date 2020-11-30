using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityNet.Utils;

namespace UnityNet.Tcp
{
    public class TcpSocket : UNetSocket
    {
        /// <summary>
        /// Get the port to which the socket is remotely connected.
        /// If the socket is not connected, this property returns 0.
        /// </summary>
        public ushort RemotePort
        {
            get
            {
                if (m_endpoint == null)
                    return 0;
                return (ushort)m_endpoint.Port;
            }
        }
        /// <summary>
        /// The local port of the socket.
        /// </summary>
        public ushort LocalPort
        {
            private set; get;
        }
        /// <summary>
        /// Get the the address to which the socket is remotely connected.
        /// </summary>
        public IPAddress RemoteAddress
        {
            get
            {
                if (m_endpoint == null)
                    return IPAddress.None;
                return m_endpoint.Address;
            }
        }

        /// <summary>
        /// Gets or sets the size of the receive buffer in bytes.
        /// </summary>
        public int ReceiveBufferSize
        {
            get
            { return Socket.ReceiveBufferSize; }
            set
            { Socket.ReceiveBufferSize = value; }
        }
        /// <summary>
        /// Gets or sets the size of the send buffer in bytes.
        /// </summary>
        public int SendBufferSize
        {
            get
            { return Socket.SendBufferSize; }
            set
            { Socket.SendBufferSize = value; }
        }
        /// <summary>
        /// Gets or sets the receive time out value of the connection in seconds.
        /// </summary>
        protected int ReceiveTimeout
        {
            get
            { return Socket.ReceiveTimeout; }
            set
            { Socket.ReceiveTimeout = value; }
        }
        /// <summary>
        /// Gets or sets the send time out value of the connection in seconds.
        /// </summary>
        protected int SendTimeout
        {
            get
            { return Socket.SendTimeout; }
            set
            { Socket.SendTimeout = value; }
        }

        public int Available
        {
            get
            { return Socket.Available; }
        }
        public bool Connected
        {
            get
            { return Socket.Connected; }
        }
        public bool ExclusiveAddressUse
        {
            get
            {
                return Socket.ExclusiveAddressUse;
            }
            set
            {
                Socket.ExclusiveAddressUse = value;
            }
        }

        public TcpSocket()
            : base(SocketType.TCP)
        { }

        internal TcpSocket(Socket socket)
            : base(socket)
        { }

        public SocketStatus Connect(IPAddress address, ushort port)
        {
            return Connect(new IPEndPoint(address, port), 0);
        }

        public SocketStatus Connect(IPEndPoint endpoint)
        {
            return Connect(endpoint, 0);
        }

        public SocketStatus Connect(IPAddress address, ushort port, uint timeout)
        {
            return Connect(new IPEndPoint(address, port), timeout);
        }

        public SocketStatus Connect(string hostname, ushort port)
        {
            if (hostname == null)
                throw new ArgumentNullException("hostname");

            IPAddress[] addresses = Dns.GetHostAddresses(hostname);
            foreach (var address in addresses)
            {
                return Connect(address, port);
            }

            Logger.Error("No IpAddress for hostname " + hostname);
            return SocketStatus.Error;
        }

        private SocketStatus Connect(IPEndPoint endpoint, uint timeout)
        {
            //Always disconnect first...
            Close();

            m_endpoint = endpoint ?? throw new ArgumentNullException("endpoint");

            if (timeout == 0)
            {
                try
                {
                    Socket.Connect(endpoint);
                }
                catch (Exception ex)
                {
                    Close();
                    Logger.Error(ex);
                    return SocketStatus.Error;
                }

                return SocketStatus.Done;
            }
            else
            {
                //TODO Connect with delay?
                //Save the previous blocking state
                bool blocking = Blocking;

                Blocking = false;
                try
                {
                    Socket.Connect(endpoint);
                }
                catch (Exception ex)
                {
                    Close();
                    Logger.Error(ex);
                    return SocketStatus.Error;
                }
            }

            return SocketStatus.Error;
        }
    }
}
