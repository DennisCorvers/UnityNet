using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnityNet.Serialization;

namespace UnityNetTest.Packet
{
    [TestFixture]
    public class StringTests
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

        [TestCase("MyString")]
        public void ASCIITest(string value)
        {
            packet.WriteString(value, Encoding.ASCII);
            Assert.AreEqual(sizeof(ushort) + value.Length, packet.Size);

            packet.ResetRead();
            Assert.AreEqual(value, packet.ReadString(Encoding.ASCII));
        }
        [TestCase("手機瀏覽")]
        public void UTF16Test(string value)
        {
            packet.WriteString(value, Encoding.Unicode);
            Assert.AreEqual(sizeof(ushort) + value.Length * 2, packet.Size);

            packet.ResetRead();
            Assert.AreEqual(value, packet.ReadString(Encoding.Unicode));
        }

        [TestCase("手機瀏覽")]
        [TestCase("MyString")]
        public void UTF8Test(string value)
        {
            packet.WriteString(value, Encoding.UTF8);
            packet.ResetRead();

            Assert.AreEqual(value, packet.ReadString(Encoding.UTF8));
        }

        [TestCase("手機瀏覽")]
        [TestCase("HelloWorld!")]
        public void CharArrayTest(string value)
        {
            char[] arr = value.ToCharArray();
            char[] rep = new char[16];

            packet.WriteString(arr, BitEncoding.UTF16);
            packet.ResetRead();

            int charCount = packet.ReadString(rep, BitEncoding.UTF16);

            Assert.AreEqual(charCount, value.Length);
            string repStr = new string(rep, 0, charCount);
            Assert.AreEqual(value, repStr);
        }

        [Test]
        public void CharArrayOffsetTest()
        {
            const string value = "myTestString";

            char[] arr = value.ToCharArray();
            char[] rep = new char[16];

            packet.WriteString(arr, 2, 4, BitEncoding.UTF16);
            packet.ResetRead();

            int charCount = packet.ReadString(rep, BitEncoding.UTF16);

            Assert.AreEqual(4, charCount);
            string repStr = new string(rep, 0, charCount);
            Assert.AreEqual("Test", repStr);
        }

        [Test]
        public void CharArraySmallTest()
        {
            CharArraySmall(BitEncoding.ASCII);
            CharArraySmall(BitEncoding.UTF16);
        }

        private void CharArraySmall(BitEncoding encoding)
        {
            packet = new NetPacket();

            char[] rep = new char[4];

            packet.WriteString("TestString", encoding);
            packet.ResetRead();

            int charCount = packet.ReadString(rep, encoding);

            Assert.AreEqual(charCount, rep.Length);
            string repStr = new string(rep, 0, charCount);
            Assert.AreEqual("Test", repStr);

            packet.Dispose();
        }

        [TestCase("手機瀏覽")]
        [TestCase("HelloWorld!")]
        public unsafe void MixedSerializeTest(string value)
        {
            fixed (char* ptr = value)
            {
                packet.WriteString(ptr, value.Length, BitEncoding.UTF16);
            }

            string replica = "";
            packet.ResetRead();

            packet.Serialize(ref replica, BitEncoding.UTF16);

            Assert.AreEqual(value, replica);
        }

        [TestCase("手機瀏覽")]
        [TestCase("HelloWorld!")]
        public void StringSerializeTest(string value)
        {
            string replica = "";
            packet.Serialize(ref value, Encoding.UTF32);
            packet.ResetRead();

            packet.Serialize(ref replica, Encoding.UTF32);

            Assert.AreEqual(value, replica);
        }

        [Test]
        public void FStringASCIITest()
        {
            string value = "12345678";

            packet.WriteString(value, BitEncoding.ASCII);
            Assert.AreEqual(10, packet.Size);

            packet.ResetRead();

            Assert.AreEqual(value, packet.ReadString(BitEncoding.ASCII));
            Assert.AreEqual(10 * 8, packet.ReadPosition);
        }

        [Test]
        public void ASCIIOOBTest()
        {
            string value = "÷23456789÷";

            packet.WriteString(value, BitEncoding.ASCII);
            Assert.AreEqual(12, packet.Size);

            packet.ResetRead();

            Assert.AreEqual("?23456789?", packet.ReadString(BitEncoding.ASCII));
            Assert.AreEqual(12 * 8, packet.ReadPosition);
        }

        [Test]
        public void NonASCIICompressed()
        {
            string value = "÷23456789÷";
            double size = 16 + (value.Length * 7);

            packet.WriteString(value, BitEncoding.ASCIICompressed);
            Assert.AreEqual(size, packet.WritePosition);

            packet.ResetRead();

            Assert.AreNotEqual("?23456789?", packet.ReadString(BitEncoding.ASCIICompressed));
            Assert.AreEqual(size, packet.ReadPosition);
        }

        [Test]
        public void ASCIICompressed()
        {
            string value = "1234567890";
            double size = 16 + value.Length * 7;

            packet.WriteString(value, BitEncoding.ASCIICompressed);
            Assert.AreEqual(size, packet.WritePosition);

            packet.ResetRead();

            Assert.AreEqual(value, packet.ReadString(BitEncoding.ASCIICompressed));
            Assert.AreEqual(size, packet.ReadPosition);
        }

        [Test]
        public void FStringUTF16Test()
        {
            string value = "12345678";

            packet.WriteString(value);
            Assert.AreEqual(18, packet.Size);

            packet.ResetRead();

            Assert.AreEqual(value, packet.ReadString());
            Assert.AreEqual(18 * 8, packet.ReadPosition);
        }

        [TestCase("手機瀏覽")]
        [TestCase("HelloWorld!")]
        public void FUTF16SpecialCharTest(string value)
        {
            packet.WriteString(value);
            Assert.AreEqual(sizeof(ushort) + value.Length * 2, packet.Size);

            packet.ResetRead();
            Assert.AreEqual(value, packet.ReadString(Encoding.Unicode));
        }

        [TestCase("手機瀏覽", BitEncoding.UTF16)]
        [TestCase("HelloWorld!", BitEncoding.UTF16)]
        [TestCase("HelloWorld!", BitEncoding.ASCII)]
        public void FStringSerializeTest(string value, BitEncoding encoding)
        {
            string replica = "";
            packet.Serialize(ref value, encoding);
            packet.ResetRead();

            packet.Serialize(ref replica, encoding);

            Assert.AreEqual(value, replica);
        }
    }
}
