# Changelog

## v1.0 - Initial Release

### Added
- TCP SYN scanner with IPv4 and IPv6 support.
- UDP scanner with ICMP response detection.
- Argument parsing for:
    - Interface selection
    - Port ranges for TCP and UDP
    - Timeout configuration
- Interface listing functionality (`--interface` with or without value).
- Formatted output compliant with assignment spec.
- Support for domain name resolution and multiple IP addresses.
- IPv4/IPv6 header and TCP header manual construction.

### Fixed
- Exit codes for different error conditions (invalid input, DNS failure, etc.)
- Input validation for port numbers and ranges.

### Known Limitations
- Does not support scanning multiple interfaces simultaneously.
- No concurrency/multi-threading for parallel scans (sequential scanning). 
- Low Timeout Issues: When the timeout value is set too low, the program may not detect replies in a timely manner, potentially leading to missed scans.