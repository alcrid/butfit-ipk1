using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace project1;

internal static class Program
{
    static void Main(string[] args)
    {
        string target = "";
        int timeout = 5000; // Default timeout is 5000ms
        string? interfaceName = null;
        List<int> tcpPorts = new List<int>();
        List<int> udpPorts = new List<int>();

        // Handle no args or --interface without value
        if (args.Length == 0 || (args.Length == 1 && (args[0] == "-i" || args[0] == "--interface")))
        {
            ListInterfaces();
            return;
        }

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
                    if (!int.TryParse(args[++i], out timeout) || timeout <= 0)
                    {
                        Console.Error.WriteLine("Invalid timeout value.");
                        Environment.Exit(1);
                    }
                    break;

                default:
                    if (string.IsNullOrEmpty(target))
                        target = args[i];
                    break;
            }
        }

        if (string.IsNullOrEmpty(target))
        {
            Console.Error.WriteLine("Error: No target specified.");
            Environment.Exit(1);
        }

        try
        {
            IPAddress[] resolvedAddresses = Dns.GetHostAddresses(target);
            
            if (resolvedAddresses.Length == 0)
            {
                Console.Error.WriteLine("Error: No IP addresses resolved.");
                Environment.Exit(1);
            }
            
            var ipv4Address = resolvedAddresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork);
            var ipv6Address = resolvedAddresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetworkV6);

            // Scan TCP ports
            foreach (var port in tcpPorts)
            {
                if (ipv4Address != null)
                {
                    TcpScanner.Scan(ipv4Address.ToString(), port, timeout, isIpv6: false, interfaceName);
                }
                if(ipv6Address != null)
                {
                    TcpScanner.Scan(ipv6Address.ToString(), port, timeout, isIpv6: true, interfaceName);
                }
            }

            // Scan UDP ports
            foreach (var port in udpPorts)
            {
                if (ipv4Address != null)
                {
                    UdpScanner.Scan(ipv4Address.ToString(), port, timeout, isIpv6: false, interfaceName);
                }
                if(ipv6Address != null)
                {
                    UdpScanner.Scan(ipv6Address.ToString(), port, timeout, isIpv6: true, interfaceName);
                }
            }
        }
        catch (SocketException ex)
        {
            Console.Error.WriteLine($"Error: Failed to resolve target '{target}': {ex.Message}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    // Parse the port ranges
    static List<int> ParsePorts(string input)
    {
        List<int> ports = new List<int>();
        foreach (string part in input.Split(','))
        {
            if (part.Contains('-'))
            {
                var range = part.Split('-');
                if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                {
                    if (start > end || start < 1 || end > 65535)
                    {
                        Console.Error.WriteLine($"Invalid port range: {start}-{end} (must be between 1-65535)");
                        Environment.Exit(1);
                    }

                    for (var i = start; i <= end; i++)
                        ports.Add(i);
                }
                else
                {
                    Console.Error.WriteLine($"Invalid port range format: {part}");
                    Environment.Exit(1);
                }
            }
            else
            {
                if (int.TryParse(part, out int port))
                {
                    if (port < 1 || port > 65535)
                    {
                        Console.Error.WriteLine($"Invalid port value: {port} (must be between 1-65535)");
                        Environment.Exit(1);
                    }
                    ports.Add(port);
                }
                else
                {
                    Console.Error.WriteLine($"Invalid port value: {part}");
                    Environment.Exit(1);
                }
            }
        }
        return ports;
    }

    // Lists out the interfaces like nmap
    private static void ListInterfaces()
    {
        Console.WriteLine("************************INTERFACES************************");
        Console.WriteLine("DEV    (SHORT)  IP/MASK                        TYPE     UP MTU   MAC");

        var interfaces = NetworkInterface.GetAllNetworkInterfaces();

        foreach (var iface in interfaces)
        {
            var devName = iface.Name;
            var shortName = iface.Name;
            var type = iface.NetworkInterfaceType.ToString().ToLower();
            var status = iface.OperationalStatus == OperationalStatus.Up ? "up" : "down";
            var mtu = iface.GetIPProperties().GetIPv4Properties()?.Mtu ?? 0;

            var macBytes = iface.GetPhysicalAddress().GetAddressBytes();
            var mac = macBytes.Length > 0
                ? string.Join(":", macBytes.Select(b => b.ToString("X2")))
                : "";

            foreach (var addrInfo in iface.GetIPProperties().UnicastAddresses)
            {
                var ip = addrInfo.Address.ToString();
                var prefix = addrInfo.PrefixLength;
                var ipMask = $"{ip}/{prefix}";

                Console.WriteLine(
                    $"{devName,-7}({shortName,-7}) {ipMask,-29} {type,-8} {status,-2} {mtu,-5} {mac}");
            }
        }
    }
}
