using System.Net;

namespace project1.models;

public class Ipv4Header
{
    public static byte[] Build(IPAddress sourceIp, IPAddress destIp, int totalLength, byte protocol)
    {
        byte[] buffer = new byte[20];

        buffer[0] = 0x45; // Version 4, Header Length = 5 (20 bytes)
        buffer[1] = 0x00; // Type of Service
        buffer[2] = (byte)(totalLength >> 8); // Total Length
        buffer[3] = (byte)(totalLength & 0xFF);
        buffer[4] = 0x00; buffer[5] = 0x00; // Identification
        buffer[6] = 0x40; // Flags (Don't Fragment)
        buffer[7] = 0x00; // Fragment Offset
        buffer[8] = 64;   // TTL
        buffer[9] = protocol; // Protocol (TCP = 6)

        Array.Copy(sourceIp.GetAddressBytes(), 0, buffer, 12, 4);
        Array.Copy(destIp.GetAddressBytes(), 0, buffer, 16, 4);

        // Compute and insert header checksum
        ushort checksum = ComputeChecksum(buffer);
        buffer[10] = (byte)(checksum >> 8);
        buffer[11] = (byte)(checksum & 0xFF);

        return buffer;
    }

    private static ushort ComputeChecksum(byte[] header)
    {
        uint sum = 0;
        for (int i = 0; i < header.Length; i += 2)
        {
            sum += (ushort)((header[i] << 8) + (i + 1 < header.Length ? header[i + 1] : 0));
        }

        while ((sum >> 16) != 0)
            sum = (sum & 0xFFFF) + (sum >> 16);

        return (ushort)(~sum);
    }
}
