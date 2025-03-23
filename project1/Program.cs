using System.Net;
using System.Net.NetworkInformation;

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
                    Console.WriteLine(
                        "Usage: ./ipk-l4-scan [-i interface] [-t ports] [-u ports] [-w timeout] hostname/ip");
                    return;

                case "-i":
                case "--interface":
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("-"))
                    {
                        ListInterfaces();
                        return;
                    }
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
        var isIpv6 = selectedIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;
        
        foreach (int port in tcpPorts)
        {
            Console.WriteLine($"Pinging {selectedIp} port {port} (tcp)");
            TcpScanner.Scan(selectedIp.ToString(), port, timeout, isIpv6);
        }

        foreach (int port in udpPorts)
        {
            Console.WriteLine($"Pinging {selectedIp} port {port} (udp)");
            UdpScanner.Scan(selectedIp.ToString(), port, timeout, isIpv6);
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

    static void ListInterfaces()
    {
        Console.WriteLine("************************INTERFACES************************");
        Console.WriteLine("DEV    (SHORT)  IP/MASK                        TYPE     UP MTU   MAC");

        var interfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (var iface in interfaces)
        {
            string devName = iface.Name;
            string shortName = iface.Name;
            string type = iface.NetworkInterfaceType.ToString().ToLower();
            string status = iface.OperationalStatus == OperationalStatus.Up ? "up" : "down";
            int mtu = iface.GetIPProperties().GetIPv4Properties()?.Mtu ?? 0;

            byte[] macBytes = iface.GetPhysicalAddress().GetAddressBytes();
            string mac = macBytes.Length > 0
                ? string.Join(":", macBytes.Select(b => b.ToString("X2")))
                : "";

            foreach (var addrInfo in iface.GetIPProperties().UnicastAddresses)
            {
                string ip = addrInfo.Address.ToString();
                int prefix = addrInfo.PrefixLength;
                string ipMask = $"{ip}/{prefix}";
                
                Console.WriteLine(
                    $"{devName,-7}({shortName,-7}) {ipMask,-29} {type,-8} {status,-2} {mtu,-5} {mac}");
            }
        }
    }
}