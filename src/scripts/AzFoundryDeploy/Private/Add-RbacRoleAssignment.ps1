<#
.SYNOPSIS
    Adds an RBAC role assignment to a principal.

.DESCRIPTION
    Assigns the specified role to the current signed-in user (or specified principal) on the given scope.
    Skips if the role is already assigned.

.PARAMETER Scope
    The resource ID scope for the role assignment.

.PARAMETER RoleName
    The name of the role to assign (e.g., "Cognitive Services OpenAI User").

.PARAMETER PrincipalId
    The object ID of the principal. If not provided, uses the current signed-in user.

.EXAMPLE
    Add-RbacRoleAssignment -Scope "/subscriptions/.../resourceGroups/rg-myapp" -RoleName "Cognitive Services OpenAI User"
#>
function Add-RbacRoleAssignment {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Scope,

        [Parameter(Mandatory)]
        [string]$RoleName,

        [Parameter()]
        [string]$PrincipalId
    )

    Write-Information "Configuring RBAC role '$RoleName'..."

    # Get current user's object ID if not provided
    if (-not $PrincipalId) {
        $currentUser = az ad signed-in-user show 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
        if (-not $currentUser) {
            Write-Warning "Could not get current user. Skipping RBAC assignment."
            return
        }
        $PrincipalId = $currentUser.id
        Write-Information "  Assigning to current user: $($currentUser.userPrincipalName)"
    }

    # Check if role assignment already exists
    $existing = az role assignment list `
        --assignee $PrincipalId `
        --scope $Scope `
        --role $RoleName `
        2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue

    if ($existing -and $existing.Count -gt 0) {
        Write-Information "  [OK] Role '$RoleName' already assigned"
        return $existing[0]
    }

    # Create role assignment
    Write-Information "  Creating role assignment..."
    $result = az role assignment create `
        --assignee $PrincipalId `
        --scope $Scope `
        --role $RoleName `
        2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Could not create role assignment. You may need to assign '$RoleName' manually."
        return
    }

    Write-Information "  [OK] Role '$RoleName' assigned"
    return $result
}
