<#
.SYNOPSIS
    Initializes a Foundry Project under an AI Services account.

.DESCRIPTION
    Ensures a Microsoft.CognitiveServices/accounts/projects resource exists.
    This is the container for agents, files, threads, and evaluations.
    This operation is idempotent.

.PARAMETER ProjectName
    The name of the Foundry project.

.PARAMETER AccountName
    The name of the parent AI Services account.

.PARAMETER ResourceGroupName
    The name of the resource group.

.PARAMETER Location
    The Azure region for the project.

.EXAMPLE
    Initialize-FoundryProject -ProjectName "proj-agents" -AccountName "ais-agents" -ResourceGroupName "rg-agents" -Location "westus"
#>
function Initialize-FoundryProject {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$ProjectName,

        [Parameter(Mandatory)]
        [string]$AccountName,

        [Parameter(Mandatory)]
        [string]$ResourceGroupName,

        [Parameter(Mandatory)]
        [string]$Location
    )

    Write-Information "Checking Foundry Project '$ProjectName'..."

    $existing = az cognitiveservices account project show `
        --name $AccountName `
        --project-name $ProjectName `
        --resource-group $ResourceGroupName `
        2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue

    if ($existing) {
        Write-Information "  [OK] Foundry Project '$ProjectName' already exists"
        return $existing
    }

    if (-not $PSCmdlet.ShouldProcess($ProjectName, "Create Foundry Project")) {
        return
    }

    Write-Information "  Creating Foundry Project '$ProjectName' under '$AccountName'..."

    $result = az cognitiveservices account project create `
        --name $AccountName `
        --project-name $ProjectName `
        --resource-group $ResourceGroupName `
        --location $Location `
        --output json 2>&1

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create Foundry Project '$ProjectName': $result"
    }

    $project = $result | ConvertFrom-Json -ErrorAction SilentlyContinue

    Write-Information "  [OK] Foundry Project '$ProjectName' created"
    return $project
}
