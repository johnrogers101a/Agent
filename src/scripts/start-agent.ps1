# Start the Personal Agent

$ErrorActionPreference = "Stop"

$projectPath = "$PSScriptRoot\..\Agents\Personal"

Write-Host "Starting Personal Agent..." -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Host ""

Push-Location $projectPath
try {
    dotnet run
}
finally {
    Pop-Location
}
