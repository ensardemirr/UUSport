[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$principal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Bu betik yonetici yetkisiyle calistirilmalidir.'
}

$installer = 'C:\UUSport\tools\installers\postgresql-18.6-4-windows-x64.exe'
$expectedHash = 'A98074954015F3D733DBB92315BD06E01A381778F0A8AE4CEBF9CB199D73AAB0'
$installDir = 'C:\Program Files\PostgreSQL\18'
$dataDir = Join-Path $installDir 'data'
$serviceName = 'postgresql-x64-18'

$actualHash = (Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash
if ($actualHash -ne $expectedHash) {
    throw "PostgreSQL kurucu ozeti eslesmiyor: $actualHash"
}

$signature = Get-AuthenticodeSignature -LiteralPath $installer
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notlike '*EnterpriseDB Corporation*') {
    throw "PostgreSQL kurucu imzasi gecersiz: $($signature.Status)"
}

$secretDir = Join-Path $env:LOCALAPPDATA 'UUSport\secrets'
$secretFile = Join-Path $secretDir 'postgres-18-superuser.dpapi'
[System.IO.Directory]::CreateDirectory($secretDir) | Out-Null

if (Test-Path -LiteralPath $secretFile) {
    $securePassword = Get-Content -LiteralPath $secretFile | ConvertTo-SecureString
} else {
    $randomBytes = New-Object byte[] 36
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($randomBytes)
    } finally {
        $rng.Dispose()
    }

    $plainGenerated = ([Convert]::ToBase64String($randomBytes).TrimEnd('=') -replace '\+', '-' -replace '/', '_') + '!Aa9'
    $securePassword = ConvertTo-SecureString $plainGenerated -AsPlainText -Force
    $securePassword | ConvertFrom-SecureString | Set-Content -LiteralPath $secretFile
    $plainGenerated = $null
}

$credential = New-Object Management.Automation.PSCredential('postgres', $securePassword)
$plainPassword = $credential.GetNetworkCredential().Password

$installerArgs = @(
    '--mode', 'unattended',
    '--unattendedmodeui', 'minimal',
    '--prefix', $installDir,
    '--datadir', $dataDir,
    '--serverport', '5432',
    '--servicename', $serviceName,
    '--superaccount', 'postgres',
    '--superpassword', $plainPassword,
    '--enable-components', 'server,pgAdmin,commandlinetools',
    '--disable-components', 'stackbuilder',
    '--create_shortcuts', '1'
)

try {
    & $installer @installerArgs
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL kurulumu hata koduyla sonlandi: $LASTEXITCODE"
    }
} finally {
    $plainPassword = $null
    $credential = $null
    $securePassword = $null
    [GC]::Collect()
}

Write-Output "PostgreSQL kurulumu tamamlandi: $installDir"
