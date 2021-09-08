using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnityNet;
using UnityNet.Serialization;
using UnityNet.Tcp;

namespace UnityNetTest.TcpTests
{
    public class CustomPacketTests
    {
        private const ushort PORT = 666;

        [Test]
        public void SendReceiveTest()
        {
            const string serverMessage = "HelloFromServer";
            const string clientMessage = "ResponseFromClient";

            using (TcpListener listener = new TcpListener())
            {
                listener.Blocking = true;
                listener.Listen(PORT);

                TcpSocket clientSock = new TcpSocket();
                var connectionResult = clientSock.ConnectAsync("localhost", PORT).Result;

                var status = listener.Accept(out TcpSocket serverSock);

                MyPacket packet = new MyPacket();
                MyPacket clientPacket = new MyPacket();

                packet.WriteString(serverMessage);

                // Send message to client.
                Assert.AreEqual(SocketStatus.Done, serverSock.Send(ref packet));

                // Read message from server.
                Assert.AreEqual(SocketStatus.Done, clientSock.Receive(ref clientPacket));
                Assert.AreEqual(serverMessage, clientPacket.ReadString());

                // Send message back to server.
                clientPacket.WriteString(clientMessage);
                Assert.AreEqual(SocketStatus.Done, clientSock.Send(ref clientPacket));

                // Read message from client.
                Assert.AreEqual(SocketStatus.Done, serverSock.Receive(ref packet));
                Assert.AreEqual(clientMessage, packet.ReadString());

                clientSock.Dispose();
                serverSock.Dispose();
            }
        }


        private class MyPacket : IPacket
        {
            private byte[] m_buffer;

            public ReadOnlySpan<byte> Data
                => new ReadOnlySpan<byte>(m_buffer);

            public int SendOffset
            { get; set; }

            public void Receive(ReadOnlySpan<byte> data)
            {
                Array.Resize(ref m_buffer, data.Length);
                data.CopyTo(new Span<byte>(m_buffer));
            }

            public void WriteString(string value)
            {
                int byteCount = Encoding.UTF8.GetByteCount(value);
                Array.Resize(ref m_buffer, byteCount + 4);

                Write(m_buffer, 0, byteCount);
                Encoding.UTF8.GetBytes(value, 0, value.Length, m_buffer, 4);
            }

            public unsafe string ReadString()
            {
                var stringByteLength = Read<int>(m_buffer, 0);

                fixed (byte* ptr = m_buffer)
                {
                    return new string((sbyte*)ptr, 4, stringByteLength, Encoding.UTF8);
                }
            }

            public unsafe void Write<T>(byte[] dest, int offset, T value)
                where T : unmanaged
            {
                fixed (byte* ptr = &dest[offset])
                {
                    *(T*)ptr = value;
                }
            }

            public unsafe T Read<T>(byte[] bin, int offset)
                where T : unmanaged
            {
                fixed (byte* ptr = &bin[offset])
                {
                    return *(T*)ptr;
                }
            }
        }
    }
}
