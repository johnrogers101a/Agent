<#
.SYNOPSIS
    Ensures an Azure OpenAI model deployment exists.

.DESCRIPTION
    Checks if the specified model deployment exists. If not, creates it.
    Uses the Cognitive Services API to deploy the model.

.PARAMETER AccountName
    The name of the Cognitive Services account (AI Foundry Project).

.PARAMETER ResourceGroupName
    The name of the resource group.

.PARAMETER DeploymentName
    The name for the deployment.

.PARAMETER ModelName
    The name of the model to deploy (e.g., gpt-oss-120b).

.PARAMETER ModelVersion
    The version of the model.

.PARAMETER ModelFormat
    The format of the model (e.g., OpenAI-OSS).

.PARAMETER SkuName
    The SKU name (e.g., GlobalStandard).

.PARAMETER SkuCapacity
    The SKU capacity (tokens per minute in thousands).

.EXAMPLE
    Ensure-ModelDeployment -AccountName "proj-myapp" -ResourceGroupName "rg-myapp" -DeploymentName "gpt-oss-120b" -ModelName "gpt-oss-120b" -ModelVersion "1" -ModelFormat "OpenAI-OSS" -SkuName "GlobalStandard" -SkuCapacity 10
#>
function Ensure-ModelDeployment {
    [CmdletBinding()]
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

    Write-Host "Checking model deployment '$DeploymentName'..." -ForegroundColor Cyan

    $existing = az cognitiveservices account deployment show `
        --name $AccountName `
        --resource-group $ResourceGroupName `
        --deployment-name $DeploymentName `
        2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue

    if ($existing) {
        Write-Host "  Model deployment '$DeploymentName' already exists" -ForegroundColor Green
        return $existing
    }

    Write-Host "  Creating model deployment '$DeploymentName' (this may take several minutes)..." -ForegroundColor Yellow
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

    Write-Host "  Model deployment '$DeploymentName' created successfully" -ForegroundColor Green
    return $result
}
