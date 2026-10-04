# UU Sport Gelistirme, Veri ve Canliya Alma Proseduru

**Durum:** `[KARAR]` Temel calisma modeli; sunucuya ozel degerler daha sonra doldurulacak  
**Tarih:** 23 Eylul 2026  
**Kapsam:** Gelistirme, kaynak dosya kabulü, test, surumleme, migration, canliya alma,
yedek, geri donus ve kanit kaydi

Bu belge UU Sport uygulamasinin gelistirme ortamini barindiran kiralik RDP'den test ve canli ortama
tekrarlanabilir bicimde tasinmasi icin temel prosedurdur. Sunucunun normal Windows
bilgisayar, tek RDP/VDS veya ayri uygulama ve PostgreSQL sunuculari olmasi temel
akisi degistirmez.

## 1. Degismez sinirlar

- Gelistirme ve test yalniz maskeli veya sentetik veriyle yapilir.
- Gercek sporcu, cocuk, veli, odeme veya saglik verisi Git'e, test veritabanina,
  proje belgelerine veya yapay zeka araclarina konmaz.
- Masaustu, web ve mobil istemci PostgreSQL'e dogrudan baglanmaz. Trafik merkezi
  HTTPS API uzerinden gider.
- Kaynak kod, migration ve ortam kurulumu Git ve surumlu paketlerle tasinir.
  Calisan proje klasoru elle kopyalanarak ortam kurulmaz.
- Bir release paketinin diskte bulunmasi, onun canlida kuruldugunu kanitlamaz.
  Canli surum, migration seviyesi, dosya hashleri ve smoke test ayri dogrulanir.
- Canliya yazma, veri aktarimi, migration, geri yukleme ve rollback icin once
  hedef ortam ve plan acikca belirlenir.

## 2. Excel, PDF ve resim kabul akisi

Kullanici gelistirme boyunca Excel, PDF veya resim verebilir. Her dosya su sirayla
islenir:

1. Orijinal dosya salt okunur kabul edilir; uzerine yazilmaz.
2. Dosya adi, boyutu, tarih bilgisi ve SHA256 ozeti bir kaynak kaydina yazilir.
3. Excel icin sayfa, kolon, formul, hata ve veri turu haritasi cikarilir.
4. PDF ve resim icin OCR veya goruntu okuma sonucu `teyit bekleyen` sayilir.
5. Kisisel alanlar tanimlanir ve maskelenmis/sentetik calisma kopyasi uretilir.
6. Alan esleme tablosu ile zorunlu alan, veri turu ve is kurali kontrolleri
   belgelenir.
7. Veri once staging alanina alinip hata/uyari raporu uretilir.
8. Mutabakat sonucu onaylanmadan ana tablolara aktarim yapilmaz.

Gercek veya hassas kaynaklar repo disinda tutulur. Onerilen yerel kabul alani:

`%LOCALAPPDATA%\UUSport\incoming\<batch_id>`

Repo icindeki `docs/source` yalniz kisisel veri icermeyen sema, alan esleme
belgesi ve sentetik ornek barindirabilir.

### 2.1 Aktarim partisi kaydi

Her aktarimda en az su bilgiler tutulur:

- `batch_id`
- kaynak turu ve dosya SHA256 ozeti
- dosyanin alinma zamani
- aktarici surumu
- okunan, kabul edilen, reddedilen ve uyarili satir sayisi
- baslatan ve onaylayan kullanici
- dry-run ve canli uygulama sonucu
- geri alma veya ters kayit baglantisi

Ayni dosya ve ayni satir ikinci kez aktarildiginda mukerrer olusturmayacak bir
idempotency anahtari kullanilir.

## 3. Ortamlar

| Ortam | Veri | Amac |
|---|---|---|
| DEV | Sentetik/maskeli | Gelistirme ve otomatik test |
| TEST | Sentetik/maskeli | Entegrasyon ve migration provasi |
| UAT | Maskeli, canliyi temsil eden | Kullanici kabul testi |
| PROD | Gercek veri | Canli kullanim |

Ortamlarin API adresi, veritabani, dosya deposu, gizli bilgileri ve kullanici
hesaplari ayridir. UAT veya TEST, PROD veritabanina baglanamaz.

## 4. Sunucu topolojisi

`APP01` ve `DB01` fiziksel makine adi degil, mantiksal roldur.

### 4.1 Ilk canli: mevcut normal Windows bilgisayar

- Gelistirme icin ayrica bir RDP kiralanacak. Bu RDP canli hedef degildir.
- Ilk canli kurulum kullanicinin mevcut fiziksel Windows bilgisayarindadir.
  Donanim uygunsa APP01 ve DB01 rolleri yerel sanal makinede birlikte baslar.
- Gelistirme RDP'sinden canliya yalniz surumlu paket, dogrulanmis migration ve
  ortamdan bagimsiz kurulum adimlari aktarilir; canli veri gelistirmeye alinmaz.
- Daha sonra canli uygulama ve PostgreSQL, canli icin yeni kiralanacak ayri
  RDP sunucusuna tasinir. Gelistirme RDP'si bu tasimanin hedefi olamaz.
- Tasima oncesinde yeni canli RDP'de kurulum ve bos/veri tasima provasi yapilir;
  kaynak canli veritabani yedeklenir, yazmalar durdurulur, hedefe geri yukleme
  ve mutabakat yapilir, ardindan uygulama trafigi yeni hedefe yonlendirilir.
  Eski canli ortam, yeni hedef dogrulanana kadar kontrollu tutulur.

- `APP01` ve `DB01` ayni Windows makinede bulunabilir.
- API, PostgreSQL, dosya alani ve yedek gorevleri ayri servis hesabi ve klasorlerle
  sinirlanir.
- PostgreSQL yine yalniz localhost veya ozel agdan dinler; 5432 internete acilmaz.
- Bu yerlesim baslangic/pilot icin uygundur; kaynak kullanimi ve yedekler izlenir.

### 4.2 Iki RDP/VDS

- `APP01`: API, web, masaustu dagitim servisi ve zamanlanmis uygulama isleri.
- `DB01`: PostgreSQL, WAL arsivi ve yerel kisa sureli yedek.
- APP01 ile DB01 ozel ag veya VPN uzerinden haberlesir.
- Disariya yalniz HTTPS 443 acilir. DB01 icin halka acik 5432 yasaktir.
- Yedegin degistirilemez kopyasi iki sunucunun disindaki farkli hesapta tutulur.

### 4.3 Normal ofis bilgisayari

Canli sunucu olarak secilirse en az UPS, BitLocker, sabit ag yapisi, otomatik servis
baslatma, uzaktan izleme, ayrik yonetici hesabi ve harici degistirilemez yedek
zorunludur. Web/mobil erisimi icin veritabanina port yonlendirme yapilmaz; trafik
HTTPS API veya guvenli tunel uzerinden gider.

`[ACIK]` Kesin topoloji, isletim sistemi surumu, IP/DNS, diskler, saglayici,
lisanslar ve kapasite canli ortami secildiginde ortam envanterine yazilacak.

## 5. Gizli bilgiler ve erisim

- Parola, API anahtari, sertifika ozel anahtari ve canli baglanti metni Git'e veya
  ana hafizaya yazilmaz.
- Gelistirmede Windows DPAPI veya yerel secret store; canlida sirket kontrolundeki
  secret store kullanilir.
- Servis hesaplari en az yetkiyle calisir.
- Yonetim erisimi VPN veya RD Gateway ve MFA arkasindadir.
- Canli erisim bilgileri verildiginde once salt okunur ortam envanteri cikarilir.
  Degisiklik yetkisi ayrica ve islem bazinda kullanilir.
- Hangi kullanicinin hangi islemi yaptigi append-only denetim kaydinda tutulur.

## 6. Release ve paket standardi

Her canli surum yeni ve degistirilemez bir klasorde hazirlanir. Mevcut release
klasorunun ustune ikinci kez publish yapilmaz.

Onerilen paket sekli:

```text
deployment/releases/<surum>/
|-- manifest.json
|-- checksums.sha256
|-- app/                 # API/web/masaustu yayin ciktisi
|-- database/
|   |-- 01_precheck/
|   |-- 02_apply/
|   |-- 03_verify/
|   `-- 04_recovery/
|-- runbook.md
`-- third-party-notices/
```

`manifest.json` en az sunlari icerir:

- uygulama surumu ve Git commit'i
- olusturma tarihi
- hedef ortam
- uygulanan migration araligi
- gereken runtime ve servisler
- dosya sayisi ve hash manifesti
- hazirlayan ve onaylayan

Hash dogrulamasi sifir dosya kontrol ettiyse basarili sayilmaz. Beklenen ve
dogrulanan dosya sayilari esit olmadan paket kabul edilmez.

Runtime paketi config, secrets, logs, uploads ve canli rapor klasorlerini icermez.
Kopyalama islemi hedefteki fazla dosyalari otomatik silen ayna modu kullanmaz.

### 6.1 CI/on kontrol betigi (M1, S-17, 27 Eylul 2026 - uygulandi)

`scripts/ci.ps1`: derleme (uyari=hata), tum testler (sema koruma dahil, sifirdan
veritabaninda da), gizli bilgi taramasi, gercek veri/yasakli klasor taramasi, NuGet ve
npm bagimlilik acik taramasi. `-Fast` bayragiyla yalniz derleme + taramalar calisir
(commit-oncesi kanca icin). Kanca kurulumu: `scripts/install-git-hooks.ps1` (elle, bir
kez calistirilir; `.git/hooks` izlenmez). Yukaridaki paket/manifest standardini
uygulayan bir `scripts/release.ps1` ve Windows servis kurulum betikleri (S-18) henuz
yazilmadi; bu bolumdeki standart hala hedeftir, otomasyonu degil.

## 7. Canliya alma kapilari

### Kapi A - Gelistirme tamamlama

- Kod review ve gerekli testler basarili.
- Maskeli/sentetik veriyle ilgili is akisi calisiyor.
- Yeni ekran/aksiyon icin rol ve yetki kontrolleri API seviyesinde test edildi.
- Migration sifirdan kurulumda ve onceki surumden geciste test edildi.

### Kapi B - Paket dogrulama

- Release yeni bos klasorde uretildi.
- Manifest ve SHA256 dogrulamasi gecti.
- Paket kendi klasorunden en az iki temiz baslangicla calistirildi.
- Ucuncu taraf lisans bildirimleri paket icinde.
- Gelistirme belgeleri, gercek veri, secrets ve yedekler pakette yok.

### Kapi C - Canli oncesi

- Hedef makine, disk boslugu, servisler ve mevcut surum kaydedildi.
- Aktif kullanicilar ve calisan islemler kontrol edildi.
- PostgreSQL tam yedegi ve PITR/WAL durumu dogrulandi.
- Yedegin farkli bir yere kopyasi ve geri yukleme kaniti mevcut.
- Degisiklik penceresi, sorumlu kisi ve geri donus siniri belirlendi.
- `01_precheck` yalniz okuma kontrolleri basarili.

### Kapi D - Migration

- Migration yalniz hedef veritabaninda calisir; yanlis ortam korumasi bulunur.
- Migration kaydi ve checksum'u merkezi migration tablosuna yazilir.
- Uygun DDL islemleri transaction icinde calisir.
- `02_apply` sonrasinda `03_verify` nesne, kolon, constraint, index ve veri
  kontrollerini yapar.
- Dogrulama gecmeden uygulama trafigi acilmaz.

### Kapi E - Uygulama yayinlama

- API/web servisi kontrollu bicimde durdurulur veya trafik disina alinir.
- Canli klasor yerinde degistirilmez; yeni surum klasoru etkinlestirilir.
- Ortam ayarlari ve secrets release paketinden ayri kalir.
- Servis hesabi izinleri, sertifika ve dosya erisimi kontrol edilir.
- Servis baslatilir ve health endpoint sonucu kaydedilir.

### Kapi F - Canli kabul

En az su smoke testler yapilir:

1. Giris ve rol/yetki
2. Cari arama ve Toplam / Belgeli-Resmi / Muhasebe Bekleyen gorunumleri
3. Stok cikisi ve stok azalmasi
4. Hizmet satisi
5. Kullanici bazli iskonto siniri
6. Tahsilat alma
7. Resmi islem secildiginde fatura taslagi olusmasi
8. Resmi secenek kapaliyken Muhasebe Bekleyen listesine dusme
9. Denetim kaydi
10. Yedek ve izleme alarmlarinin calismasi

Uygulama surumu, Git commit'i, migration seviyesi, dosya hashleri ve smoke test
sonucu ayri ayri kaydedilmeden canliya alma tamamlandi sayilmaz.

## 8. Migration kurali

- Veritabani degisikligi yalniz surumlu migration ile yapilir.
- Canlida elle yapilan her zorunlu SQL ayni calismada migration kaynagina eklenir.
- Uygulanmis migration dosyasi sonradan degistirilmez; yeni duzeltme migration'i
  yazilir.
- Veri kaybi riski olan degisiklikler genislet-gecir-daralt yaklasimiyla yapilir:
  once yeni alan/nesne eklenir, veri tasinir ve dogrulanir, eski alan sonraki
  surumde kaldirilir.
- Buyuk veri donusumu parcalara ayrilir; kilit suresi, disk ve WAL buyumesi onceden
  olculur.
- Migration ile uygulama surumu uyumluluk matrisi manifestte bulunur.

## 9. Geri donus ve kurtarma

Uygulama geri donusu ile veritabani geri donusu ayri islemdir.

- Uygulama: onceki degistirilemez release yeniden etkinlestirilir.
- Veritabani: tercih edilen yol ileri yonlu duzeltme migration'idir.
- Yedekten donus, yalniz veri kaybi kesim zamani ve etkilenen islemler acikca
  kabul edildikten sonra yapilir.
- Basarisiz canli durumun da yedegi alinmadan eski yedek uzerine donulmez.
- Rollback sonrasi da ayni smoke testler ve surum/migration kaniti kaydedilir.

## 10. Yedekleme

- PostgreSQL WAL arsivi ve PITR
- Gunluk tam yedek
- Uygulama tarafinda belge/uploads yedegi
- Farkli hesap ve tercihen farkli saglayicida degistirilemez kopya
- Hedef RPO 15 dakika, RTO 4 saat
- Ayda en az bir gercek geri yukleme testi
- Yedek basarili mesaji tek basina yeterli degildir; acilan veritabani ve temel
  mutabakat sorgulari ile geri yukleme kanitlanir.

### 10.1 Uygulama (M1, S-10, 27 Eylul 2026)

- `scripts/backup.ps1`: `pg_dump` (custom format) + Data Protection anahtar klasoru,
  parola tabanli AES-256 ile sifrelenir (`-OffsiteDir` verilirse oraya da kopyalar).
  Sifreleme DPAPI DEGILDIR: DPAPI bu makineye baglidir, sunucu kaybedilirse DPAPI ile
  sifrelenmis yedek de kurtarilamaz olur. Parola `UUSPORT_BACKUP_KEY` ortam degiskeninden
  okunur ve **sunucuda saklanmaz** (sifre yoneticisinde tutulur).
  - `pg_dump`/`pg_restore`/`createdb`/`dropdb` `postgres` superuser hesabiyla calisir
    (`%LOCALAPPDATA%\UUSport\secrets\postgres-18-superuser.dpapi`): `uusport_owner`
    NOSUPERUSER/NOBYPASSRLS'tir ve tablolar FORCE ROW LEVEL SECURITY tasir, yani sahibi
    bile `app.tenant_id` olmadan satir goremez - tam yedek icin RLS'i asan tek yerel
    hesap budur.
- `scripts/restore-test.ps1`: en yeni sifreli yedegi bulur, cozer, gecici bir
  veritabanina geri yukler, migration/kullanici sayimiyla dogrular, gecici veritabanini
  her kosulda siler. Basarisizlikta cikis kodu 1 doner (izleme/uyari icin kullanilabilir).
  Elle dogrulandi (27 Eylul 2026): dogru parolayla basarili, yanlis parolayla temiz
  hata ile basarisiz - sessizce "basarili" gorunmuyor.
- `scripts/protect-backup-key.ps1` + `scripts/install-scheduled-tasks.ps1`: gecelik
  yedek (02:00) ve haftalik geri yukleme provasi (Pazar 03:00) icin Windows Gorev
  Zamanlayici kaydi. **Bilerek otomatik calistirilmadi**: once yurt ici uzak/harici
  yedek konumu (`-OffsiteDir`) ve parolanin sunucu disinda nerede saklanacagi karara
  baglanmali.
- WAL arsivleme (PITR) bu betiklerin kapsami disinda; `postgresql.conf` uzerinde
  `archive_mode=on` + `archive_command` ile ayrica yapilandirilmalidir (canliya alma
  oncesi, bolum 7).

## 11. ENS_Stp.App'ten aktarılan kalici dersler

- Paket, belge veya scriptin diskte olmasi canli uygulama kaniti degildir.
- Ayni publish klasorune ikinci kez cikti yazilmaz.
- SQL/migration paketi runtime dosyalarindan ayrilir.
- Acik uygulama sureci veya aktif servis varken yari kopyalama yapilmaz.
- Config, logs, uploads ve kullanici dosyalari deploy kopyalamasiyla silinmez.
- Hash dogrulamasinda kontrol edilen dosya sayisi sifirsa islem durur.
- Yedek, migration, uygulama yayini ve canli UI kabul sonucu ayri kaydedilir.
- Bir canli geciste bulunan hata, yalniz sohbet notunda kalmaz; prosedure kalici
  kontrol maddesi olarak eklenir.
- Klasor veya RDP kopyalamak yerine Git, migration, manifest ve ortam ayari kullanilir.

## 12. Canli ortam belli oldugunda doldurulacak envanter

- APP01 ve DB01 gercek makine adlari
- Windows ve PostgreSQL surumleri
- CPU, RAM, disk, bos alan ve disk turu
- ozel ag, DNS, HTTPS ve sertifika yapisi
- servis hesaplari ve yetki sinirlari
- API/web/masaustu yayin yollari
- PostgreSQL veri, WAL, log ve yedek yollari
- zamanlanmis gorevler ve izleme
- yedek hedefleri ve geri yukleme testi tarihi
- mevcut canli surum, Git commit'i ve migration seviyesi
- destek, sorumlu kisi ve acil durum erisim yolu


## 13. Ilk kurulum onkosul paketi - 25 Eylul 2026

Kullanici gereksinimi: Canliya gecmeden once hedef bilgisayarda gereken programlarin
kurulum dosyalari ve uygulanacak PowerShell/CMD komutu kullaniciya teslim edilir.
Yalniz indirme baglantisi teslim edilmesi yeterli degildir.
Ilk hedef mevcut normal bilgisayardir. Sonraki canli hedef ayri kiralanacak
RDP'dir; gelistirme icin kiralanacak RDP bunlardan bagimsizdir.

- Ureticinin resmi EXE/MSI kurulumlari, lisanslarinin izin verdigi bicimde,
  prerequisites/ klasorunde toplanir. Windows bilesenleri EXE olarak sunulmaz;
  etkinlestirme adimi ve gerekirse resmi Windows kurulum medyasi belirtilir.
- ASP.NET Core + React secilirse ve IIS kullanilirsa once IIS etkinlestirilir,
  sonra hedef uygulamayla uyumlu .NET Hosting Bundle kurulur. Bu hosting yolu
  teknoloji ADR'si ve hedef isletim sistemi kesinlesince sabitlenir.
- PostgreSQL Windows x64 kurulumu hedef surumle sabitlenir; mevcut cluster varsa
  yeniden baslatilmaz veya uzerine kurulmaz. Roller, veri dizini, servis hesabi
  ve yedekleme ayrica yapilandirilir.
- Derlenmis React arayuzu pakete dahildir; standart statik arayuz yayini icin
  canliya Node.js, .NET SDK, Visual Studio veya VS Code kurulmasi gerekmez.
- pgAdmin istege bagli yonetim aracidir; uygulamanin calisma onkosulu degildir.
- Kurulum dosyalarinin resmi kaynak URL'si, surumu, mimarisi, SHA256 ozeti ve
  varsa uretici dijital imza kontrol sonucu onkosul manifestine kaydedilir.
- Misafir isletim sistemi secimi ve lisansi kurulumdan once dogrulanir. Windows
  Server secilirse host Windows Pro lisansi misafir lisansi yerine gecmez.
- Onkosul kontrolu, ilk kurulum, guncelleme ve saglik kontrolu ayri betiklerdir.
  Betikler yeniden calistirmaya dayanir; cikis kodlarini ve yeniden baslatma
  gereksinimini kontrol eder, hata sonrasinda sonraki adima devam etmez.
- Parolalar pakete veya verilen komuta yazilmaz; hedefte guvenli olarak alinir.
- Ilk bos kurulum ile mevcut verili ortam guncellemesi ayri ele alinir.
  Yedek kapisi mevcut verili ortam icindir; ilk kurulumda canli veri girisinden
  once yedekleme ve sentetik veriyle geri yukleme provasi tamamlanir.
- Basarisiz migration veya surum uyumsuzlugunda trafik acilmaz; veritabani
  otomatik eski yedege dondurulmez. Bolum 9 kurtarma kurallari uygulanir.
- Kullaniciya verilen komut yalniz dogrulanmis paket, hedef yol ve betik adlari
  belli olduktan sonra uretilir; ornek komut calisir kurulum gibi sunulmaz.

Durum: Gelistirme RDP'si, yerel bilgisayardaki ilk canli hedef ve sonraki
ayri canli RDP hedefi birbirinden ayrildi.
Kurulum dosyalari ve betikler henuz hazirlanmadi; temiz sanal makine testi ve
canli ortam dogrulamasi yapilmadi.

## 14. Mobil uygulama ve havadan guncelleme (OTA) - 26 Eylul 2026

`[KARAR]` Kaynak: `docs/decisions/ADR-001_TEKNOLOJI_YIGINI_VE_ISTEMCI_MIMARISI.md`
bolum 3.4. Faz 2'de mobil uygulama acildiginda uygulanir.

- OTA yalniz ekran, is mantigi, metin ve gorsel degisikligi tasir. Yeni izin, yeni yerel
  kutuphane veya Expo SDK yukseltmesi magaza surumu ve inceleme gerektirir.
- Uygulamanin temel amacini veya niteligini degistiren degisiklik OTA ile yapilmaz.
- OTA bir dagitim kanalidir. Bu prosedurdeki surum, Git commit'i, dogrulama ve geri
  alma kayitlari OTA icin de ayri ayri tutulur.
- Once test kanali, sonra kademeli yayin. Geri alma yolu yayindan once hazirdir.
- Kotu bir OTA guncellemesi ayni anda tum telefonlara gider; test kanalinda dogrulanmamis
  paket uretim kanalina gonderilmez.
- API geriye uyumlu ilerler. Desteklenen en eski istemci surumu tanimlanir; daha eski
  surume zorunlu guncelleme ekrani gosterilir.
- Mobil uygulama surumu ile API surumu arasindaki uyumluluk matrisi manifestte bulunur.
