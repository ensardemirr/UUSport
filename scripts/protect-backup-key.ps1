<#
.SYNOPSIS
  Yedek sifreleme parolasini bu makineye ozel (DPAPI LocalMachine) korumali bir dosyaya yazar; boylece
  gecelik zamanlanmis gorev interaktif olmadan calisabilir (bkz. scripts/install-scheduled-tasks.ps1).

  ONEMLI: Aynı parolanın bir kopyasını ayrıca SUNUCU DIŞINDA (şifre yöneticisi, kilitli bir yerde)
  saklayın. Bu dosya yalnız otomasyon icindir; sunucu kaybedilirse bu kopya da kaybolur.

.NOTES
  Calistirmak icin: powershell -File scripts/protect-backup-key.ps1 -KeyFile "C:\...\backup.key.dpapi"
  (parola konsoldan gizli girilir, ekranda gorunmez)
#>
param(
    [Parameter(Mandatory = $true)][string]$KeyFile
)

$secure = Read-Host -AsSecureString -Prompt 'Yedek sifreleme parolasini girin (en az 16 karakter, ekranda gorunmez)'
$plain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
if ($plain.Length -lt 16) { throw "Parola en az 16 karakter olmali." }

New-Item -ItemType Directory -Force -Path (Split-Path $KeyFile) | Out-Null
$secure | ConvertFrom-SecureString | Set-Content -LiteralPath $KeyFile
Write-Host "Yazildi: $KeyFile (bu makineye ozel korumali; baska makinede acilamaz)" -ForegroundColor Green
Write-Host "UNUTMAYIN: Ayni parolanin bir kopyasini sunucu disinda (sifre yoneticisi) da saklayin." -ForegroundColor Yellow
