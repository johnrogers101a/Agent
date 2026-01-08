<#
.SYNOPSIS
    Configures network rules to whitelist an IP address.

.DESCRIPTION
    Restricts public access to the Cognitive Services account and adds
    the specified IP address to the allowed list.

.PARAMETER AccountName
    The name of the Cognitive Services account.

.PARAMETER ResourceGroupName
    The name of the resource group.

.PARAMETER IpAddress
    The IP address to whitelist.

.EXAMPLE
    Set-NetworkRule -AccountName "proj-myapp" -ResourceGroupName "rg-myapp" -IpAddress "203.0.113.50"
#>
function Set-NetworkRule {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$AccountName,

        [Parameter(Mandatory)]
        [string]$ResourceGroupName,

        [Parameter(Mandatory)]
        [string]$IpAddress
    )

    Write-Host "Configuring network rules for '$AccountName'..." -ForegroundColor Cyan
    Write-Host "  Whitelisting IP: $IpAddress" -ForegroundColor Yellow

    # Get current network rules
    $account = az cognitiveservices account show `
        --name $AccountName `
        --resource-group $ResourceGroupName `
        2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue

    if (-not $account) {
        throw "Cognitive Services account '$AccountName' not found"
    }

    # Check if IP is already whitelisted
    $existingRules = $account.properties.networkAcls.ipRules
    $ipAlreadyExists = $existingRules | Where-Object { $_.value -eq $IpAddress }

    if ($ipAlreadyExists) {
        Write-Host "  IP '$IpAddress' is already whitelisted" -ForegroundColor Green
        return
    }

    # Update network rules - set default action to Deny and add IP
    $result = az cognitiveservices account update `
        --name $AccountName `
        --resource-group $ResourceGroupName `
        --custom-domain $AccountName `
        --api-properties "networkAcls={defaultAction:'Deny',ipRules:[{value:'$IpAddress'}]}" `
        2>$null

    if ($LASTEXITCODE -ne 0) {
        # Try alternative method using network-rule add
        Write-Host "  Using alternative network rule method..." -ForegroundColor Yellow
        
        az cognitiveservices account network-rule add `
            --name $AccountName `
            --resource-group $ResourceGroupName `
            --ip-address $IpAddress `
            2>$null

        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Could not configure network rules. You may need to configure them manually in the Azure portal."
            return
        }
    }

    Write-Host "  Network rules configured successfully" -ForegroundColor Green
}
