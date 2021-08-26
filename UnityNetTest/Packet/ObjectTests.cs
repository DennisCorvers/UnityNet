using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnityNet.Serialization;

namespace UnityNetTest.Packet
{
    public unsafe class ObjectTests
    {
        private string m_name;
        private int m_age;
        private float m_height;


        public ObjectTests()
        {
            m_name = Guid.NewGuid().ToString();
            Random r = new Random();
            m_age = r.Next();
            int next = r.Next();
            m_height = *(float*)&next;
        }

        [Test]
        public void SerializeReferenceTest()
        {
            var obj = new SomeObject(m_name, m_age);

            NetPacket packet = new NetPacket();
            packet.Serialize(ref obj);

            Assert.Greater(packet.Size, 0);

            packet.ResetRead();
            var replica = new SomeObject();
            packet.Serialize(ref replica);

            Assert.AreEqual(obj, replica);
        }

        [Test]
        public void SerializeValueTest()
        {
            var obj = new SomeStruct(m_name, m_age);

            NetPacket packet = new NetPacket();
            packet.Serialize(ref obj);

            Assert.Greater(packet.Size, 0);

            packet.ResetRead();
            var replica = new SomeStruct();
            packet.Serialize(ref replica);

            Assert.AreEqual(obj, replica);
        }

        [Test]
        public void SerializeDerivedTest()
        {
            var obj = new SomeChild(m_name, m_age, 1.80f);

            NetPacket packet = new NetPacket();
            packet.Serialize(ref obj);

            Assert.Greater(packet.Size, 0);

            packet.ResetRead();
            var replica = new SomeChild();
            packet.Serialize(ref replica);

            Assert.AreEqual(obj, replica);
        }

        private class SomeObject : INetSerializable
        {
            public string Name;
            public int Age;

            public SomeObject() { }

            public SomeObject(string name, int age)
            {
                Name = name;
                Age = age;
            }

            public override bool Equals(object obj)
            {
                var @object = obj as SomeObject;
                return @object != null &&
                       Name == @object.Name &&
                       Age == @object.Age;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Name, Age);
            }

            public virtual void Serialize(NetPacket packet)
            {
                if (packet.Mode == SerializationMode.Reading)
                {
                    Name = packet.ReadString();
                    Age = packet.ReadInt32();
                }
                else
                {
                    packet.WriteString(Name);
                    packet.WriteInt32(Age);
                }
            }
        }

        private class SomeChild : SomeObject
        {
            public float Height;

            public SomeChild() { }

            public SomeChild(string name, int age, float height)
                : base(name, age)
            {
                Height = height;
            }

            public override void Serialize(NetPacket packet)
            {
                base.Serialize(packet);
                if (packet.IsReading)
                {
                    Height = packet.ReadFloat();
                }
                else
                {
                    packet.WriteFloat(Height);
                }
            }

            public override bool Equals(object obj)
            {
                var child = obj as SomeChild;
                return child != null &&
                       base.Equals(obj) &&
                       Height == child.Height;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(base.GetHashCode(), Height);
            }
        }

        private struct SomeStruct : INetSerializable
        {
            public string Name;
            public int Age;

            public SomeStruct(string name, int age)
            {
                Name = name;
                Age = age;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is SomeStruct))
                {
                    return false;
                }

                var @struct = (SomeStruct)obj;
                return Name == @struct.Name &&
                       Age == @struct.Age;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Name, Age);
            }

            public void Serialize(NetPacket packet)
            {
                if (packet.Mode == SerializationMode.Reading)
                {
                    Name = packet.ReadString();
                    Age = packet.ReadInt32();
                }
                else
                {
                    packet.WriteString(Name);
                    packet.WriteInt32(Age);
                }
            }
        }
    }
}
