<#
.SYNOPSIS
    Ensures an Azure AI Services account exists.

.DESCRIPTION
    Checks if the specified AI Services (Cognitive Services) account exists. If not, creates it.
    This is required for deploying models like gpt-oss-120b.

.PARAMETER AccountName
    The name of the AI Services account.

.PARAMETER ResourceGroupName
    The name of the resource group.

.PARAMETER Location
    The Azure region for the account.

.EXAMPLE
    Ensure-AIServicesAccount -AccountName "ai-myapp" -ResourceGroupName "rg-myapp" -Location "westus"
#>
function Ensure-AIServicesAccount {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$AccountName,

        [Parameter(Mandatory)]
        [string]$ResourceGroupName,

        [Parameter(Mandatory)]
        [string]$Location
    )

    Write-Host "Checking AI Services account '$AccountName'..." -ForegroundColor Cyan

    $existing = az cognitiveservices account show `
        --name $AccountName `
        --resource-group $ResourceGroupName `
        2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue

    if ($existing) {
        Write-Host "  AI Services account '$AccountName' already exists" -ForegroundColor Green
        return $existing
    }

    Write-Host "  Creating AI Services account '$AccountName'..." -ForegroundColor Yellow
    $result = az cognitiveservices account create `
        --name $AccountName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --kind AIServices `
        --sku S0 `
        --custom-domain $AccountName `
        --yes `
        | ConvertFrom-Json

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create AI Services account '$AccountName'"
    }

    Write-Host "  AI Services account '$AccountName' created successfully" -ForegroundColor Green
    return $result
}
