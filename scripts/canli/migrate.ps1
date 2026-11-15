<#
.SYNOPSIS
  Gecerli surumun (app\current) migration'ini uygular. SERVIS HESABI altinda calisir
  (sevkpro-<ortam>-migrate gorevi veya kur-ic.ps1); cunku migration rolunun parolasi o hesaba kilitlidir.
#>
param(
    [Parameter(Mandatory = $true)][string]$Ortam,
    [switch]$Ic   # kur-ic.ps1 icinden cagrildi: ayri log acma
)

$ErrorActionPreference = 'Continue'
. "$PSScriptRoot\ortak.ps1"
$o = Get-OrtamTanimi $Ortam
$exe = Join-Path $o.Current 'UUSport.Api.exe'
if (-not (Test-Path -LiteralPath $exe)) { Write-Output "Uygulama yok: $exe"; exit 2 }

$env:Database__Port = "$($o.PgPort)"
$env:Database__Database = $o.Veritabani
Push-Location $o.Current
try {
    if ($Ic) { & $exe migrate }
    else {
        New-Item -ItemType Directory -Force -Path $o.Loglar | Out-Null
        $log = Join-Path $o.Loglar ("migrate-{0}.log" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
        & $exe migrate *> $log
    }
    $kod = $LASTEXITCODE
}
finally { Pop-Location }
exit $kod
