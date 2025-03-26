using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace project1;

public class Utils
{
    public static IPAddress GetInterfaceAddress(string? interfaceName, bool isIpv6)
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