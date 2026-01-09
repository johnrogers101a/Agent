# Start the Personal Agent Azure Function
# Run this script and leave it running, then test in another terminal

$ErrorActionPreference = "Stop"

Write-Host "Starting Personal Agent Azure Function..." -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Host ""

Push-Location $PSScriptRoot\..\Agents\Personal
try {
    func start
}
finally {
    Pop-Location
}
