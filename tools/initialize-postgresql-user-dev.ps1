[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$installDir = 'C:\Program Files\PostgreSQL\18'
$binDir = Join-Path $installDir 'bin'
$initdb = Join-Path $binDir 'initdb.exe'
$pgCtl = Join-Path $binDir 'pg_ctl.exe'
$pgIsReady = Join-Path $binDir 'pg_isready.exe'
$baseDir = Join-Path $env:LOCALAPPDATA 'UUSport\PostgreSQL\18'
$dataDir = Join-Path $baseDir 'data'
$logDir = Join-Path $baseDir 'logs'
$serverLog = Join-Path $logDir 'postgresql.log'
$secretFile = Join-Path $env:LOCALAPPDATA 'UUSport\secrets\postgres-18-superuser.dpapi'

foreach ($required in @($initdb, $pgCtl, $pgIsReady, $secretFile)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Gerekli dosya bulunamadi: $required"
    }
}

[System.IO.Directory]::CreateDirectory($dataDir) | Out-Null
[System.IO.Directory]::CreateDirectory($logDir) | Out-Null

$clusterVersion = Join-Path $dataDir 'PG_VERSION'
if (-not (Test-Path -LiteralPath $clusterVersion)) {
    $existing = @(Get-ChildItem -LiteralPath $dataDir -Force -ErrorAction SilentlyContinue)
    if ($existing.Count -gt 0) {
        throw 'Kullanici veri klasoru bos degil ancak PG_VERSION yok; otomatik kurulum durduruldu.'
    }

    $securePassword = Get-Content -LiteralPath $secretFile | ConvertTo-SecureString
    $credential = New-Object Management.Automation.PSCredential('postgres', $securePassword)
    $plainPassword = $credential.GetNetworkCredential().Password

    $passwordFile = Join-Path $env:TEMP ("uusport-pg-{0}.tmp" -f [Guid]::NewGuid().ToString('N'))
    $deleteOnClose = [IO.FileOptions]::DeleteOnClose
    $shareMode = [IO.FileShare]::Read -bor [IO.FileShare]::Delete
    $stream = New-Object IO.FileStream($passwordFile, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, $shareMode, 4096, $deleteOnClose)
    $writer = New-Object IO.StreamWriter($stream, (New-Object Text.UTF8Encoding($false)))

    try {
        $writer.WriteLine($plainPassword)
        $writer.Flush()

        & $initdb -D $dataDir --username=postgres --encoding=UTF8 --locale=C --locale-provider=icu --icu-locale=tr-TR --auth-local=scram-sha-256 --auth-host=scram-sha-256 --data-checksums --pwfile=$passwordFile
        if ($LASTEXITCODE -ne 0) {
            throw "initdb basarisiz oldu: $LASTEXITCODE"
        }
    } finally {
        $plainPassword = $null
        $credential = $null
        $securePassword = $null
        $writer.Dispose()
        $stream.Dispose()
        [GC]::Collect()
    }
}

& $pgCtl status -D $dataDir | Out-Null
if ($LASTEXITCODE -ne 0) {
    & $pgCtl start -D $dataDir -l $serverLog -w -t 60
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL baslatilamadi: $LASTEXITCODE"
    }
}

& $pgIsReady -h 127.0.0.1 -p 5432 -t 10
if ($LASTEXITCODE -ne 0) {
    throw "PostgreSQL 5432 portunda hazir degil: $LASTEXITCODE"
}

Write-Output "PostgreSQL kullanici gelistirme kumesi hazir: $dataDir"
