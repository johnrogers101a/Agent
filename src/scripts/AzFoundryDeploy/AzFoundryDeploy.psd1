<#
.SYNOPSIS
    Module manifest for AzFoundryDeploy.

.DESCRIPTION
    PowerShell module for deploying Azure AI Foundry with gpt-oss-120b model.
#>
@{
    # Script module or binary module file associated with this manifest
    RootModule        = 'AzFoundryDeploy.psm1'

    # Version number of this module (SemVer)
    ModuleVersion     = '1.0.0'

    # ID used to uniquely identify this module
    GUID              = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'

    # Author of this module
    Author            = '4JS'

    # Company or vendor of this module
    CompanyName       = '4JS'

    # Description of the functionality provided by this module
    Description       = 'Deploys Azure AI Foundry infrastructure with model deployments, durable agents, IP whitelisting, and auto-configures PersonalAgent.'

    # Minimum version of the PowerShell engine required by this module
    PowerShellVersion = '7.0'

    # Functions to export from this module
    FunctionsToExport = @(
        'Add-NetworkRule',
        'Add-RbacRoleAssignment',
        'Get-AzAccessToken',
        'Get-PublicIpAddress',
        'Initialize-AIServiceAccount',
        'Initialize-FoundryProject',
        'Initialize-FunctionApp',
        'Initialize-ModelDeployment',
        'Initialize-ResourceGroup',
        'Initialize-StorageAccount',
        'Update-AppSetting'
    )

    # Cmdlets to export from this module
    CmdletsToExport   = @()

    # Variables to export from this module
    VariablesToExport = @()

    # Aliases to export from this module
    AliasesToExport   = @()

    # Private data to pass to the module specified in RootModule
    PrivateData       = @{
        PSData = @{
            # Tags applied to this module for discovery
            Tags         = @('Azure', 'Foundry', 'OpenAI', 'IaC', 'gpt-oss-120b', 'DurableAgents', 'AzureFunctions')

            # License URI for this module
            LicenseUri   = ''

            # Project site URI for this module
            ProjectUri   = ''

            # Release notes for this module
            ReleaseNotes = 'v1.1.0 - Added Deploy-DurableAgent for Azure Functions durable agent deployment.'
        }
    }
}
