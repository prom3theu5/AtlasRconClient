using AtlasRcon.Enums;
using AtlasRcon.Models;
using AtlasRcon.Models.EventArgs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AtlasRcon
{
    /// <summary>
    /// Class Rcon.
    /// </summary>
    public class Rcon
    {
        /// <summary>
        /// Gets or sets the current server information.
        /// </summary>
        /// <value>The current server information.</value>
        public IConnectionInfo CurrentServerInfo { get; set; }

        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger<Rcon> _logger;
        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        /// <value>The client.</value>
        private Client Client { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is running.
        /// </summary>
        /// <value><c>true</c> if this instance is running; otherwise, <c>false</c>.</value>
        public bool IsRunning { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [got chat response].
        /// </summary>
        /// <value><c>true</c> if [got chat response]; otherwise, <c>false</c>.</value>
        public bool GotChatResponse { get; set; }
        /// <summary>
        /// The last sent admin message
        /// </summary>
        private string LastSentAdminMessage;

        /// <summary>
        /// Gets or sets the packet handlers.
        /// </summary>
        /// <value>The packet handlers.</value>
        private Dictionary<PacketTypeEnum, Dictionary<OpcodeEnum, Action<Packet>>> PacketHandlers { get; }
        /// <summary>
        /// Occurs when [hostname updated].
        /// </summary>
        public event EventHandler<HostnameEventArgs> HostnameUpdated;
        /// <summary>
        /// Occurs when [current player count updated].
        /// </summary>
        public event EventHandler<PlayerCountEventArgs> CurrentPlayerCountUpdated;
        /// <summary>
        /// Occurs when [console log updated].
        /// </summary>
        public event EventHandler<ConsoleLogEventArgs> ConsoleLogUpdated;
        /// <summary>
        /// Occurs when [chat log updated].
        /// </summary>
        public event EventHandler<ChatLogEventArgs> ChatLogUpdated;
        /// <summary>
        /// Occurs when [sent message updated].
        /// </summary>
        public event EventHandler<ChatLogEventArgs> SentMessageUpdated;
        /// <summary>
        /// Occurs when [players updated].
        /// </summary>
        public event EventHandler<PlayersEventArgs> PlayersUpdated;
        /// <summary>
        /// Occurs when [server authentication failed].
        /// </summary>
        public event EventHandler<ServerAuthEventArgs> ServerAuthFailed;
        /// <summary>
        /// Occurs when [server authentication succeeded].
        /// </summary>
        public event EventHandler<ServerAuthEventArgs> ServerAuthSucceeded;
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
        /// Initializes a new instance of the <see cref="Rcon" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public Rcon(ILogger<Rcon> logger)
        {
            _logger = logger;

            PacketHandlers = new Dictionary<PacketTypeEnum, Dictionary<OpcodeEnum, Action<Packet>>>
            {
                [PacketTypeEnum.Server] = new Dictionary<OpcodeEnum, Action<Packet>>(),
                [PacketTypeEnum.ResponseValue] = new Dictionary<OpcodeEnum, Action<Packet>>(),
                [PacketTypeEnum.AuthResponse] = new Dictionary<OpcodeEnum, Action<Packet>>(),
                [PacketTypeEnum.Server] = { [OpcodeEnum.ServerResponse] = OnConsoleLogUpdated },
                [PacketTypeEnum.ResponseValue] = { [OpcodeEnum.Generic] = OnConsoleLogUpdated },
                [PacketTypeEnum.ResponseValue] = { [OpcodeEnum.GetPlayers] = OnGetPlayers },
                [PacketTypeEnum.ResponseValue] = { [OpcodeEnum.ChatMessage] = OnGetChatMessage },
                [PacketTypeEnum.ResponseValue] = { [OpcodeEnum.KickPlayer] = OnKickPlayer },
                [PacketTypeEnum.ResponseValue] = { [OpcodeEnum.Whitelist] = OnWhitelistPlayer },
                [PacketTypeEnum.ResponseValue] = { [OpcodeEnum.UnWhitelist] = OnUnWhitelistPlayer },
                [PacketTypeEnum.ResponseValue] = { [OpcodeEnum.BanPlayer] = OnBanPlayer },
                [PacketTypeEnum.ResponseValue] = { [OpcodeEnum.ScheduledTask] = OnScheduledTask },
                [PacketTypeEnum.ResponseValue] = { [OpcodeEnum.Keepalive] = (p) => _logger.LogInformation("Keepalive") },
                [PacketTypeEnum.AuthResponse] = { [OpcodeEnum.Auth] = OnServerAuthSuccess },
                [PacketTypeEnum.AuthResponse] = { [OpcodeEnum.AuthFailed] = OnServerAuthFail }
            };
        }

        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value><c>true</c> if this instance is connected; otherwise, <c>false</c>.</value>
        public bool IsConnected => Client != null && Client.IsConnected;

        /// <summary>
        /// Connects the specified connection information.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <returns>Task.</returns>
        public async Task Connect(IConnectionInfo connectionInfo)
        {
            // Connect can be called while we're already connected to a server so disconnect first

            Disconnect();

            CurrentServerInfo = connectionInfo;

            Client = new Client();
            Client.ServerConnectionFailed += OnServerConnectionFailed;
            Client.ServerConnectionDropped += OnServerConnectionDropped;
            Client.ServerConnectionSucceeded += OnServerConnectionSucceeded;
            Client.ServerConnectionStarting += OnServerConnectionStarting;
            Client.ServerConnectionDisconnected += OnServerConnectionDisconnected;
            Client.ReceivedPacket += OnReceivedPacket;

            await Client.Connect(CurrentServerInfo.Hostname, CurrentServerInfo.Port);
        }

        /// <summary>
        /// Disconnects this instance.
        /// </summary>
        public void Disconnect()
        {
            if (IsConnected)
                Client.Disconnect();
        }

        /// <summary>
        /// Purges the client.
        /// </summary>
        private void PurgeClient()
        {
            if (CurrentServerInfo != null)
                CurrentServerInfo = null;

            Client?.Dispose();
            HostnameUpdated?.Invoke(this, new HostnameEventArgs() { NewHostname = "", Timestamp = DateTime.Now });
            CurrentPlayerCountUpdated?.Invoke(this, new PlayerCountEventArgs() { PlayerCount = 0 });
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Run()
        {
            IsRunning = true;
            while (IsRunning)
            {
                if (IsConnected)
                {
                    await Client.Update();
                }
                await Task.Delay(10);
            }
        }

        #region Commands

        /// <summary>
        /// Updates this instance.
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Update()
        {
            await Client.Update();
        }
        /// <summary>
        /// Requests the authentication.
        /// </summary>
        /// <param name="password">The password.</param>
        public void RequestAuth(string password)
        {
            Client.SendPacket(new Packet(OpcodeEnum.Auth, PacketTypeEnum.Auth, password));
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <param name="command">The command.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool ExecCommand(OpcodeEnum code, string command)
        {
            if (!IsConnected)
                return false;

            Client.SendPacket(new Packet(code, PacketTypeEnum.ExecCommand, command));
            return true;
        }

        /// <summary>
        /// Executes the get chat.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool ExecGetChat()
        {
            if (!IsConnected)
                return false;
            Client.SendPacket(new Packet(OpcodeEnum.ChatMessage, PacketTypeEnum.ExecCommand, "getchat"));
            GotChatResponse = false;
            return true;
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="writeToConsole">if set to <c>true</c> [write to console].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool ExecCommand(string command, bool writeToConsole = false)
        {
            if (!ExecCommand(OpcodeEnum.Generic, command))
                return false;

            if (writeToConsole)
                ConsoleLogUpdated?.Invoke(this, new ConsoleLogEventArgs() { Message = "> " + command, Timestamp = DateTime.Now });

            return true;
        }

        /// <summary>
        /// Executes the scheduled task.
        /// </summary>
        /// <param name="TaskName">Name of the task.</param>
        /// <param name="TaskCommand">The task command.</param>
        public void ExecuteScheduledTask(string TaskName, string TaskCommand)
        {
            if (!ExecCommand(OpcodeEnum.ScheduledTask, TaskCommand))
                return;

            ConsoleLogUpdated?.Invoke(this, new ConsoleLogEventArgs() { Message = "EXECUTED SCHEDULED TASK: " + TaskName, Timestamp = DateTime.Now });
            ConsoleLogUpdated?.Invoke(this, new ConsoleLogEventArgs() { Message = "TASK COMMAND: " + TaskCommand, Timestamp = DateTime.Now });
        }

        /// <summary>
        /// Says the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="nickname">The nickname.</param>
        public void Say(string message, string nickname)
        {

            string formattedMessage = (nickname == null) ? message : "(" + nickname + "): " + message;
            LastSentAdminMessage = formattedMessage;
            if (!ExecCommand(OpcodeEnum.ChatMessage, "serverchat " + formattedMessage))
                return;
        }

        /// <summary>
        /// Sends the private message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="steamID">The steam identifier.</param>
        public void SendPrivateMessage(string message, ulong steamID)
        {
            string formattedMessage = $"serverchatto \"{steamID.ToString()}\" PM From Admin: {message}";
            if (IsConnected)
                ExecCommand(OpcodeEnum.ChatMessage, formattedMessage);
        }

        /// <summary>
        /// Consoles the command.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="nickname">The nickname.</param>
        public void ConsoleCommand(string message, string nickname)
        {

            string formattedMessage = (nickname == null) ? message : "(" + nickname + "): " + message;
            if (!ExecCommand(OpcodeEnum.Generic, message))
                return;

            ConsoleLogUpdated?.Invoke(this, new ConsoleLogEventArgs() { Message = formattedMessage, Timestamp = DateTime.Now });
        }

        /// <summary>
        /// Echoes the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Echo(string message)
        {
            if (IsConnected)
                ExecCommand("echo " + message);
        }

        /// <summary>
        /// Gets the players.
        /// </summary>
        public void GetPlayers()
        {
            if (IsConnected)
                ExecCommand(OpcodeEnum.GetPlayers, "listplayers");
        }

        /// <summary>
        /// Kicks the player.
        /// </summary>
        /// <param name="steamid">The steamid.</param>
        public void KickPlayer(ulong steamid)
        {
            if (IsConnected)
                ExecCommand(OpcodeEnum.KickPlayer, "kickplayer " + steamid.ToString());
        }

        /// <summary>
        /// Whitelists the player.
        /// </summary>
        /// <param name="steamid">The steamid.</param>
        public void WhitelistPlayer(ulong steamid)
        {
            if (IsConnected)
                ExecCommand(OpcodeEnum.Whitelist, "AllowPlayerToJoinNoCheck " + steamid.ToString());
        }

        /// <summary>
        /// Uns the whitelist player.
        /// </summary>
        /// <param name="steamid">The steamid.</param>
        public void UnWhitelistPlayer(ulong steamid)
        {
            if (IsConnected)
                ExecCommand(OpcodeEnum.UnWhitelist, "DisallowPlayerToJoinNoCheck " + steamid.ToString());
        }

        /// <summary>
        /// Bans the player.
        /// </summary>
        /// <param name="steamid">The steamid.</param>
        public void BanPlayer(ulong steamid)
        {
            if (IsConnected)
                ExecCommand(OpcodeEnum.BanPlayer, "banplayer " + steamid.ToString());
        }

        #endregion Commands

        #region Client Handlers

        /// <summary>
        /// Handles the <see cref="E:ReceivedPacket" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="PacketEventArgs" /> instance containing the event data.</param>
        private void OnReceivedPacket(object sender, PacketEventArgs args)
        {
            Packet packet = args.Packet;
            _logger.LogInformation(packet.Type.ToString() + "," + packet.Opcode.ToString());

            if (PacketHandlers.ContainsKey(packet.Type))
                if (PacketHandlers[packet.Type].ContainsKey(packet.Opcode))
                    if (PacketHandlers[packet.Type].ContainsKey(packet.Opcode) && PacketHandlers[packet.Type][packet.Opcode] != null)
                        PacketHandlers[packet.Type][packet.Opcode](packet);
        }

        /// <summary>
        /// Handles the <see cref="E:ServerConnectionFailed" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="ServerConnectionEventArgs" /> instance containing the event data.</param>
        private void OnServerConnectionFailed(object sender, ServerConnectionEventArgs args)
        {
            try
            {
                args.ConnectionInfo = CurrentServerInfo;
                PurgeClient();

                ServerConnectionFailed?.Invoke(this, args);
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Handles the <see cref="E:ServerConnectionDropped" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="ServerConnectionEventArgs" /> instance containing the event data.</param>
        public void OnServerConnectionDropped(object sender, ServerConnectionEventArgs args)
        {
            try
            {
                args.ConnectionInfo = CurrentServerInfo;
                PurgeClient();

                ServerConnectionDropped?.Invoke(this, args);
            }
            catch
            {
                // ignored
            }

        }

        /// <summary>
        /// Handles the <see cref="E:ServerConnectionSucceeded" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="ServerConnectionEventArgs" /> instance containing the event data.</param>
        public void OnServerConnectionSucceeded(object sender, ServerConnectionEventArgs args)
        {
            try
            {
                args.ConnectionInfo = CurrentServerInfo;
                ServerConnectionSucceeded?.Invoke(this, args);
                HostnameUpdated?.Invoke(this,
                    new HostnameEventArgs() { NewHostname = CurrentServerInfo.Hostname, Timestamp = DateTime.Now });
                RequestAuth(CurrentServerInfo.Password);
            }
            catch
            {
                // ignored 
            }
        }

        /// <summary>
        /// Handles the <see cref="E:ServerConnectionStarting" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="ServerConnectionEventArgs" /> instance containing the event data.</param>
        private void OnServerConnectionStarting(object sender, ServerConnectionEventArgs args)
        {
            try
            {
                args.ConnectionInfo = CurrentServerInfo;
                ServerConnectionStarting?.Invoke(this, args);
            }
            catch
            {
                //ignored
            }
        }

        /// <summary>
        /// Handles the <see cref="E:ServerConnectionDisconnected" /> event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="ServerConnectionEventArgs" /> instance containing the event data.</param>
        private void OnServerConnectionDisconnected(object sender, ServerConnectionEventArgs args)
        {
            try
            {
                args.ConnectionInfo = CurrentServerInfo;
                PurgeClient();

                ServerConnectionDisconnected?.Invoke(this, args);
            }
            catch
            {
                //ignored
            }
        }
        #endregion Client Handlers

        #region Rcon Handlers

        /// <summary>
        /// Called when [server authentication success].
        /// </summary>
        /// <param name="packet">The packet.</param>
        private void OnServerAuthSuccess(Packet packet)
        {
            ServerAuthSucceeded?.Invoke(this, new ServerAuthEventArgs { Message = "Successfully authenticated.", Timestamp = packet.Timestamp });
            Echo("Clearing incoming packet stream.");
            GetPlayers();
        }


        /// <summary>
        /// Called when [server authentication fail].
        /// </summary>
        /// <param name="packet">The packet.</param>
        private void OnServerAuthFail(Packet packet)
        {
            ServerAuthFailed?.Invoke(this, new ServerAuthEventArgs { Message = "Server authentication failed.  Check that your server password is correct.", Timestamp = packet.Timestamp });

            Disconnect();
        }

        /// <summary>
        /// Called when [console log updated].
        /// </summary>
        /// <param name="packet">The packet.</param>
        private void OnConsoleLogUpdated(Packet packet)
        {
            string message = packet.DataAsString();
            if (message.Trim() == "Server received, But no response!!") message = "Command Executed Successfully!";

            if (packet.Opcode == OpcodeEnum.Generic || packet.Opcode == OpcodeEnum.ServerResponse)
            {
                ConsoleLogUpdated?.Invoke(this, new ConsoleLogEventArgs()
                {
                    Message = "SERVER CONSOLE: " + message,
                    Timestamp = packet.Timestamp
                });
            }

        }

        /// <summary>
        /// Called when [scheduled task].
        /// </summary>
        /// <param name="packet">The packet.</param>
        private void OnScheduledTask(Packet packet)
        {
            string message = packet.DataAsString();

            if (packet.Opcode == OpcodeEnum.ScheduledTask)
            {
                ConsoleLogUpdated?.Invoke(this, new ConsoleLogEventArgs()
                {
                    Message = "TASK SERVER RESPONSE: " + message,
                    Timestamp = packet.Timestamp
                });
            }

        }

        /// <summary>
        /// Called when [get chat message].
        /// </summary>
        /// <param name="packet">The packet.</param>
        private void OnGetChatMessage(Packet packet)
        {
            string message = packet.DataAsString();
            if (packet.Opcode == OpcodeEnum.ChatMessage)
            {
                GotChatResponse = true;
                if (message.Trim() == "Server received, But no response!!") return;
                string[] messages = message.Split('\n');
                foreach (string newMessage in messages)
                {
                    if (string.IsNullOrWhiteSpace(newMessage)) continue;
                    string[] splitMessage = newMessage.Split(new char[] { ':' }, 2);

                    if (newMessage.StartsWith("SERVER:") && newMessage == "SERVER: " + LastSentAdminMessage)
                    {
                        if (SentMessageUpdated != null)
                        {
                            ChatLogEventArgs chatLog = new ChatLogEventArgs
                            {
                                Timestamp = packet.Timestamp,
                                IsAdmin = true,
                                Message = newMessage.Replace("SERVER: ", "")
                            };

                            SentMessageUpdated(this, chatLog);
                        }
                    }
                    else
                    {
                        if (ChatLogUpdated != null)
                        {
                            ChatLogEventArgs chatLog = new ChatLogEventArgs()
                            {
                                Message = splitMessage[1],
                                Sender = splitMessage[0],
                                Timestamp = packet.Timestamp,
                                IsAdmin = false
                            };

                            ChatLogUpdated(this, chatLog);
                        }
                    }
                }

            }
        }

        /// <summary>
        /// Called when [un whitelist player].
        /// </summary>
        /// <param name="packet">The packet.</param>
        private void OnUnWhitelistPlayer(Packet packet)
        {
            string message = packet.DataAsString();
            if (message.Trim() == "Server received, But no response!!") return;

            if (packet.Opcode == OpcodeEnum.UnWhitelist)
            {
                ConsoleLogUpdated?.Invoke(this, new ConsoleLogEventArgs()
                {
                    Message = "REMOVE PLAYER FROM WHITELIST COMMAND EXECUTED: " + message,
                    Timestamp = packet.Timestamp
                });
            }
        }

        /// <summary>
        /// Called when [whitelist player].
        /// </summary>
        /// <param name="packet">The packet.</param>
        private void OnWhitelistPlayer(Packet packet)
        {
            string message = packet.DataAsString();
            if (message.Trim() == "Server received, But no response!!") return;

            if (packet.Opcode == OpcodeEnum.Whitelist)
            {
                ConsoleLogUpdated?.Invoke(this, new ConsoleLogEventArgs()
                {
                    Message = "PLAYER ADDED TO WHITELIST COMMAND EXECUTED: " + message,
                    Timestamp = packet.Timestamp
                });
            }
        }

        /// <summary>
        /// Called when [kick player].
        /// </summary>
        /// <param name="packet">The packet.</param>
        private void OnKickPlayer(Packet packet)
        {
            string message = packet.DataAsString();
            if (message.Trim() == "Server received, But no response!!") return;

            if (packet.Opcode == OpcodeEnum.KickPlayer)
            {
                ConsoleLogUpdated?.Invoke(this, new ConsoleLogEventArgs()
                {
                    Message = "KICK PLAYER COMMAND EXECUTED: " + message,
                    Timestamp = packet.Timestamp
                });

                GetPlayers();
            }
        }

        /// <summary>
        /// Called when [ban player].
        /// </summary>
        /// <param name="packet">The packet.</param>
        private void OnBanPlayer(Packet packet)
        {
            string message = packet.DataAsString();
            if (message.Trim() == "Server received, But no response!!") return;

            if (packet.Opcode == OpcodeEnum.BanPlayer)
            {
                ConsoleLogUpdated?.Invoke(this, new ConsoleLogEventArgs()
                {
                    Message = "BAN PLAYER COMMAND EXECUTED: " + message,
                    Timestamp = packet.Timestamp
                });

                GetPlayers();
            }
        }

        /// <summary>
        /// Called when [get players].
        /// </summary>
        /// <param name="packet">The packet.</param>
        private void OnGetPlayers(Packet packet)
        {
            try
            {
                List<Player> players = new List<Player>();
                string message = packet.DataAsString();
                if (message.Trim() == "No Players Connected")
                {
                    CurrentPlayerCountUpdated?.Invoke(this, new PlayerCountEventArgs() { PlayerCount = players.Count });
                    PlayersUpdated?.Invoke(this, new PlayersEventArgs() { Players = players });
                    ConsoleLogUpdated?.Invoke(this, new ConsoleLogEventArgs()
                    {
                        Message = "Server Response: No Players Connected",
                        Timestamp = packet.Timestamp
                    });
                    return;
                }
                string str = packet.DataAsString();
                string[] lines = str.Split('\n');
                string[] cleanLines = lines.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                foreach (string line in cleanLines)
                {
                    string lineData = line.Replace("...", "");
                    string[] split1 = lineData.Split(new char[] { '.' }, 2);
                    string[] split2 = split1[1].Split(new char[] { ',' }, 2);
                    string playerNumber = split1[0].Trim();
                    string name = split2[0].Trim();
                    string steamId = split2[1].Trim();

                    players.Add(new Player
                    {
                        PlayerNumber = int.Parse(playerNumber),
                        Name = name,
                        SteamID = ulong.Parse(steamId)
                    });

                }

                CurrentPlayerCountUpdated?.Invoke(this, new PlayerCountEventArgs() { PlayerCount = players.Count });
                PlayersUpdated?.Invoke(this, new PlayersEventArgs() { Players = players });
                ConsoleLogUpdated?.Invoke(this, new ConsoleLogEventArgs()
                {
                    Message = "Server Response: Player List Updated",
                    Timestamp = packet.Timestamp
                });
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Exception in OnGetPlayers: {exception}", ex.Message);
            }
        }
        #endregion Rcon Handlers

    }
}
