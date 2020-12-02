using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityNet;
using UnityNet.Tcp;

namespace UnityNetTest.TcpTests
{
    public class TcpSocketTests
    {
        [Test]
        public void Constructor1Test()
        {
            TcpSocket sock = new TcpSocket();

            Assert.AreEqual(sock.Blocking, false);
            Assert.AreEqual(sock.Connected, false);
            Assert.AreEqual(sock.RemoteAddress, IPAddress.None);
        }

        [Test]
        public void ConstructorFaultTest()
        {
            Assert.Catch<ArgumentNullException>(() =>
            { TcpSocket sock = new TcpSocket(null); });

            Assert.Catch(() =>
            { TcpSocket sock = new TcpSocket(new byte[1000]); });
        }

        [Test]
        public void NoConnectTest()
        {
            TcpSocket sock = new TcpSocket();
            var result = sock.Connect("localhost", 1002);

            Assert.AreEqual(result, SocketStatus.Error);
        }

        [Test]
        public void DoubleConnectTest()
        {
            TcpSocket sock = new TcpSocket();
            sock.Connect("localhost", 1002);
            var result = sock.Connect("localhost", 1002);

            Assert.AreEqual(result, SocketStatus.Error);
        }

        [Test]
        public void SocketCloseTest()
        {
            TcpSocket sock = new TcpSocket();
            sock.Close(false);

            //Never connected, so should ignore inner socket dispose
            Assert.DoesNotThrow(() => { sock.Connect("localhost", 1002); });
        }

        [Test]
        public void SocketDisposeTest()
        {
            TcpSocket sock = new TcpSocket();
            sock.Dispose();

            Assert.Catch<InvalidOperationException>(() => { sock.Connect("localhost", 1002); });
        }
    }
}
