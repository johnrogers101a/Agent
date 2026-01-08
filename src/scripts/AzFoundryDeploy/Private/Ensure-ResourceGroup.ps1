<#
.SYNOPSIS
    Ensures an Azure resource group exists.

.DESCRIPTION
    Checks if the specified resource group exists. If not, creates it.
    Returns the resource group information.

.PARAMETER ResourceGroupName
    The name of the resource group.

.PARAMETER Location
    The Azure region for the resource group.

.EXAMPLE
    Ensure-ResourceGroup -ResourceGroupName "rg-myapp" -Location "westus"
#>
function Ensure-ResourceGroup {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ResourceGroupName,

        [Parameter(Mandatory)]
        [string]$Location
    )

    Write-Host "Checking resource group '$ResourceGroupName'..." -ForegroundColor Cyan

    $existing = az group show --name $ResourceGroupName 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue

    if ($existing) {
        Write-Host "  Resource group '$ResourceGroupName' already exists" -ForegroundColor Green
        return $existing
    }

    Write-Host "  Creating resource group '$ResourceGroupName' in '$Location'..." -ForegroundColor Yellow
    $result = az group create `
        --name $ResourceGroupName `
        --location $Location `
        | ConvertFrom-Json

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create resource group '$ResourceGroupName'"
    }

    Write-Host "  Resource group '$ResourceGroupName' created successfully" -ForegroundColor Green
    return $result
}
