[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Pooled', 'Standalone')]
    [string]$Mode
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\00-config.ps1"

function Get-DemoDatabase {
    param([string]$Name)
    Invoke-Az @('sql', 'db', 'show', '--subscription', $SubscriptionId,
        '--resource-group', $ResourceGroup, '--server', $SqlServerName, '--name', $Name, '--output', 'json')
}

function Test-DesiredState {
    param($Database, $Target)
    if ($Mode -eq 'Pooled') {
        return ($Database.elasticPoolId -eq $pool.id -and $Database.status -eq 'Online')
    }

    if ($Database.elasticPoolId -or $Database.currentServiceObjectiveName -ne $Target.Objective) { return $false }
    if ($Database.status -notin @('Online', 'Paused')) { return $false }
    if ($Target.Name -eq $CaldovaDatabase) {
        return ($Database.minCapacity -eq $CaldovaMinVCores -and
            $Database.autoPauseDelay -eq $AutoPauseDelayMinutes)
    }
    return $true
}

function Set-DemoDatabase {
    param([string]$Name, [string[]]$ComputeArguments)
    Invoke-Az (@('sql', 'db', 'update', '--subscription', $SubscriptionId,
        '--resource-group', $ResourceGroup, '--server', $SqlServerName, '--name', $Name) +
        $ComputeArguments + @('--output', 'json')) | Out-Null
}

$targets = @(
    [pscustomobject]@{ Name = $CaldovaDatabase; Objective = "HS_S_Gen5_$CaldovaMaxVCores" }
    foreach ($acquired in $AcquiredDatabases) {
        [pscustomobject]@{ Name = $acquired.Name; Objective = "HS_Gen5_$($acquired.VCores)" }
    }
)

Write-Step "Preflight: $Mode on $SqlServerFqdn (subscription $SubscriptionId)"
$pool = Invoke-Az @('sql', 'elastic-pool', 'show', '--subscription', $SubscriptionId,
    '--resource-group', $ResourceGroup, '--server', $SqlServerName, '--name', $ElasticPoolName, '--output', 'json')
$expectedPoolId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Sql/servers/$SqlServerName/elasticPools/$ElasticPoolName"
if ($pool.id -ne $expectedPoolId -or $pool.sku.tier -ne 'Hyperscale' -or $pool.state -ne 'Ready') {
    throw "Expected the existing, Ready Hyperscale pool $expectedPoolId; received id '$($pool.id)', tier '$($pool.sku.tier)', state '$($pool.state)'. No databases were changed."
}

$databases = @{}
foreach ($target in $targets) {
    $database = Get-DemoDatabase $target.Name
    $expectedDatabaseId = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Sql/servers/$SqlServerName/databases/$($target.Name)"
    if ($database.id -ne $expectedDatabaseId -or $database.sku.tier -ne 'Hyperscale' -or $database.sku.family -ne 'Gen5') {
        throw "Expected Hyperscale Gen5 database $expectedDatabaseId. No databases were changed."
    }
    if ($database.elasticPoolId -and $database.elasticPoolId -ne $pool.id) {
        throw "$($target.Name) belongs to another pool. Refusing to move it. No databases were changed."
    }
    if ($database.status -notin @('Online', 'Paused')) {
        throw "$($target.Name) is $($database.status); wait for its current operation to finish. No databases were changed."
    }
    if ($Mode -eq 'Pooled' -and
        ($database.zoneRedundant -ne $pool.zoneRedundant -or
        $database.requestedBackupStorageRedundancy -ne $BackupStorageRedundancy)) {
        throw "$($target.Name) must match pool zone redundancy and demo backup redundancy ($BackupStorageRedundancy). No databases were changed."
    }
    $databases[$target.Name] = $database
}

Write-Host 'Stop appointment traffic before moving databases. Connections can be interrupted.'
Write-Host 'Moves are sequential, not atomic. On failure, inspect 05-verify-sql.ps1 and rerun this command to finish.'
if ($Mode -eq 'Standalone') {
    Write-Host 'Native cross-database queries will no longer work. Caldova returns to serverless; the empty pool is retained.'
}

$allApproved = $true
foreach ($target in $targets) {
    $database = $databases[$target.Name]
    if (Test-DesiredState $database $target) {
        Write-Skip "$($target.Name) already matches $Mode"
        continue
    }

    $action = if ($Mode -eq 'Pooled') {
        "Move into $ElasticPoolName (convert serverless to provisioned first if needed)"
    }
    else {
        "Remove from pool and restore $($target.Objective)" +
        $(if ($target.Name -eq $CaldovaDatabase) { " (min $CaldovaMinVCores, auto-pause $AutoPauseDelayMinutes min)" })
    }
    if (-not $PSCmdlet.ShouldProcess("$SqlServerFqdn/$($target.Name)", $action)) {
        $allApproved = $false
        continue
    }

    Write-Step "$($target.Name): $action"
    try {
        if ($Mode -eq 'Pooled') {
            # Pools use provisioned compute; make the serverless conversion explicit.
            if ($database.currentServiceObjectiveName -like 'HS_S_*') {
                Set-DemoDatabase $target.Name @('--edition', 'Hyperscale', '--compute-model', 'Provisioned',
                    '--service-objective', "HS_Gen5_$CaldovaMaxVCores")
                $database = Get-DemoDatabase $target.Name
                if ($database.elasticPoolId -or $database.currentServiceObjectiveName -ne "HS_Gen5_$CaldovaMaxVCores" -or
                    $database.status -ne 'Online') {
                    throw 'Serverless-to-provisioned conversion did not reach the expected online standalone state.'
                }
            }
            Set-DemoDatabase $target.Name @('--elastic-pool', $pool.id)
        }
        elseif ($target.Name -eq $CaldovaDatabase) {
            if ($database.elasticPoolId) {
                # A standalone service objective removes pool membership before enabling serverless.
                Set-DemoDatabase $target.Name @('--service-objective', "HS_Gen5_$CaldovaMaxVCores")
                $database = Get-DemoDatabase $target.Name
                if ($database.elasticPoolId -or $database.currentServiceObjectiveName -ne "HS_Gen5_$CaldovaMaxVCores" -or
                    $database.status -ne 'Online') {
                    throw 'Pool removal did not reach the expected online standalone state.'
                }
            }
            Set-DemoDatabase $target.Name @('--edition', 'Hyperscale', '--compute-model', 'Serverless',
                '--service-objective', $target.Objective, '--min-capacity', "$CaldovaMinVCores",
                '--auto-pause-delay', "$AutoPauseDelayMinutes")
        }
        else {
            Set-DemoDatabase $target.Name @('--service-objective', $target.Objective)
        }

        if (-not (Test-DesiredState (Get-DemoDatabase $target.Name) $target)) {
            throw 'Azure returned without the requested final database configuration.'
        }
        Write-Ok "$($target.Name) verified"
    }
    catch {
        throw "$($target.Name) failed while applying $Mode. Earlier moves may have completed; no automatic rollback was attempted. Inspect 05-verify-sql.ps1 before rerunning. Details: $_"
    }
}

if (-not $allApproved) {
    Write-Host 'Plan/partial execution only: at least one change was not applied. No overall success is claimed.'
    return
}

Write-Step 'Verifying all three databases'
foreach ($target in $targets) {
    $database = Get-DemoDatabase $target.Name
    if (-not (Test-DesiredState $database $target)) {
        throw "$($target.Name) no longer matches $Mode. Run 05-verify-sql.ps1 to inspect the current state."
    }
    [pscustomobject]@{
        Database = $database.name
        Status = $database.status
        Objective = $database.currentServiceObjectiveName
        Pool = $database.elasticPoolId
        MinVCores = $database.minCapacity
        AutoPauseMinutes = $database.autoPauseDelay
    } | Format-List
}
Write-Ok "All three databases verified: $Mode. The pool was retained; no data was deleted."
