using AtlasRcon.Models.EventArgs;
using System;
using System.Threading.Tasks;

namespace AtlasRcon.Interfaces
{
    /// <summary>
    /// Interface IClient
    /// </summary>
    public interface IClient
    {
        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value><c>true</c> if this instance is connected; otherwise, <c>false</c>.</value>
        bool IsConnected { get; }

        /// <summary>
        /// Occurs when [received packet].
        /// </summary>
        event EventHandler<PacketEventArgs> ReceivedPacket;
        /// <summary>
        /// Occurs when [server connection disconnected].
        /// </summary>
        event EventHandler<ServerConnectionEventArgs> ServerConnectionDisconnected;
        /// <summary>
        /// Occurs when [server connection dropped].
        /// </summary>
        event EventHandler<ServerConnectionEventArgs> ServerConnectionDropped;
        /// <summary>
        /// Occurs when [server connection failed].
        /// </summary>
        event EventHandler<ServerConnectionEventArgs> ServerConnectionFailed;
        /// <summary>
        /// Occurs when [server connection starting].
        /// </summary>
        event EventHandler<ServerConnectionEventArgs> ServerConnectionStarting;
        /// <summary>
        /// Occurs when [server connection succeeded].
        /// </summary>
        event EventHandler<ServerConnectionEventArgs> ServerConnectionSucceeded;

        /// <summary>
        /// Connects the specified hostname.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        /// <param name="port">The port.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        Task<bool> Connect(string hostname, int port);
        /// <summary>
        /// Disconnects this instance.
        /// </summary>
        void Disconnect();
        /// <summary>
        /// Disposes this instance.
        /// </summary>
        void Dispose();
        /// <summary>
        /// Sends the packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        void SendPacket(Packet packet);
        /// <summary>
        /// Updates this instance.
        /// </summary>
        /// <returns>Task.</returns>
        Task Update();
    }
}