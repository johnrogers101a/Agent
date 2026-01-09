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

    Write-Information "Detecting public IP address..."

    try {
        $ip = (Invoke-RestMethod -Uri "https://ifconfig.me/ip" -TimeoutSec 10).Trim()
        Write-Information "  Public IP: $ip"
        return $ip
    }
    catch {
        # Try alternative service
        try {
            $ip = (Invoke-RestMethod -Uri "https://api.ipify.org" -TimeoutSec 10).Trim()
            Write-Information "  Public IP: $ip"
            return $ip
        }
        catch {
            throw "Failed to detect public IP address. Check your internet connection."
        }
    }
}
