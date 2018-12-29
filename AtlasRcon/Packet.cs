using AtlasRcon.Enums;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AtlasRcon
{
    /// <summary>
    /// Class Packet.
    /// </summary>
    public class Packet
    {
        /// <summary>
        /// Gets or sets the opcode.
        /// </summary>
        /// <value>The opcode.</value>
        public OpcodeEnum Opcode { get; set; }
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public PacketTypeEnum Type { get; set; }
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public byte[] Data { get; set; }
        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        /// <value>The timestamp.</value>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Packet"/> class.
        /// </summary>
        public Packet()
        {
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Packet"/> class.
        /// </summary>
        /// <param name="opcode">The opcode.</param>
        /// <param name="type">The type.</param>
        /// <param name="data">The data.</param>
        public Packet(OpcodeEnum opcode, PacketTypeEnum type, byte[] data) : this()
        {
            Opcode = opcode;
            Type = type;
            Data = data;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Packet"/> class.
        /// </summary>
        /// <param name="opcode">The opcode.</param>
        /// <param name="type">The type.</param>
        /// <param name="data">The data.</param>
        public Packet(OpcodeEnum opcode, PacketTypeEnum type, string data) : this(opcode, type, Encoding.Default.GetBytes(data + "\0")) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Packet"/> class.
        /// </summary>
        /// <param name="opcode">The opcode.</param>
        /// <param name="type">The type.</param>
        /// <param name="data">The data.</param>
        public Packet(OpcodeEnum opcode, PacketTypeEnum type, int data) : this(opcode, type, data.ToString()) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Packet"/> class.
        /// </summary>
        /// <param name="opcode">The opcode.</param>
        /// <param name="type">The type.</param>
        public Packet(OpcodeEnum opcode, PacketTypeEnum type) : this(opcode, type, "") { }

        /// <summary>
        /// Datas as string.
        /// </summary>
        /// <returns>System.String.</returns>
        public string DataAsString()
        {
            return Encoding.Default.GetString(Data, 0, Data.Length - 1);
        }

        /// <summary>
        /// Gets or sets the size of the data.
        /// </summary>
        /// <value>The size of the data.</value>
        public int DataSize
        {
            get => Data.Length;
            set => Data = new byte[value];
        }

        /// <summary>
        /// Gets or sets the size of the packet.
        /// </summary>
        /// <value>The size of the packet.</value>
        public int PacketSize
        {
            get => sizeof(OpcodeEnum) +
                   sizeof(PacketTypeEnum) +
                   DataSize +
                   sizeof(byte);
            set => DataSize = value - sizeof(OpcodeEnum) - sizeof(PacketTypeEnum) - sizeof(byte);
        }

        /// <summary>
        /// Converts to talpacketsize.
        /// </summary>
        /// <value>The total size of the packet.</value>
        public int TotalPacketSize => sizeof(int) + // Integer to hold PacketSize
                                      PacketSize;

        /// <summary>
        /// write to stream as an asynchronous operation.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>Task.</returns>
        public async Task WriteToStreamAsync(NetworkStream stream)
        {
            byte[] packet = new byte[TotalPacketSize];

            BitConverter.GetBytes(PacketSize).CopyTo(packet, 0);
            BitConverter.GetBytes((int)Opcode).CopyTo(packet, 4);
            BitConverter.GetBytes((int)Type).CopyTo(packet, 8);
            Data.CopyTo(packet, 12);
            packet[packet.Length - 1] = 0x00;

            await stream.WriteAsync(packet, 0, TotalPacketSize);
        }

        /// <summary>
        /// read from stream as an asynchronous operation.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>Task&lt;Packet&gt;.</returns>
        /// <exception cref="System.Exception">NetworkStream failed to read data.  Connection may have been lost!</exception>
        public static async Task<Packet> ReadFromStreamAsync(NetworkStream stream)
        {
            Packet packet = new Packet();

            byte[] sizeBuffer = new byte[4];
            stream.Read(sizeBuffer, 0, 4);
            packet.PacketSize = BitConverter.ToInt32(sizeBuffer, 0);

            byte[] packetBuffer = new byte[packet.PacketSize];
            if (packet.PacketSize != 0)
            {
                int readProgress = 0;
                while (readProgress < packet.PacketSize)
                {
                    int read = await stream.ReadAsync(packetBuffer, readProgress, packet.PacketSize - readProgress);
                    readProgress += read;

                    if (read == 0)
                    {
                        throw new Exception("NetworkStream failed to read data.  Connection may have been lost!");
                    }
                }
            }

            packet.Opcode = (OpcodeEnum)BitConverter.ToInt32(packetBuffer, 0);
            packet.Type = (PacketTypeEnum)BitConverter.ToInt32(packetBuffer, 4);
            Array.Copy(packetBuffer, 8, packet.Data, 0, packet.DataSize);

            return packet;
        }

    }
}
