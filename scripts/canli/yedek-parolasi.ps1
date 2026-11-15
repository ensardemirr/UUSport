<#
.SYNOPSIS
  Yedek sifreleme parolasini belirler (YONETICI olarak, elle calistirilir; parola ekranda gorunmez).
  Parola bu makineye ozel korumali dosyaya yazilir ki gecelik yedek gorevi sormadan calisabilsin.

  ONEMLI: Ayni parolayi bu makinenin DISINDA da (sifre yoneticisi veya kilitli bir yerde) saklayin.
  Makine kaybolursa yedegi acabilecek tek sey o kopyadir.

.NOTES
  powershell -ExecutionPolicy Bypass -File yedek-parolasi.ps1 -Ortam canli
#>
param([Parameter(Mandatory = $true)][string]$Ortam)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\ortak.ps1"
Assert-Yonetici
Add-Type -AssemblyName System.Security
$o = Get-OrtamTanimi $Ortam

function ConvertTo-Duz([Security.SecureString]$s) { (New-Object Management.Automation.PSCredential('x', $s)).GetNetworkCredential().Password }
$p1 = ConvertTo-Duz (Read-Host -AsSecureString -Prompt 'Yedek parolasi (en az 16 karakter)')
$p2 = ConvertTo-Duz (Read-Host -AsSecureString -Prompt 'Yedek parolasi (tekrar)')
if ($p1 -cne $p2) { throw 'Iki giris ayni degil.' }
if ($p1.Length -lt 16) { throw 'Parola en az 16 karakter olmali.' }

$blob = [Security.Cryptography.ProtectedData]::Protect([Text.Encoding]::UTF8.GetBytes($p1), $null, [Security.Cryptography.DataProtectionScope]::LocalMachine)
[IO.File]::WriteAllText($o.YedekAnahtari, [Convert]::ToBase64String($blob))
$p1 = $null; $p2 = $null
& icacls $o.YedekAnahtari /inheritance:r /grant:r 'SYSTEM:(F)' 'Administrators:(F)' "$($o.Kullanici):(R)" | Out-Null

Write-Host "Parola kaydedildi. Ilk yedek simdi aliniyor..." -ForegroundColor Cyan
Wait-Gorev $o.GorevYedek
$son = Get-ChildItem -LiteralPath $o.Yedekler -Filter '*.zip.enc' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Write-Host "Yedek alindi: $($son.FullName)" -ForegroundColor Green
Write-Host 'Geri yukleme provasi yapiliyor...' -ForegroundColor Cyan
Wait-Gorev $o.GorevProva
Write-Host 'Geri yukleme provasi basarili.' -ForegroundColor Green
Write-Host "UNUTMAYIN: parolayi makine disinda saklayin ve $($o.Yedekler) klasorundeki dosyalari duzenli olarak baska yere kopyalayin." -ForegroundColor Yellow
