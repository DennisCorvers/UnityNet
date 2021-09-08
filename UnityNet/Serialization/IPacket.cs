using System;

namespace UnityNet.Serialization
{
    public interface IPacket
    {
        /// <summary>
        /// The data that this packet contains.
        /// </summary>
        ReadOnlySpan<byte> Data { get; }

        /// <summary>
        /// The current offset at which the packet is being sent.
        /// Resets to 0 after a complete send.
        /// </summary>
        int SendOffset { get; set; }

        /// <summary>
        /// Receives new data into the packet.
        /// </summary>
        /// <param name="data">An entire TCP packet received from the endpoint.</param>
        void Receive(ReadOnlySpan<byte> data);
    }
}
