# Project 1 - OMEGA: L4 Scanner
**Author**: xkomanj00 (xkomanj00@vutbr.cz)

---

## Executive Summary

This project implements a TCP and UDP port scanner in C#, compliant with the assignment specification of the IPK course at FIT VUT. It performs Layer 4 scanning on IPv4 and IPv6 hosts to determine the state of given ports using crafted network packets and raw sockets. The scanner supports both TCP SYN scans and UDP scans, following RFC standards, and provides output in the required format for automated evaluation.

---

## Theory and Technical Background

### TCP SYN Scan
A SYN scan is a type of half-open scanning that sends a SYN packet to the target port and waits for a response. If a SYN/ACK response is received, the port is open. If an RST response is received, the port is closed. If no response is received (even after retransmissions), the port is considered filtered.

| Probe Response         | Assigned State |
|------------------------|----------------|
| TCP SYN/ACK response   | open           |
| TCP RST response       | closed         |
| No response (even after retransmissions) | filtered |

**Scan of open port**  
![Open Port](img.png)

**Scan of closed port**  
![Closed Port](img_1.png)

**Scan of filtered port**  
![Filtered Port](img_2.png)

Reference: [Nmap SYN Scan](https://nmap.org/book/synscan.html)

### UDP Scan
UDP scanning works by sending a UDP packet to every targeted port. For most ports, this packet will be empty (no payload), but for a few of the more common ports, a protocol-specific payload will be sent. Based on the response, or lack thereof, the port is assigned to one of the following states:

| Probe Response | Assigned State   |
|----------------|------------------|
| Any UDP response from the target port (unusual) | open        |
| No response received (even after retransmissions) | open or filtered |
| ICMP port unreachable error (type 3, code 3) | closed       |
| Other ICMP unreachable errors (type 3, code 1, 2, 9, 10, or 13) | filtered |

In your project, the UDP scan will report both open and filtered states together, as the distinction between "open" and "filtered" is not required. If no response is received, the port is treated as **filtered**. If an ICMP "Port Unreachable" (ICMP type 3, code 3 for IPv4 or type 1, code 4 for IPv6) is received, the port is marked **closed**.

**UDP scan flow:**
- **Any UDP response** from the target port (though this is unusual) is marked as **open**.
- **No response** means the port is either **open** or **filtered**.
- **ICMP type 3, code 3** (IPv4) or **type 1, code 4** (IPv6) means the port is **closed**.
- **Other ICMP unreachable errors** (type 3, codes 1, 2, 9, 10, or 13) suggest the port is **filtered**.

**UDP scan example:**
If you are scanning port `53` (commonly used for DNS):
- If the port is **open**, the scanner will not receive any response.
- If the port is **closed**, the scanner will receive an ICMP "Port Unreachable" message (ICMP type 3, code 3 for IPv4 or type 1, code 4 for IPv6).

**UDP Scan Example:**
![UDP Scan](img_3.png)

Reference: [Nmap UDP Scan](https://nmap.org/book/scan-methods-udp-scan.html)

---

## Application Structure

### Main Components
- **`Program.cs`**: Entry point, parses CLI arguments, and dispatches to scanner modules.
- **`TcpScanner.cs`**: Crafts and sends SYN packets, receives and analyzes responses.
- **`UdpScanner.cs`**: Sends UDP datagrams, listens for ICMP messages.
- **`Utils.cs`**: Network utility functions (e.g., interface IP resolution).
- **`Ipv4Header.cs`, `Ipv6Header.cs`, `TcpHeader.cs`**: Responsible for raw header creation and checksum computation.

### Output Format
<IP> <PORT> <tcp|udp> <open|closed|filtered>

**Example Output:**
127.0.0.1 22 tcp open 127.0.0.1 53 udp closed

### Usage
```bash
./ipk-l4-scan [-i interface] [-t ports] [-u ports] [-w timeout] target
```
# Example
```bash
./ipk-l4-scan -i eth0 -t 80,443 -u 53 www.vutbr.cz
```

## TCP Scanning

- Raw sockets are used to send SYN packets.
- Custom implementation of TCP, IPv4, and IPv6 header crafting.
- Responses are parsed manually from raw bytes.
- Handles both IPv4 and IPv6 packet parsing.

### Creating a custom IPv4 header:
The `Ipv4Header` class is responsible for crafting the IPv4 header for each packet, ensuring the correct values for the IP version, header length, total length, protocol, and checksum.

### Creating an IPv6 header:
The `Ipv6Header` class constructs the IPv6 header, including the source and destination IPs and payload length. It also sets the next header value (TCP for this case) and the hop limit (set to 64).

### Creating TCP header:
The `TcpHeader` class builds the TCP header, setting values such as source and destination ports, sequence numbers, flags (SYN in this case), and window size.

## UDP Scanning

- Sends UDP packets using `UdpClient`.
- Listens for ICMP error responses using raw sockets.
- Differentiates port status based on expected ICMP types and codes.

In UDP scanning, when a UDP datagram is sent to a target port, if the port is closed, an ICMP "Port Unreachable" message (ICMP type 3, code 3 for IPv4 or type 1, code 4 for IPv6) is expected. If no ICMP message is received, it indicates that the port is open.

## Header Construction

- `Ipv4Header` and `Ipv6Header` classes create valid IP headers with correct checksums.
- `TcpHeader` constructs SYN packets and computes pseudo-header checksums manually.

## Testing

### What was tested:
- TCP and UDP port status detection (open, closed, filtered)
- IPv4 and IPv6 functionality
- Timeout behavior
- Interface listing
- Invalid input handling

### Why it was tested:
To verify scanner reliability, and usability under different scenarios and edge cases.

## Argparser tests
In the file `test_args.sh`, combinations of invalid inputs were tested to see if they return invalid:

- `"sudo ./ipk-l4-scan -i eht0"`  # Typo in interface
- `"sudo ./ipk-l4-scan --interface enp0s3"`  # Missing target
- `"sudo ./ipk-l4-scan -u 20"`  # Missing target
- `"sudo ./ipk-l4-scan --pu 20 127.0.0.1"`  # `--pu` needs range
- `"sudo ./ipk-l4-scan --pt 20 127.0.0.1"`  # `--pt` needs range
- `"sudo ./ipk-l4-scan -i enp0s3 -w -t 20 localhost"`  # `-w` missing value
- `"sudo ./ipk-l4-scan -i enp0s3 -w 300 -t 2000000 localhost"`  # Port out of range
- `"sudo ./ipk-l4-scan -i enp0s3 --pu -20 127.0.0.1 www.fit.vutbr.cz"`  # Negative port
- `"sudo ./ipk-l4-scan -i enp0s3 --pt 20,30,40,-20 127.0.0.1"`  # Negative port
- `"sudo ./ipk-l4-scan -i enp0s3 -w 120 -u 200-45 --pu 33 www.fit.vutbr.cz"`  # Invalid range
- `"sudo ./ipk-l4-scan -i enp0s3 --pt 20-30-40 127.0.0.1"`  # Invalid range format

### How it was tested:
- Ran the scanner against localhost and known public IPs.
- Verified known open and closed ports using `nmap`.
- Simulated filtered ports using `iptables` on localhost.

### Example Test:

bash
$ ./ipk-l4-scan -i eth0 -t 22,80 -u 53 localhost
127.0.0.1 22 tcp open
127.0.0.1 80 tcp closed
Compared against:

bash
$ nmap -sS -sU -p 22,80,53 localhost

## IPV4
### UDP Tests
- **Open port on localhost**: sudo nc -lu 123 → shows open in both Nmap and our scanner
- **Public UDP test**: sudo nmap -sU -p 33400-33500 8.8.8.8 → found port 33440 closed
- **Our scanner result**: also reports 33440 as closed (matches Nmap)

### TCP Tests
- Used Nmap to find open ports on localhost:

631/tcp  open  ipp
1000/tcp open  cadlock


- Ran scanner:

bash
sudo ./ipk-l4-scan --pt 630-635 localhost


Output:

127.0.0.1 630 tcp closed
127.0.0.1 631 tcp open
127.0.0.1 632 tcp closed
127.0.0.1 633 tcp closed
127.0.0.1 634 tcp closed
127.0.0.1 635 tcp closed

### TCP Filtered Simulation with iptables

bash
sudo iptables -I INPUT -i lo -p tcp --dport 632 -j DROP

Scanner output:

127.0.0.1 632 tcp filtered


IPV6
UDP
sudo sysctl -w net.ipv6.conf.all.disable_ipv6=0
sudo sysctl -w net.ipv6.conf.default.disable_ipv6=0
enable ipv6 on device



## Bibliography
- RFC 793: Transmission Control Protocol
- RFC 768: User Datagram Protocol
- RFC 791: Internet Protocol
- RFC 8200: IPv6 Specification
- Nmap Network Scanning: [Nmap SYN Scan](https://nmap.org/book/synscan.html)
- Wikipedia: Port Scanner - [Port Scanner Wikipedia](https://en.wikipedia.org/wiki/Port_scanner)
- Satrapa, Pavel: *IPv6: Internetový protokol verze 6*, CZ.NIC, 2019
- RFC 2553: Basic Socket Interface Extensions for IPv6

## License
See LICENSE for licensing details.

## Changelog
See CHANGELOG.md for implementation history and known issues.
