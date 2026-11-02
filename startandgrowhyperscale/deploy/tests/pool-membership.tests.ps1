[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot '..\06-set-pool-membership.ps1'
$subscription = '00000000-0000-0000-0000-000000000000'
$serverId = "/subscriptions/$subscription/resourceGroups/caldova-test-rg/providers/Microsoft.Sql/servers/sql-caldova-test"
$poolId = "$serverId/elasticPools/caldova-network-pool"
$names = @('caldova-scheduling', 'northbrook-scheduling', 'ridgeview-scheduling')
$passed = 0

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw "ASSERTION FAILED: $Message" }
}

function Reset-Fixture {
    param([switch]$Pooled)
    $script:fixture = @{
        Updates = [System.Collections.Generic.List[string]]::new()
        FailName = ''
        IgnoreUpdates = $false
        Pool = @{
            id = $poolId; state = 'Ready'; zoneRedundant = $false
            sku = @{ tier = 'Hyperscale'; family = 'Gen5' }
        }
        Databases = @{}
    }
    $objectives = @('HS_S_Gen5_8', 'HS_Gen5_2', 'HS_Gen5_4')
    for ($i = 0; $i -lt $names.Count; $i++) {
        $script:fixture.Databases[$names[$i]] = @{
            id = "$serverId/databases/$($names[$i])"; name = $names[$i]
            elasticPoolId = $(if ($Pooled) { $poolId } else { $null })
            currentServiceObjectiveName = $(if ($Pooled) { 'ElasticPool' } else { $objectives[$i] })
            status = 'Online'; minCapacity = 1; autoPauseDelay = 60
            zoneRedundant = $false; requestedBackupStorageRedundancy = 'Local'
            sku = @{ tier = 'Hyperscale'; family = $(if ($Pooled) { $null } else { 'Gen5' }) }
        }
    }
}

function az {
    $arguments = @($args)
    $global:LASTEXITCODE = 0
    Assert-True ($arguments -contains '--subscription') 'Every Azure call must pin the subscription'
    $subscriptionIndex = [Array]::IndexOf($arguments, '--subscription')
    Assert-True ($arguments[$subscriptionIndex + 1] -eq $subscription) 'Unexpected subscription'
    $command = $arguments[0..3] -join ' '
    if ($command -eq 'sql elastic-pool show --subscription') {
        return ($fixture.Pool | ConvertTo-Json -Depth 6 -Compress)
    }
    Assert-True ($arguments[0] -eq 'sql' -and $arguments[1] -eq 'db') 'Unexpected Azure command'
    $nameIndex = [Array]::IndexOf($arguments, '--name')
    $name = $arguments[$nameIndex + 1]
    Assert-True ($name -in $names) 'Only demo databases may be touched'
    $database = $fixture.Databases[$name]
    if ($arguments[2] -eq 'show') {
        return ($database | ConvertTo-Json -Depth 6 -Compress)
    }
    Assert-True ($arguments[2] -eq 'update') 'Unexpected Azure operation'
    Assert-True ($arguments -notcontains '--no-wait') 'Updates must wait for completion'
    $fixture.Updates.Add(($arguments -join ' '))
    if ($name -eq $fixture.FailName) {
        $global:LASTEXITCODE = 1
        return 'Simulated Azure update failure'
    }
    if (-not $fixture.IgnoreUpdates) {
        if ($arguments -contains '--elastic-pool') {
            $index = [Array]::IndexOf($arguments, '--elastic-pool')
            Assert-True ($arguments[$index + 1] -eq $poolId) 'Move must use the exact pool resource ID'
            Assert-True ($database.currentServiceObjectiveName -notlike 'HS_S_*') 'Serverless must be converted before pooling'
            $database.elasticPoolId = $poolId
            $database.currentServiceObjectiveName = 'ElasticPool'
            $database.sku.family = $null
        }
        else {
            $index = [Array]::IndexOf($arguments, '--service-objective')
            Assert-True ($index -ge 0) 'Standalone updates must specify the service objective'
            $objective = $arguments[$index + 1]
            if ($objective -like 'HS_S_*') {
                Assert-True (-not $database.elasticPoolId) 'Must detach from pool before enabling serverless'
                $index = [Array]::IndexOf($arguments, '--min-capacity')
                $database.minCapacity = [double]$arguments[$index + 1]
                $index = [Array]::IndexOf($arguments, '--auto-pause-delay')
                $database.autoPauseDelay = [int]$arguments[$index + 1]
            }
            $database.elasticPoolId = $null
            $database.currentServiceObjectiveName = $objective
            $database.sku.family = 'Gen5'
        }
        $database.status = 'Online'
    }
    return ($database | ConvertTo-Json -Depth 6 -Compress)
}

function Invoke-ExpectedFailure {
    param([string]$Mode, [string]$Pattern)
    $errorText = $null
    try { & $scriptPath -Mode $Mode | Out-Null }
    catch { $errorText = $_.ToString() }
    Assert-True ($null -ne $errorText -and $errorText -like $Pattern) "Expected failure '$Pattern'; got '$errorText'"
}

$overrides = @{
    AZURE_SUBSCRIPTION_ID = $subscription
    CALDOVA_NETWORK_RESOURCE_GROUP = 'caldova-test-rg'
    CALDOVA_NETWORK_SQL_SERVER = 'sql-caldova-test'
}
$previous = @{}
try {
    foreach ($key in $overrides.Keys) {
        $previous[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
        [Environment]::SetEnvironmentVariable($key, $overrides[$key], 'Process')
    }

    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref]$tokens, [ref]$parseErrors) | Out-Null
    Assert-True ($parseErrors.Count -eq 0) 'Script must parse'
    $passed++

    Reset-Fixture
    $script:fixture.Databases[$names[0]].status = 'Paused'
    & $scriptPath -Mode Pooled -WhatIf | Out-Null
    Assert-True ($script:fixture.Updates.Count -eq 0) 'WhatIf must not update resources'
    $passed++

    & $scriptPath -Mode Pooled | Out-Null
    Assert-True ($script:fixture.Updates.Count -eq 4) 'Pool join requires conversion plus three moves'
    foreach ($name in $names) {
        Assert-True ($script:fixture.Databases[$name].elasticPoolId -eq $poolId) "$name must join the pool"
    }
    $passed++

    $script:fixture.Updates.Clear()
    $summaryOutput = @(& $scriptPath -Mode Pooled)
    Assert-True ($script:fixture.Updates.Count -eq 0) 'Repeated pooled operation must be a no-op'
    Assert-True ($summaryOutput.Count -eq 0) 'Pooled summary must render to the host without leaking formatting-control objects into the output pipeline'
    $passed++

    & $scriptPath -Mode Standalone -WhatIf | Out-Null
    Assert-True ($script:fixture.Updates.Count -eq 0) 'Standalone WhatIf must not update resources'
    $passed++

    $summaryOutput = @(& $scriptPath -Mode Standalone)
    Assert-True ($script:fixture.Updates.Count -eq 4) 'Reset requires detach, serverless conversion, and two provisioned resets'
    Assert-True ($summaryOutput.Count -eq 0) 'Standalone summary must render to the host without leaking formatting-control objects into the output pipeline'
    Assert-True ($script:fixture.Databases[$names[0]].currentServiceObjectiveName -eq 'HS_S_Gen5_8') 'Restore Caldova serverless'
    Assert-True ($script:fixture.Databases[$names[0]].minCapacity -eq 1) 'Restore minimum vCores'
    Assert-True ($script:fixture.Databases[$names[0]].autoPauseDelay -eq 60) 'Restore auto-pause'
    Assert-True ($script:fixture.Databases[$names[1]].currentServiceObjectiveName -eq 'HS_Gen5_2') 'Restore Northbrook size'
    Assert-True ($script:fixture.Databases[$names[2]].currentServiceObjectiveName -eq 'HS_Gen5_4') 'Restore Ridgeview size'
    foreach ($name in $names) {
        Assert-True (-not $script:fixture.Databases[$name].elasticPoolId) "$name must leave the pool"
    }
    $passed++

    $script:fixture.Updates.Clear()
    $script:fixture.Databases[$names[0]].status = 'Paused'
    & $scriptPath -Mode Standalone | Out-Null
    Assert-True ($script:fixture.Updates.Count -eq 0) 'Repeated standalone operation must not resume a paused database'
    $passed++

    foreach ($fault in @('OtherPool', 'WrongDatabaseId', 'WrongTier', 'ZoneMismatch', 'BackupMismatch', 'Busy', 'WrongPoolId', 'WrongPoolFamily')) {
        Reset-Fixture
        $database = $script:fixture.Databases[$names[2]]
        switch ($fault) {
            'OtherPool' { $database.elasticPoolId = "$serverId/elasticPools/unrelated" }
            'WrongDatabaseId' { $database.id = "$serverId/databases/unrelated" }
            'WrongTier' { $database.sku.tier = 'GeneralPurpose' }
            'ZoneMismatch' { $database.zoneRedundant = $true }
            'BackupMismatch' { $database.requestedBackupStorageRedundancy = 'Geo' }
            'Busy' { $database.status = 'Scaling' }
            'WrongPoolId' { $script:fixture.Pool.id = "$serverId/elasticPools/unrelated" }
            'WrongPoolFamily' { $script:fixture.Pool.sku.family = 'PRMS' }
        }
        Invoke-ExpectedFailure Pooled '*No databases were changed*'
        Assert-True ($script:fixture.Updates.Count -eq 0) "$fault must fail before any update"
        $passed++
    }

    Reset-Fixture
    $script:fixture.FailName = $names[1]
    Invoke-ExpectedFailure Pooled '*Simulated Azure update failure*'
    Assert-True ($script:fixture.Databases[$names[0]].elasticPoolId -eq $poolId) 'Completed first move must not be rolled back'
    Assert-True (-not $script:fixture.Databases[$names[2]].elasticPoolId) 'Failure must stop before the third database'
    $script:fixture.FailName = ''
    $script:fixture.Updates.Clear()
    & $scriptPath -Mode Pooled | Out-Null
    Assert-True ($script:fixture.Updates.Count -eq 2) 'Rerun must finish only the remaining moves'
    $passed++

    Reset-Fixture
    $script:fixture.IgnoreUpdates = $true
    Invoke-ExpectedFailure Pooled '*conversion did not reach*'
    Assert-True ($script:fixture.Updates.Count -eq 1) 'Unexpected conversion result must stop execution'
    $passed++

    Reset-Fixture -Pooled
    $script:fixture.IgnoreUpdates = $true
    Invoke-ExpectedFailure Standalone '*removal did not reach*'
    Assert-True ($script:fixture.Updates.Count -eq 1) 'Unexpected detach result must stop execution'
    $passed++

    Reset-Fixture
    $script:fixture.Databases[$names[0]].currentServiceObjectiveName = 'HS_Gen5_8'
    $script:fixture.IgnoreUpdates = $true
    Invoke-ExpectedFailure Pooled '*requested final database configuration*'
    $passed++

    Reset-Fixture
    $script:fixture.Databases[$names[0]].minCapacity = 2
    $script:fixture.Databases[$names[0]].autoPauseDelay = -1
    & $scriptPath -Mode Standalone | Out-Null
    Assert-True ($script:fixture.Updates.Count -eq 1) 'Reset must repair serverless settings even when already standalone'
    $passed++

    Write-Host "`nPASS: $passed pool-membership tests (mocked Azure CLI; no live updates)." -ForegroundColor Green
}
finally {
    foreach ($key in $previous.Keys) {
        [Environment]::SetEnvironmentVariable($key, $previous[$key], 'Process')
    }
}
