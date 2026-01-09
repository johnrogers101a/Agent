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

param(
    [Parameter(Mandatory)]
    [hashtable]$DeploymentInfo
)

Describe "Azure AI Foundry Deployment" -Tag "Integration" {

    BeforeAll {
        # Get deployment info from parameter
        $script:Info = $DeploymentInfo

        if (-not $script:Info) {
            throw "DeploymentInfo not found. Run deploy.ps1 first."
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

        It "AI Services has project management enabled" {
            $aiServices = az cognitiveservices account show `
                --name $script:Info.AIServicesName `
                --resource-group $script:Info.ResourceGroupName `
                2>$null | ConvertFrom-Json
            $aiServices.properties.allowProjectManagement | Should -Be $true
        }

        It "Foundry Project exists under AI Services" {
            $project = az cognitiveservices account project show `
                --name $script:Info.AIServicesName `
                --project-name $script:Info.FoundryProjectName `
                --resource-group $script:Info.ResourceGroupName `
                2>$null | ConvertFrom-Json
            $project | Should -Not -BeNullOrEmpty
            $project.properties.provisioningState | Should -Be "Succeeded"
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

        It "Model name matches configuration" {
            $deployment = az cognitiveservices account deployment show `
                --name $script:Info.AIServicesName `
                --resource-group $script:Info.ResourceGroupName `
                --deployment-name $script:Info.ModelName `
                2>$null | ConvertFrom-Json
            $deployment.properties.model.name | Should -Be $script:Info.ModelName
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

        It "Network default action is 'Deny'" {
            $aiServices = az cognitiveservices account show `
                --name $script:Info.AIServicesName `
                --resource-group $script:Info.ResourceGroupName `
                2>$null | ConvertFrom-Json
            $aiServices.properties.networkAcls.defaultAction | Should -Be "Deny"
        }

        It "Current IP is whitelisted" {
            $aiServices = az cognitiveservices account show `
                --name $script:Info.AIServicesName `
                --resource-group $script:Info.ResourceGroupName `
                2>$null | ConvertFrom-Json
            $ipRules = $aiServices.properties.networkAcls.ipRules
            $ipRules.value | Should -Contain $script:Info.PublicIp
        }
    }

    Context "Agent API" {

        It "Agent 'Personal' exists" {
            $script:Info.AgentId | Should -Not -BeNullOrEmpty
        }

        It "Can list assistants via API" {
            $token = az account get-access-token --resource "https://cognitiveservices.azure.com" --query accessToken -o tsv
            $headers = @{
                "Authorization" = "Bearer $token"
                "Content-Type" = "application/json"
            }
            $url = "$($script:Info.Endpoint)/openai/assistants?api-version=2025-01-01-preview"
            $response = Invoke-RestMethod -Uri $url -Headers $headers -ErrorAction Stop
            $response.data | Should -Not -BeNullOrEmpty
        }
    }

    Context "API Connectivity" {

        It "Endpoint is accessible (chat completions)" {
            $token = az account get-access-token --resource "https://cognitiveservices.azure.com" --query accessToken -o tsv
            $headers = @{
                "Authorization" = "Bearer $token"
                "Content-Type" = "application/json"
            }
            $body = @{
                model = $script:Info.ModelName
                messages = @(
                    @{ role = "user"; content = "Say 'test' and nothing else." }
                )
                max_tokens = 10
            } | ConvertTo-Json -Depth 5

            $url = "$($script:Info.Endpoint)/openai/deployments/$($script:Info.ModelName)/chat/completions?api-version=2024-02-15-preview"
            $response = Invoke-RestMethod -Uri $url -Method POST -Headers $headers -Body $body -ErrorAction Stop
            $response.choices | Should -Not -BeNullOrEmpty
        }

        It "Model responds with valid OpenAI format" {
            $token = az account get-access-token --resource "https://cognitiveservices.azure.com" --query accessToken -o tsv
            $headers = @{
                "Authorization" = "Bearer $token"
                "Content-Type" = "application/json"
            }
            $body = @{
                model = $script:Info.ModelName
                messages = @(
                    @{ role = "user"; content = "Reply with exactly: Hello from Azure" }
                )
                max_tokens = 20
            } | ConvertTo-Json -Depth 5

            $url = "$($script:Info.Endpoint)/openai/deployments/$($script:Info.ModelName)/chat/completions?api-version=2024-02-15-preview"
            $response = Invoke-RestMethod -Uri $url -Method POST -Headers $headers -Body $body -ErrorAction Stop

            # Verify OpenAI response format
            $response.id | Should -Match "^chatcmpl-"
            $response.object | Should -Be "chat.completion"
            $response.model | Should -Not -BeNullOrEmpty
            $response.choices[0].message.role | Should -Be "assistant"
            $response.choices[0].message.content | Should -Not -BeNullOrEmpty
        }
    }
}
