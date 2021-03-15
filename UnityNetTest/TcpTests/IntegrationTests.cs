using NUnit.Framework;
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
            listener.Listen(PORT);

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

            TcpListener listener = new TcpListener();
            listener.Listen(PORT);

            TcpSocket clientSock = new TcpSocket();
            var connectionResult = clientSock.ConnectAsync("localhost", PORT).Result;

            var status = listener.Accept(out TcpSocket serverSock);

            NetPacket packet = new NetPacket();
            NetPacket clientPacket = new NetPacket();

            packet.WriteString(serverMessage);

            // Send message to client.
            Assert.AreEqual(SocketStatus.Done, serverSock.Send(ref packet));

            // Read message from server.
            Assert.AreEqual(SocketStatus.Done, clientSock.Receive(ref clientPacket));
            Assert.AreEqual(serverMessage, clientPacket.ReadString());

            // Send message back to server.
            clientPacket.Clear(SerializationMode.Writing);
            clientPacket.WriteString(clientMessage);
            Assert.AreEqual(SocketStatus.Done, clientSock.Send(ref clientPacket));

            // Read message from client.
            Assert.AreEqual(SocketStatus.Done, serverSock.Receive(ref packet));
            Assert.AreEqual(clientMessage, packet.ReadString());

            listener.Dispose();
            clientSock.Dispose();
            serverSock.Dispose();
            packet.Dispose();
            clientPacket.Dispose();
        }
    }
}
