<#
.SYNOPSIS
  UU Sport CI betigi (guvenlik standardi S-17). Derleme, testler, sema koruma (SchemaGuardTests
  test paketinin bir parcasidir), bagimlilik acik taramasi, gizli bilgi taramasi ve gercek veri
  taramasini tek komutta calistirir. Herhangi bir adim basarisiz olursa tum betik basarisiz olarak
  cikar (exit code != 0); commit-oncesi kancadan ve elle cagrilabilir.

.NOTES
  Calistirmak icin: powershell -File scripts/ci.ps1
  Sadece hizli kontroller icin (pre-commit): powershell -File scripts/ci.ps1 -Fast
#>
param(
    [switch]$Fast
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$failed = @()

function Step($name, [scriptblock]$action) {
    Write-Host "== $name ==" -ForegroundColor Cyan
    try {
        & $action
        if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE) { throw "cikis kodu $LASTEXITCODE" }
        Write-Host "OK: $name" -ForegroundColor Green
    } catch {
        Write-Host "HATA: $name -> $_" -ForegroundColor Red
        $script:failed += $name
    }
}

# 1) Derleme - uyari hataya cevrilir (ADR-002/003 yasakli API listesi de derleme sirasinda kontrol edilir).
#    Debug ile: calisan gelistirme sunucusu (dotnet run, genelde Debug) bin/obj kilitlemez.
#    Yayin (Release) derlemesi S-18 paketleme adiminda, sunucu durdurulmusken yapilir.
Step "Derleme (API)" { dotnet build src/UUSport.Api/UUSport.Api.csproj -c Debug /p:TreatWarningsAsErrors=true /warnaserror }
Step "Derleme (Testler)" { dotnet build tests/UUSport.Tests/UUSport.Tests.csproj -c Debug /p:TreatWarningsAsErrors=true /warnaserror }

# 2) Gizli bilgi taramasi - yuksek guvenli (dusuk yanlis pozitif) imzalar. Bu proje sifreleri DPAPI +
#    ConvertTo-SecureString ile korur (tools/*.ps1); bu API'nin kendisi degil, sabit kodlanmis bir
#    deger sorun olur - o yuzden burada mekanizma degil, somut sizinti imzalari aranir.
Step "Gizli bilgi taramasi" {
    $patterns = @(
        'BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY', 'AKIA[0-9A-Z]{16}', 'xoxb-[0-9A-Za-z-]+'
    )
    $files = git ls-files -- ':!:*.dpapi' ':!:node_modules' ':!:bin' ':!:obj' ':!:*.dll' ':!:*.png' ':!:*.pdf'
    $hit = $false
    foreach ($p in $patterns) {
        $m = $files | ForEach-Object { Select-String -Path $_ -Pattern $p -ErrorAction SilentlyContinue } 2>$null
        if ($m) { $m | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }; $hit = $true }
    }
    if ($hit) { throw "olasi gizli bilgi bulundu (yukarida)" }
}

# 3) Gercek veri taramasi - yasakli klasor/isim izlenen dosyalarda gecmemeli (proje kurali)
Step "Gercek veri taramasi" {
    $files = git ls-files
    $bad = $files | Select-String -Pattern 'uu sport ltd' -SimpleMatch -ErrorAction SilentlyContinue
    if ($bad) { throw "yasakli klasor referansi izlenen dosyalarda: $($bad -join ', ')" }
    # Ogrenci/cari aktarim sablonu disinda repoya eklenmis xlsx/csv veri dosyasi var mi?
    $dataFiles = $files | Where-Object { $_ -match '\.(xlsx|xls|csv)$' -and $_ -notmatch '^docs/reviews/' }
    if ($dataFiles) { Write-Host "  Uyari: incelenmesi gereken veri dosyalari: $($dataFiles -join ', ')" -ForegroundColor Yellow }
}

if ($Fast) {
    if ($failed.Count -gt 0) { Write-Host "`nBASARISIZ adimlar: $($failed -join ', ')" -ForegroundColor Red; exit 1 }
    Write-Host "`nHizli kontroller basarili." -ForegroundColor Green
    exit 0
}

# 4) Sema koruma dahil tum testler (SchemaGuardTests sifirdan kurulan veritabaninda da dener)
Step "Testler (sema koruma dahil)" { dotnet test tests/UUSport.Tests/UUSport.Tests.csproj -c Debug }

# 5) NuGet bagimlilik acik taramasi
Step "NuGet acik taramasi" {
    $out = dotnet list src/UUSport.Api/UUSport.Api.csproj package --vulnerable --include-transitive 2>&1
    Write-Host $out
    if ($out -match 'has the following vulnerable packages') { throw "guvenlik acigi olan NuGet paketi bulundu" }
}

# 6) npm bagimlilik acik taramasi (web)
Step "npm acik taramasi" {
    Push-Location "$root/src/UUSport.Web"
    try {
        npm audit --omit=dev --audit-level=high
    } finally { Pop-Location }
}

if ($failed.Count -gt 0) {
    Write-Host "`nBASARISIZ adimlar: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}
Write-Host "`nTum CI adimlari basarili." -ForegroundColor Green
exit 0
