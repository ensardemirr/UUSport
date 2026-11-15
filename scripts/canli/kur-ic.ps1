<#
.SYNOPSIS
  Kurulumun SERVIS HESABI altinda calisan kismi (kur.ps1 tarafindan zamanlanmis gorevle cagrilir).
  Gizli bilgileri bu hesabin DPAPI'siyle uretir, ayri PostgreSQL kumesini kurar, rolleri ve
  veritabanini olusturur, migration'i uygular ve kumeyi durdurur (sonra Windows servisi baslatir).
  Tekrar calistirilabilir: var olan gizli bilgi, kume, rol ve veritabani yeniden olusturulmaz.
#>
param(
    [Parameter(Mandatory = $true)][string]$Ortam,
    # Yalniz gelistirici denemesi icin: gizli bilgi ve veri koku (varsayilan: hesabin LOCALAPPDATA'si).
    [string]$VeriKoku = (Join-Path $env:LOCALAPPDATA 'UUSport')
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot\ortak.ps1"
$o = Get-OrtamTanimi $Ortam
New-Item -ItemType Directory -Force -Path $o.Loglar | Out-Null
$log = Join-Path $o.Loglar ("kur-ic-{0}.log" -f (Get-Date -Format 'yyyyMMdd-HHmmss'))
Start-Transcript -LiteralPath $log | Out-Null

function Get-OrCreateSecret([string]$dir, [string]$name) {
    $file = Join-Path $dir $name
    if (-not (Test-Path -LiteralPath $file)) {
        ConvertTo-SecureString (New-RastgeleParola 40) -AsPlainText -Force | ConvertFrom-SecureString | Set-Content -LiteralPath $file
    }
    $secure = Get-Content -LiteralPath $file | ConvertTo-SecureString
    return (New-Object Management.Automation.PSCredential('x', $secure)).GetNetworkCredential().Password
}

$kod = 1
try {
    $secretDir = Join-Path $VeriKoku 'secrets'
    $dataDir = Join-Path $VeriKoku 'PostgreSQL\18\data'
    $pgLogDir = Join-Path $VeriKoku 'PostgreSQL\18\logs'
    foreach ($d in @($secretDir, $dataDir, $pgLogDir)) { New-Item -ItemType Directory -Force -Path $d | Out-Null }

    Write-Output "1/5 Gizli bilgiler ($env:USERNAME hesabina kilitli)..."
    $superPw = Get-OrCreateSecret $secretDir 'postgres-18-superuser.dpapi'
    $ownerPw = Get-OrCreateSecret $secretDir 'uusport-db-owner.dpapi'
    $appPw = Get-OrCreateSecret $secretDir 'uusport-db-app.dpapi'

    Write-Output "2/5 PostgreSQL kumesi (port $($o.PgPort))..."
    if (-not (Test-Path -LiteralPath (Join-Path $dataDir 'PG_VERSION'))) {
        if (@(Get-ChildItem -LiteralPath $dataDir -Force).Count -gt 0) { throw "Veri klasoru bos degil ama kume yok: $dataDir" }
        $pwFile = Join-Path $secretDir ("initdb-{0}.tmp" -f [Guid]::NewGuid().ToString('N'))
        try {
            [IO.File]::WriteAllText($pwFile, $superPw, (New-Object Text.UTF8Encoding($false)))
            & "$PgBin\initdb.exe" -D $dataDir --username=postgres --encoding=UTF8 --locale=C --locale-provider=icu `
                --icu-locale=tr-TR --auth-local=scram-sha-256 --auth-host=scram-sha-256 --data-checksums --pwfile=$pwFile
            if ($LASTEXITCODE -ne 0) { throw "initdb basarisiz: $LASTEXITCODE" }
        }
        finally { Remove-Item -LiteralPath $pwFile -Force -ErrorAction SilentlyContinue }
        Add-Content -LiteralPath (Join-Path $dataDir 'postgresql.conf') -Encoding ASCII -Value @(
            '', "# sevk.pro $Ortam kurulumu", "port = $($o.PgPort)", "listen_addresses = '127.0.0.1'")
    }

    & "$PgBin\pg_ctl.exe" status -D $dataDir | Out-Null
    $bizBaslattik = $LASTEXITCODE -ne 0
    if ($bizBaslattik) {
        # Start-Process sart: "& pg_ctl start | Out-Null" sunucu sureci cikti borusunu devraldigi icin
        # hic donmez (15.11.2026 denemesinde yakalandi).
        # -Wait de kullanilmaz (alt surecleri de bekler); yalniz pg_ctl'in kendisi beklenir.
        $baslat = Start-Process -FilePath "$PgBin\pg_ctl.exe" -WindowStyle Hidden -PassThru -ArgumentList @(
            'start', '-D', "`"$dataDir`"", '-l', "`"$(Join-Path $pgLogDir 'kurulum.log')`"", '-w', '-t', '90')
        $null = $baslat.Handle
        $baslat.WaitForExit()
        if ($baslat.ExitCode -ne 0) { throw "PostgreSQL baslatilamadi: $($baslat.ExitCode)" }
    }

    try {
        Write-Output '3/5 Roller ve veritabani...'
        $env:PGPASSWORD = $superPw
        $db = $o.Veritabani
        $sql = @"
DO `$`$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'uusport_owner') THEN CREATE ROLE uusport_owner LOGIN; END IF;
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'uusport_app') THEN CREATE ROLE uusport_app LOGIN; END IF;
END
`$`$;
ALTER ROLE uusport_owner WITH LOGIN NOSUPERUSER NOCREATEROLE NOCREATEDB NOBYPASSRLS PASSWORD '$ownerPw';
ALTER ROLE uusport_app WITH LOGIN NOSUPERUSER NOCREATEROLE NOCREATEDB NOBYPASSRLS PASSWORD '$appPw';
SELECT format('CREATE DATABASE %I OWNER uusport_owner TEMPLATE template0 ENCODING %L LOCALE_PROVIDER icu ICU_LOCALE %L LOCALE %L', '$db', 'UTF8', 'tr-TR', 'C')
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$db')
\gexec
\connect $db
REVOKE ALL ON DATABASE $db FROM PUBLIC;
GRANT CONNECT, TEMPORARY ON DATABASE $db TO uusport_app;
GRANT CONNECT ON DATABASE $db TO uusport_owner;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
"@
        $sql | & "$PgBin\psql.exe" -X -q -v ON_ERROR_STOP=1 -h 127.0.0.1 -p $o.PgPort -U postgres -d postgres -f -
        if ($LASTEXITCODE -ne 0) { throw "psql hata kodu: $LASTEXITCODE" }
        Remove-Item Env:\PGPASSWORD

        Write-Output '4/5 Migration...'
        # Deneme kosusunda (-VeriKoku) uygulama gizli bilgiyi hesabin gercek profilinde arar; orada yoktur.
        if ($PSBoundParameters.ContainsKey('VeriKoku')) { $env:UUSPORT_SECRET_UUSPORT_DB_OWNER_DPAPI = $ownerPw }
        & "$PSScriptRoot\migrate.ps1" -Ortam $Ortam -Ic
        if ($LASTEXITCODE -ne 0) { throw "Migration basarisiz: $LASTEXITCODE" }
    }
    finally {
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
        if ($bizBaslattik) {
            Write-Output '5/5 Kume durduruluyor (Windows servisi baslatacak)...'
            & "$PgBin\pg_ctl.exe" stop -D $dataDir -m fast -w -t 90 | Out-Null
        }
    }
    Write-Output 'kur-ic tamam.'
    $kod = 0
}
catch {
    Write-Output "HATA: $($_.Exception.Message)"
}
finally {
    $superPw = $null; $ownerPw = $null; $appPw = $null
    Stop-Transcript | Out-Null
}
exit $kod
