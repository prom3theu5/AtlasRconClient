namespace AtlasRcon.Models.EventArgs
{
    /// <summary>
    /// Class PacketEventArgs.
    /// Implements the <see cref="System.EventArgs" />
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class PacketEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets or sets the packet.
        /// </summary>
        /// <value>The packet.</value>
        public Packet Packet { get; set; }
    }
}