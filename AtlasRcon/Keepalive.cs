using AtlasRcon.Enums;
using System.Diagnostics;

namespace AtlasRcon
{
    /// <summary>
    /// Class Keepalive.
    /// </summary>
    public class Keepalive
    {
        /// <summary>
        /// The interval
        /// </summary>
        private const int Interval = 2000;
        /// <summary>
        /// Gets the client.
        /// </summary>
        /// <value>The client.</value>
        private Client Client { get; }
        /// <summary>
        /// Gets the timer.
        /// </summary>
        /// <value>The timer.</value>
        private Stopwatch Timer { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Keepalive"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        public Keepalive(Client client)
        {
            Client = client;
            Timer = new Stopwatch();
            Timer.Restart();
        }

        /// <summary>
        /// Updates this instance.
        /// </summary>
        public void Update()
        {
            if (Timer.ElapsedMilliseconds <= Interval) return;

            Client.SendPacket(new Packet(OpcodeEnum.Keepalive, PacketTypeEnum.ResponseValue));
            Reset();
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        public void Reset()
        {
            Timer.Restart();
        }
    }
}
