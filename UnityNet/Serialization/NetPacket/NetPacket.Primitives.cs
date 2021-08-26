using System.Runtime.CompilerServices;
using UnityNet.Compression;

namespace UnityNet.Serialization
{
    public unsafe partial class NetPacket
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDouble()
        {
            ulong val = Read(64);
            return *(double*)&val;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadFloat()
        {
            uint val = unchecked((uint)Read(32));
            return *(float*)&val;

        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal ReadDecimal()
        {
            decimal result;
            ((ulong*)&result)[0] = Read(sizeof(ulong) * 8);
            ((ulong*)&result)[1] = Read(sizeof(ulong) * 8);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBool()
        {
            return Read(1) == 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte(int bitCount = 8)
        {
            return (sbyte)ZigZag.Zag(ReadUInt32(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadShort(int bitCount = 16)
        {
            return (short)ZigZag.Zag(ReadUInt32(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32(int bitCount = 32)
        {
            return ZigZag.Zag(ReadUInt32(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadLong(int bitCount = 64)
        {
            return ZigZag.Zag(ReadULong(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte(int bitCount = 8)
        {
            return unchecked((byte)Read(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUShort(int bitCount = 16)
        {
            return unchecked((ushort)Read(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32(int bitCount = 32)
        {
            return unchecked((uint)Read(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong ReadULong(int bitCount = 64)
        {
            return unchecked(Read(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char ReadChar(int bitCount = 16)
        {
            return unchecked((char)Read(bitCount));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double PeekDouble()
        {
            ulong val = Peek(64);
            return *(double*)&val;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float PeekFloat()
        {
            ulong val = Peek(32);
            return *(float*)&val;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public decimal PeekDecimal()
        {
            decimal result;
            ((ulong*)&result)[0] = Peek(sizeof(ulong) * 8);
            ((ulong*)&result)[1] = Peek(sizeof(ulong) * 8);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool PeekBool()
        {
            return Peek(1) == 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte PeekSByte(int bitCount = 8)
        {
            return (sbyte)ZigZag.Zag(PeekUInt32(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short PeekShort(int bitCount = 16)
        {
            return (short)ZigZag.Zag(PeekUInt32(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PeekInt32(int bitCount = 32)
        {
            return ZigZag.Zag(PeekUInt32(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long PeekLong(int bitCount = 64)
        {
            return ZigZag.Zag(PeekULong(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte PeekByte(int bitCount = 8)
        {
            return unchecked((byte)Peek(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort PeekUShort(int bitCount = 16)
        {
            return unchecked((ushort)Peek(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint PeekUInt32(int bitCount = 32)
        {
            return unchecked((uint)Peek(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong PeekULong(int bitCount = 64)
        {
            return unchecked(Peek(bitCount));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public char PeekChar(int bitCount = 16)
        {
            return unchecked((char)Peek(bitCount));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDouble(double value)
        {
            Write(*(ulong*)&value, sizeof(double) * 8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFloat(float value)
        {
            Write(*(uint*)&value, sizeof(float) * 8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDecimal(decimal value)
        {
            Write(((ulong*)&value)[0], sizeof(ulong) * 8);
            Write(((ulong*)&value)[1], sizeof(ulong) * 8);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBool(bool value)
        {
            Write(value ? (ulong)1 : 0, 1);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteSByte(sbyte value, int bitCount = 8)
        {
            Write(ZigZag.Zig(value), bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteShort(short value, int bitCount = 16)
        {
            Write(ZigZag.Zig(value), bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(int value, int bitCount = 32)
        {
            Write(ZigZag.Zig(value), bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLong(long value, int bitCount = 64)
        {
            Write(ZigZag.Zig(value), bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value, int bitCount = 8)
        {
            Write(value, bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUShort(ushort value, int bitCount = 16)
        {
            Write(value, bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32(uint value, int bitCount = 32)
        {
            Write(value, bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteULong(ulong value, int bitCount = 64)
        {
            Write(value, bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteChar(char value, int bitCount = 16)
        {
            Write(value, bitCount);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(ref double value)
        {
            if (m_mode == SerializationMode.Writing) WriteDouble(value);
            else value = ReadDouble();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(ref float value)
        {
            if (m_mode == SerializationMode.Writing) WriteFloat(value);
            else value = ReadFloat();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(ref decimal value)
        {
            if (m_mode == SerializationMode.Writing) WriteDecimal(value);
            else value = ReadDecimal();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(ref bool value)
        {
            if (m_mode == SerializationMode.Writing) WriteBool(value);
            else value = ReadBool();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(ref sbyte value, int bitCount = 8)
        {
            if (m_mode == SerializationMode.Writing) WriteSByte(value, bitCount);
            else value = ReadSByte(bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(ref byte value, int bitCount = 8)
        {
            if (m_mode == SerializationMode.Writing) WriteByte(value, bitCount);
            else value = ReadByte(bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(ref short value, int bitCount = 16)
        {
            if (m_mode == SerializationMode.Writing) WriteShort(value, bitCount);
            else value = ReadShort(bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(ref ushort value, int bitCount = 16)
        {
            if (m_mode == SerializationMode.Writing) WriteUShort(value, bitCount);
            else value = ReadUShort(bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(ref int value, int bitCount = 32)
        {
            if (m_mode == SerializationMode.Writing) WriteInt32(value, bitCount);
            else value = ReadInt32(bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(ref uint value, int bitCount = 32)
        {
            if (m_mode == SerializationMode.Writing) WriteUInt32(value, bitCount);
            else value = ReadUInt32(bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(ref long value, int bitCount = 64)
        {
            if (m_mode == SerializationMode.Writing) WriteLong(value, bitCount);
            else value = ReadLong(bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(ref ulong value, int bitCount = 64)
        {
            if (m_mode == SerializationMode.Writing) WriteULong(value, bitCount);
            else value = ReadULong(bitCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(ref char value, int bitCount = 16)
        {
            if (m_mode == SerializationMode.Writing) WriteChar(value, bitCount);
            else value = ReadChar(bitCount);
        }
    }
}
