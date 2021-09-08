using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityNet;
using UnityNet.Tcp;

namespace UnityNetTest.TcpTests
{
    public class TcpSocketTests
    {
        [Test]
        public void ConstructorTest()
        {
            TcpSocket sock = new TcpSocket();

            Assert.AreEqual(sock.Connected, false);
            Assert.AreEqual(sock.RemoteAddress, IPAddress.None);
        }

        [Test]
        public void ConstructorFaultTest()
        {
            Assert.Catch(() =>
            { TcpSocket sock = new TcpSocket(System.Net.Sockets.AddressFamily.AppleTalk); });
        }

        [Test]
        public void NoConnectTest()
        {
            TcpSocket sock = new TcpSocket();
            var result = sock.ConnectAsync("localhost", 1002).Result;

            Assert.AreEqual(result, SocketStatus.Error);
        }

        [Test]
        public async Task DoubleConnectTest()
        {
            TcpSocket sock = new TcpSocket();
            await sock.ConnectAsync("localhost", 1002);
            var result = await sock.ConnectAsync("localhost", 1002);

            Assert.AreEqual(result, SocketStatus.Error);
        }

        [Test]
        public void SocketCloseTest()
        {
            TcpSocket sock = new TcpSocket();
            sock.Close();

            Assert.DoesNotThrowAsync(() => { return sock.ConnectAsync("localhost", 1002).AsTask(); });
        }

        [Test]
        public void SocketDisposeTest()
        {
            TcpSocket sock = new TcpSocket();
            sock.Dispose();

            Assert.CatchAsync<ObjectDisposedException>(() => { return sock.ConnectAsync("localhost", 1002).AsTask(); });
        }
    }
}
