<#
.SYNOPSIS
  PROVA ortamini tamamen kaldirir (YONETICI olarak): servisler, gorevler, hesap, profil, klasor.
  Canli ortam icin CALISMAZ: canli veriyi silen bir betik bilerek yazilmadi.
#>
param([Parameter(Mandatory = $true)][ValidateSet('prova')][string]$Ortam)

$ErrorActionPreference = 'Continue'
. "$PSScriptRoot\ortak.ps1"
Assert-Yonetici
$o = Get-OrtamTanimi $Ortam

foreach ($g in @($o.GorevMigrate, $o.GorevYedek, $o.GorevProva, 'sevkpro-kurulum-gecici')) {
    Unregister-ScheduledTask -TaskName $g -Confirm:$false -ErrorAction SilentlyContinue
}
foreach ($s in @($o.ApiServis, $o.PgServis)) {
    if (Get-Service -Name $s -ErrorAction SilentlyContinue) {
        Stop-Service -Name $s -Force -ErrorAction SilentlyContinue
        & sc.exe delete $s | Out-Null
    }
}
Start-Sleep -Seconds 8   # PostgreSQL sureclerinin kapanmasi icin
$hesap = Get-LocalUser -Name $o.Kullanici -ErrorAction SilentlyContinue
$sid = $(if ($hesap) { $hesap.SID.Value } else { '' })
$profilSec = { $_.SID -eq $sid -or $_.LocalPath -like "*\$($o.Kullanici)" -or $_.LocalPath -like "*\$($o.Kullanici).*" }
foreach ($deneme in 1..3) {
    $profil = Get-CimInstance Win32_UserProfile | Where-Object $profilSec
    if (-not $profil) { break }
    $profil | Remove-CimInstance -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 5
}
if (Get-CimInstance Win32_UserProfile | Where-Object $profilSec) {
    Write-Output "UYARI: profil klasoru silinemedi (dosya kullanimda olabilir). Makineyi yeniden baslatip kaldir.ps1'i tekrar calistirin."
}
if ($hesap) { Remove-LocalUser -Name $o.Kullanici }
if (Test-Path -LiteralPath $o.Current) { & cmd /c rmdir "$($o.Current)" | Out-Null }
if (Test-Path -LiteralPath $o.Kok) { Remove-Item -LiteralPath $o.Kok -Recurse -Force }
Write-Output "Prova ortami kaldirildi: $($o.Kok), hesap $($o.Kullanici)."
