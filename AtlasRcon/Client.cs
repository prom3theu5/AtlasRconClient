using AtlasRcon.Enums;
using AtlasRcon.Interfaces;
using AtlasRcon.Models.EventArgs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AtlasRcon
{
    /// <summary>
    /// Class Client.
    /// Implements the <see cref="AtlasRcon.Interfaces.IClient" />
    /// Implements the <see cref="System.IDisposable" />
    /// </summary>
    /// <seealso cref="AtlasRcon.Interfaces.IClient" />
    /// <seealso cref="System.IDisposable" />
    public class Client : IClient, IDisposable
    {
        /// <summary>
        /// Gets or sets the TCP client.
        /// </summary>
        /// <value>The TCP client.</value>
        private TcpClient TcpClient { get; set; }
        /// <summary>
        /// Gets or sets the TCP client stream.
        /// </summary>
        /// <value>The TCP client stream.</value>
        private NetworkStream TcpClientStream { get; set; }
        /// <summary>
        /// Gets or sets the outgoing packets.
        /// </summary>
        /// <value>The outgoing packets.</value>
        private Queue<Packet> OutgoingPackets { get; set; }
        /// <summary>
        /// Gets or sets the keepalive.
        /// </summary>
        /// <value>The keepalive.</value>
        private Keepalive Keepalive { get; set; }
        /// <summary>
        /// Gets a value indicating whether this instance can send packet.
        /// </summary>
        /// <value><c>true</c> if this instance can send packet; otherwise, <c>false</c>.</value>
        private bool CanSendPacket => OutgoingPacketCooldown.ElapsedMilliseconds >= 800 || IncomingPacketReceieved;
        /// <summary>
        /// Gets or sets a value indicating whether [incoming packet receieved].
        /// </summary>
        /// <value><c>true</c> if [incoming packet receieved]; otherwise, <c>false</c>.</value>
        private bool IncomingPacketReceieved { get; set; }
        /// <summary>
        /// Gets or sets the outgoing packet cooldown.
        /// </summary>
        /// <value>The outgoing packet cooldown.</value>
        private Stopwatch OutgoingPacketCooldown { get; set; }

        /// <summary>
        /// Occurs when [server connection failed].
        /// </summary>
        public event EventHandler<ServerConnectionEventArgs> ServerConnectionFailed;
        /// <summary>
        /// Occurs when [server connection dropped].
        /// </summary>
        public event EventHandler<ServerConnectionEventArgs> ServerConnectionDropped;
        /// <summary>
        /// Occurs when [server connection succeeded].
        /// </summary>
        public event EventHandler<ServerConnectionEventArgs> ServerConnectionSucceeded;
        /// <summary>
        /// Occurs when [server connection starting].
        /// </summary>
        public event EventHandler<ServerConnectionEventArgs> ServerConnectionStarting;
        /// <summary>
        /// Occurs when [server connection disconnected].
        /// </summary>
        public event EventHandler<ServerConnectionEventArgs> ServerConnectionDisconnected;
        /// <summary>
        /// Occurs when [received packet].
        /// </summary>
        public event EventHandler<PacketEventArgs> ReceivedPacket;

        /// <summary>
        /// Initializes a new instance of the <see cref="Client"/> class.
        /// </summary>
        public Client()
        {
            OutgoingPackets = new Queue<Packet>();
            Keepalive = new Keepalive(this);
            OutgoingPacketCooldown = new Stopwatch();
            OutgoingPacketCooldown.Restart();
            IncomingPacketReceieved = true;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value><c>true</c> if this instance is connected; otherwise, <c>false</c>.</value>
        public bool IsConnected => TcpClient != null && TcpClient.Connected;

        /// <summary>
        /// Sends the packet.
        /// </summary>
        /// <param name="packet">The packet.</param>
        public void SendPacket(Packet packet)
        {
            OutgoingPackets.Enqueue(packet);
        }

        /// <summary>
        /// Connects the specified hostname.
        /// </summary>
        /// <param name="hostname">The hostname.</param>
        /// <param name="port">The port.</param>
        /// <returns>Task&lt;System.Boolean&gt;.</returns>
        public async Task<bool> Connect(string hostname, int port)
        {
            ServerConnectionStarting?.Invoke(this, e: new ServerConnectionEventArgs { Message = $"Connecting to {hostname}:{port}...", Status = ServerConnectionStatusEnum.Connecting, Timestamp = DateTime.Now });

            try
            {
                TcpClient = new TcpClient
                {
                    NoDelay = true
                };

                await TcpClient.ConnectAsync(hostname, port);
            }
            catch
            {
                ServerConnectionFailed?.Invoke(this, new ServerConnectionEventArgs { Message = "Failed to connect!  Make sure the server is running and that your hostname and port are correct.", Status = ServerConnectionStatusEnum.Disconnected, Timestamp = DateTime.Now });
            }

            if (!IsConnected) return false;

            TcpClientStream = TcpClient.GetStream();

            ServerConnectionSucceeded?.Invoke(this, new ServerConnectionEventArgs { Message = "Successfully connected.", Status = ServerConnectionStatusEnum.Connected, Timestamp = DateTime.Now });

            Keepalive.Reset();
            return true;

        }

        /// <summary>
        /// Disconnects this instance.
        /// </summary>
        public void Disconnect()
        {
            ServerConnectionDisconnected?.Invoke(this, new ServerConnectionEventArgs { Message = "Disconnected from server.", Status = ServerConnectionStatusEnum.Disconnected, Timestamp = DateTime.Now });
        }

        /// <summary>
        /// Updates this instance.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Update()
        {
            if (IsConnected)
            {
                await ProcessPacketStream();
                Keepalive.Update();
            }
        }


        /// <summary>
        /// Processes the packet stream.
        /// </summary>
        /// <returns>Task.</returns>
        private async Task ProcessPacketStream()
        {
            try
            {
                while (IsConnected && (OutgoingPackets.Count != 0 || TcpClient.Available != 0))
                {
                    while (IsConnected && TcpClient.Available != 0)
                    {
                        ReceivedPacket?.Invoke(this, new PacketEventArgs { Packet = await Packet.ReadFromStreamAsync(TcpClientStream) });

                        IncomingPacketReceieved = true;
                    }


                    if (!IsConnected || !CanSendPacket || OutgoingPackets.Count == 0) continue;

                    await OutgoingPackets.Dequeue().WriteToStreamAsync(TcpClientStream);
                    OutgoingPacketCooldown.Restart();
                    IncomingPacketReceieved = false;

                    //// We've successfully sent or recieved data so the keepalive can be pushed back.
                    Keepalive.Reset();
                }
            }
            catch
            {
                // Lost connection with the server
                // No handling is necessary here as the TCPClient will set Connected to false.
                ServerConnectionDropped?.Invoke(this, new ServerConnectionEventArgs { Message = "Connection to the server has been lost.", Status = ServerConnectionStatusEnum.Disconnected, Timestamp = DateTime.Now });
            }
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            if (TcpClientStream != null)
            {
                TcpClientStream.Dispose();
                TcpClientStream = null;
            }

            if (TcpClient != null)
            {
                TcpClient.Close();
                TcpClient = null;
            }

            OutgoingPackets.Clear();
        }
    }
}
