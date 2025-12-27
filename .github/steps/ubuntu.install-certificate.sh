#!/bin/bash
# Install mycert2.crt certificate
crt='./test/Ocelot.AcceptanceTests/mycert2.crt'
if [ -f "$crt" ]; then
  echo mycert2.crt file found
fi

openssl version

# Copy the certificate to the system's trusted CA directory
echo Moving the certificate to the trusted CA store...
cert='/usr/local/share/ca-certificates/mycert2.crt'
sudo cp $crt $cert

echo Updating the trusted certificates...
sudo update-ca-certificates # This will add mycert.crt to the trusted root storage

echo Verifying installation by listing in /etc/ssl/certs/ folder...
sudo ls /etc/ssl/certs/ | grep mycert

echo Verifying installation by openssl for $cert file...
sudo chmod 644 $cert # adjusting the permissions
ls -l $cert # verify ownership
openssl x509 -in $cert -text -noout

echo Installation is DONE
