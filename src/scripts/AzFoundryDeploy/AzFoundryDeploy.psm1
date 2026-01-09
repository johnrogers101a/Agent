<#
.SYNOPSIS
    Module loader for AzFoundryDeploy.

.DESCRIPTION
    Dot-sources all Public and Private functions to make them available in the module scope.
#>

# Get the module root path
$ModuleRoot = $PSScriptRoot

# Dot-source all Private functions
$PrivateFunctions = Get-ChildItem -Path "$ModuleRoot/Private/*.ps1" -ErrorAction SilentlyContinue
foreach ($function in $PrivateFunctions) {
    try {
        . $function.FullName
        Write-Verbose "Imported private function: $($function.BaseName)"
    }
    catch {
        Write-Error "Failed to import private function $($function.FullName): $_"
    }
}

# Dot-source all Public functions
$PublicFunctions = Get-ChildItem -Path "$ModuleRoot/Public/*.ps1" -ErrorAction SilentlyContinue
foreach ($function in $PublicFunctions) {
    try {
        . $function.FullName
        Write-Verbose "Imported public function: $($function.BaseName)"
    }
    catch {
        Write-Error "Failed to import public function $($function.FullName): $_"
    }
}

# Export all functions from the module
Export-ModuleMember -Function *
