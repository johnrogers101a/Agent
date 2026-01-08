<#
.SYNOPSIS
    Deploys Azure AI Foundry infrastructure with gpt-oss-120b model.

.DESCRIPTION
    Orchestrates the deployment of all required Azure resources:
    - Resource Group
    - AI Services Account (Cognitive Services)
    - gpt-oss-120b Model Deployment
    - Network Rules (IP whitelisting)
    - RBAC Role Assignment

    Also updates the appsettings.json file with the endpoint configuration.

.PARAMETER Config
    A hashtable containing all configuration values. Typically loaded from Config.psd1.

.EXAMPLE
    $config = Import-PowerShellDataFile "./Config.psd1"
    Deploy-AzFoundry -Config $config
#>
function Deploy-AzFoundry {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [hashtable]$Config
    )

    $ErrorActionPreference = "Stop"

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Magenta
    Write-Host "  Azure AI Foundry Deployment" -ForegroundColor Magenta
    Write-Host "========================================" -ForegroundColor Magenta
    Write-Host ""
    Write-Host "Configuration:" -ForegroundColor Cyan
    Write-Host "  Subscription:    $($Config.SubscriptionName)" -ForegroundColor White
    Write-Host "  Resource Group:  $($Config.ResourceGroupName)" -ForegroundColor White
    Write-Host "  Location:        $($Config.Location)" -ForegroundColor White
    Write-Host "  Model:           $($Config.ModelName)" -ForegroundColor White
    Write-Host ""

    # Verify Azure CLI is logged in
    Write-Host "Verifying Azure CLI session..." -ForegroundColor Cyan
    $account = az account show 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
    if (-not $account) {
        throw "Not logged in to Azure CLI. Run 'az login' first."
    }
    Write-Host "  Logged in as: $($account.user.name)" -ForegroundColor Green

    # Set subscription
    Write-Host "Setting subscription to '$($Config.SubscriptionName)'..." -ForegroundColor Cyan
    az account set --subscription $Config.SubscriptionName
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to set subscription. Verify the subscription name is correct."
    }
    Write-Host "  Subscription set successfully" -ForegroundColor Green
    Write-Host ""

    # Get public IP for whitelisting
    $publicIp = Get-PublicIpAddress
    Write-Host ""

    # Generate unique suffix for globally unique resource names
    $suffix = (Get-Random -Minimum 10000 -Maximum 99999).ToString()
    $prefix = $Config.ResourcePrefix.ToLower() -replace '[^a-z0-9]', ''

    # Resource names
    $aiServicesName = "$($prefix)-ai-$suffix"

    # Step 1: Resource Group
    Write-Host ""
    $rg = Ensure-ResourceGroup `
        -ResourceGroupName $Config.ResourceGroupName `
        -Location $Config.Location

    # Step 2: AI Services Account
    Write-Host ""
    $aiServices = Ensure-AIServicesAccount `
        -AccountName $aiServicesName `
        -ResourceGroupName $Config.ResourceGroupName `
        -Location $Config.Location

    # Step 3: Model Deployment
    Write-Host ""
    $deployment = Ensure-ModelDeployment `
        -AccountName $aiServicesName `
        -ResourceGroupName $Config.ResourceGroupName `
        -DeploymentName $Config.ModelName `
        -ModelName $Config.ModelName `
        -ModelVersion $Config.ModelVersion `
        -ModelFormat $Config.ModelFormat `
        -SkuName $Config.SkuName `
        -SkuCapacity $Config.SkuCapacity

    # Step 4: Network Rules
    Write-Host ""
    Set-NetworkRule `
        -AccountName $aiServicesName `
        -ResourceGroupName $Config.ResourceGroupName `
        -IpAddress $publicIp

    # Step 5: RBAC Role Assignment
    Write-Host ""
    $subscriptionId = (az account show --query id -o tsv)
    $aiServicesResourceId = "/subscriptions/$subscriptionId/resourceGroups/$($Config.ResourceGroupName)/providers/Microsoft.CognitiveServices/accounts/$aiServicesName"
    Set-RbacRole `
        -Scope $aiServicesResourceId `
        -RoleName "Cognitive Services OpenAI User"

    # Step 6: Update appsettings.json
    Write-Host ""
    $endpoint = "https://$aiServicesName.openai.azure.com"
    
    if ($Config.AppSettingsPath -and (Test-Path $Config.AppSettingsPath)) {
        Update-AppSettings `
            -AppSettingsPath $Config.AppSettingsPath `
            -Endpoint $endpoint `
            -ModelName $Config.ModelName
    }
    else {
        Write-Warning "AppSettingsPath not configured or file not found. Skipping appsettings.json update."
    }

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Deployment Complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Resources created:" -ForegroundColor Cyan
    Write-Host "  Resource Group:   $($Config.ResourceGroupName)" -ForegroundColor White
    Write-Host "  AI Services:      $aiServicesName" -ForegroundColor White
    Write-Host "  Model Deployment: $($Config.ModelName)" -ForegroundColor White
    Write-Host ""
    Write-Host "Endpoint: $endpoint" -ForegroundColor Yellow
    Write-Host ""

    # Return deployment info for tests
    return @{
        ResourceGroupName  = $Config.ResourceGroupName
        AIServicesName     = $aiServicesName
        ModelName          = $Config.ModelName
        Endpoint           = $endpoint
        ApiVersion         = $Config.ApiVersion
        PublicIp           = $publicIp
    }
}
