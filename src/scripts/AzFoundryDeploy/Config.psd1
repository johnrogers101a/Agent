<#
.SYNOPSIS
    Configuration for Azure AI Foundry deployment.

.DESCRIPTION
    Contains all default values for deploying agents to Azure AI Foundry.
    Edit these values to customize the deployment.
#>
@{
    # Azure subscription name
    SubscriptionName  = "Visual Studio Enterprise Subscription"

    # Resource group settings
    ResourceGroupName = "rg-agents-ai-westus"
    Location          = "westus"

    # Naming prefix for resources
    ResourcePrefix    = "agents"

    # Model deployment settings
    ModelName         = "gpt-4.1"
    ModelVersion      = "2025-04-14"
    ModelFormat       = "OpenAI"
    SkuName           = "GlobalStandard"
    SkuCapacity       = 10

    # API version for Azure OpenAI
    ApiVersion        = "2024-02-15-preview"

    # Path to appsettings.json (relative to script root or absolute)
    AppSettingsPath   = "c:\Users\John\code\4JS\Agent\src\PersonalAgent\appsettings.json"

    # ========================================
    # Foundry Resource and Project settings
    # Pure Foundry architecture (no ML Hub)
    # ========================================

    # AI Services account (Foundry Resource - parent container)
    AIServicesName     = "ais-agents"

    # Foundry Project (child of AIServices - container for all agents)
    FoundryProjectName = "proj-agents"

    # ========================================
    # Durable Agent Infrastructure settings
    # ========================================

    # Storage account for Azure Functions runtime + Durable Functions state
    StorageAccountName = "stagents4js"

    # Azure Functions app (Flex Consumption)
    FunctionAppName    = "func-agents-4js"

    # Agent deployment scaling
    MinReplicas        = 1
    MaxReplicas        = 5
}
