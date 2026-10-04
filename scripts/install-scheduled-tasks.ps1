<#
.SYNOPSIS
  Gecelik yedek ve haftalik geri yukleme provasi icin Windows Gorev Zamanlayici gorevlerini kurar
  (S-10). BU BETIK OTOMATIK CALISTIRILMAZ - elle, bilinçli bir kararla calistirilmalidir; zamanlanmis
  gorev kurmak sunucunun surekli durumunu degistirir.

.PARAMETER OffsiteDir
  Yurt ici, bu sunucudan FIZIKSEL OLARAK FARKLI bir konum (agdaki baska bir makine/NAS, harici disk).

.PARAMETER BackupKeyFile
  Yedek sifreleme parolasini iceren, bu makinede LocalMachine-DPAPI ile korunan bir dosya. Boylece
  gecelik gorev interaktif olmadan calisabilir. AYNI PAROLANIN bir kopyasi ayrica sunucu DISINDA
  (sifre yoneticisi, kasa) saklanmalidir - felaket durumunda (sunucu kaybi) geri yukleme icin
  gereken budur; bu dosyadaki DPAPI-LocalMachine kopyasi sunucuyla birlikte kaybolur.

.NOTES
  Once bir kere calistirin: powershell -File scripts/protect-backup-key.ps1 -KeyFile "C:\...\backup.key.dpapi"
  Sonra:   powershell -File scripts/install-scheduled-tasks.ps1 -OffsiteDir "\\NAS\UUSportYedek" -BackupKeyFile "C:\...\backup.key.dpapi"
#>
param(
    [Parameter(Mandatory = $true)][string]$OffsiteDir,
    [Parameter(Mandatory = $true)][string]$BackupKeyFile,
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Continue'
if (-not (Test-Path $BackupKeyFile)) { throw "Anahtar dosyasi yok: $BackupKeyFile (once scripts/protect-backup-key.ps1 calistirin)" }

$backupCmd = "`$env:UUSPORT_BACKUP_KEY = (Get-Content -Raw '$BackupKeyFile' | ConvertTo-SecureString | ForEach-Object { [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($_)) }); " +
    "& '$RepoRoot\scripts\backup.ps1' -OffsiteDir '$OffsiteDir'"
$restoreCmd = "`$env:UUSPORT_BACKUP_KEY = (Get-Content -Raw '$BackupKeyFile' | ConvertTo-SecureString | ForEach-Object { [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($_)) }); " +
    "& '$RepoRoot\scripts\restore-test.ps1' -BackupDir '$OffsiteDir'"

$backupAction = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -ExecutionPolicy Bypass -Command `"$backupCmd`""
$backupTrigger = New-ScheduledTaskTrigger -Daily -At 2:00AM
Register-ScheduledTask -TaskName 'UUSport-GeceilkYedek' -Action $backupAction -Trigger $backupTrigger `
    -RunLevel Highest -User $env:USERNAME -Description 'UU Sport gecelik veritabani yedegi (S-10)' -Force

$restoreAction = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -ExecutionPolicy Bypass -Command `"$restoreCmd`""
$restoreTrigger = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At 3:00AM
Register-ScheduledTask -TaskName 'UUSport-HaftalikGeriYuklemeProvasi' -Action $restoreAction -Trigger $restoreTrigger `
    -RunLevel Highest -User $env:USERNAME -Description 'UU Sport haftalik otomatik geri yukleme provasi (S-10)' -Force

Write-Host "Kuruldu: UUSport-GeceilkYedek (her gun 02:00), UUSport-HaftalikGeriYuklemeProvasi (Pazar 03:00)" -ForegroundColor Green
Write-Host "Provanin basarisiz oldugunu fark etmek icin Gorev Zamanlayici'da 'Son Calistirma Sonucu' sutununu izleyin." -ForegroundColor Yellow
