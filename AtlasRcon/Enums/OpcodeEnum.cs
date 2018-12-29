namespace AtlasRcon.Enums
{
    /// <summary>
    /// Enum OpcodeEnum
    /// </summary>
    public enum OpcodeEnum
    {
        /// <summary>
        /// The authentication failed
        /// </summary>
        AuthFailed = -1,
        /// <summary>
        /// The server response
        /// </summary>
        ServerResponse = 0,
        /// <summary>
        /// The generic
        /// </summary>
        Generic,
        /// <summary>
        /// The authentication
        /// </summary>
        Auth,
        /// <summary>
        /// The keepalive
        /// </summary>
        Keepalive,
        /// <summary>
        /// The get players
        /// </summary>
        GetPlayers,
        /// <summary>
        /// The kick player
        /// </summary>
        KickPlayer,
        /// <summary>
        /// The ban player
        /// </summary>
        BanPlayer,
        /// <summary>
        /// The scheduled task
        /// </summary>
        ScheduledTask,
        /// <summary>
        /// The chat message
        /// </summary>
        ChatMessage,
        /// <summary>
        /// The whitelist
        /// </summary>
        Whitelist,
        /// <summary>
        /// The un whitelist
        /// </summary>
        UnWhitelist
    }
}
