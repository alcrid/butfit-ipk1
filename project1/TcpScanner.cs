using System.Net;
using System.Net.Sockets;

namespace project1;

public static class TcpScanner
{
    public static void Scan(string ipAddress, int port, int timeout, bool isIpv6)
    {
        var destinationIp = IPAddress.Parse(ipAddress);
        var sourceIp = GetLocalIpAddress();
        var sourcePort = (ushort)new Random().Next(1024, 65535);

        var tcpHeader = new TcpHeader(sourcePort, (ushort)port, sourceIp, destinationIp);
        byte[] tcpBytes = tcpHeader.GetBytes();

        bool gotResponse = false;

        for (int attempt = 0; attempt < 2; attempt++)
        {
            // Send SYN packet
            using (Socket sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Tcp))
            {
                sendSocket.SendTo(tcpBytes, new IPEndPoint(destinationIp, port));
            }

            // Listen for response
            using (Socket recvSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.Tcp))
            {
                recvSocket.Bind(new IPEndPoint(sourceIp, 0));
                recvSocket.ReceiveTimeout = timeout;

                byte[] buffer = new byte[4096];
                EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);

                try
                {
                    while (true)
                    {
                        var received = recvSocket.ReceiveFrom(buffer, ref remoteEp);
                        if (received <= 0) continue;

                        var srcIp = new IPAddress(new byte[] { buffer[12], buffer[13], buffer[14], buffer[15] });
                        var dstIp = new IPAddress(new byte[] { buffer[16], buffer[17], buffer[18], buffer[19] });

                        if (!srcIp.Equals(destinationIp) || !dstIp.Equals(sourceIp))
                            continue;

                        var ipHeaderLen = (buffer[0] & 0x0F) * 4;
                        if (ipHeaderLen + 20 > received) continue;

                        var srcPort = (ushort)((buffer[ipHeaderLen] << 8) + buffer[ipHeaderLen + 1]);
                        var dstPort = (ushort)((buffer[ipHeaderLen + 2] << 8) + buffer[ipHeaderLen + 3]);
                        var flags = buffer[ipHeaderLen + 13];

                        if (srcPort != port || dstPort != sourcePort)
                            continue;

                        if ((flags & 0x12) == 0x12) // SYN + ACK
                        {
                            Console.WriteLine($"{ipAddress} {port} tcp open");
                            gotResponse = true;
                        }
                        else if ((flags & 0x14) == 0x14) // RST + ACK
                        {
                            Console.WriteLine($"{ipAddress} {port} tcp closed");
                            gotResponse = true;
                        }
                        else
                        {
                            Console.WriteLine($"{ipAddress} {port} tcp filtered");
                            gotResponse = true;
                        }

                        return; // Done after valid response
                    }
                }
                catch (SocketException ex)
                {
                    // Only ignore timeout errors
                    if (ex.SocketErrorCode != SocketError.TimedOut)
                    {
                        Console.WriteLine($"Socket error: {ex.Message}");
                        return;
                    }
                }
            }
        }

        // After 2 attempts with no valid reply
        if (!gotResponse)
        {
            Console.WriteLine($"{ipAddress} {port} tcp filtered");
        }
    }

    private static IPAddress GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip;
        }

        throw new Exception("No local address found.");
    }
}
