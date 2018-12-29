namespace AtlasRcon.Models
{
    /// <summary>
    /// Interface IConnectionInfo
    /// </summary>
    public interface IConnectionInfo
    {
        /// <summary>
        /// Gets or sets the hostname.
        /// </summary>
        /// <value>The hostname.</value>
        string Hostname { get; set; }
        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
        int Port { get; set; }
        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>The password.</value>
        string Password { get; set; }
    }
}
