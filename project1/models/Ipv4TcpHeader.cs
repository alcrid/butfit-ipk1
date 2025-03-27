using System.Net;

public class Ipv4TcpHeader
{
    public ushort SourcePort;
    public ushort DestinationPort;
    public uint SequenceNumber = 0;
    public uint AcknowledgmentNumber = 0;
    public byte DataOffset = 5; // 5 * 4 = 20 bytes
    public byte Flags = 0x02; // SYN
    public ushort WindowSize = 8192;
    public ushort Checksum = 0;
    public ushort UrgentPointer = 0;

    public IPAddress SourceIP;
    public IPAddress DestinationIP;

    public Ipv4TcpHeader(ushort sourcePort, ushort destinationPort, IPAddress sourceIP, IPAddress destinationIP)
    {
        SourcePort = sourcePort;
        DestinationPort = destinationPort;
        SourceIP = sourceIP;
        DestinationIP = destinationIP;
    }

    public byte[] GetBytes()
    {
        byte[] buffer = new byte[20];

        buffer[0] = (byte)(SourcePort >> 8);
        buffer[1] = (byte)(SourcePort & 0xFF);
        buffer[2] = (byte)(DestinationPort >> 8);
        buffer[3] = (byte)(DestinationPort & 0xFF);
        buffer[4] = 0; buffer[5] = 0; buffer[6] = 0; buffer[7] = 0; // Seq
        buffer[8] = 0; buffer[9] = 0; buffer[10] = 0; buffer[11] = 0; // Ack
        buffer[12] = (byte)((DataOffset << 4) | 0); // Header length, reserved
        buffer[13] = Flags;
        buffer[14] = (byte)(WindowSize >> 8);
        buffer[15] = (byte)(WindowSize & 0xFF);
        buffer[16] = 0; buffer[17] = 0; // Checksum
        buffer[18] = 0; buffer[19] = 0; // Urgent

        // Now compute checksum
        Checksum = ComputeChecksum(buffer);
        buffer[16] = (byte)(Checksum >> 8);
        buffer[17] = (byte)(Checksum & 0xFF);

        return buffer;
    }

    private ushort ComputeChecksum(byte[] tcpSegment)
    {
        byte[] pseudoHeader = new byte[12 + tcpSegment.Length];
        Array.Copy(SourceIP.GetAddressBytes(), 0, pseudoHeader, 0, 4);
        Array.Copy(DestinationIP.GetAddressBytes(), 0, pseudoHeader, 4, 4);
        pseudoHeader[8] = 0;
        pseudoHeader[9] = 6; // TCP protocol
        pseudoHeader[10] = (byte)(tcpSegment.Length >> 8);
        pseudoHeader[11] = (byte)(tcpSegment.Length & 0xFF);
        Array.Copy(tcpSegment, 0, pseudoHeader, 12, tcpSegment.Length);

        uint sum = 0;
        for (int i = 0; i < pseudoHeader.Length - 1; i += 2)
        {
            sum += (uint)((pseudoHeader[i] << 8) + pseudoHeader[i + 1]);
        }

        if (pseudoHeader.Length % 2 == 1)
            sum += (uint)(pseudoHeader[^1] << 8);

        while ((sum >> 16) != 0)
            sum = (sum & 0xFFFF) + (sum >> 16);

        return (ushort)~sum;
    }
}
