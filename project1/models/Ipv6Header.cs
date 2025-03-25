using System.Net;

namespace project1.models;

public class Ipv6Header
{
    public static byte[] Build(IPAddress sourceIp, IPAddress destIp, int payloadLength, byte nextHeader)
    {
        byte[] buffer = new byte[40];

        buffer[0] = 0x60; // Version 6
        // Traffic Class + Flow Label left as 0
        buffer[4] = (byte)(payloadLength >> 8);
        buffer[5] = (byte)(payloadLength & 0xFF);
        buffer[6] = nextHeader; // Next Header = TCP (6)
        buffer[7] = 64;         // Hop Limit

        Array.Copy(sourceIp.GetAddressBytes(), 0, buffer, 8, 16);
        Array.Copy(destIp.GetAddressBytes(), 0, buffer, 24, 16);

        return buffer;
    }
}
