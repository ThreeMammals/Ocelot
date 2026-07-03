#!/bin/bash
# Add DNS records (hosts file) - Bash version for Windows

echo "Hello from Bash"
date

# Append entry to hosts file (requires Administrator privileges)
echo "127.0.0.1 threemammals.com" | tee -a "$SYSTEMROOT/System32/drivers/etc/hosts"

echo "------------------------"
cat "$SYSTEMROOT/System32/drivers/etc/hosts"
echo "------------------------"

# Ping 3 times
ping -n 3 threemammals.com
