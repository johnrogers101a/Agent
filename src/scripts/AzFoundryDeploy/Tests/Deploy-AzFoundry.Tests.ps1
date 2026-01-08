<#
.SYNOPSIS
    Pester tests for Azure AI Foundry deployment.

.DESCRIPTION
    Integration tests that verify:
    - All Azure resources exist and are properly configured
    - Network rules are applied
    - Model deployment is accessible
    - Model responds to API requests in OpenAI format
#>

BeforeDiscovery {
    # Load deployment info from script scope (set by deploy-foundry.ps1)
    $script:DeploymentInfo = $Global:DeploymentInfo
}

Describe "Azure AI Foundry Deployment" -Tag "Integration" {

    BeforeAll {
        # Get deployment info from global scope
        $script:Info = $Global:DeploymentInfo

        if (-not $script:Info) {
            throw "DeploymentInfo not found. Run deploy-foundry.ps1 first."
        }

        # Import module for helper functions
        $modulePath = Split-Path -Parent $PSScriptRoot
        Import-Module $modulePath -Force
    }

    Context "Resource Provisioning" {

        It "Resource group exists" {
            $rg = az group show --name $script:Info.ResourceGroupName 2>$null | ConvertFrom-Json
            $rg | Should -Not -BeNullOrEmpty
            $rg.properties.provisioningState | Should -Be "Succeeded"
        }

        It "AI Services account exists" {
            $aiServices = az cognitiveservices account show `
                --name $script:Info.AIServicesName `
                --resource-group $script:Info.ResourceGroupName `
                2>$null | ConvertFrom-Json
            $aiServices | Should -Not -BeNullOrEmpty
            $aiServices.properties.provisioningState | Should -Be "Succeeded"
        }
    }

    Context "Model Deployment" {

        It "Model deployment exists" {
            $deployment = az cognitiveservices account deployment show `
                --name $script:Info.AIServicesName `
                --resource-group $script:Info.ResourceGroupName `
                --deployment-name $script:Info.ModelName `
                2>$null | ConvertFrom-Json
            $deployment | Should -Not -BeNullOrEmpty
        }

        It "Model name is 'gpt-oss-120b'" {
            $deployment = az cognitiveservices account deployment show `
                --name $script:Info.AIServicesName `
                --resource-group $script:Info.ResourceGroupName `
                --deployment-name $script:Info.ModelName `
                2>$null | ConvertFrom-Json
            $deployment.properties.model.name | Should -Be "gpt-oss-120b"
        }

        It "SKU is 'GlobalStandard'" {
            $deployment = az cognitiveservices account deployment show `
                --name $script:Info.AIServicesName `
                --resource-group $script:Info.ResourceGroupName `
                --deployment-name $script:Info.ModelName `
                2>$null | ConvertFrom-Json
            $deployment.sku.name | Should -Be "GlobalStandard"
        }
    }

    Context "Network Security" {

        It "Current IP is whitelisted" {
            $account = az cognitiveservices account show `
                --name $script:Info.AIServicesName `
                --resource-group $script:Info.ResourceGroupName `
                2>$null | ConvertFrom-Json

            $ipRules = $account.properties.networkAcls.ipRules
            $whitelisted = $ipRules | Where-Object { $_.value -eq $script:Info.PublicIp }
            $whitelisted | Should -Not -BeNullOrEmpty
        }
    }

    Context "Model API Verification" {

        BeforeAll {
            # Get access token for API calls
            $script:Token = az account get-access-token `
                --resource "https://cognitiveservices.azure.com" `
                --query accessToken `
                --output tsv

            $script:ApiUrl = "$($script:Info.Endpoint)/openai/deployments/$($script:Info.ModelName)/chat/completions?api-version=$($script:Info.ApiVersion)"

            $script:Headers = @{
                "Authorization" = "Bearer $($script:Token)"
                "Content-Type"  = "application/json"
            }

            $script:Body = @{
                messages   = @(
                    @{
                        role    = "user"
                        content = "Say 'Hello' and nothing else."
                    }
                )
                max_tokens = 10
            } | ConvertTo-Json

            # Make a single API call and cache the response to avoid rate limiting
            $script:ApiResponse = Invoke-RestMethod `
                -Uri $script:ApiUrl `
                -Method POST `
                -Headers $script:Headers `
                -Body $script:Body `
                -ErrorAction Stop
        }

        It "Returns 200 on chat/completions endpoint" {
            # If we got here via BeforeAll, the request succeeded
            $script:ApiResponse | Should -Not -BeNullOrEmpty
        }

        It "Response contains 'choices' array" {
            $script:ApiResponse.choices | Should -Not -BeNullOrEmpty
            # PowerShell unwraps single-element arrays, so check for at least one choice
            @($script:ApiResponse.choices).Count | Should -BeGreaterOrEqual 1
        }

        It "Response follows OpenAI format with 'message' in choice" {
            $choice = @($script:ApiResponse.choices)[0]
            $choice.message | Should -Not -BeNullOrEmpty
            $choice.message.role | Should -Be "assistant"
            # Content may be in 'content' directly or could be empty for some responses
            ($choice.message.content -or $choice.message.PSObject.Properties.Name -contains 'content') | Should -BeTrue
        }

        It "Response contains 'id' field" {
            $script:ApiResponse.id | Should -Not -BeNullOrEmpty
        }

        It "Response contains 'model' field" {
            $script:ApiResponse.model | Should -Not -BeNullOrEmpty
        }

        It "Response contains 'usage' statistics" {
            $script:ApiResponse.usage | Should -Not -BeNullOrEmpty
            $script:ApiResponse.usage.prompt_tokens | Should -BeGreaterThan 0
            $script:ApiResponse.usage.completion_tokens | Should -BeGreaterThan 0
            $script:ApiResponse.usage.total_tokens | Should -BeGreaterThan 0
        }
    }
}
