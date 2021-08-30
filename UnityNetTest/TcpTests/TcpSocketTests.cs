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
        public void DoubleConnectTest()
        {
            TcpSocket sock = new TcpSocket();
            sock.ConnectAsync("localhost", 1002).Wait();
            var result = sock.ConnectAsync("localhost", 1002).Result;

            Assert.AreEqual(result, SocketStatus.Error);
        }

        [Test]
        public void SocketCloseTest()
        {
            TcpSocket sock = new TcpSocket();
            sock.Close();

            //Never connected, so should ignore inner socket dispose
            Assert.DoesNotThrow(() => { sock.ConnectAsync("localhost", 1002).Wait(0); });
        }

        [Test]
        public void SocketDisposeTest()
        {
            TcpSocket sock = new TcpSocket();
            sock.Dispose();

            Assert.Catch<ObjectDisposedException>(() =>
            {
                try
                {
                    sock.ConnectAsync("localhost", 1002).Wait(0);
                }
                catch(AggregateException e)
                {
                    throw e.InnerException;
                }
            });
        }
    }
}
