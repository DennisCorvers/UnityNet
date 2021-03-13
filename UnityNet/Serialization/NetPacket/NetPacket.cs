using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using UnityNet.Utils;

namespace UnityNet.Serialization
{
    public unsafe partial struct NetPacket : IDisposable
    {
        private const int DefaultSize = 16;

#pragma warning disable IDE0032
        private ulong* m_data;
        // The total size of m_data in bits.
        private int m_capacity;
        // The current size of the packet in bits.
        private int m_size;

        private int m_readPosition;
        private bool m_isValid;
        private SerializationMode m_mode;
#pragma warning restore IDE0032

        internal void* Data
            => m_data;
        internal int SendPosition;

        /// <summary>
        /// Returns <see langword="true"/> if the previosu Read operation was successful.
        /// </summary>
        public bool IsValid
        {
            get => m_isValid;
        }
        /// <summary>
        /// Get the current reading position in the packet.
        /// </summary>
        public int ReadPosition
        {
            get => m_readPosition;
        }
        /// <summary>
        /// Returns <see langword="true"/> if the reading position has reached the end of the packet.
        /// </summary>
        public bool EndOfPacket
        {
            get => m_readPosition >= m_size;
        }
        /// <summary>
        /// The current size of the packet in bytes.
        /// </summary>
        public int ByteSize
            => (m_size + 7) >> 3;
        /// <summary>
        /// The current capacity of the packet in bytes.
        /// </summary>
        public int ByteCapacity
            => m_capacity >> 3;

        /// <summary>
        /// The current streaming mode.
        /// </summary>
        public SerializationMode Mode
            => m_mode;
        /// <summary>
        /// Determines if the <see cref="BitStreamer"/> is writing.
        /// </summary>
        public bool IsWriting
            => m_mode == SerializationMode.Writing;
        /// <summary>
        /// Determines if the <see cref="BitStreamer"/> is reading.
        /// </summary>
        public bool IsReading
            => m_mode == SerializationMode.Reading;


        /// <summary>
        /// Resets the Netpacket read offset.
        /// </summary>
        public void ResetRead()
        {
            m_mode = SerializationMode.Reading;
            m_readPosition = 0;
            m_isValid = true;
        }

        /// <summary>
        /// Resets the Netpacket write offset. Existing data will be overwritten.
        /// </summary>
        public void ResetWrite()
        {
            m_mode = SerializationMode.Writing;
            m_size = 0;
            m_isValid = true;
        }


        /// <summary>
        /// Skips a certain number of bytes. Writes 0 bits when in write-mode.
        /// </summary>
        /// <param name="bitCount">Amount of bits to skip</param>
        public void Skip(byte byteCount)
        {
            Skip(byteCount * 8);
        }

        /// <summary>
        /// Skips a certain number of bits. Writes 0 bits when in write-mode.
        /// </summary>
        /// <param name="bitCount">Amount of bits to skip</param>
        public void Skip(int bitCount)
        {
            if (bitCount <= 0)
                return;

            if (m_mode == SerializationMode.Writing)
            {
                EnsureWriteSize(bitCount);

                // Write the long values first.
                while (bitCount > 64)
                {
                    Write(0, 64);
                    bitCount -= 64;
                }

                // Write the remaining bits.
                if (bitCount > 0)
                    Write(0, bitCount);
            }
            else
            {
                if (EnsureReadSize(bitCount))
                    m_readPosition += bitCount;
            }

            return;
        }


        private ulong Read(int bits)
        {
            if (EnsureReadSize(bits))
            {
                ulong value = InternalPeek(bits);
                m_readPosition += bits;
                return value;
            }

            return 0;
        }

        /// <summary>
        /// Reads a value without ensuring the buffer size.
        /// </summary>
        private ulong ReadUnchecked(int bits)
        {
            ulong value = InternalPeek(bits);
            m_readPosition += bits;
            return value;
        }

        private ulong Peek(int bits)
        {
            if (EnsureReadSize(bits))
                return InternalPeek(bits);

            return 0;
        }


        private void Write(ulong value, int bits)
        {
            EnsureWriteSize(bits);

            InternalWrite(value, bits);
            m_size += bits;
        }

        /// <summary>
        /// Writes a value without ensuring the buffer size.
        /// </summary>
        private void WriteUnchecked(ulong value, int bits)
        {
            InternalWrite(value, bits);
            m_size += bits;
        }


        /// <summary>
        /// Reads a value without increasing the offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ulong InternalPeek(int bits)
        {
            Debug.Assert(m_mode == SerializationMode.Reading);
            Debug.Assert(bits > 0);
            Debug.Assert(bits < 65);

            int longOffsetStart = m_readPosition >> 6;
            int longOffsetEnd = (m_readPosition + bits - 1) >> 6;

            ulong basemask = ulong.MaxValue >> (64 - bits);
            int placeOffset = m_readPosition & 0x3F;

            ulong value = m_data[longOffsetStart] >> placeOffset;

            if (longOffsetEnd != longOffsetStart)
            {
                value |= m_data[longOffsetEnd] << (64 - placeOffset);
            }

            return value & basemask;
        }

        /// <summary>
        /// Writes a value without increasing the offset.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InternalWrite(ulong value, int bits)
        {
            Debug.Assert(m_mode == SerializationMode.Writing);
            Debug.Assert(bits > 0);
            Debug.Assert(bits < 65);

            int longOffsetStart = m_size >> 6;
            int longOffsetEnd = (m_size + bits - 1) >> 6;

            ulong basemask = ulong.MaxValue >> (64 - bits);
            int placeOffset = m_size & 0x3F;

            value = value & basemask;
            m_data[longOffsetStart] |= value << placeOffset;

            if (longOffsetEnd != longOffsetStart)
                m_data[longOffsetEnd] = value >> (64 - placeOffset);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool EnsureReadSize(int bitCount)
        {
            Debug.Assert(bitCount >= 0);
            Debug.Assert(m_mode == SerializationMode.Reading);

            if (m_readPosition + bitCount <= m_size)
                return m_isValid = true;

            return m_isValid = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureWriteSize(int bitCount)
        {
            Debug.Assert(bitCount >= 0);
            Debug.Assert(m_mode == SerializationMode.Reading);

            // Casting to uint checks negative numbers.
            int newSize = m_size + bitCount;

            if (m_capacity < newSize)
                ExpandSize(newSize);
        }

        private void ExpandSize(int bufferBitSize)
        {
            if (m_capacity < bufferBitSize)
            {
                int newByteSize = 0;

                // Allocate a new buffer.
                if (m_data == null)
                {
                    int newBitSize = Math.Max(DefaultSize * 8, bufferBitSize);
                    newByteSize = MathUtils.GetNextMultipleOf8(newBitSize >> 3);

                    m_data = (ulong*)Memory.Alloc(newByteSize);
                }
                // Double the existing capacity.
                else
                {
                    int newBitSize = Math.Max(m_capacity * 2, bufferBitSize);
                    newByteSize = MathUtils.GetNextMultipleOf8(newBitSize >> 3);

                    m_data = (ulong*)Memory.Realloc((IntPtr)m_data, m_capacity >> 3, newByteSize);
                }

                m_capacity = newByteSize * 8;
            }
        }

        internal void OnReceive(void* data, int size)
        {
            // Clear packet first.
            Clear();

            if (m_capacity < size * 8)
            {
                int alignedSize = MathUtils.GetNextMultipleOf8(size);

                // Free any existing buffer.
                if (m_data != null)
                    Memory.Free(m_data);

                // Allocate the expanded one. We don't need to preserve old data.
                m_data = (ulong*)Memory.Alloc(alignedSize);
                m_capacity = alignedSize * 8;
            }

            m_size = size * 8;
            Memory.MemCpy(data, m_data, size);

            m_mode = SerializationMode.Reading;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Clear()
        {
            m_size = 0;
            m_readPosition = 0;
            SendPosition = 0;

            m_isValid = true;
            m_mode = default;
        }


        public void Dispose()
        {
            if (m_data != null)
                Memory.Free(m_data);

            Clear();
            m_data = null;
            m_capacity = 0;
        }
    }
}
