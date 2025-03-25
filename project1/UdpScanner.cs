using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace project1;

public static class UdpScanner
{
    public static void Scan(string ipAddress, int port, int timeout, bool isIpv6, string? interfaceName)
    {
        IPAddress targetIp = IPAddress.Parse(ipAddress);
        IPAddress sourceIp = GetInterfaceAddress(interfaceName, isIpv6);
        IPEndPoint udpTarget = new IPEndPoint(targetIp, port);

        AddressFamily addressFamily = isIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;

        // Send UDP packet from correct interface using UdpClient
        using (UdpClient udpClient = new UdpClient(addressFamily))
        {
            udpClient.Client.Bind(new IPEndPoint(sourceIp, 0));
            byte[] payload = [];
            udpClient.Send(payload, payload.Length, udpTarget);
        }

        // ICMP receive logic 
        ProtocolType icmpProtocol = isIpv6 ? ProtocolType.IcmpV6 : ProtocolType.Icmp;
        using (Socket icmpSocket = new Socket(addressFamily, SocketType.Raw, icmpProtocol))
        {
            IPAddress bindIp = isIpv6 ? IPAddress.IPv6Any : IPAddress.Any;
            icmpSocket.Bind(new IPEndPoint(bindIp, 0));
            icmpSocket.ReceiveTimeout = timeout;

            byte[] buffer = new byte[4096];
            EndPoint remoteEp = new IPEndPoint(bindIp, 0);

            try
            {
                int received = icmpSocket.ReceiveFrom(buffer, ref remoteEp);
                Console.WriteLine("Packet recieved");

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
                if (ex.SocketErrorCode == SocketError.TimedOut)
                    Console.WriteLine($"{ipAddress} {port} udp open t imed out");
                else
                    Console.WriteLine($"Socket error: {ex.Message}");
            }
        }
    }

    private static IPAddress GetInterfaceAddress(string? interfaceName, bool isIpv6)
    {
        var interfaces = NetworkInterface.GetAllNetworkInterfaces();
        
        foreach (var ni in interfaces)
        {
            if (!string.IsNullOrEmpty(interfaceName) && ni.Name != interfaceName)
                continue;

            if (ni.OperationalStatus != OperationalStatus.Up ||
                ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;

            var ipProps = ni.GetIPProperties();

            foreach (var addr in ipProps.UnicastAddresses)
            {
                if (isIpv6 &&
                    addr.Address.AddressFamily == AddressFamily.InterNetworkV6)
                    return addr.Address;

                if (!isIpv6 &&
                    addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    return addr.Address;
            }
        }

        throw new Exception(!string.IsNullOrEmpty(interfaceName)
            ? $"Interface '{interfaceName}' with {(isIpv6 ? "IPv6" : "IPv4")} address not found or inactive."
            : $"No active {(isIpv6 ? "IPv6" : "IPv4")} address found on any interface.");
    }
}
