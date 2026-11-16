<#
.SYNOPSIS
  sevk.pro ilk kurulum (YONETICI olarak, surum paketinin kurulum\ klasorunden calistirilir).

  Ne yapar:
    1. Paketi dogrular (SHA256, dosya sayisi).
    2. Ayri bir Windows hesabi acar (sevkpro_<ortam>); parolasi rastgeledir, hicbir yere yazilmaz.
       Veritabani parolalari, oturum anahtarlari ve PostgreSQL verisi bu hesaba kilitlenir.
    3. C:\SevkPro\<ortam> klasorunu kurar; yalniz Administrators, SYSTEM ve servis hesabi erisir.
    4. Servis hesabi altinda: ayri PostgreSQL kumesi, roller, veritabani, migration (kur-ic.ps1).
    5. Iki Windows servisi: PostgreSQL ve uygulama (makine acilinca kendiligindan baslar).
    6. Zamanlanmis gorevler: migration (elle), gecelik yedek 02:00, haftalik geri yukleme provasi.
    7. Saglik kontrolu ve ilk kurulum jetonu.

  Gelistirme ortamina (5432, uusport/uusport_test, Ensaradmin profili) DOKUNMAZ.

.NOTES
  Once prova:  powershell -ExecutionPolicy Bypass -File kur.ps1 -Ortam prova
  Sonra canli: powershell -ExecutionPolicy Bypass -File kur.ps1 -Ortam canli
#>
param([Parameter(Mandatory = $true)][ValidateSet('canli', 'prova')][string]$Ortam)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\ortak.ps1"
Assert-Yonetici
$o = Get-OrtamTanimi $Ortam
$paket = Split-Path -Parent $PSScriptRoot
$manifest = Get-Content -LiteralPath (Join-Path $paket 'manifest.json') -Raw | ConvertFrom-Json
$surum = $manifest.surum

New-Item -ItemType Directory -Force -Path 'C:\SevkPro\kurulum-loglari' | Out-Null
$log = "C:\SevkPro\kurulum-loglari\kur-$Ortam-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
Start-Transcript -LiteralPath $log | Out-Null
$parola = $null
try {
    Write-Output "== sevk.pro $surum -> $Ortam kurulumu =="

    Write-Output '1/7 Paket dogrulaniyor...'
    Test-PaketHash $paket
    foreach ($gerekli in @("$PgBin\initdb.exe", "$PgBin\pg_ctl.exe")) {
        if (-not (Test-Path -LiteralPath $gerekli)) { throw "PostgreSQL 18 bulunamadi: $gerekli" }
    }
    if (-not (& dotnet --list-runtimes | Select-String 'Microsoft.AspNetCore.App 10\.')) { throw 'ASP.NET Core Runtime 10 kurulu degil.' }
    if (Get-LocalUser -Name $o.Kullanici -ErrorAction SilentlyContinue) { throw "Hesap zaten var: $($o.Kullanici). Bu ortam kurulmus; guncelleme icin guncelle.ps1 kullanin." }
    if (Get-Service -Name $o.ApiServis, $o.PgServis -ErrorAction SilentlyContinue) { throw 'Servisler zaten var; once kaldir.ps1 (yalniz prova) veya guncelle.ps1.' }
    foreach ($port in @($o.PgPort, $o.ApiPort)) {
        if (Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue) { throw "Port kullanimda: $port" }
    }

    Write-Output "2/7 Servis hesabi: $($o.Kullanici)"
    $parola = New-RastgeleParola 40
    $guvenli = ConvertTo-SecureString $parola -AsPlainText -Force
    New-LocalUser -Name $o.Kullanici -Password $guvenli -PasswordNeverExpires -UserMayNotChangePassword `
        -Description "sevk.pro $Ortam servis hesabi" | Out-Null   # aciklama en fazla 48 karakter olabilir
    $kullanicilarGrubu = (New-Object Security.Principal.SecurityIdentifier('S-1-5-32-545')).Translate([Security.Principal.NTAccount]).Value.Split('\')[-1]
    Add-LocalGroupMember -Group $kullanicilarGrubu -Member $o.Kullanici
    Grant-OturumHakki "$env:COMPUTERNAME\$($o.Kullanici)" @('SeServiceLogonRight', 'SeBatchLogonRight')

    Write-Output "3/7 Klasorler: $($o.Kok)"
    foreach ($d in @($o.Kok, $o.AppKok, $o.Kurulum, $o.Loglar, $o.Yedekler)) { New-Item -ItemType Directory -Force -Path $d | Out-Null }
    & icacls $o.Kok /inheritance:r /grant:r 'SYSTEM:(OI)(CI)(F)' 'Administrators:(OI)(CI)(F)' "$($o.Kullanici):(OI)(CI)(RX)" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "icacls basarisiz: $($o.Kok)" }
    & icacls $o.Loglar /grant "$($o.Kullanici):(OI)(CI)(M)" | Out-Null
    & icacls $o.Yedekler /grant "$($o.Kullanici):(OI)(CI)(M)" | Out-Null
    $hedefApp = Join-Path $o.AppKok $surum
    Copy-Item -LiteralPath (Join-Path $paket 'app') -Destination $hedefApp -Recurse
    Copy-Item -Path (Join-Path $paket 'kurulum\*') -Destination $o.Kurulum -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $paket 'manifest.json') -Destination (Join-Path $hedefApp 'manifest.json')
    & cmd /c mklink /J "$($o.Current)" "$hedefApp" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'app\current baglantisi olusturulamadi.' }

    Write-Output '4/7 Veritabani (servis hesabi altinda; birkac dakika surebilir)...'
    Register-KullaniciGorevi 'sevkpro-kurulum-gecici' (Join-Path $o.Kurulum 'kur-ic.ps1') "-Ortam $Ortam" $o.Kullanici $parola $null
    try { Wait-Gorev 'sevkpro-kurulum-gecici' 1200 }
    catch {
        $icLog = Get-ChildItem -LiteralPath $o.Loglar -Filter 'kur-ic-*.log' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($icLog) { Get-Content -LiteralPath $icLog.FullName | Select-Object -Last 30 | Write-Output }
        throw
    }
    finally { Unregister-ScheduledTask -TaskName 'sevkpro-kurulum-gecici' -Confirm:$false -ErrorAction SilentlyContinue }

    Write-Output '5/7 Windows servisleri...'
    # Profil klasoru adla degil SID ile bulunur (eski profil kalintisi varsa klasor adi farkli olur).
    $sid = (Get-LocalUser -Name $o.Kullanici).SID.Value
    $profil = (Get-CimInstance Win32_UserProfile | Where-Object { $_.SID -eq $sid } | Select-Object -First 1).LocalPath
    if (-not $profil) { throw 'Servis hesabinin profil klasoru bulunamadi.' }
    $dataDir = Join-Path $profil 'AppData\Local\UUSport\PostgreSQL\18\data'
    & "$PgBin\pg_ctl.exe" register -N $o.PgServis -U ".\$($o.Kullanici)" -P $parola -D $dataDir -S auto -w
    if ($LASTEXITCODE -ne 0) { throw "PostgreSQL servisi kaydedilemedi: $LASTEXITCODE" }
    Start-Service -Name $o.PgServis

    $cred = New-Object Management.Automation.PSCredential(".\$($o.Kullanici)", $guvenli)
    New-Service -Name $o.ApiServis -DisplayName "sevk.pro API ($Ortam)" -StartupType Automatic -Credential $cred `
        -BinaryPathName ('"{0}"' -f (Join-Path $o.Current 'UUSport.Api.exe')) -DependsOn $o.PgServis `
        -Description "sevk.pro uygulamasi ($Ortam), http://127.0.0.1:$($o.ApiPort)" | Out-Null
    Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\$($o.ApiServis)" -Name Environment -Type MultiString -Value @(
        'ASPNETCORE_ENVIRONMENT=Production',
        "Ortam__Ad=$Ortam",
        "Urls=http://127.0.0.1:$($o.ApiPort)",
        "Database__Port=$($o.PgPort)",
        "Database__Database=$($o.Veritabani)")
    & sc.exe failure $o.ApiServis reset= 86400 actions= restart/60000/restart/60000/restart/60000 | Out-Null
    Start-Service -Name $o.ApiServis

    Write-Output '6/7 Zamanlanmis gorevler...'
    Register-KullaniciGorevi $o.GorevMigrate (Join-Path $o.Kurulum 'migrate.ps1') "-Ortam $Ortam" $o.Kullanici $parola $null
    Register-KullaniciGorevi $o.GorevYedek (Join-Path $o.Kurulum 'yedek-al.ps1') "-Ortam $Ortam" $o.Kullanici $parola `
        (New-ScheduledTaskTrigger -Daily -At 2:00AM)
    Register-KullaniciGorevi $o.GorevProva (Join-Path $o.Kurulum 'yedek-al.ps1') "-Ortam $Ortam -Prova" $o.Kullanici $parola `
        (New-ScheduledTaskTrigger -Weekly -DaysOfWeek Sunday -At 3:00AM)

    Write-Output '7/7 Saglik kontrolu...'
    Wait-Saglik $o.ApiPort 90
    Invoke-WebRequest -Uri "http://127.0.0.1:$($o.ApiPort)/api/setup/status" -UseBasicParsing -TimeoutSec 15 | Out-Null
    [pscustomobject]@{ ortam = $Ortam; surum = $surum; gitCommit = $manifest.gitCommit; sonMigration = $manifest.sonMigration; kurulum = (Get-Date -Format 'yyyy-MM-dd HH:mm') } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $o.Kok 'kurulu-surum.json')
    Write-Output "KURULUM TAMAM: $Ortam, surum $surum"
}
catch {
    Write-Output "KURULUM BASARISIZ: $($_.Exception.Message)"
    Write-Output "Log: $log  |  Yarim kalan prova kurulumunu temizlemek icin: kaldir.ps1 -Ortam prova"
    Stop-Transcript | Out-Null
    exit 1
}
finally { $parola = $null; $guvenli = $null; $cred = $null }
Stop-Transcript | Out-Null

# Jeton log dosyasina yazilmasin diye kayit kapandiktan sonra gosterilir.
$jetonDosyasi = Join-Path $profil 'AppData\Local\UUSport\secrets\setup-token.txt'
Write-Host ''
Write-Host "Adres : http://127.0.0.1:$($o.ApiPort)" -ForegroundColor Green
if (Test-Path -LiteralPath $jetonDosyasi) {
    Write-Host "Ilk kurulum jetonu (bir kez gecerli): $((Get-Content -LiteralPath $jetonDosyasi -Raw).Trim())" -ForegroundColor Green
}
Write-Host "Siradaki adim: yedek-parolasi.ps1 -Ortam $Ortam  (yedek parolasini siz belirlersiniz)" -ForegroundColor Yellow
