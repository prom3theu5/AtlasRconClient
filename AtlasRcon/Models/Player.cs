namespace AtlasRcon.Models
{
    /// <summary>
    /// Class Player.
    /// </summary>
    public class Player
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the steam identifier.
        /// </summary>
        /// <value>The steam identifier.</value>
        public ulong SteamID { get; set; }
        /// <summary>
        /// Gets or sets the player number.
        /// </summary>
        /// <value>The player number.</value>
        public int PlayerNumber { get; set; }
    }
}
