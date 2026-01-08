<#
.SYNOPSIS
    Gets the current public IP address.

.DESCRIPTION
    Retrieves the public IP address of the current machine using an external service.

.EXAMPLE
    $ip = Get-PublicIpAddress
#>
function Get-PublicIpAddress {
    [CmdletBinding()]
    param()

    Write-Host "Detecting public IP address..." -ForegroundColor Cyan

    try {
        $ip = (Invoke-RestMethod -Uri "https://ifconfig.me/ip" -TimeoutSec 10).Trim()
        Write-Host "  Public IP: $ip" -ForegroundColor Green
        return $ip
    }
    catch {
        # Try alternative service
        try {
            $ip = (Invoke-RestMethod -Uri "https://api.ipify.org" -TimeoutSec 10).Trim()
            Write-Host "  Public IP: $ip" -ForegroundColor Green
            return $ip
        }
        catch {
            throw "Failed to detect public IP address. Check your internet connection."
        }
    }
}
