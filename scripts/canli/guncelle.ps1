<#
.SYNOPSIS
  Kurulu ortami yeni surume gunceller (YONETICI olarak, YENI surum paketinin kurulum\ klasorunden).
  Sira: paket dogrulama -> yedek -> uygulamayi durdur -> yeni surume gec -> migration -> baslat -> saglik.
  Migration veya saglik basarisizsa uygulama ONCEKI surume dondurulur; veritabani geri donusu ayri
  istir (yedekten geri yukleme) ve otomatik yapilmaz.
#>
param(
    [Parameter(Mandatory = $true)][ValidateSet('canli', 'prova')][string]$Ortam,
    [switch]$YedeksizDevam
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\ortak.ps1"
Assert-Yonetici
$o = Get-OrtamTanimi $Ortam
$paket = Split-Path -Parent $PSScriptRoot
$manifest = Get-Content -LiteralPath (Join-Path $paket 'manifest.json') -Raw | ConvertFrom-Json
$surum = $manifest.surum
$log = "C:\SevkPro\kurulum-loglari\guncelle-$Ortam-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
Start-Transcript -LiteralPath $log | Out-Null
try {
    Write-Output "== sevk.pro $Ortam -> $surum guncellemesi =="
    Test-PaketHash $paket
    if (-not (Get-Service -Name $o.ApiServis -ErrorAction SilentlyContinue)) { throw 'Bu ortam kurulu degil; once kur.ps1.' }
    $hedefApp = Join-Path $o.AppKok $surum
    if (Test-Path -LiteralPath $hedefApp) { throw "Bu surum zaten kurulu: $hedefApp" }
    $onceki = (Get-Item -LiteralPath $o.Current).Target | Select-Object -First 1

    if ($YedeksizDevam) { Write-Output 'UYARI: yedek alinmadan devam ediliyor (-YedeksizDevam).' }
    else { Write-Output 'Yedek aliniyor...'; Wait-Gorev $o.GorevYedek 1800 }

    Copy-Item -LiteralPath (Join-Path $paket 'app') -Destination $hedefApp -Recurse
    Copy-Item -LiteralPath (Join-Path $paket 'manifest.json') -Destination (Join-Path $hedefApp 'manifest.json')
    Stop-Service -Name $o.ApiServis
    Copy-Item -Path (Join-Path $paket 'kurulum\*') -Destination $o.Kurulum -Recurse -Force
    & cmd /c rmdir "$($o.Current)" | Out-Null
    & cmd /c mklink /J "$($o.Current)" "$hedefApp" | Out-Null
    try {
        Write-Output 'Migration...'
        Wait-Gorev $o.GorevMigrate 1800
        Start-Service -Name $o.ApiServis
        Wait-Saglik $o.ApiPort 90
    }
    catch {
        Write-Output "Guncelleme basarisiz: $($_.Exception.Message). Uygulama onceki surume donduruluyor: $onceki"
        Stop-Service -Name $o.ApiServis -ErrorAction SilentlyContinue
        & cmd /c rmdir "$($o.Current)" | Out-Null
        & cmd /c mklink /J "$($o.Current)" "$onceki" | Out-Null
        Start-Service -Name $o.ApiServis
        throw 'Onceki surume donuldu. Migration kismen uygulandiysa veritabani yedekten geri yuklenmelidir; loglara bakin.'
    }
    [pscustomobject]@{ ortam = $Ortam; surum = $surum; gitCommit = $manifest.gitCommit; sonMigration = $manifest.sonMigration; kurulum = (Get-Date -Format 'yyyy-MM-dd HH:mm') } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $o.Kok 'kurulu-surum.json')
    Write-Output "GUNCELLEME TAMAM: $Ortam, surum $surum"
}
catch {
    Write-Output "HATA: $($_.Exception.Message)"
    Stop-Transcript | Out-Null
    exit 1
}
Stop-Transcript | Out-Null
