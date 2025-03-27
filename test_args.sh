invalid_commands=(
"sudo ./ipk-l4-scan -i eht0"  # Typo in interface
"sudo ./ipk-l4-scan --interface enp0s3"  # Missing target
"sudo ./ipk-l4-scan -u 20"  # Missing target
"sudo ./ipk-l4-scan -i enp0s3 -w -t 20 localhost"  # -w missing value
"sudo ./ipk-l4-scan -i enp0s3 -w 300 -t 2000000 localhost"  # Port out of range
"sudo ./ipk-l4-scan -i enp0s3 --pu -20 127.0.0.1 www.fit.vutbr.cz"  # Negative port
"sudo ./ipk-l4-scan -i enp0s3 --pt 20,30,40,-20 127.0.0.1"  # Negative port
"sudo ./ipk-l4-scan -i enp0s3 -w 120 -u 200-45 --pu 33 www.fit.vutbr.cz"  # Invalid range
"sudo ./ipk-l4-scan -i enp0s3 --pt 20-30-40 127.0.0.1"  # Invalid range format
)

passed=0
total=${#invalid_commands[@]}

# Loop through all commands
for cmd in "${invalid_commands[@]}"; do
    eval "$cmd" > /dev/null 2>&1
    if [[ $? -ne 0 ]]; then
        # If the command fails, print out the command and the failure
        ((passed++))
    else
        echo "Failed: $cmd"
    fi
done

# Output the final result
echo "Passed tests: $passed / $total"
