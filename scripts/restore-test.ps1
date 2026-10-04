<#
.SYNOPSIS
  Otomatik geri yukleme provasi (guvenlik standardi S-10): en yeni yedegi bulur, parolayla cozer,
  gecici bir veritabanina pg_restore ile geri yukler ve temel butunluk kontrolu yapar. Yedek dosyasi
  acilamiyorsa veya geri yukleme/kontrol basarisiz olursa cikis kodu 1 doner - haftalik zamanlanmis
  gorevde bu, bir uyari/e-posta tetiklemek icin kullanilabilir. Gecici veritabani her kosulda silinir.

.NOTES
  Calistirmak icin: $env:UUSPORT_BACKUP_KEY = '...'; powershell -File scripts/restore-test.ps1 -BackupDir "D:\UUSportYedek"
#>
param(
    [Parameter(Mandatory = $true)][string]$BackupDir,
    [string]$PgBin = "C:\Program Files\PostgreSQL\18\bin"
)

# 'Stop' kullanilmaz: Windows PowerShell 5.1'de native araclarin (dropdb/psql vb.) stderr'e yazdigi
# zararsiz NOTICE satirlari bile "Stop" altinda sonlandirici hataya donusuyor. Kritik adimlarda
# $LASTEXITCODE acikca kontrol edilip throw edilir.
$ErrorActionPreference = 'Continue'
Add-Type -AssemblyName System.Security
if (-not $env:UUSPORT_BACKUP_KEY) { throw "UUSPORT_BACKUP_KEY ortam degiskeni ayarli degil." }

$latest = Get-ChildItem $BackupDir -Filter '*.zip.enc' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $latest) { throw "Yedek bulunamadi: $BackupDir" }
Write-Host "En yeni yedek: $($latest.Name) ($(Get-Date $latest.LastWriteTime -Format 'yyyy-MM-dd HH:mm'))" -ForegroundColor Cyan

function Unprotect-WithPassphrase([string]$InFile, [string]$OutFile, [string]$Passphrase) {
    $inStream = [IO.File]::OpenRead($InFile)
    $salt = New-Object byte[] 16; $inStream.Read($salt, 0, 16) | Out-Null
    $iv = New-Object byte[] 16; $inStream.Read($iv, 0, 16) | Out-Null
    $kdf = New-Object System.Security.Cryptography.Rfc2898DeriveBytes($Passphrase, $salt, 200000, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
    $key = $kdf.GetBytes(32)
    $aes = [System.Security.Cryptography.Aes]::Create()
    $aes.Key = $key; $aes.IV = $iv; $aes.Mode = 'CBC'
    $decryptor = $aes.CreateDecryptor()
    $out = [IO.File]::Create($OutFile)
    $crypto = New-Object System.Security.Cryptography.CryptoStream($inStream, $decryptor, [System.Security.Cryptography.CryptoStreamMode]::Read)
    $crypto.CopyTo($out)
    $out.Close(); $crypto.Close(); $inStream.Close()
}

$work = Join-Path $env:TEMP "uusport-restore-test-$(Get-Date -Format 'yyyyMMddHHmmss')"
New-Item -ItemType Directory -Force -Path $work | Out-Null
$dbName = "uusport_restoretest_$(Get-Date -Format 'yyyyMMddHHmmss')"

function ConvertFrom-HexStringCompat([string]$Hex) {
    $bytes = New-Object byte[] ($Hex.Length / 2)
    for ($i = 0; $i -lt $bytes.Length; $i++) { $bytes[$i] = [Convert]::ToByte($Hex.Substring($i * 2, 2), 16) }
    return $bytes
}

function Read-SuperuserCredential {
    # createdb/pg_restore/dropdb icin: RLS FORCE altindaki tablolari sinamak icin dump/restore
    # dongusunun kendisi de RLS'i asabilmelidir (bkz. scripts/backup.ps1 aciklamasi).
    $hex = Get-Content -LiteralPath "$env:LOCALAPPDATA\UUSport\secrets\postgres-18-superuser.dpapi" -Raw
    $bytes = ConvertFrom-HexStringCompat $hex.Trim()
    $plain = [System.Security.Cryptography.ProtectedData]::Unprotect($bytes, $null, [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
    return [System.Text.Encoding]::Unicode.GetString($plain)
}

$ok = $false
try {
    $zipFile = Join-Path $work 'backup.zip'
    Unprotect-WithPassphrase -InFile $latest.FullName -OutFile $zipFile -Passphrase $env:UUSPORT_BACKUP_KEY
    Expand-Archive -Path $zipFile -DestinationPath $work
    $dumpFile = Get-ChildItem $work -Filter '*.dump' -Recurse | Select-Object -First 1
    if (-not $dumpFile) { throw "Yedek icinde .dump dosyasi bulunamadi (sifre yanlis olabilir)." }

    $env:PGPASSWORD = Read-SuperuserCredential
    & "$PgBin\createdb.exe" -h 127.0.0.1 -U postgres $dbName
    if ($LASTEXITCODE -ne 0) { throw "createdb basarisiz" }
    & "$PgBin\pg_restore.exe" -h 127.0.0.1 -U postgres -d $dbName --no-owner --no-privileges $dumpFile.FullName
    if ($LASTEXITCODE -ne 0) { throw "pg_restore basarisiz (kod $LASTEXITCODE)" }

    # Temel butunluk: migration gecmisi var mi ve en az bir tenant/kullanici kaydi geri geldi mi.
    # Sorgu bir dosyaya yazilir; PowerShell'den native exe'ye tirnak isaretli argüman gecirmek
    # guvenilmez oldugundan (-c ile once denendi, sessizce bozuldu).
    $sqlFile = Join-Path $work 'check.sql'
    Set-Content -LiteralPath $sqlFile -Encoding ASCII -Value `
        'SELECT (SELECT count(*) FROM "__EFMigrationsHistory") || ''|'' || (SELECT count(*) FROM identity.app_user);'
    $check = & "$PgBin\psql.exe" -h 127.0.0.1 -U postgres -d $dbName -t -A -f $sqlFile
    Remove-Item Env:\PGPASSWORD
    if (-not $check) { throw "Butunluk sorgusu bos dondu (yukaridaki hataya bakin)." }
    $parts = ($check -join '').Trim().Split('|')
    if ([int]$parts[0] -lt 1) { throw "Migration gecmisi bos donuyor; yedek gecerli gorunmuyor." }
    Write-Host "Kontrol: $($parts[0]) migration, $($parts[1]) kullanici satiri geri yuklendi." -ForegroundColor Green
    $ok = $true
} finally {
    if ($env:PGPASSWORD) { Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue }
    $env:PGPASSWORD = Read-SuperuserCredential
    & "$PgBin\dropdb.exe" -h 127.0.0.1 -U postgres --if-exists $dbName 2>$null
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}

if (-not $ok) { Write-Host "GERI YUKLEME PROVASI BASARISIZ" -ForegroundColor Red; exit 1 }
Write-Host "Geri yukleme provasi basarili: $($latest.Name)" -ForegroundColor Green
exit 0
