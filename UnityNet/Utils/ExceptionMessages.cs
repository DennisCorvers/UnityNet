using System;
using System.Collections.Generic;
using System.Text;

namespace UnityNet.Utils
{
    /// <summary>
    /// Exception messages that get logged during runtime.
    /// </summary>
    internal static class ExceptionMessages
    {
        internal const string LISTENER_BOUND = "TcpListener is already bound. Call Close() first.";
        internal const string NOT_LISTENING = "Failed to accept a new connection, the socket is not listening.";

        internal const string NO_DATA = "Cannot send data over the network. No data to send.";
        internal const string INVALID_BUFFER = "Cannot receive data from the network. The destination buffer is invalid.";
    }
}
