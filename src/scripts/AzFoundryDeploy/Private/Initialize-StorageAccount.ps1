<#
.SYNOPSIS
    Initializes an Azure Storage Account.

.DESCRIPTION
    Ensures the specified storage account exists. If it doesn't exist, creates it.
    Storage accounts are required for Azure Functions runtime. This operation is idempotent.

.PARAMETER StorageAccountName
    The name of the storage account (must be globally unique, 3-24 lowercase alphanumeric).

.PARAMETER ResourceGroupName
    The name of the resource group.

.PARAMETER Location
    The Azure region for the storage account.

.EXAMPLE
    Initialize-StorageAccount -StorageAccountName "stmyapp" -ResourceGroupName "rg-myapp" -Location "westus"
#>
function Initialize-StorageAccount {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [ValidatePattern('^[a-z0-9]{3,24}$')]
        [string]$StorageAccountName,

        [Parameter(Mandatory)]
        [string]$ResourceGroupName,

        [Parameter(Mandatory)]
        [string]$Location
    )

    Write-Information "Checking storage account '$StorageAccountName'..."

    # First check if storage account exists in our resource group
    $existing = az storage account show `
        --name $StorageAccountName `
        --resource-group $ResourceGroupName `
        2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue

    if ($existing) {
        Write-Information "  [OK] Storage account '$StorageAccountName' already exists"
        return $existing
    }

    # Check if we own this storage account in a different resource group (same subscription)
    $allStorageAccounts = az storage account list --query "[?name=='$StorageAccountName']" 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
    if ($allStorageAccounts -and $allStorageAccounts.Count -gt 0) {
        $existingRg = $allStorageAccounts[0].resourceGroup
        throw "Storage account '$StorageAccountName' exists in resource group '$existingRg'. Either delete it or update StorageAccountName in Config.psd1."
    }

    # Check if the name is taken globally (by someone else)
    $nameCheck = az storage account check-name --name $StorageAccountName 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
    if ($nameCheck -and -not $nameCheck.nameAvailable) {
        throw "Storage account name '$StorageAccountName' is already taken globally by another Azure subscription. Update StorageAccountName in Config.psd1 to a unique name."
    }

    if (-not $PSCmdlet.ShouldProcess($StorageAccountName, "Create storage account")) {
        return
    }

    Write-Information "  Creating storage account '$StorageAccountName'..."
    $result = az storage account create `
        --name $StorageAccountName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --sku Standard_LRS `
        2>$null | ConvertFrom-Json

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create storage account '$StorageAccountName'"
    }

    Write-Information "  [OK] Storage account '$StorageAccountName' created"
    return $result
}
