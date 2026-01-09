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
    Update-AppSetting -AppSettingsPath "./appsettings.json" -Endpoint "https://myproject.openai.azure.com" -ModelName "gpt-4.1"
#>
function Update-AppSetting {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$AppSettingsPath,

        [Parameter(Mandatory)]
        [string]$Endpoint,

        [Parameter(Mandatory)]
        [string]$ModelName
    )

    Write-Information "Updating appsettings.json..."

    if (-not (Test-Path -Path $AppSettingsPath)) {
        throw "appsettings.json not found at: $AppSettingsPath"
    }

    if (-not $PSCmdlet.ShouldProcess($AppSettingsPath, "Update app settings")) {
        return
    }

    # Read existing settings
    $settings = Get-Content -Path $AppSettingsPath -Raw | ConvertFrom-Json

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
    $settings | ConvertTo-Json -Depth 10 | Set-Content -Path $AppSettingsPath -Encoding UTF8

    Write-Information "  [OK] Provider.Type = 'AzureFoundry'"
    Write-Information "  [OK] Provider.Endpoint = '$Endpoint'"
    Write-Information "  [OK] Provider.ModelName = '$ModelName'"
}
