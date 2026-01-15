Write-Host "Hello from PowerShell"
Get-Date

# Install mycert2.crt certificate
$crt = ".\test\Ocelot.AcceptanceTests\mycert2.crt"
if (Test-Path $crt) {
  Write-Output "mycert2.crt file found"
}

openssl version

Write-Output "Moving the certificate to the trusted CA store..."
# Import into the Local Machine Trusted Root Certification Authorities store
$crt = ".\test\Ocelot.AcceptanceTests\mycert2.crt"
Import-Certificate -FilePath $crt -CertStoreLocation Cert:\LocalMachine\Root

Write-Output "Verifying installation by listing trusted root certificates..."
# List certificates in the Trusted Root store and filter for 'mycert'
Get-ChildItem -Path Cert:\LocalMachine\Root | Where-Object { $_.Subject -like "*threemammals*" }

Write-Output "Verifying installation by openssl for $crt file..."
$cert = Get-ChildItem Cert:\LocalMachine\Root | Where-Object { $_.Subject -like "*threemammals*" }
$cert_file = "C:\temp\mycert2_installed.cer"
Export-Certificate -Cert $cert -FilePath $cert_file
if (Test-Path $cert_file) {
  Write-Output "$cert_file file found"
}

# Display certificate details using OpenSSL (if installed)
openssl x509 -in $cert_file -text -noout

Write-Output "Installation is DONE"
