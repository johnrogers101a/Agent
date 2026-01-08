<#
.SYNOPSIS
    Assigns an RBAC role to a user or service principal.

.DESCRIPTION
    Assigns the specified role to the current signed-in user on the given scope.

.PARAMETER Scope
    The resource ID scope for the role assignment.

.PARAMETER RoleName
    The name of the role to assign (e.g., "Cognitive Services OpenAI User").

.PARAMETER PrincipalId
    The object ID of the principal. If not provided, uses the current signed-in user.

.EXAMPLE
    Set-RbacRole -Scope "/subscriptions/.../resourceGroups/rg-myapp" -RoleName "Cognitive Services OpenAI User"
#>
function Set-RbacRole {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Scope,

        [Parameter(Mandatory)]
        [string]$RoleName,

        [Parameter()]
        [string]$PrincipalId
    )

    Write-Host "Configuring RBAC role '$RoleName'..." -ForegroundColor Cyan

    # Get current user's object ID if not provided
    if (-not $PrincipalId) {
        $currentUser = az ad signed-in-user show 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
        if (-not $currentUser) {
            Write-Warning "Could not get current user. Skipping RBAC assignment."
            return
        }
        $PrincipalId = $currentUser.id
        Write-Host "  Assigning to current user: $($currentUser.userPrincipalName)" -ForegroundColor Yellow
    }

    # Check if role assignment already exists
    $existing = az role assignment list `
        --assignee $PrincipalId `
        --scope $Scope `
        --role $RoleName `
        2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue

    if ($existing -and $existing.Count -gt 0) {
        Write-Host "  Role '$RoleName' already assigned" -ForegroundColor Green
        return $existing[0]
    }

    # Create role assignment
    Write-Host "  Creating role assignment..." -ForegroundColor Yellow
    $result = az role assignment create `
        --assignee $PrincipalId `
        --scope $Scope `
        --role $RoleName `
        2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Could not create role assignment. You may need to assign '$RoleName' manually."
        return
    }

    Write-Host "  Role '$RoleName' assigned successfully" -ForegroundColor Green
    return $result
}
