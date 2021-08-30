using System;
using System.Net.Sockets;

namespace UnityNet.Serialization
{
    public abstract class UNetSocket : IDisposable
    {
        private Socket m_socket;
        private bool m_isDisposed;

        public UNetSocket()
        {

        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_isDisposed)
                return;

            if (!m_isDisposed)
            {
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

                m_isDisposed = true;
            }
        }

        public void Dispose()
            => Dispose(true);

        ~UNetSocket()
            => Dispose(false);
    }
}
