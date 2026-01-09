<#
.SYNOPSIS
    Adds an IP address to the network rules for a Cognitive Services account.

.DESCRIPTION
    Adds the specified IP address to the allowed list for the Cognitive Services account.
    If the IP is already whitelisted, does nothing.

.PARAMETER AccountName
    The name of the Cognitive Services account.

.PARAMETER ResourceGroupName
    The name of the resource group.

.PARAMETER IpAddress
    The IP address to whitelist.

.EXAMPLE
    Add-NetworkRule -AccountName "ais-agents" -ResourceGroupName "rg-agents" -IpAddress "203.0.113.50"
#>
function Add-NetworkRule {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$AccountName,

        [Parameter(Mandatory)]
        [string]$ResourceGroupName,

        [Parameter(Mandatory)]
        [string]$IpAddress
    )

    Write-Information "Configuring network rules for '$AccountName'..."
    Write-Information "  Whitelisting IP: $IpAddress"

    # Get current network rules
    $account = az cognitiveservices account show `
        --name $AccountName `
        --resource-group $ResourceGroupName `
        2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue

    if (-not $account) {
        throw "Cognitive Services account '$AccountName' not found"
    }

    # Build IP rules array with existing + new IP
    $existingRules = $account.properties.networkAcls.ipRules
    $ipAlreadyExists = $existingRules | Where-Object { $_.value -eq $IpAddress }

    if ($ipAlreadyExists) {
        Write-Information "  [OK] IP '$IpAddress' is already whitelisted"
        $ipRulesArray = @($existingRules | ForEach-Object { @{ value = $_.value } })
    } else {
        # Add new IP to existing rules
        $ipRulesArray = @()
        if ($existingRules) {
            $ipRulesArray += @($existingRules | ForEach-Object { @{ value = $_.value } })
        }
        $ipRulesArray += @{ value = $IpAddress }
        Write-Information "  [OK] IP '$IpAddress' whitelisted"
    }

    # Use REST API to set everything in one call: IP rules, default action, and bypass
    $subscriptionId = az account show --query id -o tsv
    $resourceUrl = "/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.CognitiveServices/accounts/$AccountName"
    $apiVersion = "2024-10-01"
    
    $networkBody = @{
        properties = @{
            networkAcls = @{
                defaultAction = "Deny"
                bypass = "AzureServices"
                ipRules = $ipRulesArray
            }
        }
    } | ConvertTo-Json -Depth 10
    
    $bodyFile = [System.IO.Path]::GetTempFileName()
    $networkBody | Set-Content $bodyFile -Encoding UTF8
    
    az rest --method PATCH --url "$resourceUrl`?api-version=$apiVersion" --body "@$bodyFile" --output none 2>$null
    Remove-Item $bodyFile -ErrorAction SilentlyContinue
    
    Write-Information "  [OK] Network rules configured (Azure services bypass enabled)"
}
