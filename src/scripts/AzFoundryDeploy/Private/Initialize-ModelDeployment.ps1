<#
.SYNOPSIS
    Initializes an Azure OpenAI model deployment.

.DESCRIPTION
    Ensures the specified model deployment exists. If it doesn't exist, creates it.
    Uses the Cognitive Services API to deploy the model. This operation is idempotent.

.PARAMETER AccountName
    The name of the AI Services account.

.PARAMETER ResourceGroupName
    The name of the resource group.

.PARAMETER DeploymentName
    The name for the deployment.

.PARAMETER ModelName
    The name of the model to deploy (e.g., gpt-4.1).

.PARAMETER ModelVersion
    The version of the model.

.PARAMETER ModelFormat
    The format of the model (e.g., OpenAI).

.PARAMETER SkuName
    The SKU name (e.g., GlobalStandard).

.PARAMETER SkuCapacity
    The SKU capacity (tokens per minute in thousands).

.EXAMPLE
    Initialize-ModelDeployment -AccountName "ais-agents" -ResourceGroupName "rg-agents" -DeploymentName "gpt-4.1" -ModelName "gpt-4.1" -ModelVersion "2025-04-14" -ModelFormat "OpenAI" -SkuName "GlobalStandard" -SkuCapacity 10
#>
function Initialize-ModelDeployment {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$AccountName,

        [Parameter(Mandatory)]
        [string]$ResourceGroupName,

        [Parameter(Mandatory)]
        [string]$DeploymentName,

        [Parameter(Mandatory)]
        [string]$ModelName,

        [Parameter(Mandatory)]
        [string]$ModelVersion,

        [Parameter(Mandatory)]
        [string]$ModelFormat,

        [Parameter(Mandatory)]
        [string]$SkuName,

        [Parameter(Mandatory)]
        [int]$SkuCapacity
    )

    Write-Information "Checking model deployment '$DeploymentName'..."

    $existing = az cognitiveservices account deployment show `
        --name $AccountName `
        --resource-group $ResourceGroupName `
        --deployment-name $DeploymentName `
        2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue

    if ($existing) {
        Write-Information "  [OK] Model deployment '$DeploymentName' already exists"
        return $existing
    }

    if (-not $PSCmdlet.ShouldProcess($DeploymentName, "Create model deployment")) {
        return
    }

    Write-Information "  Creating model deployment '$DeploymentName' (this may take several minutes)..."
    $result = az cognitiveservices account deployment create `
        --name $AccountName `
        --resource-group $ResourceGroupName `
        --deployment-name $DeploymentName `
        --model-name $ModelName `
        --model-version $ModelVersion `
        --model-format $ModelFormat `
        --sku-capacity $SkuCapacity `
        --sku-name $SkuName `
        | ConvertFrom-Json

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create model deployment '$DeploymentName'"
    }

    Write-Information "  [OK] Model deployment '$DeploymentName' created"
    return $result
}
