using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnityNet.Serialization;
using UnityNet.Utils;

namespace UnityNetTest.Packet
{
    public class GeneralTests
    {
        private unsafe NetPacket CreatePacket(int size)
        {
            IntPtr mem = Memory.Alloc(size);
            NetPacket packet = new NetPacket();
            packet.OnReceive(mem.ToPointer(), size);

            return packet;
        }

        [Test]
        public unsafe void CTorTest()
        {
            NetPacket bs = new NetPacket();
            Assert.IsTrue(bs.IsWriting);
            Assert.IsFalse(bs.IsReading);
            Assert.IsTrue(bs.Data == null);
        }

        [Test]
        public unsafe void CTorTest2()
        {
            NetPacket bs = new NetPacket(8);

            Assert.IsTrue(bs.IsWriting);
            Assert.AreEqual(16, bs.Capacity);
            Assert.AreEqual(0, bs.Size);

            bs.Dispose();
        }

        [Test]
        public void SizeTest()
        {
            NetPacket bs = new NetPacket();

            bs.WriteInt32(1, 28);
            Assert.AreEqual(4, bs.Size);

            bs.Dispose();
        }

        [Test]
        public void ResetReadTest1()
        {
            NetPacket bs = CreatePacket(10);

            Assert.AreEqual(10, bs.Size);
            Assert.AreEqual(0, bs.ReadPosition);
            Assert.IsTrue(bs.IsReading);

            bs.Dispose();
        }

        [Test]
        public void DisposeTest()
        {
            var pack = CreatePacket(12);
            Assert.AreEqual(12, pack.Size);
            Assert.AreEqual(16, pack.Capacity);

            pack.Dispose();
            Assert.AreEqual(0, pack.Size);
            Assert.AreEqual(0, pack.Capacity);
        }

        [Test]
        public void ExpandTest()
        {
            NetPacket bs = new NetPacket();

            bs.WriteLong(1);
            bs.WriteLong(2);

            Assert.AreEqual(16, bs.Size);
            Assert.AreEqual(16, bs.Capacity);

            bs.WriteLong(3);

            // Expect another resize.
            Assert.AreEqual(32, bs.Capacity);
            Assert.AreEqual(24, bs.Size);


            // Confirm data is still there.
            bs.Mode = SerializationMode.Reading;
            Assert.AreEqual(1, bs.ReadLong());
            Assert.AreEqual(2, bs.ReadLong());
            Assert.AreEqual(3, bs.ReadLong());

            bs.Dispose();
        }

        [Test]
        public void ZeroLargeTest()
        {
            NetPacket bs = new NetPacket();

            bs.Skip(20 << 3);

            Assert.AreEqual(20, bs.Size);
            Assert.AreEqual(24, bs.Capacity);
            bs.Dispose();
        }

        [Test]
        public void ReadInvalidateTest()
        {
            NetPacket bs = CreatePacket(4);

            Assert.IsTrue(bs.IsValid);

            bs.ReadInt32();
            Assert.IsTrue(bs.IsValid);

            bs.ReadInt32();
            Assert.IsFalse(bs.IsValid);

            // Confirm offset hasn't increased.
            Assert.AreEqual(4 * 8, bs.ReadPosition);

            bs.ResetRead();
            Assert.IsTrue(bs.IsValid);

            bs.Dispose();
        }

        [Test]
        public void WriteResizeTest()
        {
            NetPacket bs = new NetPacket(4);

            bs.WriteULong(123);
            Assert.IsTrue(bs.IsValid);

            Assert.AreEqual(8, bs.Size);

            bs.WriteInt32(321);
            Assert.IsTrue(bs.IsValid);

            Assert.AreEqual(12, bs.Size);
            Assert.AreEqual(16, bs.Capacity);

            // Confirm offset has increased.
            Assert.AreEqual(12, bs.Size);

            Assert.IsTrue(bs.IsValid);

            bs.Dispose();
        }

        [Test]
        public void ClearTest()
        {
            var packet = new NetPacket(12);

            // Write some data;
            for (int i = 0; i < 3; i++)
                packet.WriteInt32(i + 1);

            Assert.AreEqual(16, packet.Capacity);

            packet.Clear();

            Assert.AreEqual(16, packet.Capacity);

            // Confirm all values are 0.
            packet.Mode = SerializationMode.Reading;

            Assert.AreEqual(0, packet.ReadLong());
            Assert.AreEqual(0, packet.ReadInt32());
        }
    }
}
