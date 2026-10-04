<#
.SYNOPSIS
  scripts/git-hooks icindeki kancalari .git/hooks altina kurar (S-17). .git/hooks izlenmedigi
  icin bu adim her klonda bir kez elle calistirilmalidir.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'scripts/git-hooks/pre-commit'
$target = Join-Path $root '.git/hooks/pre-commit'

if (-not (Test-Path (Join-Path $root '.git'))) { throw "Bu bir git deposu degil: $root" }
Copy-Item $source $target -Force
Write-Host "Kuruldu: $target (her commit oncesi hizli kontrolleri calistirir)" -ForegroundColor Green
