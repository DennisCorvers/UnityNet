using System;
using UnityNet.Tcp;

namespace UnityNet.Utils
{
    /// <summary>
    /// Exception messages that get logged during runtime.
    /// </summary>
    internal static class ExceptionHelper
    {
        internal const string NO_DATA = "Cannot send data over the network. No data to send.";
        internal const string INVALID_BUFFER = "Cannot receive data from the network. The destination buffer is invalid.";

        internal static void ThrowNotListening()
        {
            throw new InvalidOperationException("Failed to accept a new connection, the socket is not listening.");
        }

        internal static void ThrowAlreadyBound()
        {
            throw new InvalidOperationException("Socket is already bound. Call Close() first.");
        }

        internal static void ThrowPacketSizeExceeded()
        {
            throw new InvalidOperationException($"Maximum packet size of {TcpSocket.MaxPacketSize} exceeded.");
        }

        internal static void ThrowArgumentOutOfRange(string argumentName)
        {
            throw new ArgumentOutOfRangeException(argumentName);
        }

        internal static void ThrowAlreadyActive()
        {
            throw new InvalidOperationException("Socket is already connected.");
        }

        internal static void ThrowNoData()
        {
            throw new InvalidOperationException(NO_DATA);
        }

        internal static void ThrowArgumentNull(string argumentName)
        {
            throw new ArgumentNullException(argumentName);
        }
    }
}
