using System.Net;
using System.Net.Sockets;

namespace project1;

public static class UdpScanner
{
    public static void Scan(string ipAddress, int port, int timeout)
    {
        IPAddress targetIp = IPAddress.Parse(ipAddress);
        IPEndPoint udpTarget = new IPEndPoint(targetIp, port);

        using (Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            byte[] payload = [];
            udpSocket.SendTo(payload, udpTarget);
        }

        using (Socket icmpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Icmp))
        {
            icmpSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            icmpSocket.ReceiveTimeout = timeout;

            byte[] buffer = new byte[4096];
            EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0); 
            try {
                icmpSocket.ReceiveFrom(buffer, ref remoteEp);
                
                int icmpType = buffer[20]; 
                int icmpCode = buffer[21]; 

                if (icmpType == 3 && icmpCode == 3)
                {
                    Console.WriteLine($"{ipAddress} {port} udp closed");
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.SocketErrorCode == SocketError.TimedOut
                    ? $"{ipAddress} {port} udp open"
                    : $"Socket error: {ex.Message}");
            }
        }
    }
}