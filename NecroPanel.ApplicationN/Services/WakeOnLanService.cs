using NecroPanel.ApplicationN.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NecroPanel.ApplicationN.Services
{
    public class WakeOnLanService : IWakeOnLanService
    {
        public async Task<string> EnviarMagicPacket()
        {
            try
            {
                string macAddress = Preferences.Get("MAC_ADDRESS", "");
                macAddress = macAddress
                    .Replace(":", "")
                    .Replace("-", "")
                    .Replace(",", "");

                string broadcastIP_01 = Preferences.Get("BROADCAST_IP_01", "");
                string broadcastIP_02 = Preferences.Get("BROADCAST_IP_02", "");

                if (macAddress.Length != 12)
                {
                    throw new Exception("Mac Address Inválido");
                }

                byte[] macBytes = new byte[6];
                for (int x = 0; x < 6; x++)
                {
                    macBytes[x] = Convert.ToByte(macAddress.Substring(x * 2, 2), 16);
                }

                byte[] packet = new byte[102];
                for (int x = 0; x < 6; x++)
                {
                    packet[x] = 0xFF;
                }

                for (int x = 0; x < 16; x++)
                {
                    Buffer.BlockCopy(macBytes, 0, packet, 6 + (x * 6), 6);
                }

                using UdpClient client = new UdpClient();
                client.EnableBroadcast = true;

                await client.SendAsync(packet, packet.Length, new IPEndPoint(IPAddress.Parse(broadcastIP_01), 9));
                await client.SendAsync(packet, packet.Length, new IPEndPoint(IPAddress.Parse(broadcastIP_02), 9));
                await client.SendAsync(packet, packet.Length, new IPEndPoint(IPAddress.Parse(broadcastIP_01), 7));
                await client.SendAsync(packet, packet.Length, new IPEndPoint(IPAddress.Parse(broadcastIP_02), 7));
                return "Pacote Mágico enviado.\nAguarde a inicialização do servidor.";
            }
            catch (Exception ex)
            {
                return $"ERRO: ${ex.Message}";
            }
        }
    }
}
