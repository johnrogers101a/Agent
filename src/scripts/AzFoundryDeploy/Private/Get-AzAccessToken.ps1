<#
.SYNOPSIS
    Gets an Azure access token for API calls.

.DESCRIPTION
    Retrieves an access token from Azure CLI for the specified resource.
    Used for authenticating API calls to Azure OpenAI.

.PARAMETER Resource
    The resource URI to get the token for. Defaults to Cognitive Services.

.EXAMPLE
    $token = Get-AzAccessToken
    $token = Get-AzAccessToken -Resource "https://cognitiveservices.azure.com"
#>
function Get-AzAccessToken {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Resource = "https://cognitiveservices.azure.com"
    )

    $tokenResponse = az account get-access-token `
        --resource $Resource `
        --query accessToken `
        --output tsv `
        2>$null

    if ($LASTEXITCODE -ne 0 -or -not $tokenResponse) {
        throw "Failed to get access token. Ensure you are logged in with 'az login'."
    }

    return $tokenResponse
}
