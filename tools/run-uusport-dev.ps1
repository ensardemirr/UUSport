[CmdletBinding()]
param(
    [switch]$NoBrowser
)

# UUSport gelistirme ortamini baslatir (masaustu kisayolu bunu calistirir):
# PostgreSQL kapaliysa baslatir, semayi gunceller, uygulamayi acar ve tarayiciyi gosterir.
# Uygulama zaten calisiyorsa yalniz tarayiciyi acar. Yalniz bu bilgisayardan erisilir (127.0.0.1).

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$apiDir = Join-Path $root 'src\UUSport.Api'
$app = Join-Path $apiDir 'bin\Release\net10.0\UUSport.Api.dll'
$pgReady = 'C:\Program Files\PostgreSQL\18\bin\pg_isready.exe'
$url = 'http://127.0.0.1:5080'

function Test-Health {
    try { return (Invoke-WebRequest -UseBasicParsing -Uri "$url/api/health" -TimeoutSec 2).StatusCode -eq 200 }
    catch { return $false }
}

$host.UI.RawUI.WindowTitle = 'UU Sport'

& $pgReady -h 127.0.0.1 -p 5432 -q
if ($LASTEXITCODE -ne 0) {
    Write-Output 'PostgreSQL baslatiliyor...'
    & (Join-Path $PSScriptRoot 'start-postgresql-dev.ps1')
}

if (-not (Test-Health)) {
    if (-not (Test-Path -LiteralPath $app) -or -not (Test-Path -LiteralPath (Join-Path $apiDir 'wwwroot\index.html'))) {
        Write-Output 'Uygulama derlenmemis; derleniyor...'
        & (Join-Path $PSScriptRoot 'build-uusport.ps1') -SkipTests
    }

    Write-Output 'Veritabani semasi kontrol ediliyor...'
    & dotnet $app migrate
    if ($LASTEXITCODE -ne 0) { throw "Migration basarisiz: $LASTEXITCODE" }

    Write-Output 'Uygulama baslatiliyor...'
    # Calisma klasoru proje klasorudur: wwwroot ve appsettings.json buradan okunur.
    Start-Process -FilePath 'dotnet' -ArgumentList "`"$app`"" -WorkingDirectory $apiDir -WindowStyle Minimized
    for ($i = 0; $i -lt 60 -and -not (Test-Health); $i++) { Start-Sleep -Milliseconds 500 }
    if (-not (Test-Health)) { throw 'Uygulama baslamadi. Kucultulmus "dotnet" penceresindeki mesaji kontrol edin.' }
}

Write-Output "UU Sport hazir: $url"
if (-not $NoBrowser) { Start-Process $url }
