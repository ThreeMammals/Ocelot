# Add DNS-records
Write-Host "Hello from PowerShell"
Get-Date
    
# Append entry to hosts file
Add-Content -Path "$env:SystemRoot\System32\drivers\etc\hosts" -Value "127.0.0.1 threemammals.com"

Write-Output "------------------------"
Get-Content "$env:SystemRoot\System32\drivers\etc\hosts"
Write-Output "------------------------"

# Ping 3 times
Test-Connection -ComputerName "threemammals.com" -Count 3
