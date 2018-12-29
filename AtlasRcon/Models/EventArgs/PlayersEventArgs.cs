using System.Collections.Generic;

namespace AtlasRcon.Models.EventArgs
{
    /// <summary>
    /// Class PlayersEventArgs.
    /// Implements the <see cref="System.EventArgs" />
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class PlayersEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets or sets the players.
        /// </summary>
        /// <value>The players.</value>
        public List<Player> Players { get; set; }
    }
}