using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityNet;
using UnityNet.Serialization;
using UnityNet.Udp;

namespace UnityNetTest.UdpTests
{
    public class UdpIntegrationTests
    {
        private const ushort PORT = 666;

        [Test]
        public void SendReceiveTest()
        {
            const string serverMessage = "HelloFromServer";
            const string clientMessage = "ResponseFromClient";

            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 666);
            IPEndPoint serverEp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 666);
            UdpSocket client = new UdpSocket();
            UdpSocket server = new UdpSocket();

            server.Bind(666);
            Assert.AreEqual(SocketStatus.Done, client.Connect(serverEp));

            NetPacket packet = new NetPacket();
            NetPacket clientPacket = new NetPacket();

            packet.WriteString(serverMessage);

            // Send message to client.
            Assert.AreEqual(SocketStatus.Done, server.Send(packet, client.LocalEndpoint));

            // Read message from server.
            Assert.AreEqual(SocketStatus.Done, client.Receive(clientPacket, ref ep));
            Assert.AreEqual(serverMessage, clientPacket.ReadString());

            // Send message back to server.
            clientPacket.Clear(SerializationMode.Writing);
            clientPacket.WriteString(clientMessage);
            Assert.AreEqual(SocketStatus.Done, client.Send(clientPacket));

            // Read message from client.
            Assert.AreEqual(SocketStatus.Done, server.Receive(packet, ref ep));
            Assert.AreEqual(clientMessage, packet.ReadString());

            client.Dispose();
            server.Dispose();
            packet.Dispose();
            clientPacket.Dispose();
        }
    }
}
