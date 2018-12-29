using AtlasRcon.Enums;
using System;

namespace AtlasRcon.Models.EventArgs
{
    /// <summary>
    /// Class ServerConnectionEventArgs.
    /// Implements the <see cref="System.EventArgs" />
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ServerConnectionEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>The message.</value>
        public string Message { get; set; }
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public ServerConnectionStatusEnum Status { get; set; }
        /// <summary>
        /// Gets or sets the connection information.
        /// </summary>
        /// <value>The connection information.</value>
        public IConnectionInfo ConnectionInfo { get; set; }
        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        /// <value>The timestamp.</value>
        public DateTime Timestamp { get; set; }
    }
}