# UU Sport Ana Hafiza

**Surum:** 1.6  
**Olusturma tarihi:** 23 Eylul 2026  
**Son guncelleme:** 25 Eylul 2026  
**Ilk yazar:** Codex  
**Durum:** Proje baslangici; gelistirme kullanici istegiyle durduruldu, bagimsiz Claude Code denetimi bekleniyor

Bu dosya UU Sport / PSG Academy Samsun projesinin tek guncel proje hafizasidir.
Claude Code ve Codex her calismanin basinda bu dosyayi okur. Dosya parola, API
anahtari veya gercek ogrenci/veli verisi icermez.

## 0. Hizli Durum

- `[DOGRU]` Proje koku: `C:\UUSport`
- `[DOGRU]` VS Code bu klasorde acik ve calisma alani guvenilir.
- `[DOGRU]` Klasor ayri bir Obsidian kasasi olarak tanimli.
- `[DOGRU]` Git deposu olusturuldu; dal `master`. Denetim devri icin guvenli
  proje dosyalari yerel Git'e kaydedildi; uzak depo/remote henuz tanimli degil.
- `[DOGRU]` PostgreSQL 18.6 yerel gelistirme sunucusu `127.0.0.1:5432` adresinde
  calisiyor ve baglanti kabul ediyor.
- `[DOGRU]` pgAdmin 4 9.17 kurulu. Resmi Turkce arayuz yok.
- `[DOGRU]` Uygulama kodu, proje veritabani semasi ve migration henuz yok.
- `[KARAR]` Gunluk guncel ogrenci aktariminin tek kaynagi Excel'deki `Sayfa1 (2)`
  sayfasidir. Benzer isimli sayfalar otomatik birlestirilmeyecek.
- `[KARAR]` Gercek cocuk, sporcu ve veli verisi gelistirme/test ortamlarina veya
  yapay zeka araclarina verilmeyecek.
- `[KARAR]` Projenin ana UI/UX tasarim rehberi `Apple Design Skill` olacak.
- `[KARAR]` Gelistirme ve test, canli veriyi temsil eden maskeli veya sentetik
  veriyle yapilacak; uygulama burada gelistirilip surumlu paket ve migration ile
  canliya alinacak.
- `[KARAR]` Kullanici Excel, PDF veya resim kaynaklari verebilir. Orijinaller
  salt okunur korunur; gelistirmeye yalniz maskeli/sentetik turev ve alan eslemesi
  girer.
- `[KARAR]` Canli yerlesim normal Windows bilgisayar, tek RDP/VDS veya iki ayri
  RDP/VDS olabilir. Mimari `APP01` ve `DB01` mantiksal rollerini her uc
  yerlesimde destekleyecek.
- `[KARAR]` Gelistirme ortami icin ayri bir RDP kiralanacak; bu RDP canli
  ortamdan ayri konumlanacak ve yalniz maskeli/sentetik veri kullanacak.
- `[KARAR]` Ilk canli kurulum kullanicinin mevcut normal Windows bilgisayarinda
  yapilacak. Donanim uygunsa bilgisayarda sanal makine kurulacak; uygulama ve
  PostgreSQL ilk asamada bu sanal makinede birlikte calisacak.
- `[KARAR]` Daha sonra canli uygulama ve PostgreSQL verileri, canli icin yeni
  kiralanacak ayri RDP sunucusuna tasinacak. Gelistirme RDP'si bu tasimanin
  hedefi olmayacak; gelistirme ve canli birbirinden ayri kalacak.
- `[ACIK]` Gelistirme ve sonraki canli RDP'leri henuz kiralanmadi. Tasima
  zamani, hedef sunucunun ozellikleri ve ag yapisi daha sonra kesinlesecek.
- `[DOGRU]` Hedef bilgisayarin paylasilan ekran goruntusunde Windows 11 Pro,
  Intel i3-10100 ve 8 GB RAM goruldu. Disk ve sanallastirma ayari dogrulanmadi;
  8 GB RAM sanal makinede canli kurulum icin yeterli kabul edilmiyor.
- `[KARAR]` Ilk kurulumda gereken resmi EXE/MSI dosyalari, kurulum betikleri ve
  kullanicinin calistiracagi komut teslim paketinde bulunacak.
- `[KARAR]` Canliya alma ve veri proseduru:
  `docs/operations/UUSPORT_GELISTIRME_VERI_VE_CANLIYA_ALMA_PROSEDURU.md`
- `[KARAR]` PostgreSQL adlandirma ve migration standardi:
  `docs/standards/UUSPORT_POSTGRESQL_ADLANDIRMA_VE_MIGRATION_STANDARDI.md`
- `[KARAR]` Rapor bolumunde aylik KDV, 3/6/9/12 aylik vergi simulasyonu,
  kredi karti, kredi, maas, kira, muhtasar, SGK ve toplam odeme raporlari olacak.
- `[ACIK]` Masaustu ve backend teknoloji yigini ADR ile kesinlestirilecek.
- `[ACIK]` MVP kapsami ve fazlama kullanici onayi bekliyor.
- `[KARAR]` 25 Eylul 2026'da kullanici gelistirmeye ara verip simdiye kadarki
  calismalarin Claude Code tarafindan bastan sona bagimsiz denetlenmesini istedi.
  Denetim promptu: `docs/reviews/CLAUDE_CODE_UUSPORT_TAM_DENETIM_PROMPTU_2026-09-25.md`.
- `[DOGRU]` Su ana kadar uygulama/API/React kodu, UUSport veritabani semasi,
  migration, canli kurulum paketi ve canli ortama alim yapilmadi. Muhasebe akis
  belgeleri, mockup, rapor taslagi, SQL standardi ve operasyon proseduru hazir.
- `[DOGRU]` Masaustunde `UUSport-Gorsel-Ornekler/UUSport-Onizleme.html` dosyasi
  bulunuyor; ayni icerik `docs/designs/UUSPORT_MUHASEBE_KABA_ONIZLEME.html`
  olarak Obsidian/Git projesine alindi. Bu statik gorsel taslaktir; uygulama
  veya veri aktarimi degildir.
- `[RISK]` Repo altinda `uu sport ltd/PSG aktif tutulan basit excel.xlsx`
  kopyasi bulunuyor; gercek kisisel veri icerip icermedigi bu denetimde
  incelenmedi. Dosya kaynak veri kabul edilmeli, modele okutulmamali ve Git'e
  eklenmemeli. `.gitignore` ile `uu sport ltd/` klasorunun tamami dislandi;
  fiziksel dosya yerinde birakildi. Guvenli tasima/silme karari ayri verilecek.

## 1. Projenin Amaci

Samsun'da faaliyet gosteren spor akademisi icin asagidaki istemcilerin ortak veri
ve is kurallariyla calistigi cok kullanicili bir platform hedefleniyor:

- Windows agirlikli masaustu uygulamasi
- Yonetim ve operasyon web uygulamasi
- Veli ve antrenor mobil uygulamalari veya ilk faz PWA
- Merkezi HTTPS API
- Merkezi PostgreSQL veritabani

Ana is alanlari sporcu-veli kayitlari, paket ve tarifeler, cari hesap, tahakkuk,
tahsilat, gider, kasa-banka, e-belge, antrenor hakedisi, raporlama, sporcu gelisim
takibi ve denetim kaydidir. Resmi defter ve beyan sureci mali musavirin GMS.NET
sisteminde kalir; UU Sport yonetim ve on muhasebe katmanidir.

## 2. Kaynaklar ve Yetki Sirasi

### 2.1 Guncel urun spesifikasyonu

`C:\Users\Ensaradmin\Desktop\uu sport ltd\psg-academy-samsun-sistem-spesifikasyonu.md`

- Surum: 0.11
- Tarih: 17 Eylul 2026
- Durum: Tasarim tamamlanma asamasi, kod oncesi
- Is kurallari ve urun gereksinimleri icin guncel ana kaynak budur.

### 2.2 Diger kaynaklar

- `C:\Users\Ensaradmin\Desktop\uu sport ltd\PSG_Academy_Samsun_Yonetim_Sistemi_Karar_Raporu_v0.10.pdf`
- `C:\Users\Ensaradmin\Desktop\uu sport ltd\PSG aktif tutulan basit excel.xlsx`

Surum 0.10 PDF tarihsel kaynaktir; v0.11 ile celistiginde v0.11 kullanilir. Kaynak
dosyalar masaustundeki asil konumlarinda korunur ve salt okunur incelenir.

###2.3 Karar onceligi

1. Kullanicinin en son acik talimati
2. `docs/decisions` altindaki onaylanmis ADR
3. Bu ana hafizadaki guncel durum
4. Surum 0.11 spesifikasyonu
5. Tarihsel belgeler ve sohbetler

Yeni bir karar eski belgeyi gecersiz kiliyorsa ADR ve ana hafiza ayni calismada
guncellenir.

## 3. Kesinlesen Kararlar

### 3.1 Veri kaynagi

- Excel aktariminda yalniz `Sayfa1 (2)` guncel ogrenci listesi kabul edilir.
- `Sayfa1`, `Sayfa1 (3)` veya benzer sayfalar otomatik birlestirilmez.
- Ayni isimli kayitlar otomatik silinmez. Paket yenilemesi, tarihsel kayit ve gercek
  mukerrer birbirinden ayrilir.
- Mevcut bakiye ve toplam odeme alanlari muhasebe kaydi olarak dogrulanmadan acilis
  bakiyesine donusturulmez.

### 3.2 Sistem siniri

- Masaustu, web ve mobil istemciler merkezi HTTPS API'ye baglanir.
- Istemciler PostgreSQL'e dogrudan baglanmaz.
- Dosya depolama ve dis entegrasyonlar API arkasinda tutulur.
- Gelistirme, test/yayin oncesi ve canli ortamlar ayri guvenlik sinirlaridir.
- Proje baska masaustu veya RDP'ye klasor kopyalayarak degil; Git, surumlu
  migration, ortam ayarlari ve tekrarlanabilir kurulum ile tasinir.

### 3.3 Veri guvenligi

- Gercek cocuk, sporcu ve veli verisi gelistirme/teste veya yapay zeka araclarina
  verilmez; maskeli veya sentetik veri kullanilir.
- Gizli bilgiler kaynak kod ve Git disinda tutulur.
- Muhasebe kaydi silinmez; duzeltme ters kayitla yapilir.
- Yetki yalniz ekranda gizlemeyle saglanmaz; API ve PostgreSQL RLS katmaninda
  uygulanir.
- Saglik ve diger ozel nitelikli veri modulu bagimsiz guvenlik testi tamamlanana
  kadar kapali tutulur.

## 4. Henuz Kesinlesmeyen Mimari

### 4.1 Teknoloji yigini

Surum 0.11 dokumani su yonu tarif ediyor:

- Backend: Node.js + TypeScript
- Veritabani: PostgreSQL + RLS
- Web: React
- Mobil: ilk faz PWA; React Native revizyon adayi

Daha sonraki gorusmelerde Windows tercihi nedeniyle su alternatif onerildi:

- Masaustu: C# / .NET 10 / WPF
- API: ASP.NET Core
- Web: React
- Mobil: React Native
- Veritabani: PostgreSQL 18

`[ACIK]` PostgreSQL tercihi ve merkezi API siniri kabul edildi; Node.js/TypeScript
ile .NET/WPF seceneklerinden hangisinin kullanilacagi onaylanmadi. Kodlamadan once
`docs/decisions` altinda ADR yazilip kullanici tarafindan onaylanacak.

### 4.2 MVP adayi

Asagidakiler adaydir, kesin kapsam degildir:

1. Giris, roller ve yetkiler
2. Sporcu, veli, aile ve teslim yetkileri
3. Paket, tarife, indirim ve kayit yenileme
4. Cari hesap, tahakkuk ve tahsilat
5. Temel kasa-banka
6. Append-only denetim kaydi
7. `Sayfa1 (2)` kontrollu veri aktarimi
8. KDV/vergi simulasyonu ve aylik odeme takvimi raporlari

Performans olcumleri ve mobil yoklama sonraki faz adayi olabilir. Rapor kapsami
kararlastirildi; hangi raporun hangi release'e girecegi henuz fazlanmadi.

### 4.3 Ana tasarim dili

`[KARAR]` Masaustu, web ve mobil arayuzlerin ana tasarim ve inceleme rehberi:

`C:\Users\Ensaradmin\.codex\skills\apple-design\SKILL.md`

Apple Design Skill su konularda temel alinacak:

- Apple Human Interface Guidelines tabanli erisilebilirlik, tipografi, renk,
  yerlesim ve etkilesim ilkeleri
- Mobil ve masaustu gezinme yapisi
- Formlar, tablolar, listeler, diyaloglar, geri bildirim ve hata durumlari
- Klavye, fare/dokunma, odak sirasi ve ekran okuyucu destegi
- Acik/koyu gorunum, kontrast ve buyuk metin destegi
- Tasarimin ayirt edici fakat sade, tutarli ve amaca hizmet eden bir kimlige sahip
  olmasi
- Ekran goruntusu, mockup, wireframe veya UI kodu icin tasarim denetimi

Bu karar, Windows ve web arayuzlerinin macOS gorunumu taklit edecegi anlamina
gelmez. Skill'deki tasarim ilkeleri hedef platformun ve secilen framework'un dogal
bilesenlerine cevrilir. Windows masaustu klavye/fare davranislari, web erisilebilirlik
kurallari ve Android/iOS platform beklentileri korunur.

UI/UX calismasinda ajan ilgili ekrani degerlendirmeden once skill'in zorunlu temel
referanslarini ve ekranda kullanilan bilesenlere ait ilgili HIG sayfalarini okur.
Tasarim sirasi: erisilebilirlik, platform davranisi, gorsel hiyerarsi, etkilesim,
icerik dili ve son olarak gorsel parlatmadir.

### 4.4 Muhasebe cari, stok/hizmet ve tahsilat akisi

`[KARAR]` Ilk muhasebe ekran akisi kullanici tarafindan uygun bulundu:

- Ayni veli/musteri icin iki cari acilmaz; tek cari altinda `Toplam`,
  `Belgeli / Resmi` ve `Muhasebe Bekleyen` gorunumleri bulunur.
- Sag ustte bagimsiz `Fatura Hazirla` dugmesi bulunmaz.
- Ana islemler `Stok veya Hizmet Cikisi` ve `Tahsilat Al` olur.
- Stok seciminde urun katalogu acilir ve onaylanan miktar stoktan duser.
- Hizmet seciminde hizmet katalogu acilir; miktar/adet, fiyat ve iskonto girilir.
- Iskonto siniri kullanici/role gore belirlenir; sinir asilirsa kayit engellenir.
- `Resmi cikis/islem` secilirse belge onizlemesi acilir. Kullanici onaylarsa
  fatura dogrudan gonderilmez, taslak olarak kaydedilir.
- Resmi secenek kapaliysa stok/hizmet veya tahsilat hareketi yine kaydedilir,
  caride gorunur, audit kaydi olusur ve `Muhasebe Bekleyen` listesine gider.
- Muhasebe ve stok hareketleri silinmez; duzeltme ters kayitla yapilir.

Ayrintili akis:
`docs/designs/MUHASEBE_CARI_STOK_BELGE_AKISI_TASLAK.md`

Etkilesimli mockup:
`docs/designs/cari-hesap-mockup.html`

### 4.5 Muhasebe raporlari ve odeme takvimi

`[KARAR]` Rapor modulu en az su kapsami icerir:

- Aylik KDV simulasyonu
- 3, 6, 9 ve 12 aylik gelir/kurumlar/gecici vergi simulasyonu
- Aylik sirket kredi karti odemeleri
- POS tahsilati, komisyon ve valor raporu
- Kredi taksitleri; ana para, faiz, vergi/masraf ve kalan ana para
- Maas, isveren maliyeti, SGK ve muhtasar odemeleri
- Kira, stopaj ve sabit odemeler
- Tedarikci, cek/senet, antrenor hakedisi ve diger odemeler
- Aylik, 3/6/9/12 aylik toplam odeme ve nakit yeterlilik raporu

Butun raporlarda `Gerceklesen`, `Planlanan`, `Tahmini`,
`Muhasebe Bekleyen` ve `Belgeli/Resmi` tutarlar birbirinden ayrilir.
Simulasyon raporlari resmi beyan gibi sunulmaz.

Vergi, KDV, muhtasar, SGK ve kira kurallari koda sabit yazilmaz; tarih aralikli
parametre ve mali musavir teyidiyle calisir. Kullanici mesajindaki `SK` ifadesi
`SGK` olarak yorumlandi.

Ayrintili kapsam:
`docs/designs/MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md`

## 5. Yerel Gelistirme Ortami

### 5.1 VS Code ve ajanlar

- Calisma klasoru: `C:\UUSport`
- Calisma alani guvenilir olarak isaretlendi.
- Claude Code ve OpenAI Codex uzantilari Windows kullanici profilinde kurulu; proje
  basina tekrar kurulmaz.
- Codex `apple-design` becerisi kullanici profilinde kurulu ve bu projede
  kullanilabilir; ana UI/UX tasarim rehberidir.
- `AGENTS.md` Codex'i, `CLAUDE.md` Claude Code'u bu ana hafizaya yonlendirir.

### 5.2 Obsidian

- `C:\UUSport` ayri Obsidian kasasidir.
- Yerel `.obsidian` ayarlari `.gitignore` ile Git disinda tutulur.
- Proje kararlari ve tasarim notlari `docs` altinda tutulur; gercek kisi verileri
  Obsidian notlarina kopyalanmaz.

### 5.3 PostgreSQL

- Surum: PostgreSQL 18.6, 64 bit
- Program: `C:\Program Files\PostgreSQL\18`
- Gelistirme adresi: yalniz 127.0.0.1:5432
- Veri klasoru: `%LOCALAPPDATA%\UUSport\PostgreSQL\18\data`
- Log klasoru: `%LOCALAPPDATA%\UUSport\PostgreSQL\18\logs`
- Superuser parolasi: `%LOCALAPPDATA%\UUSport\secrets` altinda Windows DPAPI ile
  korunur; belgeye veya Git'e yazilmaz.
- Bu kurulum Windows servisi degildir; kullanici modunda yerel gelistirme kumesidir.
- Baslatma: `powershell -ExecutionPolicy Bypass -File .\tools\start-postgresql-dev.ps1`
- Durdurma: `powershell -ExecutionPolicy Bypass -File .\tools\stop-postgresql-dev.ps1`
- pgAdmin 4 9.17 kurulu ve kullanilabilir.
- `[ACIK]` pgAdmin icin resmi Turkce arayuz yoktur. Daha sonra surume ozel, gettext
  tabanli kismi Turkce dil paketi hazirlama istegi kaydedildi.

Proje veritabani, roller, tablolar ve migration henuz olusturulmadi. Mimari ADR ve
MVP onayindan once sema olusturulmaz.

### 5.4 Git durumu

- Git deposu olusturuldu.
- Dal: `master`
- Ilk yerel commit 25 Eylul 2026'da denetim devri icin olusturuldu.
- Uzak Git deposu (remote) henuz tanimlanmadi; bu commit baska RDP'ye
  kendiliginden aktarilmaz.
- `.env`, anahtarlar, sertifikalar, yedekler, loglar, kurulum dosyalari ve yerel
  Obsidian ayarlari Git disinda tutulur.

## 6. Mevcut Klasor Yapisi

```text
C:\UUSport
|-- AGENTS.md
|-- CLAUDE.md
|-- README.md
|-- database/       # migration ve seed tanimlari; su an bos
|-- deployment/     # test/canli dagitim tanimlari; su an bos
|-- docs/
|   |-- decisions/  # onaylanmis ADR dosyalari
|   |-- designs/    # ekran, akis ve tasarim notlari
|   |-- operations/ # veri kabul, release, canliya alma, yedek ve geri donus
|   |-- project-memory/
|   |   `-- UUSPORT_ANA_HAFIZA.md
|   |-- standards/  # PostgreSQL, migration ve diger teknik standartlar
|   `-- source/     # guvenli calisma kaynaklari; gercek veri konmaz
|-- src/            # uygulama kaynaklari; su an bos
|-- tests/          # otomatik testler; su an bos
`-- tools/          # gelistirme kurulum ve yardimci betikleri
```

## 7. Kaynak Veri Incelemesinin Sonucu

- Kaynak Excel 17 sayfa iceriyor.
- `Sayfa1 (2)` onayli guncel aktarim kaynagidir.
- `2025 FIYAT` sayfasinda 58 formulun 29'unda `#REF!` tespit edildi.
- Ogrenci listelerindeki bakiye ve toplam odeme alanlari dogrulanmis muhasebe
  hareketi degildir.
- Acilis bakiyeleri mali musavir veya mutabakat onayi olmadan sisteme alinmaz.
- Kaynak dosyalara henuz degisiklik yapilmadi ve veri aktarimi gerceklesmedi.

## 8. Canli Ortam, Yedek ve Saglayici Yaklasimi

Asagidakiler gelecekteki bulut yerlesimi icin tarihsel kapasite onerileridir;
ilk canli kurulumun hedefi degildir:

- Uygulama katmani icin `APP01`: 2 dedicated vCPU, 8 GB RAM, 100-150 GB NVMe
- Veritabani icin `DB01`: 4 dedicated vCPU, 16 GB RAM, 200-300 GB enterprise NVMe
- Buluta gecis asamasinda dedicated-vCPU VDS veya yonetilen PostgreSQL degerlendirmesi
- Uygulama ve veritabaninin ayri orneklerde, tercihen ayri fiziksel hostlarda olmasi
- Disariya yalniz HTTPS 443; PostgreSQL 5432 yalniz ozel agdan erisilebilir
- RDP erisiminin VPN veya RD Gateway ve MFA arkasinda olmasi
- WAL arsivi + PITR, gunluk tam yedek, farkli hesapta degistirilemez yedek
- Hedef RPO 15 dakika, RTO 4 saat; ayda bir gercek geri yukleme testi
- Dinamik olcekleme once stateless web/API katmaninda; PostgreSQL olcume gore
  kontrollu buyutulur, surekli kucultulup buyutulmez.

`[ACIK]` Ilk hedef fiziksel bilgisayar secildi; RAM yukseltmesi, disk,
sanallastirma, misafir isletim sistemi ve lisans, elektrik/internet surekliligi
ile yedek hedefi henuz dogrulanmadi. Henuz canli sunucu veya alan adi
yapilandirmasi yapilmadi. Sonraki canli RDP saglayicisi ve paketi daha sonra
secilecek; gelistirme RDP'sinden ayri olmasi kararlastirildi.

### 8.1 Yerlesimden bagimsiz canli mimari

`[KARAR]` Kesin sunucu daha sonra verilecek olsa da uygulama su yerlesimleri
destekleyecek:

1. Normal Windows bilgisayarda APP01 + DB01
2. Tek RDP/VDS icinde APP01 + DB01
3. Ayri uygulama ve PostgreSQL RDP/VDS: APP01 + DB01

Planlanan sira: ayri gelistirme RDP'sinde surum hazirlanir; mevcut normal
bilgisayara ilk canli kurulum yapilir; ileride canli uygulama/veri ayri
kiralanacak yeni RDP'ye tasinir. Gelistirme RDP'si hicbir asamada canli veri
veya canli veritabani barindirmaz.

APP01 ve DB01 fiziksel makine adi degil, mantiksal roldur. Tek makinede bile servis
hesaplari, klasorler, config, log ve yedek sinirlari ayrilir. Iki makinede PostgreSQL
yalniz ozel ag/VPN uzerinden erisilir.

Canli makine bilgileri geldiginde once salt okunur ortam envanteri cikarilacak;
IP/DNS, disk, servis, sertifika, yedek, mevcut surum ve migration seviyesi
dogrulanmadan deploy veya veri aktarimi yapilmayacak.

### 8.2 Gelistirme ve canliya alma kanit zinciri

`[KARAR]` ENS_Stp.App deneyiminden aktarilan kalici kurallar:

- Paket klasorunun varligi canli deploy kaniti degildir.
- Her surum yeni ve degistirilemez release klasorunde uretilir.
- Runtime paketi ile migration paketi ayri tutulur.
- Manifest ve SHA256 kontrolunde sifir dosya dogrulanmasi basari sayilmaz.
- Config, secrets, logs, uploads ve canli raporlar deploy ile silinmez veya ezilmez.
- Canli oncesi yedek ve geri yukleme kaniti olmadan migration uygulanmaz.
- `precheck -> apply -> verify -> recovery` akisi kullanilir.
- Uygulama surumu, Git commit'i, migration seviyesi, hashler ve smoke test sonucu
  ayri ayri kaydedilir.
- Uygulama geri donusu ile veritabani geri donusu ayni islem sayilmaz.

Ayrintili ve baglayici prosedur:
`docs/operations/UUSPORT_GELISTIRME_VERI_VE_CANLIYA_ALMA_PROSEDURU.md`

### 8.3 PostgreSQL isim ve migration temeli

`[KARAR]` PostgreSQL nesneleri ASCII `lower_snake_case`, schema ile
nitelendirilmis ve tekil tablo adlariyla tasarlanir. Para alanlari
`numeric(18,2)`, zaman alanlari UTC `timestamptz` olur. Finans ve stok kayitlari
silinmez; ters kayitla duzeltilir. Tenant verisinde `tenant_id` ve RLS zorunludur.

Tek `is_official` alani yerine `pending_accounting`, `draft_document`,
`documented`, `partially_documented`, `reversed` ve `cancelled` gibi acik
durumlar kullanilir.

`[ACIK]` Migration araci teknoloji ADR'sinden sonra secilecek. Arac secilene ve
schema tasarimi onaylanana kadar tablo/migration olusturulmayacak.

Ayrintili standart:
`docs/standards/UUSPORT_POSTGRESQL_ADLANDIRMA_VE_MIGRATION_STANDARDI.md`

## 9. Veri Mulkiyeti ve Saglayicidan Cikis

- Alan adi, sunucu, Git, Apple/Google, odeme, SMS ve e-belge hesaplari sirket adina
  acilir.
- Standart formatta duzenli veri disa aktarimi bulunur.
- Yedekler uygulama sunucusundan ayri hesapta, tercihen ayri saglayicida tutulur.
- Sifreleme anahtarlari ve kaynak kod erisimi sirket kontrolunde olur.
- Sozlesmede tam veri teslimi, cikis proseduru ve silme belgesi yer alir.
- Tek sunucu veya tek saglayici hesabi butun kopyalari yok edebilecek guven siniri
  haline getirilmez.

## 10. Dis Onay ve Bilgi Bekleyen Konular

- Mali musavir: e-Defter plani, faturalama ani, KDV ve gider kurallari, acilis
  bakiyeleri, GMS.NET aktarim sablonu ve hesap plani
- Avukat: kod mulkiyeti, veri isleme sozlesmesi, KVKK metinleri, ihlal plani ve
  kayit/iptal sozlesmesi
- Federasyon/Il Mudurlugu: lisansli sporcu ve kulup gereklilikleri
- Kurum ici: MEB izin kapsami, antrenor formlari ve IT sorumlusu
- Urun: sporcu performansi, raporlar, yoklama/iletisim ve kesin MVP fazlamasi

## 11. Siradaki Calisma Sirasi

1. Teknoloji yigini icin ADR hazirla ve onayla.
2. MVP kapsam ve fazlarini karara bagla.
3. Is terimleri ve ana veri varliklarini gozden gecir.
4. PostgreSQL standardina gore veritabani semasini ve migration stratejisini
   tasarla; onaydan sonra uygula.
5. Maskeli/sentetik veriyle `Sayfa1 (2)` aktarim pilotu hazirla.
6. Onaylanan muhasebe akisini veri varliklari ve yetki matrisiyle esle.
7. Uygulama iskeletini olustur ve gelistirmeye basla.
8. Canli ortam belli oldugunda ortam envanteri, kurulum ve release planini doldur.

## 12. Claude Code ve Codex Ortak Calisma Protokolu

- Calismaya baslamadan `AGENTS.md`, `CLAUDE.md` ve bu ana hafizayi oku.
- Durum ifadelerinde su etiketleri kullan:
  - `[DOGRU]`: Dosya, komut veya calisan ortamla dogrulandi.
  - `[KARAR]`: Kullanici tarafindan acikca onaylandi.
  - `[ONERI]`: Degerlendirildi fakat henuz onaylanmadi.
  - `[ACIK]`: Karar veya dis bilgi bekliyor.
- Baska bir "ana hafiza" olusturma. Ayrintili tasarimlar `docs/designs`, mimari
  kararlar `docs/decisions` altinda tutulur ve buradan baglanir.
- Excel/PDF/resim veya veri aktariminda
  `docs/operations/UUSPORT_GELISTIRME_VERI_VE_CANLIYA_ALMA_PROSEDURU.md` okunur.
- Schema, SQL veya migration calismasinda
  `docs/standards/UUSPORT_POSTGRESQL_ADLANDIRMA_VE_MIGRATION_STANDARDI.md` okunur.
- Canli/release/restore isinde operasyon proseduru okunur ve hedef ortam envanteri
  dogrulanir.
- Bir ajan dosyayi guncellemeden once son halini yeniden okur; degisikligini asagidaki
  gecmise ekler.
- Sohbet gecmisi tek basina proje kaniti sayilmaz. Dosya, migration, test ve canli
  durum ayri ayri dogrulanir.

## 13. Degisiklik Gecmisi

| Tarih | Surum | Yazan | Degisiklik |
|---|---:|---|---|
| 25.09.2026 | 1.6 | Codex | Calisma denetim icin durduruldu; mevcut belge/uygulama durumu kaydedildi, Claude Code tam denetim promptu ve statik onizleme Obsidian/Git'e eklendi, kaynak dosyalar Git disina alindi. |
| 25.09.2026 | 1.5 | Codex | Canli ilk olarak mevcut normal Windows bilgisayara kurulur; sonra yeni kiralanacak canli RDP'ye tasinir. Gelistirme RDP'si ayri konumlanir. Ilk kurulum EXE/MSI ve betik teslim gereksinimi netlestirildi. |
| 24.09.2026 | 1.3 | Codex | Aylik KDV, 3/6/9/12 aylik vergi simulasyonu; kredi karti, kredi, maas, kira, muhtasar, SGK, toplam odeme ve nakit yeterlilik raporlari karar olarak eklendi. |
| 23.09.2026 | 1.2 | Codex | Maskeli/sentetik gelistirme modeli, Excel/PDF/resim kabul akisi, tek/iki sunucuya tasinabilir APP01-DB01 rolleri, muhasebe ekran kararlari, canliya alma proseduru ve PostgreSQL standardi eklendi. |
| 23.09.2026 | 1.1 | Codex | Apple Design Skill projenin ana UI/UX tasarim rehberi olarak kesin kararlara ve tasarim protokolune eklendi. |
| 23.09.2026 | 1.0 | Codex | Ilk ortak ana hafiza olusturuldu; kaynak incelemesi, ortam kurulumu, kararlar ve acik konular birlestirildi. |
