[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Bu betik yonetici yetkisiyle calistirilmalidir.'
}

$installDir = 'C:\Program Files\PostgreSQL\18'
$dataDir = Join-Path $installDir 'data'
$binDir = Join-Path $installDir 'bin'
$initdb = Join-Path $binDir 'initdb.exe'
$pgCtl = Join-Path $binDir 'pg_ctl.exe'
$serviceName = 'postgresql-x64-18'
$secretFile = Join-Path $env:LOCALAPPDATA 'UUSport\secrets\postgres-18-superuser.dpapi'

if (-not (Test-Path -LiteralPath $initdb) -or -not (Test-Path -LiteralPath $pgCtl)) {
    throw 'PostgreSQL 18 ikili dosyalari bulunamadi.'
}

if (-not (Test-Path -LiteralPath $secretFile)) {
    throw 'DPAPI ile korunan PostgreSQL parolasi bulunamadi.'
}

$clusterVersion = Join-Path $dataDir 'PG_VERSION'
if (-not (Test-Path -LiteralPath $clusterVersion)) {
    $existing = @(Get-ChildItem -LiteralPath $dataDir -Force -ErrorAction SilentlyContinue)
    if ($existing.Count -gt 0) {
        throw 'Veri klasoru bos degil ancak PG_VERSION yok; otomatik onarim durduruldu.'
    }

    $securePassword = Get-Content -LiteralPath $secretFile | ConvertTo-SecureString
    $credential = New-Object Management.Automation.PSCredential('postgres', $securePassword)
    $plainPassword = $credential.GetNetworkCredential().Password

    $psi = New-Object Diagnostics.ProcessStartInfo
    $psi.FileName = $initdb
    $psi.Arguments = ('-D "{0}" --username=postgres --encoding=UTF8 --locale=C --locale-provider=icu --icu-locale=tr-TR --auth-local=scram-sha-256 --auth-host=scram-sha-256 --data-checksums --pwprompt' -f $dataDir)
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true

    $process = New-Object Diagnostics.Process
    $process.StartInfo = $psi
    [void]$process.Start()
    $process.StandardInput.WriteLine($plainPassword)
    $process.StandardInput.WriteLine($plainPassword)
    $process.StandardInput.Close()
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    $plainPassword = $null
    $credential = $null
    $securePassword = $null
    [GC]::Collect()

    if ($process.ExitCode -ne 0) {
        throw "initdb basarisiz oldu ($($process.ExitCode)). $stdout $stderr"
    }
}

$acl = Get-Acl -LiteralPath $dataDir
$rule = New-Object Security.AccessControl.FileSystemAccessRule(
    'NT AUTHORITY\NETWORK SERVICE',
    'FullControl',
    'ContainerInherit,ObjectInherit',
    'None',
    'Allow'
)
$acl.SetAccessRule($rule)
Set-Acl -LiteralPath $dataDir -AclObject $acl

$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if (-not $existingService) {
    & $pgCtl register -N $serviceName -D $dataDir -S auto -U 'NT AUTHORITY\NetworkService'
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL servisi kaydedilemedi: $LASTEXITCODE"
    }
}

$service = Get-Service -Name $serviceName
if ($service.Status -ne 'Running') {
    Start-Service -Name $serviceName
    $service.WaitForStatus('Running', [TimeSpan]::FromSeconds(60))
}

Write-Output "PostgreSQL veri kumesi ve servisi hazir: $serviceName"
