[CmdletBinding()]
param(
    [switch]$SkipTests
)

# UUSport derlemesi: web arayuzu (React) -> API wwwroot, API (Release), testler.
# Testler uusport_test veritabaninda uygulama roluyle calisir; gercek veri kullanilmaz.

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$api = Join-Path $root 'src\UUSport.Api'
$web = Join-Path $root 'src\UUSport.Web'

Write-Output '1/4 Web arayuzu derleniyor...'
Push-Location $web
try {
    npm ci --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw "npm ci basarisiz: $LASTEXITCODE" }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "Web derlemesi basarisiz: $LASTEXITCODE" }
}
finally { Pop-Location }

Write-Output '2/4 API derleniyor (Release)...'
dotnet build (Join-Path $root 'UUSport.slnx') -c Release --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "dotnet build basarisiz: $LASTEXITCODE" }

if ($SkipTests) {
    Write-Output '3/4 Testler atlandi (-SkipTests).'
}
else {
    Write-Output '3/4 Test veritabani guncelleniyor ve testler calisiyor...'
    & dotnet (Join-Path $api 'bin\Release\net10.0\UUSport.Api.dll') migrate --database=uusport_test
    if ($LASTEXITCODE -ne 0) { throw "Test veritabani migration basarisiz: $LASTEXITCODE" }
    dotnet test (Join-Path $root 'tests\UUSport.Tests') -c Release --no-build --nologo
    if ($LASTEXITCODE -ne 0) { throw "Testler basarisiz: $LASTEXITCODE" }
}

Write-Output '4/4 Derleme tamam.'
