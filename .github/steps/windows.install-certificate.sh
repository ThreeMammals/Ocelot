#!/bin/bash
echo "Hello from Bash"
date

CRT_FILE="./acceptance/mycert2.crt"

if [ ! -f "$CRT_FILE" ]; then
  echo "Error: Certificate file not found: $CRT_FILE"
  exit 1
fi

echo "mycert2.crt file found"
openssl version

echo "Importing certificate to Trusted Root store (requires Administrator)..."
certutil -addstore -f "Root" "$CRT_FILE"

echo "------------------------"
echo "Verification:"
certutil -store Root | grep -i  "threemammals"

echo "Installation is DONE"
echo "You can also open certlm.msc and check Trusted Root Certification Authorities."
