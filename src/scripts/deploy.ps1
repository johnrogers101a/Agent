<#
.SYNOPSIS
    Unified deployment script for Azure AI Foundry agents.

.DESCRIPTION
    Single idempotent deployment that creates all required infrastructure:
    1. Resource Group
    2. AI Services Account (with project management enabled)
    3. Foundry Project
    4. Model Deployment
    5. Network Rules (IP whitelisting)
    6. RBAC Role Assignment
    7. Agent Creation (Personal agent)
    8. Storage Account + Function App for durable endpoints

    All operations are idempotent - safe to run multiple times.

.EXAMPLE
    .\deploy.ps1
    # Full deployment with verification tests
#>

#Requires -Version 7.0

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

Write-Information ""
Write-Information "╔════════════════════════════════════════════════════════════════╗"
Write-Information "║  Azure AI Agent Deployment                                     ║"
Write-Information "║  Foundry + Model + Agent + Functions                           ║"
Write-Information "╚════════════════════════════════════════════════════════════════╝"
Write-Information ""

#region Prerequisites
Write-Information "Checking prerequisites..."

# Check Azure CLI
$azVersion = az version 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
if (-not $azVersion) {
    throw "Azure CLI is not installed. Install from: https://aka.ms/installazurecli"
}
Write-Information "  [OK] Azure CLI $($azVersion.'azure-cli')"

# Check Pester
$pester = Get-Module -ListAvailable -Name Pester | Sort-Object Version -Descending | Select-Object -First 1
if (-not $pester -or $pester.Version -lt [Version]"5.0.0") {
    Write-Information "  Installing Pester module..."
    Install-Module Pester -Force -SkipPublisherCheck -Scope CurrentUser
}
Write-Information "  [OK] Pester module available"

Write-Information ""
#endregion

#region Configuration
$scriptRoot = $PSScriptRoot
$modulePath = Join-Path -Path $scriptRoot -ChildPath "AzFoundryDeploy"
$configPath = Join-Path -Path $modulePath -ChildPath "Config.psd1"
$testsPath = Join-Path -Path $modulePath -ChildPath "Tests"
$appSettingsPath = Join-Path -Path $scriptRoot -ChildPath ".." | Join-Path -ChildPath "PersonalAgent" | Join-Path -ChildPath "appsettings.json" | Resolve-Path
$sourceProjectPath = Join-Path -Path $scriptRoot -ChildPath ".." | Join-Path -ChildPath "PersonalAgent" | Resolve-Path

Write-Information "Loading configuration..."
$config = Import-PowerShellDataFile -Path $configPath
$config.AppSettingsPath = $appSettingsPath.Path
$config.ScriptRoot = $scriptRoot
$config.SourceProjectPath = $sourceProjectPath.Path
Write-Information "  [OK] Configuration loaded"
Write-Information ""

# Import module
Write-Information "Importing AzFoundryDeploy module..."
Import-Module $modulePath -Force
Write-Information "  [OK] Module imported"
Write-Information ""
#endregion

#region Azure Login & Subscription
Write-Information "Verifying Azure CLI session..."
$account = az account show 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
if (-not $account) {
    throw "Not logged in to Azure CLI. Run 'az login' first."
}
Write-Information "  Logged in as: $($account.user.name)"

Write-Information "Setting subscription to '$($config.SubscriptionName)'..."
az account set --subscription $config.SubscriptionName
if ($LASTEXITCODE -ne 0) {
    throw "Failed to set subscription. Verify the subscription name is correct."
}
Write-Information "  [OK] Subscription set"
Write-Information ""
#endregion

#region Foundry Infrastructure
Write-Information "========================================"
Write-Information "  Phase 1: Foundry Infrastructure"
Write-Information "========================================"
Write-Information ""

# Get public IP for whitelisting
$publicIp = Get-PublicIpAddress -InformationAction Continue

# Step 1: Resource Group
Initialize-ResourceGroup `
    -ResourceGroupName $config.ResourceGroupName `
    -Location $config.Location `
    -InformationAction Continue | Out-Null

# Step 2: AI Services Account
Initialize-AIServiceAccount `
    -AccountName $config.AIServicesName `
    -ResourceGroupName $config.ResourceGroupName `
    -Location $config.Location `
    -InformationAction Continue | Out-Null

# Step 3: Foundry Project
Initialize-FoundryProject `
    -ProjectName $config.FoundryProjectName `
    -AccountName $config.AIServicesName `
    -ResourceGroupName $config.ResourceGroupName `
    -Location $config.Location `
    -InformationAction Continue | Out-Null

# Step 4: Model Deployment
Initialize-ModelDeployment `
    -AccountName $config.AIServicesName `
    -ResourceGroupName $config.ResourceGroupName `
    -DeploymentName $config.ModelName `
    -ModelName $config.ModelName `
    -ModelVersion $config.ModelVersion `
    -ModelFormat $config.ModelFormat `
    -SkuName $config.SkuName `
    -SkuCapacity $config.SkuCapacity `
    -InformationAction Continue | Out-Null

# Step 5: Network Rules
Add-NetworkRule `
    -AccountName $config.AIServicesName `
    -ResourceGroupName $config.ResourceGroupName `
    -IpAddress $publicIp `
    -InformationAction Continue

# Step 6: RBAC
$subscriptionId = (az account show --query id -o tsv)
$aiServicesResourceId = "/subscriptions/$subscriptionId/resourceGroups/$($config.ResourceGroupName)/providers/Microsoft.CognitiveServices/accounts/$($config.AIServicesName)"
Add-RbacRoleAssignment `
    -Scope $aiServicesResourceId `
    -RoleName "Cognitive Services OpenAI User" `
    -InformationAction Continue | Out-Null

# Step 7: Update appsettings.json
$endpoint = "https://$($config.AIServicesName).openai.azure.com"
if (Test-Path -Path $config.AppSettingsPath) {
    Update-AppSetting `
        -AppSettingsPath $config.AppSettingsPath `
        -Endpoint $endpoint `
        -ModelName $config.ModelName `
        -InformationAction Continue
}
#endregion

#region Agent Creation
Write-Information ""
Write-Information "========================================"
Write-Information "  Phase 2: Agent Creation"
Write-Information "========================================"
Write-Information ""

# Load agent instructions from PersonalAgent/Agents/personal.md
$instructionsPath = Join-Path -Path $sourceProjectPath.Path -ChildPath "Agents" | Join-Path -ChildPath "personal.md"
$instructions = if (Test-Path -Path $instructionsPath) {
    Get-Content -Path $instructionsPath -Raw
} else {
    "You are a helpful personal assistant."
}

Write-Information "Checking agent 'Personal'..."

# Define tools for the agent (weather tools only - email requires OAuth)
$tools = @(
    @{
        type = "function"
        function = @{
            name = "GetWeatherByZip"
            description = "Gets current weather conditions for a US zip code."
            parameters = @{
                type = "object"
                properties = @{
                    zipCode = @{
                        type = "string"
                        description = "US zip code (e.g., 98052)"
                    }
                }
                required = @("zipCode")
            }
        }
    },
    @{
        type = "function"
        function = @{
            name = "GetWeatherByCityState"
            description = "Gets current weather conditions for a city and state."
            parameters = @{
                type = "object"
                properties = @{
                    city = @{
                        type = "string"
                        description = "City name (e.g., Seattle)"
                    }
                    state = @{
                        type = "string"
                        description = "State name or abbreviation (e.g., Washington or WA)"
                    }
                }
                required = @("city", "state")
            }
        }
    },
    @{
        type = "function"
        function = @{
            name = "GetDailyForecastByZip"
            description = "Gets daily weather forecast for a US zip code."
            parameters = @{
                type = "object"
                properties = @{
                    zipCode = @{
                        type = "string"
                        description = "US zip code (e.g., 98052)"
                    }
                    days = @{
                        type = "integer"
                        description = "Number of days to forecast (1-10, default 5)"
                    }
                }
                required = @("zipCode")
            }
        }
    },
    @{
        type = "function"
        function = @{
            name = "GetDailyForecastByCityState"
            description = "Gets daily weather forecast for a city and state."
            parameters = @{
                type = "object"
                properties = @{
                    city = @{
                        type = "string"
                        description = "City name (e.g., New York)"
                    }
                    state = @{
                        type = "string"
                        description = "State name or abbreviation (e.g., New York or NY)"
                    }
                    days = @{
                        type = "integer"
                        description = "Number of days to forecast (1-10, default 5)"
                    }
                }
                required = @("city", "state")
            }
        }
    },
    @{
        type = "function"
        function = @{
            name = "GetHourlyForecastByZip"
            description = "Gets hourly weather forecast for a US zip code."
            parameters = @{
                type = "object"
                properties = @{
                    zipCode = @{
                        type = "string"
                        description = "US zip code (e.g., 98052)"
                    }
                    hours = @{
                        type = "integer"
                        description = "Number of hours to forecast (1-24, default 12)"
                    }
                }
                required = @("zipCode")
            }
        }
    },
    @{
        type = "function"
        function = @{
            name = "GetHourlyForecastByCityState"
            description = "Gets hourly weather forecast for a city and state."
            parameters = @{
                type = "object"
                properties = @{
                    city = @{
                        type = "string"
                        description = "City name (e.g., New York)"
                    }
                    state = @{
                        type = "string"
                        description = "State name or abbreviation (e.g., New York or NY)"
                    }
                    hours = @{
                        type = "integer"
                        description = "Number of hours to forecast (1-24, default 12)"
                    }
                }
                required = @("city", "state")
            }
        }
    }
)

# Get access token and check for existing agent
$token = az account get-access-token --resource "https://cognitiveservices.azure.com" --query accessToken -o tsv
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}
$assistantsUrl = "https://$($config.AIServicesName).openai.azure.com/openai/assistants?api-version=2025-01-01-preview"

$existingAgents = Invoke-RestMethod -Uri $assistantsUrl -Headers $headers -ErrorAction SilentlyContinue
$personalAgent = $existingAgents.data | Where-Object { $_.name -eq "Personal" }

if ($personalAgent) {
    Write-Information "  Agent 'Personal' exists (id: $($personalAgent.id)), updating tools..."
    $agentId = $personalAgent.id

    # Update existing agent with tools
    $updateBody = @{
        tools = $tools
        instructions = $instructions
    } | ConvertTo-Json -Depth 10

    $updateUrl = "https://$($config.AIServicesName).openai.azure.com/openai/assistants/$agentId`?api-version=2025-01-01-preview"
    $null = Invoke-RestMethod -Uri $updateUrl -Method POST -Headers $headers -Body $updateBody -ErrorAction SilentlyContinue
    Write-Information "  [OK] Agent 'Personal' updated with $($tools.Count) tools"
} else {
    Write-Information "  Creating agent 'Personal' with tools..."

    $agentBody = @{
        name = "Personal"
        model = $config.ModelName
        instructions = $instructions
        tools = $tools
    } | ConvertTo-Json -Depth 10

    $newAgent = Invoke-RestMethod -Uri $assistantsUrl -Method POST -Headers $headers -Body $agentBody
    $agentId = $newAgent.id
    Write-Information "  [OK] Agent 'Personal' created with $($tools.Count) tools (id: $agentId)"
}
#endregion

#region Durable Functions
Write-Information ""
Write-Information "========================================"
Write-Information "  Phase 3: Durable Functions"
Write-Information "========================================"
Write-Information ""

# Storage Account
Initialize-StorageAccount `
    -StorageAccountName $config.StorageAccountName `
    -ResourceGroupName $config.ResourceGroupName `
    -Location $config.Location `
    -InformationAction Continue | Out-Null

# Function App
Initialize-FunctionApp `
    -FunctionAppName $config.FunctionAppName `
    -ResourceGroupName $config.ResourceGroupName `
    -StorageAccountName $config.StorageAccountName `
    -Location $config.Location `
    -InformationAction Continue | Out-Null

# Managed Identity + RBAC
Write-Information "Configuring Managed Identity..."
    az functionapp identity assign `
        --name $config.FunctionAppName `
        --resource-group $config.ResourceGroupName `
        --output none 2>$null

    $funcIdentity = az functionapp identity show `
        --name $config.FunctionAppName `
        --resource-group $config.ResourceGroupName `
        --output json 2>$null | ConvertFrom-Json

    if ($funcIdentity) {
        $principalId = $funcIdentity.principalId
        Write-Information "  [OK] Managed identity: $principalId"

        # Storage RBAC
        $storageResourceId = "/subscriptions/$subscriptionId/resourceGroups/$($config.ResourceGroupName)/providers/Microsoft.Storage/storageAccounts/$($config.StorageAccountName)"

        @("Storage Blob Data Contributor", "Storage Queue Data Contributor", "Storage Table Data Contributor") | ForEach-Object {
            az role assignment create `
                --role $_ `
                --assignee-object-id $principalId `
                --assignee-principal-type ServicePrincipal `
                --scope $storageResourceId `
                --output none 2>$null
        }
        Write-Information "  [OK] Storage RBAC configured"
    }

$durableDeploymentInfo = @{
    StorageAccountName = $config.StorageAccountName
    FunctionAppName = $config.FunctionAppName
    ResourceGroupName = $config.ResourceGroupName
}
#endregion

#region Summary
Write-Information ""
Write-Information "========================================"
Write-Information "  Deployment Complete!"
Write-Information "========================================"
Write-Information ""
Write-Information "Resources:"
Write-Information "  Resource Group:   $($config.ResourceGroupName)"
Write-Information "  AI Services:      $($config.AIServicesName)"
Write-Information "  Foundry Project:  $($config.FoundryProjectName)"
Write-Information "  Model:            $($config.ModelName)"
Write-Information "  Agent:            Personal (id: $agentId)"
Write-Information "  Storage:          $($config.StorageAccountName)"
Write-Information "  Function App:     $($config.FunctionAppName)"
Write-Information ""
Write-Information "Endpoint: $endpoint"
Write-Information ""

# Store deployment info for tests (script scope, not global)
$script:DeploymentInfo = @{
    ResourceGroupName    = $config.ResourceGroupName
    AIServicesName       = $config.AIServicesName
    FoundryProjectName   = $config.FoundryProjectName
    ModelName            = $config.ModelName
    Endpoint             = $endpoint
    ApiVersion           = $config.ApiVersion
    PublicIp             = $publicIp
    AgentId              = $agentId
}

$script:DurableDeploymentInfo = $durableDeploymentInfo
#endregion

#region Tests
Write-Information "Running verification tests..."
Write-Information ""

# Create Pester containers with data
$foundryTestPath = Join-Path -Path $testsPath -ChildPath "Deploy-AzFoundry.Tests.ps1"
$durableTestPath = Join-Path -Path $testsPath -ChildPath "Deploy-DurableAgent.Tests.ps1"

$foundryContainer = New-PesterContainer -Path $foundryTestPath -Data @{
    DeploymentInfo = $script:DeploymentInfo
}

$durableContainer = New-PesterContainer -Path $durableTestPath -Data @{
    DurableDeploymentInfo = $script:DurableDeploymentInfo
}

$pesterConfig = New-PesterConfiguration
$pesterConfig.Run.Container = @($foundryContainer, $durableContainer)
$pesterConfig.Output.Verbosity = "Detailed"
$pesterConfig.Run.Exit = $false
$pesterConfig.Run.PassThru = $true

$testResults = Invoke-Pester -Configuration $pesterConfig

Write-Information ""
if ($testResults -and $testResults.FailedCount -eq 0) {
    Write-Information "╔════════════════════════════════════════════════════════════════╗"
    Write-Information "║  All tests passed! Deployment verified.                        ║"
    Write-Information "╚════════════════════════════════════════════════════════════════╝"
}
else {
    Write-Information "╔════════════════════════════════════════════════════════════════╗"
    Write-Information "║  Some tests failed. Review output above.                       ║"
    Write-Information "╚════════════════════════════════════════════════════════════════╝"
}
#endregion

# Return deployment info for programmatic use
return @{
    DeploymentInfo = $script:DeploymentInfo
    DurableDeploymentInfo = $durableDeploymentInfo
}
