[CmdletBinding()]
param()
# UU Sport DENEME ortami: uusport_ui veritabanini sifirdan kurar, API'yi http://127.0.0.1:5081 adresinde
# acar ve tools/demo/seed_demo.py ile uc sirketli SENTETIK veriyi yukler. Gercek veri ve gelistirme
# veritabani (uusport, 5080) etkilenmez. Giris kodu: python tools\demo\totp.py (Get-Content $env:LOCALAPPDATA\UUSport\demo\patron.key)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$app = Join-Path $root 'src\UUSport.Api\bin\Release\net10.0\UUSport.Api.dll'
$c = Get-NetTCPConnection -LocalPort 5081 -State Listen -ErrorAction SilentlyContinue
if ($c) { Stop-Process -Id $c.OwningProcess -Force; Start-Sleep 1 }
$env:PGPASSWORD = (New-Object Management.Automation.PSCredential('x', (Get-Content "$env:LOCALAPPDATA\UUSport\secrets\postgres-18-superuser.dpapi" | ConvertTo-SecureString))).GetNetworkCredential().Password
try { & 'C:\Program Files\PostgreSQL\18\bin\psql.exe' -X -q -h 127.0.0.1 -U postgres -d postgres -c 'DROP DATABASE IF EXISTS uusport_ui WITH (FORCE);' }
finally { Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue }
& (Join-Path $root 'tools\setup-uusport-dev-db.ps1') -Databases uusport_ui | Out-Null
& dotnet $app migrate --database=uusport_ui
if ($LASTEXITCODE -ne 0) { throw 'Migration basarisiz' }
$env:Database__Database = 'uusport_ui'; $env:Kestrel__Endpoints__Http__Url = 'http://127.0.0.1:5081'
Start-Process -FilePath dotnet -ArgumentList "`"$app`"" -WorkingDirectory (Join-Path $root 'src\UUSport.Api') -WindowStyle Hidden
Remove-Item Env:\Database__Database, Env:\Kestrel__Endpoints__Http__Url
for ($i = 0; $i -lt 60; $i++) { try { if ((Invoke-WebRequest -UseBasicParsing http://127.0.0.1:5081/api/health -TimeoutSec 2).StatusCode -eq 200) { break } } catch {}; Start-Sleep -Milliseconds 500 }
$env:PYTHONIOENCODING = 'utf-8'
python (Join-Path $PSScriptRoot 'seed_demo.py')
if ($LASTEXITCODE -ne 0) { throw 'Sentetik veri yuklenemedi' }
Write-Output 'Deneme ortami hazir: http://127.0.0.1:5081 (kullanici: patron)'
