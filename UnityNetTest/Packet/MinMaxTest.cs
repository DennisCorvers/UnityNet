using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnityNet.Serialization;

namespace UnityNetTest.Packet
{
    [TestFixture]
    internal class MinMaxTest
    {
        private NetPacket packet = new NetPacket();

        [SetUp]
        public void Setup()
        {
            packet.Clear();
            packet.Mode = SerializationMode.Writing;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            packet.Dispose();
        }


        [TestCase(-1532)]
        public void ReadWriteIntTest(int value)
        {
            const int min = -2000, max = 0;

            packet.WriteInt32(value, min, max);
            packet.ResetRead();
            Assert.AreEqual(value, packet.ReadInt32(min, max));
        }

        [TestCase(351)]
        public void ReadWriteUIntTest(int value)
        {
            const uint min = 0, max = 500;

            packet.WriteUInt32((uint)value, min, max);
            packet.ResetRead();
            Assert.AreEqual(value, packet.ReadUInt32(min, max));
        }

        [TestCase(1.4f)]
        public void ReadWriteFloatTest(float value)
        {
            const float min = -5, max = 5, prec = 0.2f;

            packet.WriteFloat(value, min, max, prec);
            packet.ResetRead();
            Assert.AreEqual(value, packet.ReadFloat(min, max, prec), 0.000005f);
        }

        [TestCase]
        public void ReadWriteMultiple()
        {
            packet.WriteInt32(-50, -100, 100);
            packet.WriteByte(98, 0, 100);
            packet.WriteBool(true);
            packet.WriteShort(-30, -50, 0);

            packet.ResetRead();

            Assert.AreEqual(-50, packet.ReadInt32(-100, 100));
            Assert.AreEqual(98, packet.ReadByte(0, 100));
            Assert.AreEqual(true, packet.ReadBool());
            Assert.AreEqual(-30, packet.ReadShort(-50, 0));
        }

        [TestCase(-1532)]
        public void SerializeIntTest(int value)
        {
            const int min = -2000, max = 0;
            int rep = 0;

            packet.Serialize(ref value, min, max);
            Assert.IsTrue(packet.Size > 0);
            packet.ResetRead();
            packet.Serialize(ref rep, min, max);

            Assert.AreEqual(value, rep);
        }

        [TestCase(351)]
        public void SerializeUIntTest(int value)
        {
            const uint min = 0, max = 500;
            uint val = (uint)value, rep = 0;

            packet.Serialize(ref val, min, max);
            Assert.AreEqual(true, packet.Size > 0);
            packet.ResetRead();
            packet.Serialize(ref rep, min, max);

            Assert.AreEqual(value, rep);
        }

        [TestCase]
        public void SerializeMultiple()
        {
            HelperStruct hlp = new HelperStruct(-50, 98, true, -30);

            packet.Serialize(ref hlp.intVal, -100, 100);
            packet.Serialize(ref hlp.bytVal, 0, 100);
            packet.Serialize(ref hlp.bolVal);
            packet.Serialize(ref hlp.srtVal, -50, 0);

            packet.ResetRead();

            HelperStruct rep = new HelperStruct();
            packet.Serialize(ref rep.intVal, -100, 100);
            packet.Serialize(ref rep.bytVal, 0, 100);
            packet.Serialize(ref rep.bolVal);
            packet.Serialize(ref rep.srtVal, -50, 0);

            Assert.AreEqual(hlp, rep);
        }

        private struct HelperStruct
        {
            public int intVal;
            public byte bytVal;
            public bool bolVal;
            public short srtVal;

            public HelperStruct(int intVal, byte bytVal, bool bolVal, short srtVal)
            {
                this.intVal = intVal;
                this.bytVal = bytVal;
                this.bolVal = bolVal;
                this.srtVal = srtVal;
            }
        }
    }
}
