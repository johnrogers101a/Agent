# Start the Personal Agent
# Automatically detects mode from appsettings.json (FunctionMode: true/false)

$ErrorActionPreference = "Stop"

$projectPath = "$PSScriptRoot\..\Agents\Personal"
$settingsPath = "$projectPath\appsettings.json"

# Read settings to determine mode
$settings = Get-Content $settingsPath | ConvertFrom-Json
$functionMode = $settings.Provider.FunctionMode

Write-Host "Starting Personal Agent..." -ForegroundColor Cyan

if ($functionMode) {
    Write-Host "Mode: Azure Functions (FunctionMode: true)" -ForegroundColor Yellow
    Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
    Write-Host ""
    
    Push-Location $projectPath
    try {
        func start
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Host "Mode: WebApplication with DevUI (FunctionMode: false)" -ForegroundColor Yellow
    Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
    Write-Host ""
    
    # Build first
    Push-Location $projectPath
    try {
        dotnet build --configuration Debug | Out-Null
        
        # Run the DLL directly to bypass Functions SDK interception
        $dllPath = ".\bin\Debug\net10.0\Personal.dll"
        dotnet $dllPath
    }
    finally {
        Pop-Location
    }
}
