<#
.SYNOPSIS
  Surum paketi uretir (operasyon proseduru bolum 6, S-18): deployment/releases/<surum>/ altinda
  app/ (yayin ciktisi), kurulum/ (kurulum ve yedek betikleri), manifest.json, checksums.sha256.
  Paket config, gizli bilgi, log ve veri icermez. Var olan surum klasorunun ustune YAZILMAZ.

.NOTES
  Calistirmak icin: powershell -ExecutionPolicy Bypass -File scripts/release.ps1
  Calisma agaci temiz degilse (commit edilmemis kod) paket uretilmez; -KirliAgacaIzinVer yalniz deneme icindir.
#>
[CmdletBinding()]
param(
    [switch]$SkipTests,
    [switch]$KirliAgacaIzinVer
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$api = Join-Path $root 'src\UUSport.Api'

[xml]$proj = Get-Content -LiteralPath (Join-Path $api 'UUSport.Api.csproj')
$version = ($proj.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
if (-not $version) { throw 'Surum numarasi csproj icinde bulunamadi.' }

Push-Location $root
try {
    $commit = (git rev-parse HEAD).Trim()
    $dirty = @(git status --porcelain -- src tests scripts tools) | Where-Object { $_ }
}
finally { Pop-Location }
if ($dirty.Count -gt 0 -and -not $KirliAgacaIzinVer) {
    throw "Calisma agacinda commit edilmemis degisiklik var ($($dirty.Count) dosya). Once commit edin."
}

$releaseDir = Join-Path $root "deployment\releases\$version"
if (Test-Path -LiteralPath $releaseDir) {
    throw "Surum klasoru zaten var: $releaseDir (ustune yazilmaz; csproj'da surumu artirin)."
}

Write-Output "1/4 Derleme ve testler (surum $version, commit $($commit.Substring(0, 7)))..."
if ($SkipTests) { & (Join-Path $root 'tools\build-uusport.ps1') -SkipTests }
else { & (Join-Path $root 'tools\build-uusport.ps1') }

Write-Output '2/4 Yayin ciktisi uretiliyor...'
$appDir = Join-Path $releaseDir 'app'
dotnet publish (Join-Path $api 'UUSport.Api.csproj') -c Release -o $appDir --nologo -v q
if ($LASTEXITCODE -ne 0) { throw "dotnet publish basarisiz: $LASTEXITCODE" }
if (-not (Test-Path -LiteralPath (Join-Path $appDir 'wwwroot\index.html'))) { throw 'Yayin ciktisinda web arayuzu yok (wwwroot\index.html).' }
Get-ChildItem -LiteralPath $appDir -Filter '*.pdb' -Recurse | Remove-Item -Force

Write-Output '3/4 Kurulum betikleri kopyalaniyor...'
$kurulumDir = Join-Path $releaseDir 'kurulum'
New-Item -ItemType Directory -Force -Path $kurulumDir | Out-Null
Copy-Item -Path (Join-Path $root 'scripts\canli\*') -Destination $kurulumDir -Recurse
Copy-Item -LiteralPath (Join-Path $root 'scripts\backup.ps1'), (Join-Path $root 'scripts\restore-test.ps1') -Destination $kurulumDir

Write-Output '4/4 Manifest ve hash listesi...'
$files = @(Get-ChildItem -LiteralPath $releaseDir -Recurse -File | Sort-Object FullName)
$lines = foreach ($f in $files) {
    $rel = $f.FullName.Substring($releaseDir.Length + 1).Replace('\', '/')
    '{0}  {1}' -f (Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $rel
}
if ($lines.Count -eq 0) { throw 'Paket bos; hash listesi uretilemedi.' }
[IO.File]::WriteAllLines((Join-Path $releaseDir 'checksums.sha256'), $lines, (New-Object Text.UTF8Encoding($false)))

$migrations = @(Get-ChildItem -LiteralPath (Join-Path $api 'Data\Migrations') -Filter '2*.cs' |
    Where-Object { $_.Name -notlike '*.Designer.cs' } | Sort-Object Name | ForEach-Object { $_.BaseName })
$manifest = [ordered]@{
    urun           = 'sevk.pro'
    surum          = $version
    gitCommit      = $commit
    olusturma      = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    hedefOrtam     = 'Windows x64, PostgreSQL 18, ASP.NET Core Runtime 10'
    ilkMigration   = $migrations[0]
    sonMigration   = $migrations[-1]
    migrationSayisi = $migrations.Count
    dosyaSayisi    = $lines.Count
    testler        = $(if ($SkipTests) { 'ATLANDI' } else { 'gecti' })
    hazirlayan     = $env:USERNAME
}
[IO.File]::WriteAllText((Join-Path $releaseDir 'manifest.json'), ($manifest | ConvertTo-Json), (New-Object Text.UTF8Encoding($false)))

Write-Output "Paket hazir: $releaseDir ($($lines.Count) dosya, son migration $($migrations[-1]))"
