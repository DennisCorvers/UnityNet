using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using UnityNet.Tcp;
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
        private bool m_isInvalidated;
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
            get => !m_isInvalidated;
        }
        /// <summary>
        /// Gets the current <see cref="NetPacket"/> reading position in bits.
        /// </summary>
        public int ReadPosition
            => m_readPosition;
        /// <summary>
        /// Gets the current <see cref="NetPacket"/> write position in bits.
        /// </summary>
        public int WritePosition
            => m_size;
        /// <summary>
        /// Returns <see langword="true"/> if the reading position has reached the end of the packet.
        /// </summary>
        public bool EndOfPacket
        {
            get => m_readPosition >= m_size;
        }
        /// <summary>
        /// The current size of the <see cref="NetPacket"/> in bytes.
        /// </summary>
        public int Size
            => (m_size + 7) >> 3;
        /// <summary>
        /// The current capacity of the <see cref="NetPacket"/> in bytes.
        /// </summary>
        public int Capacity
            => m_capacity >> 3;

        /// <summary>
        /// The current streaming mode.
        /// </summary>
        public SerializationMode Mode
        {
            get => m_mode;
            set => m_mode = value;
        }
        /// <summary>
        /// Determines if the <see cref="NetPacket"/> is writing.
        /// </summary>
        public bool IsWriting
            => m_mode == SerializationMode.Writing;
        /// <summary>
        /// Determines if the <see cref="NetPacket"/> is reading.
        /// </summary>
        public bool IsReading
            => m_mode == SerializationMode.Reading;

        /// <summary>
        /// Creates a <see cref="NetPacket"/> in Writing mode.
        /// </summary>
        /// <param name="initialSize">The initial size of the packet in bytes.</param>
        public NetPacket(int initialSize)
        {
            if (initialSize < 0)
                ExceptionHelper.ThrowArgumentOutOfRange(nameof(initialSize));

            // Default all values.
            m_data = default;
            m_capacity = default;
            m_size = default;
            m_readPosition = default;
            m_isInvalidated = default;
            m_mode = default;
            SendPosition = default;

            ExpandSize(initialSize * 8);
        }

        /// <summary>
        /// Resets the <see cref="NetPacket"/> read offset.
        /// </summary>
        public void ResetRead()
        {
            m_mode = SerializationMode.Reading;
            m_readPosition = 0;
            m_isInvalidated = false;
        }

        /// <summary>
        /// Resets the NetPacket for writing.
        /// </summary>
        public void ResetWrite()
            => Clear(SerializationMode.Writing);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetSerializationMode(SerializationMode mode)
        {
            m_mode = mode;
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

        /// <summary>
        /// Clears the packet and its data. Keeps the capacity.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Clear(m_mode);
        }

        /// <summary>
        /// Clears the packet and its data. Keeps the capacity.
        /// </summary>
        /// <param name="serializationMode">The serialization mode to set after clearing.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(SerializationMode serializationMode)
        {
            // We only need to clear the bytes that have been used.
            Memory.ZeroMem(m_data, MathUtils.GetNextMultipleOf8(Size));

            m_readPosition = 0;
            m_isInvalidated = false;
            m_size = 0;
            m_mode = serializationMode;
        }

        /// <summary>
        /// Resets the Send Position of this <see cref="NetPacket"/>.
        /// <para>
        /// This should only be used when a <see cref="NetPacket"/> is sent to multiple <br/>
        /// Sockets where one or more returns <see cref="SocketStatus.Partial"/>.
        /// </para>
        /// The data stream for the Socket that returns <see cref="SocketStatus.Partial"/> will <br/>
        /// become corrupted once the <see cref="SendPosition"/> is reset.
        /// </summary>
        public void ResetSendPosition()
        {
            SendPosition = 0;
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
            Debug.Assert(bits >= 1);
            Debug.Assert(bits <= 64);

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

            if (m_readPosition + bitCount <= m_size)
                return !(m_isInvalidated = false);

            return !(m_isInvalidated = true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureWriteSize(int bitCount)
        {
            Debug.Assert(bitCount >= 0);

            // Casting to uint checks negative numbers.
            int newSize = m_size + bitCount;

            if (m_capacity < newSize)
                ExpandSize(newSize);
        }

        private void ExpandSize(int bufferBitSize)
        {
            int newByteSize = 0;
            int oldByteSize = m_capacity >> 3;

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

                m_data = (ulong*)Memory.Realloc((IntPtr)m_data, oldByteSize, newByteSize);
            }

            // Zero any newly allocated memory
            Memory.ZeroMem((byte*)m_data + oldByteSize, newByteSize - oldByteSize);

            m_capacity = newByteSize * 8;
        }

        internal void OnReceive(void* data, int size)
        {
            // Clear packet first.
            Reset();

            if (m_capacity < size * 8)
            {
                int alignedSize = MathUtils.GetNextMultipleOf8(size);

                // Free any existing buffer.
                if (m_data != null)
                    Memory.Free(m_data);

                // Allocate the expanded one. We don't need to preserve old data.
                m_data = (ulong*)Memory.Alloc(alignedSize);
                m_capacity = alignedSize * 8;
                // Free rest bytes
                Memory.ZeroMem(((byte*)m_data) + size, alignedSize - size);
            }

            m_size = size * 8;
            Memory.MemCpy(data, m_data, size);
            m_mode = SerializationMode.Reading;
        }

        internal void OnReceive(ref PendingPacket packet)
        {
            // Free any existing buffer.
            Dispose();

            Memory.ZeroMem(packet.Data + packet.Size, packet.Capacity - packet.Size);

            m_data = (ulong*)packet.Data;
            m_size = packet.Size * 8;
            m_capacity = packet.Capacity * 8;

            m_mode = SerializationMode.Reading;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Reset()
        {
            m_size = 0;
            m_readPosition = 0;
            SendPosition = 0;

            m_isInvalidated = false;
            m_mode = default;
        }

        public void Dispose()
        {
            if (m_data != null)
                Memory.Free(m_data);

            m_data = null;
            m_capacity = 0;

            Reset();
        }
    }
}
