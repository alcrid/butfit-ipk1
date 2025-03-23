using System.Net;
using System.Net.Sockets;

namespace project1;

public static class UdpScanner
{
    public static void Scan(string ipAddress, int port, int timeout, bool isIpv6)
    {
        IPAddress targetIp = IPAddress.Parse(ipAddress);
        IPEndPoint udpTarget = new IPEndPoint(targetIp, port);
        AddressFamily addressFamily = isIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

        using (Socket udpSocket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp))
        {
            byte[] payload = [];
            udpSocket.SendTo(payload, udpTarget);
        }

        ProtocolType icmpProtocol = isIpv6 ? ProtocolType.IcmpV6 : ProtocolType.Icmp;
        using (Socket icmpSocket = new Socket(addressFamily, SocketType.Raw, icmpProtocol))
        {
            IPAddress bindAddress = isIpv6 ? IPAddress.IPv6Any : IPAddress.Any;
            icmpSocket.Bind(new IPEndPoint(bindAddress, port));

            icmpSocket.ReceiveTimeout = timeout;

            byte[] buffer = new byte[4096];
            EndPoint remoteEp = new IPEndPoint(bindAddress, 0);

            try
            {
                icmpSocket.ReceiveFrom(buffer, ref remoteEp);

                if ((isIpv6 && buffer.Length > 2 && buffer[0] == 1 && buffer[1] == 4) ||
                    (!isIpv6 && buffer.Length > 21 && buffer[20] == 3 && buffer[21] == 3))
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