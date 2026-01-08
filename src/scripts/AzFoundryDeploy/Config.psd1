<#
.SYNOPSIS
    Configuration for Azure AI Foundry deployment.

.DESCRIPTION
    Contains all default values for deploying gpt-oss-120b to Azure AI Foundry.
    Edit these values to customize the deployment.
#>
@{
    # Azure subscription name
    SubscriptionName  = "Visual Studio Enterprise Subscription"

    # Resource group settings
    ResourceGroupName = "rg-personalagent-ai-westus"
    Location          = "westus"

    # Naming prefix for resources
    ResourcePrefix    = "personalagent"

    # Model deployment settings
    ModelName         = "gpt-oss-120b"
    ModelVersion      = "1"
    ModelFormat       = "OpenAI-OSS"
    SkuName           = "GlobalStandard"
    SkuCapacity       = 10

    # API version for Azure OpenAI
    ApiVersion        = "2024-02-15-preview"

    # Path to appsettings.json (relative to script root or absolute)
    AppSettingsPath   = "c:\Users\John\code\4JS\Agent\src\PersonalAgent\appsettings.json"
}
