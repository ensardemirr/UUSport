<#
.SYNOPSIS
  UU Sport gecelik yedek (guvenlik standardi S-10): pg_dump (custom format, sikistirilmis) +
  Data Protection anahtar klasoru, parola tabanli AES-256 ile sifrelenir, sonra yurt ici bir
  uzak/harici konuma kopyalanir. WAL arsivleme PostgreSQL tarafinda ayrica ayarlanir (bu betigin
  kapsami disinda; postgresql.conf archive_mode=on + archive_command).

  ONEMLI: DPAPI DEGIL, parola tabanli sifreleme kullanilir. DPAPI bu makineye baglidir; sunucu
  yanip/calinip giderse DPAPI ile sifrelenmis bir yedek DE onunla birlikte kurtarilamaz hale gelir.
  Felaket kurtarma yedeginin butun amaci makinenin kendisini kaybetmeye karsi calismasidir.

.PARAMETER OffsiteDir
  Yurt ici, bu sunucudan farkli bir diskte/konumda klasor (agdaki baska bir makine, harici disk vb.).
  Zorunlu degil; verilmezse yalniz yerel yedek klasorune yazilir (tek basina yeterli DEGILDIR).

.NOTES
  Once UUSPORT_BACKUP_KEY ortam degiskenini (uzun, rastgele bir parola) ayarlayin. Bu parola
  SUNUCUDA SAKLANMAZ; bir sifre yoneticisinde veya kilitli bir yerde ayri tutulur - yoksa yedek
  hicbir yerden geri yuklenemez.
  Calistirmak icin: powershell -File scripts/backup.ps1 -OffsiteDir "D:\UUSportYedek"
  Zamanlanmis gorev olarak kurulum icin scripts/install-scheduled-tasks.ps1 kullanilir (KULLANICI ONAYI GEREKIR).
#>
param(
    [string]$OffsiteDir,
    [string]$LocalDir = "$env:LOCALAPPDATA\UUSport\backups",
    [string]$Database = "uusport"
)

# 'Stop' kullanilmaz: Windows PowerShell 5.1'de native araclarin stderr'e yazdigi zararsiz NOTICE
# satirlari bile "Stop" altinda sonlandirici hataya donusuyor. Kritik adimlarda $LASTEXITCODE
# acikca kontrol edilip throw edilir.
$ErrorActionPreference = 'Continue'
Add-Type -AssemblyName System.Security
if (-not $env:UUSPORT_BACKUP_KEY -or $env:UUSPORT_BACKUP_KEY.Length -lt 16) {
    throw "UUSPORT_BACKUP_KEY ortam degiskeni ayarli degil veya cok kisa (en az 16 karakter). " +
        "Bu parola sunucu disinda (sifre yoneticisi, kasa) saklanmali; yedegi acmak icin gerekir."
}
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$workDir = Join-Path $LocalDir $stamp
New-Item -ItemType Directory -Force -Path $workDir | Out-Null

function Protect-WithPassphrase([string]$InFile, [string]$OutFile, [string]$Passphrase) {
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $salt = New-Object byte[] 16
    $rng.GetBytes($salt)
    $kdf = New-Object System.Security.Cryptography.Rfc2898DeriveBytes($Passphrase, $salt, 200000, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
    $key = $kdf.GetBytes(32)
    $iv = New-Object byte[] 16
    $rng.GetBytes($iv)
    $aes = [System.Security.Cryptography.Aes]::Create()
    $aes.Key = $key; $aes.IV = $iv; $aes.Mode = 'CBC'
    $out = [IO.File]::Create($OutFile)
    $out.Write($salt, 0, 16); $out.Write($iv, 0, 16)
    $encryptor = $aes.CreateEncryptor()
    $crypto = New-Object System.Security.Cryptography.CryptoStream($out, $encryptor, [System.Security.Cryptography.CryptoStreamMode]::Write)
    $inStream = [IO.File]::OpenRead($InFile)
    $inStream.CopyTo($crypto)
    $inStream.Close(); $crypto.FlushFinalBlock(); $crypto.Close(); $out.Close()
}

function ConvertFrom-HexStringCompat([string]$Hex) {
    # Windows PowerShell 5.1 .NET Framework'te calisir; [Convert]::FromHexString .NET 5+'tadir.
    $bytes = New-Object byte[] ($Hex.Length / 2)
    for ($i = 0; $i -lt $bytes.Length; $i++) { $bytes[$i] = [Convert]::ToByte($Hex.Substring($i * 2, 2), 16) }
    return $bytes
}

function Read-SuperuserCredential {
    # uusport_owner NOSUPERUSER/NOBYPASSRLS'tir (guvenlik standardi bolum 4) ve tablolar FORCE ROW
    # LEVEL SECURITY tasir; sahibi bile app.tenant_id olmadan satir goremez. Tam yedek TUM kiracilari
    # kapsamalidir, bu yuzden RLS'i dogal olarak asan tek yerel hesap (postgres superuser) kullanilir -
    # zaten tools/*.ps1 kurulum betiklerinin kullandigi ayni DPAPI sirrini paylasir.
    $secretDir = "$env:LOCALAPPDATA\UUSport\secrets"
    $hex = Get-Content -LiteralPath (Join-Path $secretDir 'postgres-18-superuser.dpapi') -Raw
    $bytes = ConvertFrom-HexStringCompat $hex.Trim()
    $plain = [System.Security.Cryptography.ProtectedData]::Unprotect($bytes, $null, [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
    return [System.Text.Encoding]::Unicode.GetString($plain)
}

Write-Host "== Veritabani dump aliniyor: $Database ==" -ForegroundColor Cyan
$env:PGPASSWORD = Read-SuperuserCredential
$dumpFile = Join-Path $workDir "$Database.dump"
& "C:\Program Files\PostgreSQL\18\bin\pg_dump.exe" -h 127.0.0.1 -U postgres -Fc -f $dumpFile $Database
if ($LASTEXITCODE -ne 0) { throw "pg_dump basarisiz (kod $LASTEXITCODE)" }
Remove-Item Env:\PGPASSWORD

Write-Host "== Data Protection anahtarlari kopyalaniyor ==" -ForegroundColor Cyan
$keysSrc = "$env:LOCALAPPDATA\UUSport\keys"
if (Test-Path $keysSrc) { Copy-Item $keysSrc (Join-Path $workDir 'keys') -Recurse }

Write-Host "== Parola tabanli AES-256 ile sifreleniyor ==" -ForegroundColor Cyan
$zipFile = Join-Path $LocalDir "$stamp.zip"
Compress-Archive -Path "$workDir\*" -DestinationPath $zipFile -Force
$encFile = "$zipFile.enc"
Protect-WithPassphrase -InFile $zipFile -OutFile $encFile -Passphrase $env:UUSPORT_BACKUP_KEY
Remove-Item $zipFile, $workDir -Recurse -Force

if ($OffsiteDir) {
    if (-not (Test-Path $OffsiteDir)) { throw "Uzak yedek klasoru bulunamadi: $OffsiteDir (once bir ag paylasimi/harici disk baglayin)" }
    Copy-Item $encFile $OffsiteDir -Force
    Write-Host "Uzak kopya: $(Join-Path $OffsiteDir (Split-Path -Leaf $encFile))" -ForegroundColor Green
} else {
    Write-Host "UYARI: -OffsiteDir verilmedi; yedek yalniz bu makinede. Diskin bozulmasi durumunda islevsizdir." -ForegroundColor Yellow
}

# 30 gunden eski yerel yedekleri temizle (disk dolmasin)
Get-ChildItem $LocalDir -Filter '*.zip.enc' | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } | Remove-Item -Force

Write-Host "Yedek tamam: $encFile" -ForegroundColor Green
