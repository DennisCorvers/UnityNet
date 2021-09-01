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
            var span = new ReadOnlySpan<byte>(mem.ToPointer(), size);
            NetPacket packet = new NetPacket();
            packet.OnReceive(span);

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

        [TestCase(-1532)]
        public void ReadwriteTest(int value)
        {
            var packet = new NetPacket();
            packet.WriteInt32(value);
            Assert.AreEqual(32, packet.WritePosition);

            packet.ResetRead();
            Assert.AreEqual(value, packet.ReadInt32());
            Assert.AreEqual(32, packet.ReadPosition);

            packet.Dispose();
        }

        [TestCase(-1532)]
        public void ReadwriteFloat(float value)
        {
            var packet = new NetPacket();

            int val = (int)value;
            packet.WriteByte(0, 4);
            packet.WriteInt32(val);

            packet.ResetRead();
            packet.ReadByte(4);
            Assert.AreEqual(val, packet.ReadInt32());

            packet.Dispose();
        }

        [Test]
        public void ReadWriteMultipleTest()
        {
            var packet = new NetPacket();

            const bool bVal = true;
            const double dVal = double.MaxValue / 3 * 2;
            const float fVal = float.MinValue / 5;
            const short sVal = -12345;
            const int offset = 113;

            packet.WriteBool(bVal);
            packet.WriteDouble(dVal);
            packet.WriteFloat(fVal);
            packet.WriteShort(sVal);
            Assert.AreEqual(offset, packet.WritePosition);

            packet.ResetRead();
            Assert.AreEqual(bVal, packet.ReadBool());
            Assert.AreEqual(dVal, packet.ReadDouble());
            Assert.AreEqual(fVal, packet.ReadFloat());
            Assert.AreEqual(sVal, packet.ReadShort());
            Assert.AreEqual(offset, packet.ReadPosition);

            packet.Dispose();
        }
        [Test]
        public void ReadWriteSizeTest()
        {
            var packet = new NetPacket();

            const byte bVal = 100;
            const int iVal = -100;
            const byte bitSize = 7;

            packet.WriteByte(bVal, bitSize);
            packet.WriteInt32(iVal, bitSize + 1);
            Assert.AreEqual(bitSize * 2 + 1, packet.WritePosition);

            packet.ResetRead();
            Assert.AreEqual(bVal, packet.ReadByte(bitSize));
            Assert.AreEqual(iVal, packet.ReadInt32(bitSize + 1));
            Assert.AreEqual(bitSize * 2 + 1, packet.ReadPosition);

            packet.Dispose();
        }

        [TestCase(-29183742)]
        public void SerializeReadTest(int value)
        {
            var packet = new NetPacket();

            packet.WriteInt32(value);
            packet.ResetRead();

            int ret = 0;
            packet.Serialize(ref ret);
            Assert.AreEqual(ret, value);
            Assert.AreEqual(4, packet.Size);

            packet.Dispose();
        }

        [TestCase(-976)]
        public void SerializeWriteTest(int value)
        {
            var packet = new NetPacket();

            packet.Serialize(ref value);
            Assert.AreEqual(4, packet.Size);

            packet.ResetRead();
            Assert.AreEqual(value, packet.ReadInt32());

            packet.Dispose();
        }

        [TestCase(-1.6234f)]
        public void SerializeTest(float value)
        {
            var packet = new NetPacket();

            packet.Serialize(ref value);
            Assert.AreEqual(32, packet.WritePosition);

            packet.ResetRead();
            float ret = 0;
            packet.Serialize(ref ret);
            Assert.AreEqual(value, ret);
            Assert.AreEqual(32, packet.ReadPosition);

            packet.Dispose();
        }

        [Test]
        public void ReadWriteIntTest()
        {
            var packet = new NetPacket();

            int val = 123456789;

            packet.WriteInt32(val);

            Assert.AreEqual(4, packet.Size);

            packet.ResetRead();

            Assert.AreEqual(val, packet.ReadInt32());

            packet.Dispose();
        }
    }
}
