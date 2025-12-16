#!/bin/bash
hosts="/etc/hosts"
if [ ! -f "$hosts" ]; then
  echo "$hosts not found."
  exit 1
fi

# Find the line number of the last line that starts with "##"
last_index=$(grep -n '^##' "$hosts" | tail -n 1 | cut -d: -f1)

# Check if the line exists
if [ -z "$last_index" ]; then
  echo No lines start with '##' in $hosts
  exit 1
fi

# Insert DNS-record after the last "##" line
record="127.0.0.1       threemammals.com"
# This 3-line sed script fixes the issue when embedded as a run-action script in GitHub Actions.
# The problem prevents the workflow file from being parsed correctly, which stops the workflow from starting.
sudo sed -i '' "${last_index}a\\
$record
" $hosts

echo "Inserted '$record' after line $last_index."
echo DNS-record added to $hosts
echo ------------------------
cat $hosts
echo ------------------------

# The threemammals.com domain is registered for email services
# https://registrar.ionos.com/domains_raa/whois
# So, go ahead and clear the DNS cache
sudo killall -HUP mDNSResponder

ping -c 3 threemammals.com
