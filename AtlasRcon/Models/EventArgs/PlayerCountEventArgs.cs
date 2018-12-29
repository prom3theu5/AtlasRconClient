namespace AtlasRcon.Models.EventArgs
{
    /// <summary>
    /// Class PlayerCountEventArgs.
    /// Implements the <see cref="System.EventArgs" />
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class PlayerCountEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets or sets the player count.
        /// </summary>
        /// <value>The player count.</value>
        public int PlayerCount { get; set; }
    }
}