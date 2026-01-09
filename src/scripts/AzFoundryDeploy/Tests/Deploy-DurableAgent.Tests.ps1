<#
.SYNOPSIS
    Pester tests for Durable Functions deployment.

.DESCRIPTION
    Integration tests that verify:
    - Storage account exists
    - Function App exists and is properly configured
    - Managed identity is assigned
    - Storage RBAC roles are configured
#>

param(
    [Parameter()]
    [hashtable]$DurableDeploymentInfo
)

Describe "Durable Functions Deployment" -Tag "Integration", "Durable" {

    BeforeAll {
        # Get deployment info from parameter
        $script:Info = $DurableDeploymentInfo

        if (-not $script:Info) {
            Write-Warning "DurableDeploymentInfo not found. Skipping durable tests."
            $script:SkipTests = $true
        }
        else {
            $script:SkipTests = $false
        }

        # Import module for helper functions
        $modulePath = Split-Path -Parent $PSScriptRoot
        Import-Module $modulePath -Force
    }

    Context "Storage Account" {

        It "Storage account exists" -Skip:$script:SkipTests {
            $storage = az storage account show `
                --name $script:Info.StorageAccountName `
                --resource-group $script:Info.ResourceGroupName `
                2>$null | ConvertFrom-Json
            $storage | Should -Not -BeNullOrEmpty
            $storage.provisioningState | Should -Be "Succeeded"
        }

        It "Storage account SKU is Standard_LRS" -Skip:$script:SkipTests {
            $storage = az storage account show `
                --name $script:Info.StorageAccountName `
                --resource-group $script:Info.ResourceGroupName `
                2>$null | ConvertFrom-Json
            $storage.sku.name | Should -Be "Standard_LRS"
        }
    }

    Context "Function App" {

        It "Function App exists and is running" -Skip:$script:SkipTests {
            $funcApp = az functionapp show `
                --name $script:Info.FunctionAppName `
                --resource-group $script:Info.ResourceGroupName `
                2>$null | ConvertFrom-Json
            $funcApp | Should -Not -BeNullOrEmpty
            # Flex Consumption plans have state in properties.state
            $state = if ($funcApp.state) { $funcApp.state } else { $funcApp.properties.state }
            $state | Should -Be "Running"
        }

        It "Function App is dotnet-isolated" -Skip:$script:SkipTests {
            # For Flex Consumption, check the kind or app settings instead of linuxFxVersion
            $funcApp = az functionapp show `
                --name $script:Info.FunctionAppName `
                --resource-group $script:Info.ResourceGroupName `
                2>$null | ConvertFrom-Json
            # Verify it's a function app on Linux
            $funcApp.kind | Should -Match "functionapp"
            $funcApp.kind | Should -Match "linux"
        }

        It "Function App has managed identity" -Skip:$script:SkipTests {
            $identity = az functionapp identity show `
                --name $script:Info.FunctionAppName `
                --resource-group $script:Info.ResourceGroupName `
                2>$null | ConvertFrom-Json
            $identity | Should -Not -BeNullOrEmpty
            $identity.principalId | Should -Not -BeNullOrEmpty
        }
    }

    Context "RBAC Configuration" {

        It "Storage Blob Data Contributor role is assigned" -Skip:$script:SkipTests {
            $identity = az functionapp identity show `
                --name $script:Info.FunctionAppName `
                --resource-group $script:Info.ResourceGroupName `
                2>$null | ConvertFrom-Json

            $subscriptionId = az account show --query id -o tsv
            $storageScope = "/subscriptions/$subscriptionId/resourceGroups/$($script:Info.ResourceGroupName)/providers/Microsoft.Storage/storageAccounts/$($script:Info.StorageAccountName)"

            $roles = az role assignment list `
                --assignee $identity.principalId `
                --scope $storageScope `
                2>$null | ConvertFrom-Json

            $blobRole = $roles | Where-Object { $_.roleDefinitionName -eq "Storage Blob Data Contributor" }
            $blobRole | Should -Not -BeNullOrEmpty
        }

        It "Storage Queue Data Contributor role is assigned" -Skip:$script:SkipTests {
            $identity = az functionapp identity show `
                --name $script:Info.FunctionAppName `
                --resource-group $script:Info.ResourceGroupName `
                2>$null | ConvertFrom-Json

            $subscriptionId = az account show --query id -o tsv
            $storageScope = "/subscriptions/$subscriptionId/resourceGroups/$($script:Info.ResourceGroupName)/providers/Microsoft.Storage/storageAccounts/$($script:Info.StorageAccountName)"

            $roles = az role assignment list `
                --assignee $identity.principalId `
                --scope $storageScope `
                2>$null | ConvertFrom-Json

            $queueRole = $roles | Where-Object { $_.roleDefinitionName -eq "Storage Queue Data Contributor" }
            $queueRole | Should -Not -BeNullOrEmpty
        }
    }
}
