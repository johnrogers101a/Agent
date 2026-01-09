<#
.SYNOPSIS
    Initializes an Azure Function App (Flex Consumption).

.DESCRIPTION
    Ensures the specified Function App exists. If it doesn't exist, creates it using
    Flex Consumption hosting plan for serverless durable agents. This operation is idempotent.

.PARAMETER FunctionAppName
    The name of the function app (must be globally unique).

.PARAMETER ResourceGroupName
    The name of the resource group.

.PARAMETER StorageAccountName
    The name of the storage account for the function app.

.PARAMETER Location
    The Azure region for the function app.

.EXAMPLE
    Initialize-FunctionApp -FunctionAppName "func-myapp" -ResourceGroupName "rg-myapp" -StorageAccountName "stmyapp" -Location "westus"
#>
function Initialize-FunctionApp {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$FunctionAppName,

        [Parameter(Mandatory)]
        [string]$ResourceGroupName,

        [Parameter(Mandatory)]
        [string]$StorageAccountName,

        [Parameter(Mandatory)]
        [string]$Location
    )

    Write-Information "Checking Function App '$FunctionAppName'..."

    $existing = az functionapp show `
        --name $FunctionAppName `
        --resource-group $ResourceGroupName `
        2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue

    if ($existing) {
        Write-Information "  [OK] Function App '$FunctionAppName' already exists"
        return $existing
    }

    # Check if we own this function app in a different resource group (same subscription)
    $allFunctionApps = az functionapp list --query "[?name=='$FunctionAppName']" 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
    if ($allFunctionApps -and $allFunctionApps.Count -gt 0) {
        $existingRg = $allFunctionApps[0].resourceGroup
        throw "Function App '$FunctionAppName' exists in resource group '$existingRg'. Either delete it or update FunctionAppName in Config.psd1."
    }

    if (-not $PSCmdlet.ShouldProcess($FunctionAppName, "Create Function App")) {
        return
    }

    # Check Flex Consumption availability in target region
    $flexLocations = az functionapp list-flexconsumption-locations --output json 2>$null | ConvertFrom-Json
    $targetLocation = $Location.ToLower() -replace ' ', ''

    if ($flexLocations -and $flexLocations.name -notcontains $targetLocation) {
        Write-Information "  Note: Flex Consumption not available in '$Location', using 'northeurope'"
        $targetLocation = "northeurope"
    }

    Write-Information "  Creating Function App '$FunctionAppName' (Flex Consumption)..."
    $result = az functionapp create `
        --name $FunctionAppName `
        --resource-group $ResourceGroupName `
        --storage-account $StorageAccountName `
        --flexconsumption-location $targetLocation `
        --runtime dotnet-isolated `
        --runtime-version 10.0 `
        --functions-version 4 `
        | ConvertFrom-Json

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create Function App '$FunctionAppName'"
    }

    Write-Information "  [OK] Function App '$FunctionAppName' created"
    return $result
}
