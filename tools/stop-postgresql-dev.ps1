$ErrorActionPreference = 'Stop'
$binDir = 'C:\Program Files\PostgreSQL\18\bin'
$dataDir = Join-Path $env:LOCALAPPDATA 'UUSport\PostgreSQL\18\data'
& (Join-Path $binDir 'pg_ctl.exe') stop -D $dataDir -m fast -w -t 60
