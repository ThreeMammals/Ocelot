#!/bin/bash
# Install mycert2.crt certificate
crt='./test/Ocelot.AcceptanceTests/mycert2.crt'
openssl version
echo Moving the certificate to the trusted CA store...
if [ ! -f "$crt" ]; then
  echo "Certificate file not found: $crt"
  exit 1
fi
cert_root="/Library/Keychains/System.keychain"
sudo security add-trusted-cert -d -r trustRoot -k "$cert_root" "$crt"
echo Certificate added to trusted keychain.

echo Verifying installation by listing certificates in $cert_root ...
sudo security find-certificate -a -c "threemammals" -p "$cert_root"

echo Verifying installation by openssl for $crt in $cert_root ...
# Export the matching certificate(s) in PEM directly (security find-certificate -p)
tmpcert=$(mktemp /tmp/mycert.XXXXXX.pem)
# Use sudo + tee so the redirected file is written with appropriate permissions
sudo security find-certificate -a -c "threemammals" -p "$cert_root" | sudo tee "$tmpcert" >/dev/null
if [ ! -s "$tmpcert" ]; then
  echo "Failed to export certificate to $tmpcert"
  rm -f "$tmpcert"
  exit 1
fi
chmod 644 "$tmpcert"
openssl x509 -in "$tmpcert" -text -noout
rm -f "$tmpcert"
echo Installation is DONE
