# UU Sport Ana Hafiza

**Surum:** 3.3  
**Olusturma tarihi:** 23 Eylul 2026  
**Son guncelleme:** 17 Ekim 2026  
**Ilk yazar:** Codex  
**Durum:** Surum 0.1 gelistirme ortaminda calisiyor; M0 tamamlandi, M1 buyuk olcude tamamlandi (kalanlar bolum 11). Siradaki: M1 kalanlari, **M1.5 coklu sirket (ADR-012, onayli)** ve M2. Canli kurulum yok.

Bu dosya UU Sport / PSG Academy Samsun projesinin tek guncel proje hafizasidir.
Claude Code ve Codex her calismanin basinda bu dosyayi okur. Dosya parola, API
anahtari veya gercek ogrenci/veli verisi icermez.

## 0. Hizli Durum

- `[DOGRU]` Proje koku: `C:\UUSport`
- `[DOGRU]` VS Code bu klasorde acik ve calisma alani guvenilir.
- `[DOGRU]` Klasor ayri bir Obsidian kasasi olarak tanimli.
- `[DOGRU]` Git deposu var; ana dal `master`. Uzak depo GitHub'da tanimli
  (`ensardemirr/UUSport`); son commit `6f799d3` (04.10.2026, M0 + M1).
- `[DOGRU]` Gelistirme makinesi `BAKON-DEV01` (kullanici `Ensaradmin`): PostgreSQL 18.6
  kullanici modu kumesi yerel geri dongu adreslerinde (`127.0.0.1:5432` ve `[::1]:5432`)
  calisiyor. Canli hedef bilgisayar henuz kurulmadi ve bu satir onu kapsamaz.
- `[DOGRU]` pgAdmin 4 9.17 kurulu (`web/version.py`). Resmi Turkce arayuz yok.
- `[DOGRU]` 26 Eylul 2026: **UUSport surum 0.1 calisir durumda** (gelistirme ortami).
  ASP.NET Core API + React web + PostgreSQL; `uusport` ve `uusport_test` veritabanlari,
  EF Core migration `InitialSchema`, RLS ve rol yetkileri uygulandi. 31 otomatik test ve
  44 kontrollu uctan uca duman testi gecti. Masaustunde "UU Sport" kisayolu var.
  Ayrinti: bolum 5.5. Canliya alinmadi; canli paket ve kurulum henuz yok.
- `[DOGRU]` 27 Eylul 2026: dis danisma sentezindeki M0 (dogrulanmis hatalar) tamamlandi,
  M1 (guvenli isletim temeli) buyuk olcude tamamlandi; 4 migration, 72 test geciyor
  (son kayit). Ayrinti: degisiklik gecmisi 2.7 ve 2.8, plan:
  `docs/reviews/UUSPORT_DANISMA_SENTEZ_2026-09-27.md`.
- `[DOGRU]` 17 Ekim 2026: M1.5 sunucu tarafi kismen yazildi (migration, sirket baglami, yetki, uclar); web ve testlerin bir kismi kaldi. **Devir notu:** `docs/project-memory/M15_COKLU_SIRKET_DEVIR_NOTU_2026-10-17.md`.
- `[KARAR]` 17 Ekim 2026, **ADR-012 coklu sirket**: uc vergi kimlik numarali sirket
  (**UUSPORT / FITNESS / TIYATRO**, kodlar `UUS/FIT/TIY`) ayni kurulumda; **sirket = kiraci**
  (`core.tenant`), sirket basina rol (`identity.company_membership`: Yonetici, Muhasebe,
  yeni **Ortak**; Hoca/Supervisor Faz 2), patron `is_group_admin`. RLS: okuma secili
  sirket kumesi (`app.tenant_ids`), **yazma tek sirket** (`app.tenant_id`). Ortak'in
  gorunurlugu yetki anahtarlariyla parametrik. Grup ici islem (borc, fatura, tahsilat)
  karsi sirkette onay kuyruguyla yazilir ve baglanir; ayni veli her sirkette ayri cari,
  fazla tahsilat `approved_adjustment` ile diger sirkete aktarilir. Kodlarda sirket on eki
  + tedarikci urun kodu esleme tablosu. Musavir/aktarim profili sirket basina. M2'den once
  **M1.5** olarak uygulanacak. ADR: `docs/decisions/ADR-012_COKLU_SIRKET_VE_KIRACI_MODELI.md`;
  calisma: `docs/designs/COKLU_SIRKET_YAPI_CALISMASI_2026-10-17.md`. Kod henuz yazilmadi.
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
- `[KARAR]` Teknoloji yigini 26 Eylul 2026'da ADR-001 ile kesinlesti: ASP.NET Core (C#)
  API + PostgreSQL + React web + React Native (Expo) mobil; masaustu istemci yok.
  Ayrinti: bolum 4.1 ve `docs/decisions/ADR-001_TEKNOLOJI_YIGINI_VE_ISTEMCI_MIMARISI.md`.
- `[KARAR]` 26 Eylul 2026'da uc ADR daha onaylandi (`docs/decisions/`):
  ADR-002 para tipi ve yuvarlama (`numeric(18,2)`, API'de metin, yarim yukari; gelen
  belgede KDV yazdigi gibi alinir, kendi belgemizde satir KDV'si yuvarlanmaz);
  ADR-003 harmanlama (insan metni Turkce, kod/anahtar `COLLATE "C"`, tek arama
  normalizasyonu); ADR-004 cari hareket modeli (cariye yalniz satis satiri ve tahsilat
  tutar yazar; belge ve dagitim yazmaz). Standartlara ve tasarim belgelerine islenmesi
  ayri calismada yapilacak.
- `[DOGRU]` Bagimsiz Claude Code denetimi tamamlandi; capraz dogrulama sonrasi rapor
  surum 2.0: `docs/reviews/CLAUDE_CODE_UUSPORT_DENETIM_RAPORU_2026-09-25.md`.
  Bulgularin ana hafizaya islenmesi ayri bir calismada yapilacak.
- `[KARAR]` Ilk surum kapsami ADR-005 ile kesinlesti (26.09.2026); aidat tahakkuku ve
  e-belge entegratoru disarida, fatura elle girilir, alis/gider girisi icerde. Bolum 4.2.
- `[KARAR]` Guvenlik standardi onaylandi (26.09.2026):
  `docs/standards/UUSPORT_GUVENLIK_STANDARDI.md`. Guvenlik konularinda esastir.
- `[DOGRU]` Sirketin alan adi yok (kullanici beyani, 26.09.2026). Faz 1 yerel ag HTTPS icin
  ic sertifika otoritesi kullanilacak; Faz 2 oncesi sirket adina alan adi alinmasi zorunlu.
- `[DOGRU]` (Tarihsel) 25 Eylul 2026'da gelistirmeye denetim icin ara verilmisti; denetim
  tamamlandi ve gelistirme 26 Eylul'de yeniden basladi. Bu ara artik gecerli degildir.
  Denetim promptu: `docs/reviews/CLAUDE_CODE_UUSPORT_TAM_DENETIM_PROMPTU_2026-09-25.md`.
- `[DOGRU]` Canli kurulum paketi, canli ortam ve canliya alim yoktur. Gelistirme
  ortaminda calisan surum 0.1 vardir (yukarida).
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
- Ayrintili ve baglayici kurallar: `docs/standards/UUSPORT_GUVENLIK_STANDARDI.md`.

## 4. Mimari ve Kapsam

### 4.1 Teknoloji yigini

`[KARAR]` 26 Eylul 2026'da kullanici tarafindan onaylandi. Gerekce, degerlendirilen
secenekler, riskler ve acik konular:
`docs/decisions/ADR-001_TEKNOLOJI_YIGINI_VE_ISTEMCI_MIMARISI.md`

- Arka uc: ASP.NET Core (C#), .NET 10 LTS. Tek API; tum istemciler buna baglanir.
- Veritabani: PostgreSQL 18 + RLS.
- Web: React + TypeScript. Faz 1'de API ile birlikte tek dagitilabilir uygulama olarak
  sunulur (tek surec, tek port, tek sertifika).
- Mobil: React Native (Expo). Faz 2'de personel uygulamasi (hoca, yonetici, muhasebe);
  Faz 3'te ayri veli uygulamasi. Ikisi ayni depoda, ortak TypeScript paketleriyle.
- Masaustu istemci (WPF/WinForms) yok. Kullanici tarayici tabanli calisma istedi.
- Havadan guncelleme: Expo Updates; once test kanali, kademeli yayin, geri alma hazir.
- Fis/fatura okuma: once GIB karekodu, sonra kendi sunucumuzda OCR. Her kayit kullanici
  onayiyla olusur; goruntu sirket disina cikmaz.
- Faz 1 yalniz yerel agda ve HTTPS ile calisir. Mobil uygulama RDP gecisiyle (Faz 2) acilir.

Surum 0.11 spesifikasyonu bolum 2.1'deki Node.js + TypeScript arka uc tercihi ve daha
once onerilen WPF masaustu istemci bu kararla gecersiz kilinmistir.

`[ACIK]` ADR-001 bolum 6'daki 12 acik konu (migration araci, kimlik dogrulama akislari,
dosya deposu, magaza hesaplari ve D-U-N-S, OTA barindirma, OCR motoru vb.) ilgili faz
baslamadan once kapatilacak.

### 4.2 Ilk surum kapsami

`[KARAR]` 26 Eylul 2026, ADR-005:
`docs/decisions/ADR-005_ILK_SURUM_KAPSAMI_VE_ELLE_FATURA.md`

**Surum 1 icinde:** giris ve iki rol (yonetici, muhasebe) + iki adimli dogrulama; cari ve
bagli sporcular; urun/hizmet katalogu ve tek depo stok; stok/hizmet cikisi ve iskonto
yetkisi; tahsilat, kasa/banka, dagitim ve avans; **elle fatura kaydi**; alis faturasi ve
gider girisi, tedarikci carisi ve odeme; uc gorunum ve Muhasebe Bekleyen listesi;
denetim kaydi; Excel aktarimi (bes adim, ogrenci kaynagi yalniz `Sayfa1 (2)`); temel raporlar.

**Surum 1 disinda:** aidat tahakkuku (`[KULLANICI KARARI]`), e-belge entegratoru (su an
faturalar GIB portalinda elle kesiliyor), mobil ve fis fotografi (Faz 2), veli (Faz 3),
KDV/vergi simulasyonu ve odeme takvimi raporlari, banka ekstresi eslestirme, hakedis,
yoklama, performans.

**Elle fatura:** fatura GIB portalinda kesilir, UUSport'a fatura no ve tutarlarla elle
girilir; tutarlar yazdigi gibi alinir, sistem kendi hesabini yalniz kontrol icin gosterir.
Sistemin hazirladigi ozet resmi belge degildir.

`[ACIK]` Stopajli belgelerin (serbest meslek, kira) ilk surume girip girmeyecegi mali
musavirle netlesecek. Mali musavir surece yardimci olacak (`[KULLANICI KARARI]`).

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

`[KARAR]` Cari hareket modeli ADR-004 ile kesinlesti:
`docs/decisions/ADR-004_CARI_HAREKET_MODELI_SATIS_BELGE_TAHSILAT.md`.
Cariye tutar yalniz satis satiri ve tahsilat yazar; fatura ve eslestirme kayitlari
tutar yazmaz; taslak fatura resmi belge sayilmaz. Kullanicinin kabul senaryosu (S0) ve
on ek senaryo her surumde test olarak calisir. `[ACIK]` Akis belgesindeki "fatura" islem
turu ve mockup'taki ayri fatura borc satiri bu karara gore duzeltilecek.

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
- Gelistirme adresi: yalniz yerel geri dongu (`127.0.0.1:5432` ve `[::1]:5432`);
  `pg_hba.conf` yalniz bu adreslere `scram-sha-256` ile izin veriyor (denetim O3)
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

`[DOGRU]` Proje veritabanlari `uusport` (gelistirme) ve `uusport_test` (otomatik
testler) ICU `tr-TR` ile olusturuldu (`tools/setup-uusport-dev-db.ps1`). Roller:
`uusport_owner` (migration; tablolarin sahibi) ve `uusport_app` (calisma zamani; RLS'e
tabi, cari hareket / stok hareketi / denetim kaydi tablolarinda yalniz ekleme ve okuma).
Parolalari `%LOCALAPPDATA%\UUSport\secrets\uusport-db-*.dpapi`.

`[RISK]` `tools/install-postgresql-dev.ps1` ve `repair-postgresql-dev.ps1` servis modu
kume kurar; gecerli yol kullanici modu kumesidir (denetim Y1). Bu iki betik yeni kurulumda
**kullanilmaz**; install betigi parolayi komut satirindan verir (denetim Y2).

### 5.5 Uygulamayi calistirma (gelistirme)

- Masaustundeki **"UU Sport"** kisayolu `tools/run-uusport-dev.ps1`'i calistirir: PostgreSQL
  kapaliysa baslatir, semayi gunceller, uygulamayi kucultulmus pencerede acar ve tarayicida
  `http://127.0.0.1:5080` adresini gosterir. Yalniz bu bilgisayardan erisilir.
- Ilk acilista **ilk kurulum** ekrani cikar: kurum adi ve ilk yonetici hesabi olusturulur
  (varsayilan parola yoktur). Ardindan iki adimli dogrulama (Microsoft/Google
  Authenticator) kurulmadan hicbir veri ekrani acilmaz.
- Derleme ve testler: `tools/build-uusport.ps1` (web + API + tum testler; son kayit 72 test).
- Tam kontrol (derleme, testler, gizli bilgi/gercek veri/acik taramasi): `scripts/ci.ps1`.
- Yeni veritabani/rol kurulumu: `tools/setup-uusport-dev-db.ps1`.
- Kisayolu yeniden olusturma: `tools/create-desktop-shortcut.ps1`.

### 5.6 Uygulama kararlari (kullanici yetki devriyle)

`[KARAR]` 26 Eylul 2026'da kullanici "karar vermem gerekiyorsa sen benim yerime ver"
dedi. Surum 0.1 yazilirken Claude Code su kararlari verdi; itiraz edilirse degistirilir:

- Migration araci EF Core (PostgreSQL standardi bolum 10).
- Katalog fiyati kalem bazinda KDV dahil veya haric. KDV dahilde brut tutar kesin, haric
  fiyatta matrah kesin; satir KDV'si 6 hane tutulur (ADR-002 uygulamasi).
- Cari kodu `C000001`, urun kodu `U000001` bicimindedir; bos birakilirsa sistem verir.
- Ilk kurulumda "Merkez Kasa", "Banka", "POS" hesaplari acilir.
- Parola en az 12 karakter ve bir rakam; 5 hatali denemede 15 dakika kilit; oturum 8 saat;
  kapatilan kullanicinin oturumu en gec 1 dakikada duser.
- Iki adimli dogrulama yontemi TOTP (dogrulayici uygulama).
- Eksi stoka dusurme yalniz Yonetici rolunde.
- Odeme araci ile hesap turu eslesmek zorunda: nakit->kasa, kart->POS, havale->banka,
  cek->banka veya kasa.
- Tahsilat dagitiminda "en eski borctan" dugmesi (spesifikasyon O31); elle dagitim serbest.
- Excel ogrenci aktariminda ayni veli telefonla (yoksa adla) tek cari olur; ayni veli +
  ogrenci tekrari tek sporcu olur; acilis bakiyeleri veli basina toplanip onaya duser.
  Geri alma kayit silmez, kullanim disi birakir; onayli acilis bakiyesi varsa geri alma
  engellenir.
- Iade belgesi yalniz orijinal satir belgelenmisse Muhasebe Bekleyen'de beklenir.
- Hiz siniri: giris, kod dogrulama, ilk kurulum ve iki adim acma uclarinda dakikada 20 istek.
- CSRF korumasi: `SameSite=Strict` cerez + yazan her API isteginde `X-UUSport-Request`
  basligi.
- 26.09.2026, kullanicinin dort geri bildirimi (`docs/designs/SURUM2_HAZIRLIK_NOTU_2026-09-26.md`):
  cari kodu ve urun kodu icin bos birakilabilen `account_code` alani eklendi (mizan
  gelince doldurulur, ADR henuz yazilmadi); kredi karti/kredi hesaplari bir banka
  hesabinin altina baglanir, gercek odeme yine yalniz Cash/Bank/Pos turunde hesaptan
  cikar; kredi taksit tablosu (`finance.loan`/`loan_installment`) ve cek portfoyu
  (`finance.check_note`, seri giris destekli) eklendi; Excel sablon indirme eklendi.
  Karisik stok+hizmet fatura zaten calisiyordu, kod degisikligi gerekmedi.
- 26.09.2026, kullanicinin deneme sirasinda aklina gelen iki geri bildirim islendi:
  sol menu artik daraltilabilir/genisletilebilir gruplara ayrildi (`<details>`/`<summary>`,
  acik/kapali durumu tarayicida `localStorage` ile hatirlanir; "Genel Bakis" grup disinda
  sabit ustte kalir); cari detay sayfasina ayri bir **"Ekstre Al"** dugmesi eklendi, acilan
  pencerede kucuk bir **"Resmi Cari"** tik kutusu var — tik kapaliyken tum hareketler ve
  her satirda "Resmi mi?" rozeti gorunur, tik aciliyken yalniz muhasebeye gonderilmis
  (tam belgelenmis satis/alis satiri veya belgelenmis bir satira dagitilmis tahsilat)
  kayitlar kalir. Pencere `window.print()` ile ekranda gorundugu haliyle yazdirilir/PDF
  kaydedilir (Windows yazdirma diyalogunda "PDF olarak kaydet"). "Resmi" tanimi backend'de
  `AccountingQueries.LedgerAsync` icinde `LedgerRow.IsOfficial` olarak hesaplanir, ayri bir
  alan/tablo eklenmez (ADR-004 modeli degismedi). 5 yeni test (`LedgerOfficialFlagTests`)
  ile dogrulandi; toplam 53 test geciyor.
  - `[TEKNIK NOT]` Native `<dialog>` tarayicinin "ust katmaninda" calisir ve bu katman
    Chromium'da guvenilir sekilde yazdirilmiyor (bos sayfa cikiyordu). Cozum: ekstre icerigi
    pencere acikken `createPortal` ile ayrica `document.body`'ye kopyalanir; ekranda gizli
    kalir, yalniz `@media print`'te gorunur ve gercek `<dialog>` o an tamamen gizlenir.
    Ayrica `html/body/#root/.shell`'in `height:100%`/`min-height:100%` kurallari yazdirmada
    sifirlanmazsa portal sayfanin altina itilip yine bos gorunuyordu; bunlar da print
    medyasinda `height:auto` yapildi. Ileride baska bir pencerede benzer "ekranda gordugun
    gibi yazdir" ozelligi eklenirse ayni portal deseni kullanilmali.
- `[KULLANICI KARARI]` 26.09.2026: girisde her seferinde TOTP kodu yazma surtunmesini
  azaltmak icin MFA **kapatilmadi** (guvenlik acigi olusturmamak icin); bunun yerine
  Identity'nin "bu tarayicidan hatirla" ozelligi acildi
  (`SignInManager.TwoFactorAuthenticatorSignInAsync(..., rememberClient: true)`).
  Kod bir kez dogrulanan tarayicida `uusport.mfa-remember` cerezi ile 30 gun boyunca
  tekrar sorulmaz; farkli tarayici/cihazda veya cerez silinince yine sorulur. Ilk deneme
  olarak MFA'yi tumuyle kapatan bir `Security:RequireMfa` anahtari yazilmisti; bu degisiklik
  Claude Code'un kendi guvenlik siniflandiricisi tarafindan "guvenligi zayiflatiyor" diye
  derlemeden once engellendi ve kullaniciya soruldu; kullanici "tarayiciyi hatirla" secenegini
  onayladi, kapatma denemesi tamamen geri alindi.
- `[RISK]` Kredi taksit odemesi ve cek tahsilati, kredi vereni/cariyi temsil eden
  Party'ye normal bir `Payment(Disbursement/Receipt)` yazar (ADR-004 modeli
  degistirilmedi, mevcut kod yeniden kullanildi). Bu Party'nin "cari bakiyesi" bu
  yuzden **kalan kredi borcunu göstermez** — kalan ana para daima
  `loan_installment` tablosundan okunur (`/api/loans` `remainingPrincipal`). Bu
  ayrim ADR-007'de (surum 2) netlestirilecek; simdilik ekranlarda kredi vereni
  normal bir cari gibi listelemekten kacinilmali.

`[DOGRU]` 26.09.2026 tespiti: `MigrationSecurity.Apply()` paylasilan/degisken bir
tablo listesi kullaniyordu; ikinci migration'a yeni tablolar eklenince, **sifir bir
veritabaninda** ilk migration calisirken henuz var olmayan tabloya RLS uygulamaya
calisip hata veriyordu (mevcut veritabanlarinda gorunmuyordu, cunku onlarda ilk
migration zaten uygulanmisti). Sifir veritabaninda iki migration sirayla denenerek
yakalandi ve duzeltildi: her migration artik yalniz **kendi olusturdugu** tablolar
icin `MigrationSecurity.ApplyRls(...)`/`RevokeUpdateDelete(...)` cagirir; eski
migration'lardaki liste dondurulmustur, sonradan degistirilmez. Kural: yeni bir
migration eklerken `MigrationSecurity.Apply()` (tam liste) DEGIL,
`ApplySchemaGrants` + `ApplyRls(migrationBuilder, "<yeni tablolar>")` kullanilir.
Canliya alma oncesi her migration zinciri **sifir bir veritabaninda** bastan
denenmeden guvenilir sayilmaz (operasyon proseduru Kapi C/D ile uyumlu).

### 5.7 Surum 0.1'in bilinen sinirlari

`[ACIK]` Asagidakiler bilerek sonraya birakildi:

- Diger bilgisayarlardan (yerel ag) erisim kapali; acmak icin ic sertifika otoritesiyle
  HTTPS ve `AllowedHosts` ayari gerekir (guvenlik standardi bolum 6, 13).
- Canli paket (publish, manifest, SHA256, kurulum/geri alma betikleri) yok. Yedek ve geri
  yukleme provasi betikleri var (`scripts/backup.ps1`, `scripts/restore-test.ps1`) ama
  zamanlanmis gorev kurulmadi (yedek konumu ve parola saklama karari bekliyor).
- Stopajli belgeler (serbest meslek, kira), aidat tahakkuku, e-belge entegratoru,
  GMS.NET aktarimi, KDV simulasyonu ve odeme takvimi raporlari yok (ADR-005).
- Web arayuzunde `oxlint` uslup uyarilari var (form sifirlama efektleri); islevi etkilemez.
- Tek kiracili ilk surum: kurulum ilk kiraciyi kullanir; coklu kiraci yonetimi yok.

### 5.4 Git durumu

- Git deposu olusturuldu; ana dal `master`.
- Ilk yerel commit 25 Eylul 2026'da denetim devri icin olusturuldu.
- Uzak depo GitHub'da tanimli: `ensardemirr/UUSport`. Son commit `6f799d3`
  (04.10.2026, M0 + M1). Baska RDP'ye tasima bu depodan klonlayarak yapilir.
- `.env`, anahtarlar, sertifikalar, yedekler, loglar, kurulum dosyalari ve yerel
  Obsidian ayarlari Git disinda tutulur.

## 6. Mevcut Klasor Yapisi

```text
C:\UUSport
|-- AGENTS.md
|-- CLAUDE.md
|-- README.md
|-- UUSport.slnx           # .NET cozumu
|-- dotnet-tools.json      # yerel arac (dotnet-ef) surumu
|-- scripts/              # ci, yedek, geri yukleme provasi, git hook betikleri
|-- database/              # bos; migration'lar API projesinde (EF Core)
|-- deployment/            # test/canli dagitim tanimlari; su an bos
|-- docs/
|   |-- decisions/         # ADR-001..005, ADR-012 (onayli)
|   |-- designs/           # ekran, akis ve tasarim notlari, mockup
|   |-- operations/        # veri kabul, release, canliya alma, yedek ve geri donus
|   |-- project-memory/
|   |   `-- UUSPORT_ANA_HAFIZA.md
|   |-- reviews/           # denetim istegi ve denetim raporu
|   |-- standards/         # PostgreSQL, guvenlik standartlari
|   `-- source/            # guvenli calisma kaynaklari; gercek veri konmaz
|-- src/
|   |-- UUSport.Api/       # ASP.NET Core: API + web arayuzunu sunar
|   |   |-- Data/          # DbContext, RLS, migration'lar
|   |   |-- Domain/        # varliklar
|   |   |-- Features/      # is alanina gore: Accounting, Parties, Catalog, Imports...
|   |   `-- Infrastructure/  # para, Turkce metin, DPAPI, guvenlik ara katmanlari
|   |-- UUSport.Web/       # React + TypeScript; derleme ciktisi Api/wwwroot'a gider
|   `-- UUSport.Mobile/    # bos; Faz 2 (React Native / Expo)
|-- tests/UUSport.Tests/   # xUnit; gercek PostgreSQL + uygulama rolu
|-- tools/                 # kurulum, derleme, baslatma, kisayol betikleri
|   `-- installers/        # Git disi kurulum dosyalari
`-- uu sport ltd/          # Git DISI kaynak dosyalar (denetim K2: repo disina alinmali)
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

`[KARAR]` Para tipi, yuvarlama ve KDV kurali ADR-002'de; harmanlama ve Turkce metin
kurallari ADR-003'te baglayicidir. Spesifikasyon bolum 14'teki `bigint` kurus kurali
ADR-002 ile gecersizdir. `[DOGRU]` Bu kararlar standart belgesine islendi (bolum para ve
harmanlama).

`[DOGRU]` Migration araci EF Core secildi (bolum 5.6). Migration'lar
`src/UUSport.Api/Data/Migrations` altindadir; su an 4 migration var (InitialSchema,
AddLoansChecksAndAccountCodes, FixAppendOnlyGrants, M1SecurityHardening).

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

1. ~~Teknoloji yigini icin ADR hazirla ve onayla.~~ Tamamlandi: ADR-001, 26.09.2026.
2. ~~MVP kapsami~~ Tamamlandi: ADR-005.
3. ~~Sema, migration, uygulama iskeleti ve surum 0.1~~ Tamamlandi: 26.09.2026 (bolum 5.5).
4. ~~Kullanici surum 0.1'i sentetik veriyle dener~~ Deneme yapildi; geri bildirimler
   islendi. Dis danisma sentezindeki M0 tamamlandi, M1 buyuk olcude tamamlandi.
   M1 kalani: S-16 (gercek Excel'in proje disina tasinmasi, kullanici eylemi), S-18
   (Windows servisi + release paketleme), S-11 kalani (HTTPS sertifikasi, guvenlik
   duvari, BitLocker), yedek zamanlanmis gorevinin kurulmasi. Sonra M2 (S-20..S-27).
   Kullanici kararlari KU-1..KU-7 bekliyor (sentez bolum 6).
   `[KARAR]` **M1.5 coklu sirket** (ADR-012, 17.10.2026) M2'den once yapilir: migration
   (tenant kolonlari, uyelik, grup ici baglanti, tedarikci kodu, RLS v2), sirket baglami
   ara katmani, uyelik/yetki uclari ve ekranlari, sirket secici, raporlarda sirket boyutu,
   capraz sirket testleri. Tahmini 7-9 ajan gunu; M1 kalanlariyla paralel yurutulebilir.
5. `[KULLANICI KARARI]` 26.09.2026: **yedekleme erken alinacak**, sona birakilmayacak.
   PostgreSQL gelistirme kumesi icin gunluk tam yedek + gercek geri yukleme provasi
   (operasyon proseduru bolum 10) gercek veri gelmeden, sentetik veriyle simdi
   kurulur ve test edilir. HTTPS/guvenlik duvari ile ayni asamaya (coklu bilgisayar
   erisimi) bagli DEGILDIR; tek bilgisayarda da yapilabilir ve yapilmalidir.
6. Gercek `Sayfa1 (2)` dosyasi kullanici tarafindan, kendi makinesinde aktarilir
   (gercek veri yapay zeka araclarina verilmez). Once kaynak dosyalar repo disina alinir (K2).
7. Mali musavirle acik konular: stopajli belgeler, fatura suresi, acilis bakiyesi kaynagi.
8. Coklu bilgisayardan erisim acilirken: yerel ag HTTPS (ic sertifika otoritesi) ve
   gerekirse guvenlik duvari kurali (guvenlik standardi bolum 6, 13).
9. Canli paket (publish + manifest + SHA256 + kurulum/geri alma betikleri), temiz
   makinede prova.
10. Canli ortam belli oldugunda ortam envanteri, kurulum ve release planini doldur.

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
| 17.10.2026 | 3.3 | Claude Code | **M1.5 MultiCompanyTests yazildi** (`tests/UUSport.Tests/MultiCompanyTests.cs`, 9 test; toplam 81 test, `tools/build-uusport.ps1` yesil). Testler iki **gercek hata** yakaladi ve duzeltildi: denetim kaydi (`audit.audit_event`, RLS tekil yazma sirketi) baska sirketin kimligiyle yazilinca 500 veriyordu; bu yuzden ev sirketi ilk sirket (UUS) olmayan kullanici giris yapamiyor, grup yoneticisi baska sirkette uyelik veremiyordu. Cozum: `AuditSaveExtensions.SaveChangesAcrossCompaniesAsync` (ayni transaction'da yazma baglamini hedef sirkete cevirir; is kaydi ve denetim birlikte kalir); `UserEndpoints` ve `AuthEndpoints` (LogAuthAsync, MFA) kullanir. `EndpointAuthorizationTests` ile `MultiCompanyTests` ortam degiskeni/kurulum belirteci paylastigi icin `WebHost` koleksiyonunda seri kosar. Ayrinti: devir notu bolum 2.6. |
| 17.10.2026 | 3.2 | Claude Code | **M1.5 uygulamasina baslandi** (dal `claude/uusport-durumu-102mgc`): migration `20261017050655_M15MultiCompany` (tenant kolonlari, `identity.company_membership`, `is_group_admin`, `related_tenant_id`, `supplier_item_code`, RLS v2 okuma-kume/yazma-tekil, veri tasima: ilk kiraci UUS, roller uyelige, kod on ekleri); `CompanyContextMiddleware` + `CompanyAccess` + yetki anahtarlari (`Permissions`), politikalar Staff/Admin/GroupAdmin/perm:*; `/api/companies`, kullanici uclari uyelik modeliyle yeniden; `/api/auth/me` sirket listesi; kod on eki (cari, urun, Excel). Linux bulut oturumunda .NET 10 + PostgreSQL 16 ile **72/72 test gecti**; gizli bilgi icin ortam degiskeni gecisi (yalniz Windows disi test). Web arayuzu, MultiCompanyTests, grup ici islemler ve belgeler HENUZ YOK. Devir notu: `docs/project-memory/M15_COKLU_SIRKET_DEVIR_NOTU_2026-10-17.md` (bir sonraki oturum once bunu okur). |
| 17.10.2026 | 3.1 | Claude Code | `[KULLANICI KARARI]` Coklu sirket Secenek A onaylandi, KU-8..KU-15 yanitlandi; **ADR-012** yazildi (`docs/decisions/ADR-012_COKLU_SIRKET_VE_KIRACI_MODELI.md`): sirketler UUSPORT/FITNESS/TIYATRO; sirket = kiraci; sirket basina rol + Ortak rolu parametrik gorunurluk; birlesik okuma / tekil yazma RLS; grup ici islemler onay kuyrugu ve `intercompany_link`; ayni veli ayri cari, fazla tahsilat `approved_adjustment` ile aktarim; musavir aktarim profili sirket basina; kodlarda `UUS-/FIT-/TIY-` on eki ve `supplier_item_code` esleme tablosu. Yol haritasina M1.5 eklendi. Kod degisikligi yok. |
| 17.10.2026 | 3.0 | Claude Code | Kullanici istegiyle coklu sirket (uc VKN) yapi calismasi yazildi: `docs/designs/COKLU_SIRKET_YAPI_CALISMASI_2026-10-17.md`. Mevcut kod incelendi (tek kiraci, RLS tum tablolarda, kullanici basina tek rol, capraz kiraci testi yok). Uc secenek degerlendirildi; `[ONERI]` Secenek A: sirket = kiraci, `identity.company_membership` ile sirket basina rol, okuma icin `app.tenant_ids` (ANY) ve yazma icin tekil `app.tenant_id` RLS politikasi, ust cubukta sirket secici, birlesik modda yazma kapali, raporlarda sirket boyutu, yeni "Ortak" rolu, yetki anahtarlari ile tek matris. M2'den once "M1.5" onerildi; KU-8..KU-15 sorulari acildi. Kod degisikligi yok. |
| 16.10.2026 | 2.9 | Claude Code | Eskimis ifadeler duzeltildi: durum satiri ve hizli durum M0/M1 ilerlemesiyle guncellendi; uzak depo (GitHub) ve son commit kaydedildi; 25.09 denetim arasi tarihsel olarak isaretlendi; test sayisi 31 -> 72; yedek betiklerinin varligi bilinen sinirlara islendi; migration araci `[ACIK]` -> `[DOGRU]` EF Core (4 migration); ADR-002/003'un standarda islendigi dogrulandi; siradaki calisma sirasi M1 kalanlari ve M2 ile guncellendi. Kod degisikligi yok. |
| 27.09.2026 | 2.8 | Claude Code | `[KULLANICI KARARI]` M1'e gecildi (Sonnet 5, orta efor). **S-12** kurulum ucu tek kullanimlik jetonla kilitlendi (`SetupTokenService`, sunucu logunda bir kez gorunur). **S-13** giris/cikis/kilit/MFA basari-basarisiz olaylari IP ile denetim kaydina yaziliyor (`AuditEvent.IpAddress`, yeni sutun). **S-14** `EndpointAuthorizationTests`: calisan uygulamada `EndpointDataSource`'tan TUM `/api` uclarini kesfedip oturumsuz istekte 401, Muhasebe rolunde yonetici-ozel uclarda 403 dogrular; elle bir admin-only korumasi silinerek testin gercekten kirmizi oldugu gosterildi (append-only testiyle ayni disiplin). **S-15** MFA sertlestirme: TOTP anahtari/kurtarma kodlari artik Data Protection ile sifreli saklaniyor (eski duz metin de okunabilir kalir, sonraki yazimda sifrelenir); kurtarma kodlari (10 adet) MFA acilinca gosteriliyor, giriste kurtarma kodu secenegi var; sunucu CLI'sinda `reset-mfa --user=` acil durum sifirlamasi (yalniz makine erisimi olanlar); oturum hareketsizlik zaman asimi 8 saatten 30 dakikaya cekildi (kaydirmali); tek yonetici uyarisi (`/api/auth/me.onlyAdmin`, arayuzde banner); anonim `/api/health` artik surum/migration bilgisi vermiyor. **S-19** Excel aktariminda telefon yoksa/eslesmezse artik SADECE isme bakarak mevcut cariye otomatik birlesme YAPILMAZ (X:DATA-01, iki test ile kanitlandi); zip bomb korumasi (sikistirilmamis boyut sinirlanir, acmadan once kontrol edilir). **S-11** kismi: HSTS eklendi (duz HTTP'de zararsiz, HTTPS acilinca aktif). **S-17** `scripts/ci.ps1` uygulandi ve calistirildi: derleme (uyari=hata), tum testler, gizli bilgi taramasi, gercek veri taramasi, NuGet/npm acik taramasi; `-Fast` bayragi + `scripts/install-git-hooks.ps1` (commit-oncesi, elle kurulur). **S-10** `scripts/backup.ps1` + `scripts/restore-test.ps1` yazildi VE uctan uca calistirilarak dogrulandi (dogru parolada basarili, yanlis parolada temiz basarisizlik): parola tabanli AES-256 (DPAPI DEGIL - felaket kurtarma icin sunucudan bagimsiz olmali), `postgres` superuser ile (uusport_owner FORCE RLS altinda kendi verisini bile goremez). Zamanlanmis gorev kurulumu (`install-scheduled-tasks.ps1`) YAZILDI ama calistirilmadi - yurt ici yedek konumu ve parola saklama karari kullanicidan bekliyor. 72 test geciyor. **Ertelenen (henuz baslanmadi):** S-16 (gercek Excel dosyasinin proje disina tasinmasi - kullanici eylemi), S-18 (Windows servisi + release paketleme betikleri), S-11'in kalani (fiziksel HTTPS sertifikasi, guvenlik duvari, BitLocker - sunucu donanimi belli olunca). |
| 27.09.2026 | 2.7 | Claude Code | `[KULLANICI KARARI]` Sentez plani onaylandi, M0'dan baslandi; "Resmi Cari" dugmesi **"Muhasebe Gorunumu"** oldu, "Gayri resmi" metni kaldirildi ("Belgelendi / Belge bekliyor"); gelistirme veritabanindaki deneme kayitlari kullanici beyaniyla tamamen uydurma. **M0 tamamlandi (S-01..S-08):** `20260927143530_FixAppendOnlyGrants` migration'i append-only yetki hatasini onardi (uusport, uusport_test uygulandi); `SchemaGuardTests` her kosuda sifir veritabani kurup RLS/FORCE/politika ve append-only yetkilerini katalogdan dogrular (duzeltme kapaliyken kirmizi oldugu gosterildi); finansal komutlar tek transaction + `FOR UPDATE` kilidiyle (kilitsizken es zamanli testler 3/3 kirmizi, kilitle yesil); iade sonrasi acik tutar net tutardan, fazla dagitim avansa doner; iptal edilen odemeye bagli kredi taksiti yeniden acilir; kredi/cek gerceklesme tarihi vade degil islem tarihi; yon–arac–hesap matrisi (musteri kart tahsilati yalniz POS); tahsil edilmis ceke bagli tahsilat genel iptalle geri alinamaz; koyu+yuksek kontrast duzeltildi; arayuzden gelistirici dili kaldirildi; muhasebe gorunumunde tutarsiz yuruyen bakiye gizlendi (kalici cozum M2/S-21); surum degisince "Yenile" bandi (`X-App-Build`, `wwwroot/version.json`). 69 test geciyor; ekran goruntusu ve PDF ile dogrulandi. Siradaki: M1 (yedek, HTTPS, kurulum jetonu, giris denetimi, CI...). |
| 27.09.2026 | 2.6 | Claude Code | Dis danisma raporlari geldi (Claude: `UUSPORT_DANISMA_CLAUDE_2026-09-27.md`, Codex/GPT: `UUSPORT_DANISMA_CODEX_2026-09-27.md`) ve degerlendirildi: `docs/reviews/UUSPORT_DANISMA_SENTEZ_2026-09-27.md` (`[ONERI]`, birlesik is listesi M0-M8, ADR adaylari 006-017, KU/MM/AV sorulari). **`[RISK]` dogrulanan P0 hata:** sifirdan kurulan veritabaninda `uusport_app` rolu append-only tablolarda (ledger_entry, stock_movement, audit_event, payment_allocation) UPDATE yetkisine sahip; 2. migration'daki `ApplySchemaGrants` yetkiyi geri veriyor. `uusport`/`uusport_test` etkilenmemis (duzeltme oncesi hali uygulanmisti); testler bu yuzden hatayi gizledi. Canli kurulumdan once S-01 ile duzeltilecek. Ayrica dogrulandi: iade sonrasi acik tutar hatasi, taksit odemesi iptalinde taksit odenmis kaliyor, koyu+yuksek kontrast temada ikincil metin 1,49:1. Ana hafizada "gercek veri korundu" ifadeleri "gelistirme veritabanindaki deneme kayitlari" olarak duzeltildi (Codex notu). |
| 27.09.2026 | 2.5 | Claude Code | Logo Tiger 3 arastirmasi yapildi (kredi karti Logo'da ayri banka hesabi turu; kural bazli muhasebe baglanti kodlari; 6 maliyet yontemi; GMS.NET'in C# API'si var). Kullanici istegiyle dis yapay zekalara (Claude Fable 5.1, GPT) verilecek strateji danisma promptu yazildi: `docs/reviews/DIS_YZ_STRATEJI_DANISMA_PROMPTU_2026-09-27.md` + sentetik veri ekran goruntuleri `docs/reviews/dis-yz-ekleri-2026-09-27/`. Ciktilar (`UUSPORT_DANISMA_<ARAC>_2026-09-27.md`) Claude Code'a verilip degerlendirilecek; o zamana kadar yeni ozellik baslatilmaz. Not: `CLAUDE.md`'deki "25.09 denetim icin ara" ifadesi guncel degil, kullanici onayiyla temizlenecek. |
| 26.09.2026 | 2.4 | Claude Code | Ekstre penceresi uzerinde ince ayar: "Ekstre Al" dugmesi cari sayfasinin ust islem cubugu­na tasindi; "Resmi Cari" tik kutusu artik yazdirma/PDF ciktisinda gorunmuyor (filtre durumu dipnotta yaziyor). Ayrica yeni bir sayfa eklendi: **Hesap Hareketleri** (`/hesap-hareketleri`, Gunluk is grubunda) — tum carilerin cari hareketlerini tek listede gosterir, cari/tarih araligi/"Resmi Cari" filtreleriyle. Backend'e `AccountingQueries.AllLedgerAsync` ve `GET /api/ledger` eklendi (mevcut `LedgerRow`/`IsOfficial` mantigi yeniden kullanildi, yeni tablo yok). 53 test hala geciyor; gelistirme surecine yuklendi, gelistirme veritabanindaki deneme kayitlari korundu. |
| 26.09.2026 | 2.3 | Claude Code | Kullanicinin deneme sirasindaki iki geri bildirimi islendi: sol menu daraltilabilir gruplara ayrildi; cari detayina "Ekstre Al" penceresi eklendi ("Resmi Cari" tik filtresi + ekranda gorundugu gibi yazdir/PDF, `LedgerRow.IsOfficial` hesabi, 5 yeni test). Ayrica "girisde MFA kodu kaldirilsin mi" sorusuna, guvenligi zayiflatmadan "tarayiciyi 30 gun hatirla" (`rememberClient: true`) ile cozum uretildi; MFA'yi tumden kapatan ilk deneme guvenlik siniflandiricisi tarafindan engellenip geri alindi. Gelistirme surecine yeniden derlenmis surum yuklendi, gelistirme veritabanindaki deneme kayitlari korundu. |
| 26.09.2026 | 2.2 | Claude Code | HTTPS ve guvenlik duvarinin ne zaman/nasil gerekecegi acildandi (kullanici sordu, kod degisikligi yok). Yedekleme kullanici karariyla oncelik sirasinda one alindi: coklu bilgisayar erisiminden bagimsiz, sentetik veriyle simdi kurulup test edilecek (bolum 11). |
| 26.09.2026 | 2.1 | Claude Code | Kullanicinin 4 geri bildirimi islendi (surum 2 hazirlik notu): cari/urun hesap kodu alani, kredi karti/kredi hesap hiyerarsisi, kredi taksit tablosu, cek portfoyu (seri giris dahil), Excel sablon indirme; karisik stok+hizmet fatura zaten calisiyordu. Sifir veritabaninda migration zincirini test ederken gercek bir hata bulundu ve duzeltildi: `MigrationSecurity` artik migration basina dondurulmus tablo listesi kullaniyor. 48 test + tam HTTP duman testi gecti; gelistirme veritabanindaki deneme kayitlari korunarak surum yeniden baslatildi. |
| 26.09.2026 | 2.0 | Claude Code | Surum 0.1 gelistirme ortaminda calisir hale getirildi: veritabani ve roller, EF Core migration, RLS, API, React arayuz, Excel aktarimi, 31 test, duman testi, masaustu kisayolu. Kullanicinin yetki devriyle verilen uygulama kararlari (5.6) ve bilinen sinirlar (5.7) kaydedildi; K3 duzeltildi (PostgreSQL gelistirme makinesinde calisiyor, `::1` eklendi); klasor agaci yenilendi (D1); migration araci EF Core. |
| 26.09.2026 | 1.9 | Claude Code | ADR-005 (ilk surum kapsami, elle fatura, alis/gider icerde, aidat disarida) ve guvenlik standardi onaylandi; bolum 0, 3.3 ve 4.2 guncellendi; alan adi olmadigi kaydedildi. |
| 26.09.2026 | 1.8 | Claude Code | ADR-002 (para/yuvarlama/KDV), ADR-003 (harmanlama), ADR-004 (cari hareket modeli, K1) onaylandi; bolum 0, 4.4 ve 8.3'e kayit ve baglanti eklendi. Standart, akis belgesi ve mockup guncellemeleri beklemede. |
| 26.09.2026 | 1.7 | Claude Code | ADR-001 onaylandi: ASP.NET Core API + PostgreSQL + React web + React Native (Expo) mobil, masaustu istemci yok; bolum 0 ve 4.1 guncellendi, spesifikasyon 2.1'in gecersiz kilindigi not edildi. Denetim raporu surum 2.0'a baglanti eklendi. |
| 25.09.2026 | 1.6 | Codex | Calisma denetim icin durduruldu; mevcut belge/uygulama durumu kaydedildi, Claude Code tam denetim promptu ve statik onizleme Obsidian/Git'e eklendi, kaynak dosyalar Git disina alindi. |
| 25.09.2026 | 1.5 | Codex | Canli ilk olarak mevcut normal Windows bilgisayara kurulur; sonra yeni kiralanacak canli RDP'ye tasinir. Gelistirme RDP'si ayri konumlanir. Ilk kurulum EXE/MSI ve betik teslim gereksinimi netlestirildi. |
| 24.09.2026 | 1.3 | Codex | Aylik KDV, 3/6/9/12 aylik vergi simulasyonu; kredi karti, kredi, maas, kira, muhtasar, SGK, toplam odeme ve nakit yeterlilik raporlari karar olarak eklendi. |
| 23.09.2026 | 1.2 | Codex | Maskeli/sentetik gelistirme modeli, Excel/PDF/resim kabul akisi, tek/iki sunucuya tasinabilir APP01-DB01 rolleri, muhasebe ekran kararlari, canliya alma proseduru ve PostgreSQL standardi eklendi. |
| 23.09.2026 | 1.1 | Codex | Apple Design Skill projenin ana UI/UX tasarim rehberi olarak kesin kararlara ve tasarim protokolune eklendi. |
| 23.09.2026 | 1.0 | Codex | Ilk ortak ana hafiza olusturuldu; kaynak incelemesi, ortam kurulumu, kararlar ve acik konular birlestirildi. |
