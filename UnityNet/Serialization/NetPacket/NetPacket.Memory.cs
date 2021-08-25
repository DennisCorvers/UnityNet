using System;

namespace UnityNet.Serialization
{
    public unsafe partial struct NetPacket
    {
        private void WriteMemoryUnchecked(void* ptr, int byteSize)
        {
            long c = byteSize >> 3; // longs
            ulong* lp = (ulong*)ptr;
            byte* bp = (byte*)ptr;

            int i = 0;
            for (; i < c; i++)
                WriteUnchecked(lp[i], 64);

            i = i << 3;
            for (; i < byteSize; i++)
                WriteUnchecked(bp[i], 8);
        }

        private void ReadMemoryUnchecked(void* ptr, int byteSize)
        {
            long c = byteSize >> 3; // longs
            ulong* lp = (ulong*)ptr;
            byte* bp = (byte*)ptr;

            int i = 0;
            for (; i < c; i++)
                lp[i] = ReadUnchecked(64);

            i = i << 3;
            for (; i < byteSize; i++)
                bp[i] = unchecked((byte)ReadUnchecked(8));
        }

        /// <summary>
        /// Writes raw data to the <see cref="NetPacket"/>.
        /// </summary>
        public void WriteMemory(IntPtr ptr, int byteSize)
        {
            WriteMemory((void*)ptr, byteSize);
        }

        /// <summary>
        /// Writes raw data to the <see cref="NetPacket"/>.
        /// </summary>
        public void WriteMemory(void* ptr, int byteSize)
        {
            if (byteSize < 0)
                throw new ArgumentOutOfRangeException(nameof(byteSize));

            if (ptr == null)
                throw new ArgumentNullException(nameof(ptr));

            // Make sure there is enough space for the entire memory write operation.
            EnsureWriteSize(byteSize * 8);

            WriteMemoryUnchecked(ptr, byteSize);
        }

        /// <summary>
        /// Reads raw data from the <see cref="NetPacket"/>.
        /// </summary>
        public void ReadMemory(IntPtr ptr, int byteSize)
        {
            ReadMemory((void*)ptr, byteSize);
        }

        /// <summary>
        /// Reads raw data from the <see cref="NetPacket"/>.
        /// </summary>
        public void ReadMemory(void* ptr, int byteSize)
        {
            if (byteSize < 0)
                throw new ArgumentOutOfRangeException(nameof(byteSize));

            if (ptr == null)
                throw new ArgumentNullException(nameof(ptr));

            // Make sure there is enough space for the entire memory read operation.
            if (EnsureReadSize(byteSize * 8))
                ReadMemoryUnchecked(ptr, byteSize);
        }

        public void WriteBytes(byte[] bytes, bool includeSize = false)
        {
            WriteBytes(bytes, 0, bytes.Length, includeSize);
        }

        /// <summary>
        /// Writes bytes to the <see cref="NetPacket"/>.
        /// </summary>
        public void WriteBytes(byte[] bytes, int offset, int count, bool includeSize = false)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if ((uint)offset + (uint)count > bytes.Length)
                throw new ArgumentOutOfRangeException("Offset and count exceed array size");

            if (includeSize)
                WriteUShort((ushort)count);

            // Make sure there is enough space for the entire memory write operation.
            EnsureWriteSize(count * 8);
            fixed (byte* ptr = &bytes[offset])
            {
                WriteMemoryUnchecked(ptr, count);
            }
        }

        /// <summary>
        /// Reads an array of bytes from the <see cref="NetPacket"/>.
        /// Length is automatically retrieved as an uint16.
        /// </summary>
        public byte[] ReadBytes()
        {
            ushort length = ReadUShort();

            return ReadBytes(length);
        }

        /// <summary>
        /// Reads an array of bytes from the <see cref="NetPacket"/>.
        /// </summary>
        /// <param name="count">The amount of bytes to read.</param>
        public byte[] ReadBytes(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (!EnsureReadSize(count * 8))
                return Array.Empty<byte>();

            byte[] val = new byte[count];

            fixed (byte* ptr = val)
            {
                ReadMemoryUnchecked(ptr, count);
            }

            return val;
        }

        /// <summary>
        /// Reads bytes from the <see cref="NetPacket"/>.
        /// </summary>
        public void ReadBytes(byte[] bytes, int offset, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            if ((uint)offset + (uint)count > bytes.Length)
                throw new ArgumentOutOfRangeException("Offset and count exceed array size");

            fixed (byte* ptr = &bytes[offset])
            {
                ReadMemory(ptr, count);
            }
        }
    }
}