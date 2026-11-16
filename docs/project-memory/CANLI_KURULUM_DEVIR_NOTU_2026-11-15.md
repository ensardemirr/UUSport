# Canlı Kurulum - Devir Notu (15 Kasım 2026)

Sonraki oturum (Claude Code veya Codex) önce `AGENTS.md`, ana hafıza bölüm 0, ADR-019 ve bu notu okur,
sonra **bölüm 3'ten** devam eder. Kullanıcı onay sormadan devam edilmesini istedi (ADR-019 adımları için);
gerçek veri ve canlı veritabanına ajan dokunmaz.

## 1. Kararlar (bu oturum)

- `[KARAR]` ADR-019: proje adı sevk.pro, arka uç C#, 37 adımlık liste onaylı.
- `[KARAR]` İlk canlı bu makineye (`BAKON-DEV01`) kurulur; alan adı sevk.pro alt alan adı; yedek şimdilik
  aynı makinede, kullanıcı elle dışarı alır. Windows lisansını kullanıcı halledecek (Evaluation, ~5 Şubat 2027).
- `[DOGRU]` Ajan oturumu yönetici yetkisiyle çalışmıyor: Windows hesabı, servis ve zamanlanmış görev
  **oluşturamaz**. Kurulum komutlarını kullanıcı yönetici PowerShell'de çalıştırır.

## 2. Yapılanlar - `[DOGRU]`

- Adım 1, 2, 5 bitti: ADR-019, ana hafıza 3.11, `C:\Sevk.Pro` git'e alındı, dal `master`'a birleşti ve itildi.
- Kod: `Program.cs` Windows servisi olarak çalışabiliyor (`AddWindowsService`, içerik kökü sabit);
  sürüm 0.2.0. `backup.ps1` ve `restore-test.ps1` `-Port` parametresi aldı.
- `scripts/release.ps1`: sürüm paketi (app + kurulum + manifest + SHA256). Çalıştırıldı: 83/83 test, 51 dosya.
- `scripts/canli/`: `kur.ps1` (yönetici; hesap, klasör, servisler, görevler), `kur-ic.ps1` (servis hesabı
  altında; gizli bilgiler, ayrı PostgreSQL kümesi, roller, migration), `migrate.ps1`, `yedek-al.ps1`,
  `yedek-parolasi.ps1`, `guncelle.ps1`, `kaldir.ps1` (yalnız prova), `ortak.ps1`.
- Tasarım: ortam başına ayrı Windows hesabı (`sevkpro_canli` / `sevkpro_prova`), parolası rastgele ve hiçbir
  yere yazılmaz; veritabanı parolaları, oturum anahtarları ve PostgreSQL verisi o hesabın profiline kilitli.
  Portlar: canlı PostgreSQL 5433 / uygulama 5090; prova 5434 / 5091. Klasör `C:\SevkPro\<ortam>`.
- Yetkisiz denenenler (geçici klasörde, prova portunda): `kur-ic.ps1` uçtan uca (sıfır küme, roller,
  7 migration); paketteki uygulama açıldı (`/api/health` ok, `needsSetup:true`, oturumsuz uç 401);
  yedek + geri yükleme provası `-Port` ile başarılı.
- Bulunan ve düzeltilen hata: `& pg_ctl start | Out-Null` ve `Start-Process -Wait` hiç dönmüyor (sunucu
  süreci boruyu/iş nesnesini devralıyor). Doğrusu: `Start-Process -PassThru` + `WaitForExit()`.
  Aynı tuzak Bash aracında da var: `start-postgresql-dev.ps1`'i boruya bağlama.

## 3. Kalanlar (sırayla)

1. **DENENMEDİ:** `kur.ps1`'in yönetici gerektiren kısmı (hesap açma, LSA oturum hakları, `pg_ctl register`,
   `New-Service`, zamanlanmış görevler), `guncelle.ps1`, `kaldir.ps1`, `yedek-parolasi.ps1`. İlk gerçek
   deneme kullanıcının **prova** kurulumudur; hata çıkarsa log `C:\SevkPro\kurulum-loglari\` altındadır.
2. ~~Paketi temiz ağaçtan yeniden üret.~~ Tamamlandı (16.11): `deployment\releases\0.2.1`, commit `6173e63`,
   83 test yeşil. `0.2.0` klasörü hatalı, kullanılmaz.
3. Kullanıcıya verilecek komutlar (yönetici PowerShell, sırayla):
   `cd C:\UUSport\deployment\releases\0.2.1\kurulum`
   `powershell -ExecutionPolicy Bypass -File .\kur.ps1 -Ortam prova`
   prova sorunsuzsa: `.\kaldir.ps1 -Ortam prova`, sonra `.\kur.ps1 -Ortam canli`, sonra
   `.\yedek-parolasi.ps1 -Ortam canli`.
4. ~~Belgeler.~~ Tamamlandı (16.11): operasyon prosedürü 4.1A ve 6.1, güvenlik standardı 10.1,
   ana hafıza 5.8, 11 ve değişiklik geçmişi (sürüm 3.12).
5. Adım 8 (HTTPS): canlı şimdilik yalnız `127.0.0.1:5090` (bu makinede RDP içinden). Başka bilgisayardan
   erişim için kullanıcı GoDaddy'de alt alan adı kaydı açmalı; sertifika ve 443 ayrı iş.
6. Adım 10 (M2) ve Aşama 3 tasarım oturumları (ADR-019 bölüm 5).

## 3.1 Kullanıcının prova denemeleri

- 15.11.2026 22:33, sürüm 0.2.0: `kur.ps1 -Ortam prova` adım 2'de durdu: `New-LocalUser -Description` en fazla
  48 karakter kabul ediyor. Hesap oluşmadı, kalıntı yok. Düzeltildi; **sürüm 0.2.1** paketi üretildi.
  Komutlarda klasör artık `deployment\releases\0.2.1\kurulum`.

## 4. Dikkat

- `[RISK]` Servis hesabının parolası yönetici tarafından sıfırlanırsa o hesaba kilitli gizli bilgiler
  okunamaz olur; kurtarma yolu yeni kurulum + yedekten geri yükleme + `reset-mfa`.
- `[RISK]` Yedekteki Data Protection anahtarları makineye bağlı sarılı; başka makineye geri yüklemede
  iki adımlı doğrulama sıfırlanmak zorunda kalabilir. Ayrıca incelenmeli.
- Geliştirme PostgreSQL kümesi (5432) elle başlatılır; kapalıysa testler "bağlantı reddedildi" ile düşer.
