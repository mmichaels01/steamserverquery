using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SteamServerQuery
{
    public static class SteamGameServer
    {
        /// <summary>Reads a null-terminated string into a .NET Framework compatible string.</summary>
        /// <param name="input">Binary reader to pull the null-terminated string from.  Make sure it is correctly positioned in the stream before calling.</param>
        /// <returns>String of the same encoding as the input BinaryReader.</returns>
        public static string ReadNullTerminatedString(BinaryReader input)
        {
            var sb = new StringBuilder();
            var read = input.ReadChar();
            while (read != '\x00')
            {
                sb.Append(read);
                read = input.ReadChar();
            }
            return sb.ToString();
        }
        /// <summary>
        /// Player query helper
        /// </summary>
        public static class PlayerQuery
        {
            private static readonly byte[] CHALLENGE_REQUEST = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x55, 0xFF, 0xFF, 0xFF, 0xFF };
            public static async Task<Player[]> QueryPlayersAsync(IPEndPoint ep, int timeout = 5000)
            {
                using (var udp = new UdpClient())
                {
                    udp.Client.SendTimeout = timeout;
                    udp.Client.ReceiveTimeout = timeout;
                    var playerReq = await GetPlayerReqAsync(ep, udp);
                    return await GetPlayersAsync(ep, udp, playerReq);
                }
            }
            private static async Task<byte[]> GetPlayerReqAsync(IPEndPoint ep, UdpClient udp)
            {
                await udp.SendAsync(CHALLENGE_REQUEST, CHALLENGE_REQUEST.Length, ep);
                using (var ms = new MemoryStream(udp.Receive(ref ep)))
                {
                    using (var br = new BinaryReader(ms, Encoding.UTF8))
                    {
                        ms.Seek(4, SeekOrigin.Begin);   // skip the 4 0xFFs
                        var header = br.ReadByte();
                        var chalengeNumber = br.ReadBytes(4);
                        return new byte[9] { 0xFF, 0xFF, 0xFF, 0xFF, 0x55, chalengeNumber[0], chalengeNumber[1], chalengeNumber[2], chalengeNumber[3] };
                    }
                }
            }
            public static async Task<Player[]> GetPlayersAsync(IPEndPoint ep, UdpClient udp, byte[] playerReq)
            {
                await udp.SendAsync(playerReq, playerReq.Length, ep);
                using (var ms = new MemoryStream(udp.Receive(ref ep)))
                {
                    using (var br = new BinaryReader(ms, Encoding.UTF8))
                    {
                        ms.Seek(4, SeekOrigin.Begin);   // skip the 4 0xFFs

                        var headerResp = br.ReadByte();
                        var playerCount = br.ReadByte();
                        var players = new Player[playerCount];
                        
                        for (var i = 0; i < playerCount; i++)
                        {
                            var idx = br.ReadByte();
                            var name = ReadNullTerminatedString(br);
                            var score = br.ReadInt32();
                            var duration = br.ReadSingle();
                            players[i] = new Player()
                            {
                                Duration = duration,
                                Name = name,
                                Score = score
                            };
                        }
                        return players;
                    }
                }
            }
        }
    }
}
