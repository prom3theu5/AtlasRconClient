using System;

namespace AtlasRcon.Models.EventArgs
{
    /// <summary>
    /// Class HostnameEventArgs.
    /// Implements the <see cref="System.EventArgs" />
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class HostnameEventArgs : System.EventArgs
    {
        /// <summary>
        /// Creates new hostname.
        /// </summary>
        /// <value>The new hostname.</value>
        public string NewHostname { get; set; }
        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        /// <value>The timestamp.</value>
        public DateTime Timestamp { get; set; }
    }
}