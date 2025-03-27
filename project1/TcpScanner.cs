using System.Net;
using System.Net.Sockets;
using project1.models;

namespace project1;

public static class TcpScanner
{
    public static void Scan(string ipAddress, int port, int timeout, bool isIpv6, string? interfaceName)
    {
        var destinationIp = IPAddress.Parse(ipAddress);
        var sourceIp = Utils.GetInterfaceAddress(interfaceName, isIpv6); // Assuming you have a method that provides source IP.
        var sourcePort = (ushort)new Random().Next(1024, 65535);

        // Create TCP header (SYN packet)
        var tcpHeader = isIpv6
            ? new Ipv6TcpHeader(sourcePort, (ushort)port, sourceIp, destinationIp).GetBytes()
            : new Ipv4TcpHeader(sourcePort, (ushort)port, sourceIp, destinationIp).GetBytes();

        // Build IP header (IPv4 or IPv6), total length includes both IP + TCP header
        var ipHeader = isIpv6
            ? Ipv6Header.Build(sourceIp, destinationIp, tcpHeader.Length, 6) // TCP = protocol 6
            : Ipv4Header.Build(sourceIp, destinationIp, tcpHeader.Length + 20, 6); // +20 for IPv4 header length

        // Combine headers into packet
        var packet = new byte[ipHeader.Length + tcpHeader.Length];
        Buffer.BlockCopy(ipHeader, 0, packet, 0, ipHeader.Length);
        Buffer.BlockCopy(tcpHeader, 0, packet, ipHeader.Length, tcpHeader.Length);

        var gotResponse = false;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var family = isIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
            var protocol = ProtocolType.Tcp; // Use ProtocolType.Tcp for both IPv4 and IPv6 (as done in the second code)

            using var socket = new Socket(family, SocketType.Raw, protocol);
            socket.ReceiveTimeout = timeout;

            // Ensure the socket is bound once and properly configured
            socket.Bind(new IPEndPoint(sourceIp, 0));

            if (!isIpv6)
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
                socket.SendTo(packet, new IPEndPoint(destinationIp, 0));
            }
            else
            {
                // For IPv6, we use a separate sending socket
                using var sendingSocket = new Socket(family, SocketType.Raw, protocol);
                sendingSocket.SendTo(packet, new IPEndPoint(destinationIp, 0));
            }

            byte[] buffer = new byte[4096];
            EndPoint remoteEp = family == AddressFamily.InterNetwork
                ? new IPEndPoint(IPAddress.Any, 0)
                : new IPEndPoint(IPAddress.IPv6Any, 0);

            try
            {
                while (true)
                {
                    var received = socket.ReceiveFrom(buffer, ref remoteEp);
                    if (received <= 0) continue;

                    if (family == AddressFamily.InterNetwork)
                    {
                        // Parse IPv4 header fields
                        var srcIp = new IPAddress(new[] { buffer[12], buffer[13], buffer[14], buffer[15] });
                        var dstIp = new IPAddress(new[] { buffer[16], buffer[17], buffer[18], buffer[19] });

                        if (!srcIp.Equals(destinationIp) || !dstIp.Equals(sourceIp)) continue;

                        var ipHeaderLen = (buffer[0] & 0x0F) * 4;
                        if (ipHeaderLen + 20 > received) continue;

                        var srcPort = (ushort)((buffer[ipHeaderLen] << 8) + buffer[ipHeaderLen + 1]);
                        var dstPort = (ushort)((buffer[ipHeaderLen + 2] << 8) + buffer[ipHeaderLen + 3]);
                        var flags = buffer[ipHeaderLen + 13];

                        if (srcPort != port || dstPort != sourcePort) continue;

                        // SYN-ACK = open, RST-ACK = closed, anything else = filtered
                        if ((flags & 0x12) == 0x12)
                            Console.WriteLine($"{ipAddress} {port} tcp open");
                        else if ((flags & 0x14) == 0x14)
                            Console.WriteLine($"{ipAddress} {port} tcp closed");
                        else
                            Console.WriteLine($"{ipAddress} {port} tcp filtered");
                    }
                    else
                    {
                        // Parse IPv6 header fields
                        var srcIpBytes = new byte[16];
                        var dstIpBytes = new byte[16];
                        Buffer.BlockCopy(buffer, 8, srcIpBytes, 0, 16);
                        Buffer.BlockCopy(buffer, 24, dstIpBytes, 0, 16);

                        var srcIp = new IPAddress(srcIpBytes);
                        var dstIp = new IPAddress(dstIpBytes);

                        if (!srcIp.Equals(destinationIp) || !dstIp.Equals(sourceIp)) continue;

                        var ipHeaderLen = 0;
                        if (ipHeaderLen + 20 > received) continue;

                        var srcPort = (ushort)((buffer[ipHeaderLen] << 8) + buffer[ipHeaderLen + 1]);
                        var dstPort = (ushort)((buffer[ipHeaderLen + 2] << 8) + buffer[ipHeaderLen + 3]);
                        var flags = buffer[ipHeaderLen + 13];

                        if (srcPort != port || dstPort != sourcePort) continue;

                        if ((flags & 0x12) == 0x12)
                            Console.WriteLine($"{ipAddress} {port} tcp open");
                        else if ((flags & 0x14) == 0x14)
                            Console.WriteLine($"{ipAddress} {port} tcp closed");
                        else
                            Console.WriteLine($"{ipAddress} {port} tcp filtered");
                    }

                    gotResponse = true;
                    return;
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.TimedOut) continue;
                Console.WriteLine($"Socket error: {ex.Message}");
                return;
            }
        }

        if (!gotResponse)
        {
            Console.WriteLine($"{ipAddress} {port} tcp filtered");
        }
    }
}
