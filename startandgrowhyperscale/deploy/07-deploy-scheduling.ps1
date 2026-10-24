[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Schema', 'Network', 'Verify')]
    [string]$Phase
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\00-config.ps1"
Import-Module SqlServer -ErrorAction Stop

if ($AppName -ne 'app-caldova-network-0efc44' -or $CaldovaDatabase -ne 'caldova-scheduling') {
    throw 'The SQL files name the current Caldova databases and app identity explicitly. Review them before deploying to a different environment.'
}

$databaseNames = @($CaldovaDatabase) + @($AcquiredDatabases | ForEach-Object { $_.Name })
$sqlRoot = Join-Path $PSScriptRoot '..\sql'

function Invoke-SchedulingFile {
    param([string]$Database, [string]$File)
    Write-Step "$Database : $File"
    $token = Invoke-Az @('account', 'get-access-token', '--subscription', $SubscriptionId,
        '--resource', 'https://database.windows.net/', '--output', 'json')
    Invoke-Sqlcmd -ServerInstance $SqlServerFqdn -Database $Database -AccessToken $token.accessToken `
        -InputFile (Join-Path $sqlRoot $File) -ConnectionTimeout 90 -QueryTimeout 120 `
        -Encrypt Mandatory -AbortOnError -OutputSqlErrors $true -ErrorAction Stop
    Write-Ok "$File completed"
}

if ($Phase -ne 'Schema') {
    $pool = Invoke-Az @('sql', 'elastic-pool', 'show', '--subscription', $SubscriptionId,
        '--resource-group', $ResourceGroup, '--server', $SqlServerName, '--name', $ElasticPoolName, '--output', 'json')
    foreach ($name in $databaseNames) {
        $database = Invoke-Az @('sql', 'db', 'show', '--subscription', $SubscriptionId,
            '--resource-group', $ResourceGroup, '--server', $SqlServerName, '--name', $name, '--output', 'json')
        if ($database.elasticPoolId -ne $pool.id -or $database.status -ne 'Online') {
            throw "$name must be online in $ElasticPoolName before deploying or verifying native cross-database SQL."
        }
    }
}

switch ($Phase) {
    'Schema' {
        Invoke-SchedulingFile 'master' '00-app-login.sql'
        foreach ($name in $databaseNames) {
            Invoke-SchedulingFile $name '01-scheduling-schema.sql'
        }
    }
    'Network' {
        Invoke-SchedulingFile $CaldovaDatabase '02-network-view.sql'
        foreach ($name in $databaseNames) {
            Invoke-SchedulingFile $name '03-app-permissions.sql'
        }
    }
    'Verify' {
        Invoke-SchedulingFile $CaldovaDatabase '04-verify-read.sql'
        Write-Host 'This verified the deployment identity only. Verify app-managed-identity reads and booking in the deployed app.'
    }
}
