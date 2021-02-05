namespace UnityNet
{
    public enum SocketStatus : byte
    {
        /// <summary>
        /// The socket has sent / received the data
        /// </summary>
        Done,
        /// <summary>
        /// The socket is not ready to send / receive data yet
        /// </summary>
        NotReady,
        /// <summary>
        /// The socket sent a part of the data
        /// </summary>
        Partial,
        /// <summary>
        /// The TCP socket has been disconnected
        /// </summary>
        Disconnected,
        /// <summary>
        /// An unexpected error happened
        /// </summary>
        Error,
        /// <summary>
        /// The blocking call timed-out
        /// </summary>
        TimedOut
    }
}
