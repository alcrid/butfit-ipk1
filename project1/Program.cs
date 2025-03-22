using System.Net;

namespace project1;

class Program
{
    static void Main(string[] args)
    {
        string target = "";
        int timeout = 5000;
        string? interfaceName = null;
        List<int> tcpPorts = new List<int>();
        List<int> udpPorts = new List<int>();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-h":
                case "--help":
                    Console.WriteLine("Usage: ./ipk-l4-scan [-i interface] [-t ports] [-u ports] [-w timeout] hostname/ip");
                    return;

                case "-i":
                case "--interface":
                    interfaceName = args[++i];
                    break;

                case "-t":
                case "--pt":
                    tcpPorts.AddRange(ParsePorts(args[++i]));
                    break;

                case "-u":
                case "--pu":
                    udpPorts.AddRange(ParsePorts(args[++i]));
                    break;

                case "-w":
                case "--wait":
                    timeout = int.Parse(args[++i]);
                    break;

                default:
                    if (string.IsNullOrEmpty(target))
                        target = args[i];
                    break;
            }
        }

        if (string.IsNullOrEmpty(target))
        {
            Console.WriteLine("Error: No target specified.");
            return;
        }

        IPAddress[] resolvedAddresses = Dns.GetHostAddresses(target);
        if (resolvedAddresses.Length == 0)
        {
            Console.WriteLine("Error: No IP addresses resolved.");
            return;
        }

        IPAddress selectedIp = resolvedAddresses[0];
        foreach (int port in udpPorts)
        {
            Console.WriteLine($"Pinging {selectedIp} port {port} (udp)");
            UdpScanner.Scan(selectedIp.ToString(), port, timeout);
        }
        foreach (int port in tcpPorts)
        {
            Console.WriteLine($"Pinging {selectedIp} port {port} (tcp)");
            TcpScanner.Scan(selectedIp.ToString(), port, timeout);
        }
    }

    static List<int> ParsePorts(string input)
    {
        List<int> ports = new List<int>();
        foreach (string part in input.Split(','))
        {
            if (part.Contains('-'))
            {
                var range = part.Split('-');
                int start = int.Parse(range[0]);
                int end = int.Parse(range[1]);
                for (int i = start; i <= end; i++)
                    ports.Add(i);
            }
            else
            {
                ports.Add(int.Parse(part));
            }
        }

        return ports;
    }
}
