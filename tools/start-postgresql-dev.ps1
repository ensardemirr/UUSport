$ErrorActionPreference = 'Stop'
$binDir = 'C:\Program Files\PostgreSQL\18\bin'
$dataDir = Join-Path $env:LOCALAPPDATA 'UUSport\PostgreSQL\18\data'
$logDir = Join-Path $env:LOCALAPPDATA 'UUSport\PostgreSQL\18\logs'
[System.IO.Directory]::CreateDirectory($logDir) | Out-Null
& (Join-Path $binDir 'pg_ctl.exe') start -D $dataDir -l (Join-Path $logDir 'postgresql.log') -w -t 60
