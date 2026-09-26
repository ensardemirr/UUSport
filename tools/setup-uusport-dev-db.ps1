[CmdletBinding()]
param(
    [string[]]$Databases = @('uusport', 'uusport_test')
)

# UUSport gelistirme veritabani kurulumu (ADR-003, guvenlik standardi bolum 4).
# Tekrar calistirilabilir: var olan rol/veritabani yeniden olusturulmaz, veri silinmez.
# Parolalar DPAPI ile %LOCALAPPDATA%\UUSport\secrets altinda saklanir; ekrana ve komut
# satirina yazilmaz.

$ErrorActionPreference = 'Stop'

$pgBin = 'C:\Program Files\PostgreSQL\18\bin'
$psql = Join-Path $pgBin 'psql.exe'
$secretDir = Join-Path $env:LOCALAPPDATA 'UUSport\secrets'
[System.IO.Directory]::CreateDirectory($secretDir) | Out-Null

function Read-Secret([string]$name) {
    $file = Join-Path $secretDir $name
    if (-not (Test-Path -LiteralPath $file)) { throw "Gizli bilgi dosyasi yok: $name" }
    $secure = Get-Content -LiteralPath $file | ConvertTo-SecureString
    return (New-Object Management.Automation.PSCredential('x', $secure)).GetNetworkCredential().Password
}

function Get-OrCreateSecret([string]$name) {
    $file = Join-Path $secretDir $name
    if (-not (Test-Path -LiteralPath $file)) {
        $bytes = New-Object byte[] 32
        $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
        try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
        $plain = ([Convert]::ToBase64String($bytes).TrimEnd('=') -replace '\+', 'A' -replace '/', 'B')
        ConvertTo-SecureString $plain -AsPlainText -Force | ConvertFrom-SecureString | Set-Content -LiteralPath $file
        $plain = $null
    }
    return Read-Secret $name
}

function Invoke-Psql([string]$database, [string]$sql) {
    $sql | & $psql -X -q -v ON_ERROR_STOP=1 -h 127.0.0.1 -p 5432 -U postgres -d $database -f -
    if ($LASTEXITCODE -ne 0) { throw "psql hata kodu: $LASTEXITCODE" }
}

$env:PGPASSWORD = Read-Secret 'postgres-18-superuser.dpapi'
try {
    $ownerPw = Get-OrCreateSecret 'uusport-db-owner.dpapi'
    $appPw = Get-OrCreateSecret 'uusport-db-app.dpapi'

    Invoke-Psql 'postgres' @"
DO `$`$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'uusport_owner') THEN
    CREATE ROLE uusport_owner LOGIN;
  END IF;
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'uusport_app') THEN
    CREATE ROLE uusport_app LOGIN;
  END IF;
END
`$`$;
ALTER ROLE uusport_owner WITH LOGIN NOSUPERUSER NOCREATEROLE CREATEDB NOBYPASSRLS PASSWORD '$ownerPw';
ALTER ROLE uusport_app WITH LOGIN NOSUPERUSER NOCREATEROLE NOCREATEDB NOBYPASSRLS PASSWORD '$appPw';
"@
    $ownerPw = $null; $appPw = $null

    foreach ($db in $Databases) {
        Invoke-Psql 'postgres' @"
SELECT format('CREATE DATABASE %I OWNER uusport_owner TEMPLATE template0 ENCODING %L LOCALE_PROVIDER icu ICU_LOCALE %L LOCALE %L', '$db', 'UTF8', 'tr-TR', 'C')
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$db')
\gexec
"@
        Invoke-Psql $db @"
REVOKE ALL ON DATABASE $db FROM PUBLIC;
GRANT CONNECT, TEMPORARY ON DATABASE $db TO uusport_app;
GRANT CONNECT ON DATABASE $db TO uusport_owner;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
"@
    }

    # Dogrulama: harmanlama ve roller (ADR-003 bolum 2.7 precheck temeli)
    Invoke-Psql 'postgres' @"
SELECT datname, datlocprovider, datlocale, datcollversion
FROM pg_database WHERE datname LIKE 'uusport%' ORDER BY datname;
SELECT rolname, rolsuper, rolbypassrls, rolcreatedb FROM pg_roles
WHERE rolname IN ('uusport_owner', 'uusport_app') ORDER BY rolname;
"@
    Write-Output 'UUSport gelistirme veritabanlari hazir.'
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}
