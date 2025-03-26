using System.Net;
using System.Net.Sockets;

namespace project1;

public static class UdpScanner
{
    public static void Scan(string ipAddress, int port, int timeout, bool isIpv6, string? interfaceName)
    {
        var targetIp = IPAddress.Parse(ipAddress);
        var sourceIp = Utils.GetInterfaceAddress(interfaceName, isIpv6);
        var udpTarget = new IPEndPoint(targetIp, port);

        AddressFamily addressFamily = isIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

        // Send UDP packet from correct interface using UdpClient
        using (var udpClient = new UdpClient(addressFamily))
        {
            udpClient.Client.Bind(new IPEndPoint(sourceIp, 0));
            byte[] payload = [];
            udpClient.Send(payload, payload.Length, udpTarget);
        }

        // ICMP receive logic 
        var icmpProtocol = isIpv6 ? ProtocolType.IcmpV6 : ProtocolType.Icmp;
        using (var icmpSocket = new Socket(addressFamily, SocketType.Raw, icmpProtocol))
        {
            IPAddress bindIp = isIpv6 ? IPAddress.IPv6Any : IPAddress.Any;
            icmpSocket.Bind(new IPEndPoint(bindIp, 0));
            icmpSocket.ReceiveTimeout = timeout;

            byte[] buffer = new byte[4096];
            EndPoint remoteEp = new IPEndPoint(bindIp, 0);

            try
            {
                int received = icmpSocket.ReceiveFrom(buffer, ref remoteEp);
                
                if (isIpv6)
                {
                    // ICMPv6: Type 1 (Dest Unreachable), Code 4 (Port Unreachable)
                    if (received >= 2 && buffer[0] == 1 && buffer[1] == 4)
                    {
                        Console.WriteLine($"{ipAddress} {port} udp closed");
                        return;
                    }
                }
                else
                {
                    // ICMPv4: Type 3 (Dest Unreachable), Code 3 (Port Unreachable)
                    if (received >= 22 && buffer[20] == 3 && buffer[21] == 3)
                    {
                        Console.WriteLine($"{ipAddress} {port} udp closed");
                        return;
                    }
                }

                // Got an ICMP message, but not the kind we expected — consider open
                Console.WriteLine($"{ipAddress} {port} udp open");
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.SocketErrorCode == SocketError.TimedOut
                    ? $"{ipAddress} {port} udp open t imed out"
                    : $"Socket error: {ex.Message}");
            }
        }
    }

   
}
