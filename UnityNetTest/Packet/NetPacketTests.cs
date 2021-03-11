using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnityNet.Serialization;
using UnityNet.Utils;

namespace UnityNetTest.Packet
{
    public unsafe class NetPacketTests
    {
        private unsafe NetPacket CreatePacket(int size)
        {
            NetPacket pack = new NetPacket();
            IntPtr data = Memory.Alloc(size);

            pack.OnReceive(data.ToPointer(), size);
            return pack;
        }

        [Test]
        public void ClearTest()
        {

        }

        [Test]
        public void OnReceiveEmptyTest()
        {
            var pack = CreatePacket(7);
            Assert.AreEqual(8, pack.ByteCapacity);
            Assert.AreEqual(7, pack.ByteSize);

            Assert.IsTrue(pack.IsValid);
            Assert.IsTrue(pack.Data != null);

            pack.Dispose();
        }

        [Test]
        public void OnReceiveResizeTest()
        {
            var pack = CreatePacket(8);
            Assert.AreEqual(8, pack.ByteCapacity);
            Assert.AreEqual(8, pack.ByteSize);

            var dataPtr = (ulong)pack.Data;

            // Must be larger than existing buffer.
            IntPtr ptr = Memory.Alloc(14);
            pack.OnReceive(ptr.ToPointer(), 14);

            Assert.IsFalse(dataPtr == (ulong)pack.Data);
            Assert.AreEqual(16, pack.ByteCapacity);
            Assert.AreEqual(14, pack.ByteSize);

            pack.Dispose();
        }

        [Test]
        public void OnReceiveTest()
        {
            var pack = CreatePacket(16);
            Assert.AreEqual(16, pack.ByteCapacity);
            Assert.AreEqual(16, pack.ByteSize);
            Assert.IsTrue(pack.IsValid);

            var dataPtr = (ulong)pack.Data;

            // Must be smaller than existing buffer.
            IntPtr ptr = Memory.Alloc(8);
            pack.OnReceive(ptr.ToPointer(), 8);

            Assert.IsTrue(dataPtr == (ulong)pack.Data);
            Assert.AreEqual(16, pack.ByteCapacity);
            Assert.AreEqual(8, pack.ByteSize);

            pack.Dispose();
        }

        [Test]
        public void PacketDisposeTest()
        {
            var pack = CreatePacket(5);

            Assert.AreEqual(8, pack.ByteCapacity);
            Assert.IsTrue(pack.Data != null);

            pack.Dispose();

            Assert.AreEqual(0, pack.ByteCapacity);
            Assert.IsTrue(pack.Data == null);
        }
    }
}
