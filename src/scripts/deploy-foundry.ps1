<#
.SYNOPSIS
    Deploys Azure AI Foundry with gpt-oss-120b and runs verification tests.

.DESCRIPTION
    Single entry point script that:
    1. Loads configuration from Config.psd1
    2. Imports the AzFoundryDeploy module
    3. Deploys all Azure resources
    4. Updates PersonalAgent/appsettings.json
    5. Runs Pester tests to verify deployment

.NOTES
    Prerequisites:
    - Azure CLI installed and logged in (az login)
    - Az CLI ml extension (az extension add --name ml)
    - Pester module (Install-Module Pester -Force)
    - PowerShell 7.0+

.EXAMPLE
    .\deploy-foundry.ps1
#>

#Requires -Version 7.0

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Azure AI Foundry Deployment Script                            ║" -ForegroundColor Cyan
Write-Host "║  Model: gpt-oss-120b | Region: West US                         ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Verify prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Cyan

# Check Azure CLI
$azVersion = az version 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
if (-not $azVersion) {
    throw "Azure CLI is not installed. Install from: https://aka.ms/installazurecli"
}
Write-Host "  ✓ Azure CLI $($azVersion.'azure-cli')" -ForegroundColor Green

# Check Azure CLI ml extension
$mlExtension = az extension show --name ml 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
if (-not $mlExtension) {
    Write-Host "  Installing Azure ML extension..." -ForegroundColor Yellow
    az extension add --name ml --yes
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install Azure ML extension"
    }
}
Write-Host "  ✓ Azure ML extension installed" -ForegroundColor Green

# Check Pester
$pester = Get-Module -ListAvailable -Name Pester | Sort-Object Version -Descending | Select-Object -First 1
if (-not $pester -or $pester.Version -lt [Version]"5.0.0") {
    Write-Host "  Installing Pester module..." -ForegroundColor Yellow
    Install-Module Pester -Force -SkipPublisherCheck -Scope CurrentUser
}
Write-Host "  ✓ Pester module available" -ForegroundColor Green

Write-Host ""

# Get script paths
$scriptRoot = $PSScriptRoot
$modulePath = Join-Path $scriptRoot "AzFoundryDeploy"
$configPath = Join-Path $modulePath "Config.psd1"
$testsPath = Join-Path $modulePath "Tests"
$appSettingsPath = Join-Path $scriptRoot ".." "PersonalAgent" "appsettings.json" | Resolve-Path

# Load configuration
Write-Host "Loading configuration..." -ForegroundColor Cyan
$config = Import-PowerShellDataFile $configPath
$config.AppSettingsPath = $appSettingsPath.Path
Write-Host "  ✓ Configuration loaded from Config.psd1" -ForegroundColor Green
Write-Host ""

# Import module
Write-Host "Importing AzFoundryDeploy module..." -ForegroundColor Cyan
Import-Module $modulePath -Force
Write-Host "  ✓ Module imported" -ForegroundColor Green
Write-Host ""

# Run deployment
Write-Host "Starting deployment..." -ForegroundColor Cyan
Write-Host ""

$Global:DeploymentInfo = Deploy-AzFoundry -Config $config

Write-Host ""
Write-Host "Running verification tests..." -ForegroundColor Cyan
Write-Host ""

# Run Pester tests
$pesterConfig = New-PesterConfiguration
$pesterConfig.Run.Path = $testsPath
$pesterConfig.Output.Verbosity = "Detailed"
$pesterConfig.Run.Exit = $false

$testResults = Invoke-Pester -Configuration $pesterConfig

Write-Host ""
if ($testResults.FailedCount -eq 0) {
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║  All tests passed! Deployment verified successfully.           ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
}
else {
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║  Some tests failed. Review the output above.                   ║" -ForegroundColor Red
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Red
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. cd src/PersonalAgent" -ForegroundColor White
Write-Host "  2. dotnet run" -ForegroundColor White
Write-Host ""
