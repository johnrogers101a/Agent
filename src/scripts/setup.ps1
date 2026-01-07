# Setup script for PersonalAgent user secrets

param(
    [string]$ProjectPath = (Join-Path $PSScriptRoot "..\PersonalAgent")
)

Write-Host "Setting up user secrets for PersonalAgent" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Resolve the project path
$resolvedPath = Resolve-Path $ProjectPath -ErrorAction SilentlyContinue
if (-not $resolvedPath) {
    Write-Host "Error: Could not find project at $ProjectPath" -ForegroundColor Red
    exit 1
}

Write-Host "Project path: $resolvedPath" -ForegroundColor Gray
Write-Host ""

# Prompt for secrets
$weatherApiKey = Read-Host "Enter your Weather API Key"
$gmailClientId = Read-Host "Enter your Gmail Client ID"
$gmailClientSecret = Read-Host "Enter your Gmail Client Secret"

Write-Host ""
Write-Host "Setting user secrets..." -ForegroundColor Yellow

# Set the secrets
Push-Location $resolvedPath
try {
    dotnet user-secrets set "Clients:Weather:ApiKey" $weatherApiKey
    dotnet user-secrets set "Clients:Gmail:ClientId" $gmailClientId
    dotnet user-secrets set "Clients:Gmail:ClientSecret" $gmailClientSecret
    
    Write-Host ""
    Write-Host "User secrets configured successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "To verify, run: dotnet user-secrets list" -ForegroundColor Gray
}
catch {
    Write-Host "Error setting user secrets: $_" -ForegroundColor Red
    exit 1
}
finally {
    Pop-Location
}
