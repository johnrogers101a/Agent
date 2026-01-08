<#
.SYNOPSIS
    Updates the appsettings.json file with Azure Foundry configuration.

.DESCRIPTION
    Reads the appsettings.json file, updates the Provider section with
    Azure Foundry endpoint and model information, and saves the file.

.PARAMETER AppSettingsPath
    The path to the appsettings.json file.

.PARAMETER Endpoint
    The Azure OpenAI endpoint URL.

.PARAMETER ModelName
    The name of the deployed model.

.EXAMPLE
    Update-AppSettings -AppSettingsPath "./appsettings.json" -Endpoint "https://myproject.openai.azure.com" -ModelName "gpt-oss-120b"
#>
function Update-AppSettings {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$AppSettingsPath,

        [Parameter(Mandatory)]
        [string]$Endpoint,

        [Parameter(Mandatory)]
        [string]$ModelName
    )

    Write-Host "Updating appsettings.json..." -ForegroundColor Cyan

    if (-not (Test-Path $AppSettingsPath)) {
        throw "appsettings.json not found at: $AppSettingsPath"
    }

    # Read existing settings
    $settings = Get-Content $AppSettingsPath -Raw | ConvertFrom-Json

    # Update Provider section
    if (-not $settings.Provider) {
        $settings | Add-Member -NotePropertyName "Provider" -NotePropertyValue ([PSCustomObject]@{})
    }

    $settings.Provider.Type = "AzureFoundry"
    $settings.Provider.Endpoint = $Endpoint
    $settings.Provider.ModelName = $ModelName

    # Remove ApiKey if present (we use Azure CLI credential)
    if ($settings.Provider.PSObject.Properties.Name -contains "ApiKey") {
        $settings.Provider.PSObject.Properties.Remove("ApiKey")
    }

    # Ensure DevUI settings are preserved or set defaults
    if (-not $settings.Provider.PSObject.Properties.Name -contains "DevUI") {
        $settings.Provider | Add-Member -NotePropertyName "DevUI" -NotePropertyValue $true
    }
    if (-not $settings.Provider.PSObject.Properties.Name -contains "DevUIPort") {
        $settings.Provider | Add-Member -NotePropertyName "DevUIPort" -NotePropertyValue 8080
    }

    # Write back to file with proper formatting
    $settings | ConvertTo-Json -Depth 10 | Set-Content $AppSettingsPath -Encoding UTF8

    Write-Host "  Updated Provider.Type = 'AzureFoundry'" -ForegroundColor Green
    Write-Host "  Updated Provider.Endpoint = '$Endpoint'" -ForegroundColor Green
    Write-Host "  Updated Provider.ModelName = '$ModelName'" -ForegroundColor Green
}
