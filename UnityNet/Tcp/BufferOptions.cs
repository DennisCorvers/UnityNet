using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityNet.Utils;

namespace UnityNet.Tcp
{
    public struct BufferOptions
    {
        /// <summary>
        /// The size of the read/write buffer.
        /// </summary>
        public ushort BufferSize
        { get; private set; }
        /// <summary>
        /// Determines if all Sockets from a listener should share the same buffers.
        /// When TRUE, Read/Write operations may not be used simultaneously across Sockets from the same listener.
        /// </summary>
        public bool UseSharedBuffer
        { get; private set; }

        /// <summary>
        /// Creates a new bufferoption
        /// </summary>
        /// <param name="bufferSize">The size of the Read/Write buffer. Min 1024.</param>
        /// <param name="useSharedBuffer">TRUE to share the buffer between all Sockets from the same listener.</param>
        public BufferOptions(ushort bufferSize, bool useSharedBuffer)
        {
            if (bufferSize < 1024)
                throw new ArgumentOutOfRangeException("Buffer needs to have a minimum size of 1024.");

            BufferSize = bufferSize;
            UseSharedBuffer = useSharedBuffer;
        }
    }
}
