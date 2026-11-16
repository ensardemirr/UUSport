# Canli/prova kurulum betiklerinin ortak yardimcilari. Dot-source edilir: . "$PSScriptRoot\ortak.ps1"
# Betikler ASCII yazilir (Windows PowerShell 5.1 BOM'suz UTF-8'i yanlis okur).

$script:PgBin = 'C:\Program Files\PostgreSQL\18\bin'

function Get-OrtamTanimi([string]$Ortam) {
    switch ($Ortam) {
        'canli' { $pgPort = 5433; $apiPort = 5090 }
        'prova' { $pgPort = 5434; $apiPort = 5091 }
        default { throw "Bilinmeyen ortam: $Ortam (canli veya prova)" }
    }
    $kok = "C:\SevkPro\$Ortam"
    [pscustomobject]@{
        Ortam        = $Ortam
        Kok          = $kok
        Kullanici    = "sevkpro_$Ortam"
        PgPort       = $pgPort
        ApiPort      = $apiPort
        PgServis     = "sevkpro-$Ortam-pg"
        ApiServis    = "sevkpro-$Ortam-api"
        GorevMigrate = "sevkpro-$Ortam-migrate"
        GorevYedek   = "sevkpro-$Ortam-yedek"
        GorevProva   = "sevkpro-$Ortam-geri-yukleme-provasi"
        AppKok       = Join-Path $kok 'app'
        Current      = Join-Path $kok 'app\current'
        Kurulum      = Join-Path $kok 'kurulum'
        Loglar       = Join-Path $kok 'logs'
        Yedekler     = Join-Path $kok 'yedekler'
        YedekAnahtari = Join-Path $kok 'yedek-anahtari.bin'
        Veritabani   = 'uusport'
    }
}

function Assert-Yonetici {
    $p = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()
    if (-not $p.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Bu betik yonetici olarak calistirilmalidir (PowerShell > Yonetici olarak calistir).'
    }
}

function Test-PaketHash([string]$PaketDir) {
    $listFile = Join-Path $PaketDir 'checksums.sha256'
    if (-not (Test-Path -LiteralPath $listFile)) { throw "checksums.sha256 yok: $PaketDir" }
    $lines = @(Get-Content -LiteralPath $listFile | Where-Object { $_.Trim() })
    if ($lines.Count -eq 0) { throw 'Hash listesi bos; paket kabul edilmez.' }
    $ok = 0
    foreach ($line in $lines) {
        $hash, $rel = $line -split '  ', 2
        $file = Join-Path $PaketDir ($rel.Replace('/', '\'))
        if (-not (Test-Path -LiteralPath $file)) { throw "Pakette eksik dosya: $rel" }
        if ((Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant() -ne $hash) { throw "Hash uyusmuyor: $rel" }
        $ok++
    }
    $fazla = @(Get-ChildItem -LiteralPath $PaketDir -Recurse -File |
        Where-Object { $_.Name -notin @('checksums.sha256', 'manifest.json') }).Count
    if ($fazla -ne $ok) { throw "Pakette listede olmayan dosya var (liste $ok, klasor $fazla)." }
    Write-Output "Paket dogrulandi: $ok dosya."
}

function Wait-Gorev([string]$Ad, [int]$ZamanAsimiSn = 900) {
    $oncekiKosu = (Get-ScheduledTaskInfo -TaskName $Ad).LastRunTime
    Start-ScheduledTask -TaskName $Ad
    # Gorev baslamadan sonucu okunursa eski/bos sonuc "basarisiz" sanilir; once baslamasi beklenir.
    $baslama = (Get-Date).AddSeconds(60)
    while ((Get-ScheduledTask -TaskName $Ad).State -ne 'Running' -and (Get-ScheduledTaskInfo -TaskName $Ad).LastRunTime -eq $oncekiKosu) {
        if ((Get-Date) -gt $baslama) { throw "Gorev baslamadi: $Ad" }
        Start-Sleep -Seconds 1
    }
    $bitis = (Get-Date).AddSeconds($ZamanAsimiSn)
    while ((Get-ScheduledTask -TaskName $Ad).State -eq 'Running') {
        if ((Get-Date) -gt $bitis) { throw "Gorev zaman asimina ugradi: $Ad" }
        Start-Sleep -Seconds 2
    }
    $sonuc = (Get-ScheduledTaskInfo -TaskName $Ad).LastTaskResult
    if ($sonuc -ne 0) { throw "Gorev basarisiz: $Ad (kod $sonuc). Log: C:\SevkPro\...\logs" }
}

function Register-KullaniciGorevi([string]$Ad, [string]$Betik, [string]$Arguman, [string]$Kullanici, [string]$Parola, $Tetikleyici) {
    $action = New-ScheduledTaskAction -Execute 'powershell.exe' `
        -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$Betik`" $Arguman"
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
    $p = @{ TaskName = $Ad; Action = $action; Settings = $settings; User = "$env:COMPUTERNAME\$Kullanici"; Password = $Parola; RunLevel = 'Limited'; Force = $true }
    if ($Tetikleyici) { $p.Trigger = $Tetikleyici }
    Register-ScheduledTask @p | Out-Null
}

function Wait-Saglik([int]$Port, [int]$Saniye = 60) {
    $bitis = (Get-Date).AddSeconds($Saniye)
    while ((Get-Date) -lt $bitis) {
        try {
            $r = Invoke-WebRequest -Uri "http://127.0.0.1:$Port/api/health" -UseBasicParsing -TimeoutSec 5
            if ($r.StatusCode -eq 200) { return }
        }
        catch { Start-Sleep -Seconds 2 }
    }
    throw "Uygulama $Port portunda yanit vermedi (/api/health)."
}

function Grant-OturumHakki([string]$Hesap, [string[]]$Haklar) {
    # "Hizmet olarak oturum ac" ve "toplu is olarak oturum ac" haklari; servis ve zamanlanmis gorev icin sart.
    if (-not ('SevkPro.Lsa' -as [type])) {
        Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
namespace SevkPro {
  public static class Lsa {
    [StructLayout(LayoutKind.Sequential)] struct LSA_UNICODE_STRING { public ushort Length; public ushort MaximumLength; public IntPtr Buffer; }
    [StructLayout(LayoutKind.Sequential)] struct LSA_OBJECT_ATTRIBUTES { public int Length; public IntPtr RootDirectory; public IntPtr ObjectName; public uint Attributes; public IntPtr SecurityDescriptor; public IntPtr SecurityQualityOfService; }
    [DllImport("advapi32.dll", SetLastError = true)] static extern uint LsaOpenPolicy(IntPtr systemName, ref LSA_OBJECT_ATTRIBUTES attrs, uint access, out IntPtr handle);
    [DllImport("advapi32.dll", SetLastError = true)] static extern uint LsaAddAccountRights(IntPtr handle, byte[] sid, LSA_UNICODE_STRING[] rights, uint count);
    [DllImport("advapi32.dll")] static extern uint LsaClose(IntPtr handle);
    [DllImport("advapi32.dll")] static extern int LsaNtStatusToWinError(uint status);
    public static void Grant(string account, string right) {
      var sid = (SecurityIdentifier)new NTAccount(account).Translate(typeof(SecurityIdentifier));
      var sidBytes = new byte[sid.BinaryLength]; sid.GetBinaryForm(sidBytes, 0);
      var attrs = new LSA_OBJECT_ATTRIBUTES(); attrs.Length = Marshal.SizeOf(typeof(LSA_OBJECT_ATTRIBUTES));
      IntPtr policy;
      uint st = LsaOpenPolicy(IntPtr.Zero, ref attrs, 0x00000800 | 0x00000010, out policy); // LOOKUP_NAMES | CREATE_ACCOUNT
      if (st != 0) throw new System.ComponentModel.Win32Exception(LsaNtStatusToWinError(st));
      try {
        var r = new LSA_UNICODE_STRING[1];
        r[0].Buffer = Marshal.StringToHGlobalUni(right);
        r[0].Length = (ushort)(right.Length * 2); r[0].MaximumLength = (ushort)((right.Length + 1) * 2);
        try {
          st = LsaAddAccountRights(policy, sidBytes, r, 1);
          if (st != 0) throw new System.ComponentModel.Win32Exception(LsaNtStatusToWinError(st));
        } finally { Marshal.FreeHGlobal(r[0].Buffer); }
      } finally { LsaClose(policy); }
    }
  }
}
'@
    }
    foreach ($hak in $Haklar) { [SevkPro.Lsa]::Grant($Hesap, $hak) }
}

function New-RastgeleParola([int]$Uzunluk = 40) {
    $kume = 'abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789'
    $bytes = New-Object byte[] $Uzunluk
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    $govde = -join ($bytes | ForEach-Object { $kume[$_ % $kume.Length] })
    return "Aa1!$govde"   # yerel parola karmasiklik kuralini her zaman karsilar
}
