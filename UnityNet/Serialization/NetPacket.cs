using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityNet.Utils;

namespace UnityNet.Serialization
{
    public unsafe struct NetPacket : IDisposable
    {
        private const int DefaultSize = 16;

#pragma warning disable IDE0032
        ulong* m_data;
        // The total size of m_data in bytes.
        int m_capacity;
        // The current size of the packet.
        int m_size;

        int m_readPosition;
        int m_sendPosition;
        bool m_isValid;
#pragma warning restore IDE0032

        internal void* Data
            => m_data;
        internal int SendPosition
            => m_sendPosition;

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





        private void Resize(int bufferBitSize)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckSize(int bitsToAdd)
        {
            m_isValid = m_isValid && (m_readPosition + bitsToAdd <= m_size);
            return m_isValid;
        }

        internal void OnReceive(void* data, int size)
        {
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
            m_isValid = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear()
        {
            m_size = 0;
            m_readPosition = 0;
            m_sendPosition = 0;

            m_isValid = true;
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
