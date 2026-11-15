<#
.SYNOPSIS
  Sifreli yedek alir (veya -Prova ile en yeni yedegi gecici veritabanina geri yukleyip dogrular).
  SERVIS HESABI altinda, zamanlanmis gorevle calisir. Yedek parolasi yedek-parolasi.ps1 ile
  belirlenmis olmalidir; yoksa yedek ALINMAZ ve cikis kodu 3 doner.
#>
param(
    [Parameter(Mandatory = $true)][string]$Ortam,
    [switch]$Prova
)

$ErrorActionPreference = 'Continue'
. "$PSScriptRoot\ortak.ps1"
Add-Type -AssemblyName System.Security
$o = Get-OrtamTanimi $Ortam
New-Item -ItemType Directory -Force -Path $o.Loglar, $o.Yedekler | Out-Null
$tur = $(if ($Prova) { 'geri-yukleme-provasi' } else { 'yedek' })
$log = Join-Path $o.Loglar ("{0}-{1}.log" -f $tur, (Get-Date -Format 'yyyyMMdd-HHmmss'))

if (-not (Test-Path -LiteralPath $o.YedekAnahtari)) {
    "Yedek parolasi belirlenmemis ($($o.YedekAnahtari) yok). Once yonetici olarak yedek-parolasi.ps1 calistirin." | Set-Content -LiteralPath $log
    exit 3
}
try {
    $blob = [Convert]::FromBase64String((Get-Content -LiteralPath $o.YedekAnahtari -Raw).Trim())
    $plain = [Security.Cryptography.ProtectedData]::Unprotect($blob, $null, [Security.Cryptography.DataProtectionScope]::LocalMachine)
    $env:UUSPORT_BACKUP_KEY = [Text.Encoding]::UTF8.GetString($plain)
}
catch {
    "Yedek parolasi okunamadi: $($_.Exception.Message)" | Set-Content -LiteralPath $log
    exit 4
}

if ($Prova) { & "$PSScriptRoot\restore-test.ps1" -BackupDir $o.Yedekler -Port $o.PgPort *> $log }
else { & "$PSScriptRoot\backup.ps1" -LocalDir $o.Yedekler -Database $o.Veritabani -Port $o.PgPort *> $log }
$kod = $LASTEXITCODE
Remove-Item Env:\UUSPORT_BACKUP_KEY -ErrorAction SilentlyContinue
if ($null -eq $kod) { $kod = 0 }
if (-not $Prova -and $kod -eq 0 -and -not (Get-ChildItem -LiteralPath $o.Yedekler -Filter '*.zip.enc' |
        Where-Object { $_.LastWriteTime -gt (Get-Date).AddMinutes(-30) })) { $kod = 5 }
exit $kod
