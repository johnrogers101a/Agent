<#
.SYNOPSIS
    Initializes an Azure resource group.

.DESCRIPTION
    Ensures the specified resource group exists. If it doesn't exist, creates it.
    Returns the resource group information. This operation is idempotent.

.PARAMETER ResourceGroupName
    The name of the resource group.

.PARAMETER Location
    The Azure region for the resource group.

.EXAMPLE
    Initialize-ResourceGroup -ResourceGroupName "rg-myapp" -Location "westus"
#>
function Initialize-ResourceGroup {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$ResourceGroupName,

        [Parameter(Mandatory)]
        [string]$Location
    )

    Write-Information "Checking resource group '$ResourceGroupName'..."

    $existing = az group show --name $ResourceGroupName 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue

    if ($existing) {
        Write-Information "  [OK] Resource group '$ResourceGroupName' already exists"
        return $existing
    }

    if ($PSCmdlet.ShouldProcess($ResourceGroupName, "Create resource group")) {
        Write-Information "  Creating resource group '$ResourceGroupName' in '$Location'..."
        $result = az group create `
            --name $ResourceGroupName `
            --location $Location `
            | ConvertFrom-Json

        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create resource group '$ResourceGroupName'"
        }

        Write-Information "  [OK] Resource group '$ResourceGroupName' created"
        return $result
    }
}
