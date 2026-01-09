<#
.SYNOPSIS
    Initializes an Azure AI Service account.

.DESCRIPTION
    Ensures the specified AI Services (Cognitive Services) account exists. If it doesn't exist, creates it.
    Handles soft-deleted resources by purging them first.
    Enables project management for Foundry architecture. This operation is idempotent.

.PARAMETER AccountName
    The name of the AI Services account.

.PARAMETER ResourceGroupName
    The name of the resource group.

.PARAMETER Location
    The Azure region for the account.

.EXAMPLE
    Initialize-AIServiceAccount -AccountName "ais-agents" -ResourceGroupName "rg-agents" -Location "westus"
#>
function Initialize-AIServiceAccount {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$AccountName,

        [Parameter(Mandatory)]
        [string]$ResourceGroupName,

        [Parameter(Mandatory)]
        [string]$Location
    )

    Write-Information "Checking AI Services account '$AccountName'..."

    $existing = az cognitiveservices account show `
        --name $AccountName `
        --resource-group $ResourceGroupName `
        2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue

    if ($existing) {
        Write-Information "  [OK] AI Services account '$AccountName' already exists"
        return $existing
    }

    if (-not $PSCmdlet.ShouldProcess($AccountName, "Create AI Services account")) {
        return
    }

    # Check for soft-deleted resource and purge if found
    Write-Information "  Checking for soft-deleted resource..."
    $deletedAccounts = az cognitiveservices account list-deleted 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
    $softDeleted = $deletedAccounts | Where-Object { $_.name -eq $AccountName }

    if ($softDeleted) {
        Write-Information "  Purging soft-deleted AI Services account '$AccountName'..."
        az cognitiveservices account purge `
            --name $AccountName `
            --resource-group $ResourceGroupName `
            --location $Location `
            --output none 2>$null

        Start-Sleep -Seconds 5
        Write-Information "  [OK] Soft-deleted resource purged"
    }

    Write-Information "  Creating AI Services account '$AccountName' with project management..."

    $result = az cognitiveservices account create `
        --name $AccountName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --kind AIServices `
        --sku S0 `
        --custom-domain $AccountName `
        --allow-project-management true `
        --yes `
        --output none `
        2>&1

    if ($LASTEXITCODE -ne 0) {
        # If still failing due to soft-delete, try one more purge
        if ($result -match "soft-deleted") {
            Write-Information "  Retrying purge..."
            az cognitiveservices account purge `
                --name $AccountName `
                --resource-group $ResourceGroupName `
                --location $Location `
                --output none 2>$null
            Start-Sleep -Seconds 10

            az cognitiveservices account create `
                --name $AccountName `
                --resource-group $ResourceGroupName `
                --location $Location `
                --kind AIServices `
                --sku S0 `
                --custom-domain $AccountName `
                --allow-project-management true `
                --yes `
                --output none 2>$null

            if ($LASTEXITCODE -ne 0) {
                throw "Failed to create AI Services account '$AccountName' after purge retry"
            }
        }
        else {
            throw "Failed to create AI Services account '$AccountName': $result"
        }
    }

    Write-Information "  [OK] AI Services account '$AccountName' created"
}
