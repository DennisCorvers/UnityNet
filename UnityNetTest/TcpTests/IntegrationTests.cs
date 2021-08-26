using NUnit.Framework;
using System;
using System.Net;
using System.Threading.Tasks;
using UnityNet;
using UnityNet.Serialization;
using UnityNet.Tcp;

namespace UnityNetTest.TcpTests
{
    public class IntegrationTests
    {
        private const int PORT = 666;

        [Test]
        public async Task ConnectTest()
        {
            TcpListener listener = new TcpListener();
            listener.Listen(666);

            TcpSocket clientSock = new TcpSocket();
            await clientSock.ConnectAsync("localhost", PORT);

            var status = listener.Accept(out TcpSocket serverSock);

            Assert.AreEqual(status, SocketStatus.Done);
            Assert.NotNull(serverSock);

            listener.Stop();
            clientSock.Close();
            serverSock.Close();
        }

        [Test]
        public void ListenerRestartTest()
        {
            TcpListener listener = new TcpListener();
            listener.Listen(PORT);

            Assert.IsTrue(listener.IsActive);
            Assert.AreEqual(PORT, listener.LocalPort);

            listener.Stop();
            Assert.IsFalse(listener.IsActive);
            Assert.AreEqual(0, listener.LocalPort);

            // Verify that it can listen again after inner socket closing.
            listener.Listen(PORT);
            Assert.IsTrue(listener.IsActive);
            Assert.AreEqual(PORT, listener.LocalPort);

            listener.Dispose();
        }

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

                NetPacket packet = new NetPacket();
                NetPacket clientPacket = new NetPacket();

                packet.WriteString(serverMessage);

                // Send message to client.
                Assert.AreEqual(SocketStatus.Done, serverSock.Send(packet));

                // Read message from server.
                Assert.AreEqual(SocketStatus.Done, clientSock.Receive(clientPacket));
                Assert.AreEqual(serverMessage, clientPacket.ReadString());

                // Send message back to server.
                clientPacket.Clear(SerializationMode.Writing);
                clientPacket.WriteString(clientMessage);
                Assert.AreEqual(SocketStatus.Done, clientSock.Send(clientPacket));

                // Read message from client.
                Assert.AreEqual(SocketStatus.Done, serverSock.Receive(packet));
                Assert.AreEqual(clientMessage, packet.ReadString());

                clientSock.Dispose();
                serverSock.Dispose();
                packet.Dispose();
                clientPacket.Dispose();
            }
        }

        [Test]
        public void RejectHugePacket()
        {
            using (TcpListener listener = new TcpListener())
            {
                listener.Blocking = true;
                listener.MaximumPacketSize = 1024;
                listener.Listen(PORT);

                TcpSocket clientSock = new TcpSocket();
                var connectionResult = clientSock.ConnectAsync("localhost", PORT).Result;

                var status = listener.Accept(out TcpSocket serverSock);
                Assert.AreEqual(SocketStatus.Done, status);

                var largePacket = new NetPacket();
                largePacket.WriteBytes(new byte[8192], true);

                while (clientSock.Send(largePacket) != SocketStatus.Done) ;

                Assert.AreEqual(SocketStatus.Disconnected, serverSock.Receive(largePacket));

                Assert.IsFalse(serverSock.Connected);

                clientSock.Dispose();
                serverSock.Dispose();
                largePacket.Dispose();
            }
        }

        // [Test]
        public void StressTest()
        {
            TcpListener listener = new TcpListener();
            listener.Listen(PORT);

            TcpSocket clientSock = new TcpSocket();
            var connectionResult = clientSock.ConnectAsync("localhost", PORT).Result;

            var status = listener.Accept(out TcpSocket serverSock);

            for (int i = 0; i < 1000000; i++)
            {
                NetPacket packet = new NetPacket();

                var messageTo = Guid.NewGuid().ToString();
                packet.WriteString(messageTo);
                while (serverSock.Send(packet) != SocketStatus.Done) ;

                packet.Clear();

                while (clientSock.Receive(packet) != SocketStatus.Done) ;

                Assert.AreEqual(messageTo, packet.ReadString());
                packet.ResetWrite();

                packet.WriteString($"Message with code {messageTo} received.");
                while (clientSock.Send(packet) != SocketStatus.Done) ;

                packet.Clear();

                while (serverSock.Receive(packet) != SocketStatus.Done) ;

                packet.Dispose();
            }

            listener.Dispose();
            clientSock.Dispose();
            serverSock.Dispose();
        }
    }
}
