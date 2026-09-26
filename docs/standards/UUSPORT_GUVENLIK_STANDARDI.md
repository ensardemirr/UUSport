# UU Sport Guvenlik Standardi

**Durum:** `[KARAR]` Kullanici tarafindan 26 Eylul 2026 tarihinde onaylandi
**Tarih:** 26 Eylul 2026
**Hazirlayan:** Claude Code (Opus 5.5)
**Kaynaklar:** Spesifikasyon surum 0.11 bolum 2.3, 3, 9 (O73-O87); ADR-001..005; denetim
raporu surum 2.0; ENS_Stp.App teknik tuzaklari ve guvenlik kurallari
**Iliskili:** `docs/operations/UUSPORT_GELISTIRME_VERI_VE_CANLIYA_ALMA_PROSEDURU.md`,
`docs/standards/UUSPORT_POSTGRESQL_ADLANDIRMA_VE_MIGRATION_STANDARDI.md`

Bu belge daginik haldeki guvenlik kurallarini tek yerde toplar. Operasyon proseduru ve
PostgreSQL standardi ile celisirse bu belge **guvenlik** konularinda esastir; celiski ayni
calismada giderilir.

Her maddenin yaninda hangi fazda zorunlu oldugu yazar: **F1** (ofis bilgisayari, yerel ag),
**F2** (RDP, internet, personel mobil), **F3** (veli).

---

## 1. Temel ilkeler

1. **Ekranda gizlemek yetki degildir.** Her yetki API'de ve PostgreSQL RLS'te uygulanir (O73).
2. **Varsayilan reddet.** Acikca izin verilmeyen her sey yasaktir: API ucu, RLS politikasi,
   ag portu, dosya erisimi.
3. **En az yetki.** Kullanici, servis hesabi ve veritabani rolu yalniz isini yapacak kadar yetki alir.
4. **Istemci veritabanina baglanmaz.** Web ve mobil yalniz HTTPS API ile konusur.
   (ENS dersi: istemciden dogrudan SQL baglantisi, biriken basarisiz girisimler yuzunden
   sunucunun IP'yi bloke etmesine ve Logo dahil tum sistemin dusmesine yol acti.)
5. **Gercek kisisel veri gelistirme, test ve yapay zeka araclarina girmez.** Gelistirme ve
   test yalniz sentetik veya maskeli veriyle yapilir.
6. **Kanit olmadan "guvenli" denmez.** Bir kuralin belgede yazmasi uygulandigi anlamina gelmez
   (ENS surec tuzagi); her kural bir testle veya kontrolle dogrulanir.

---

## 2. Kimlik dogrulama

### 2.1 Rollere gore yontem (O74)

| Rol | Yontem | Faz |
|---|---|---|
| Yonetici, muhasebe, IT | Parola + **iki adimli dogrulama zorunlu** (dogrulayici uygulama, TOTP) | F1 |
| Mali musavir (salt okunur, O11) | Parola + iki adimli dogrulama | Rol acildiginda |
| Hoca, yardimci antrenor, supervizor | Parola + cihaz kaydi; IT uzaktan oturum kapatabilir | F2 |
| Veli | Telefon numarasi + tek kullanimlik kod (SMS) | F3 |

**Parolasiz giris yoktur.** (ENS'te normal kullanicilarin sifresiz girisi bilincli
tasarimdi; UUSport'ta cocuk verisi ve O74 nedeniyle **yasaktir**.)

### 2.2 Parola ve oturum

- Parolalar yalniz guclu, tuzlu, yavas bir hash algoritmasiyla saklanir (ASP.NET Core
  Identity varsayilani). Duz metin veya geri cozulebilir sifreleme yasaktir.
- En az 12 karakter; sizdirilmis parola listesine karsi kontrol onerilir.
- Ard arda basarisiz giriste gecici kilit; basarisiz girisimler denetim kaydina yazilir ve
  esik asilinca IT'ye uyari gider.
- Web oturumu: `HttpOnly`, `Secure`, `SameSite` cerez; hareketsizlikte zaman asimi.
- Mobil (F2): kisa omurlu erisim belirteci + cihaza bagli yenileme belirteci; cihaz iptal
  edilince yenileme belirteci gecersiz olur.
- **Personel ayrilisinda tek islemle tum erisim kapanir** (O74, O84): kullanici pasif,
  oturumlar ve cihazlar iptal, belirtecler gecersiz.

### 2.3 Ilk kurulum

- Ilk yonetici hesabi kurulum aninda olusturulur; varsayilan veya ortak parola yoktur.
- Kurulum betigi parolayi komut satirindan almaz (denetim Y2); guvenli istemle alir.

---

## 3. Yetkilendirme

- Rol-yetki matrisi tek kaynaktan tanimlanir ve API, RLS ve arayuz ayni matristen beslenir.
  (ENS "uc yer kurali" dersi: yetki uc ayri yerde tanimlaninca biri unutuldu ve hotfix
  gerekti. Yeni ekran/yetki eklemenin kontrol listesi kod standardinda tutulur.)
- **Her API ucunun yetki testi zorunludur** (O86). Test yoksa uc birlestirilmez.
- Is kurali sinirlari (iskonto yuzdesi, eksi stok, faturasiz teslim yetkisi) **sunucuda**
  kontrol edilir; arayuzdeki kontrol yalniz kolayliktir.
- Antrenorun sporcuya erisimi grup uyeliginden turetilir (O16); veli yalniz kendi
  cocuklarini ve kendi carisini gorur. Bu kurallar RLS'te uygulanir.
- **Antrenor ve cocuk ekraninda borc bilgisi gosterilmez** (O27). API bu alani bu rollere
  hic dondurmez.

---

## 4. Veritabani

### 4.1 Roller

| Rol | Yetki | Kullanan |
|---|---|---|
| `postgres` (superuser) | Tam | Yalniz kurulum ve acil mudahale; **uygulama asla kullanmaz** |
| Migration rolu | DDL | Yalniz migration paketi |
| Uygulama rolu | Yalniz gereken schemalarda `SELECT/INSERT/UPDATE`; **audit tablosunda yalniz `INSERT`**; `DELETE` yalniz acikca izin verilen tablolarda | API calisma zamani |
| Rapor rolu (istege bagli) | Salt okunur | Raporlar, musavir paketi |

### 4.2 Kurallar

- Tenant verisi olan her tabloda RLS **acik ve zorlanmis** (`FORCE ROW LEVEL SECURITY`);
  politika varsayilan reddettir. Tenant ve kullanici baglami her transaction basinda
  ayarlanir (standart bolum 7).
- RLS negatif testleri her surumde calisir: kiraci baska kiracinin, veli baska cocugun,
  antrenor baska grubun verisini **okuyamamali** (O86).
- `public` schema'da uygulama nesnesi yoktur; `public` uzerindeki varsayilan yetkiler kaldirilir.
- Baglanti yalniz `localhost` veya ozel ag/VPN; `pg_hba.conf` yalniz bu adreslere ve
  `scram-sha-256`'ya izin verir. **5432 hicbir fazda internete acilmaz.**
- Kurulum betigi `listen_addresses` ve `pg_hba.conf` degerlerini yazar **ve** her
  calistirmada dogrular (denetim O3).
- Veri dizini ve gizli bilgi klasoru icin miras kesilir, yalniz SYSTEM, Administrators ve
  servis hesabi birakilir (denetim O4-YENI). Canli ortamda zorunlu; gelistirmede sandbox
  hesaplarinin okuma hakki bilincli karar olarak belgelenir.

---

## 5. Gizli bilgiler

- Parola, baglanti metni, API anahtari, sertifika ozel anahtari **Git'e, ana hafizaya,
  belgeye, loga ve komut satirina** yazilmaz.
- Gelistirme: Windows DPAPI (kullaniciya bagli).
- Canli: makineye bagli DPAPI veya sirket kontrolundeki bir gizli bilgi deposu; secim canli
  ortam kurulumunda karara baglanir.
- Gizli bilgi dosyalarinin erisim haklari kurulumdan sonra dogrulanir.
  (ENS dersi: ayar kaydi dosya haklarini degistirip kullanicilari kilitledi; her kayittan
  sonra hak kontrolu gerekti.)
- Parola yenileme: kisinin ayrilmasi, sizinti suphesi veya parolanin komut satirina
  dustugu durumlarda derhal. Gelistirme `postgres` parolasi denetim Y2 nedeniyle yenilenir.
- Commit oncesi gizli bilgi taramasi calisir (O86).

---

## 6. Ag ve TLS

| Konu | F1 (ofis, yerel ag) | F2/F3 (RDP, internet) |
|---|---|---|
| Disariya acik port | **Yok** | Yalniz **443** |
| HTTPS | **Zorunlu** (parola Wi-Fi'da acik gitmez; mobil tarayici fis cekimi icin gerekir) | Zorunlu, TLS 1.2+, HSTS |
| Sertifika | Bolum 11 acik konu 1 | Genel gecerli sertifika, otomatik yenileme |
| Yonetim erisimi (RDP) | Yerel | VPN veya RD Gateway + MFA arkasinda; dogrudan RDP portu internete acilmaz |
| Barindirma yeri | Ofis | **Turkiye'de** (O5, O72; denetim Y4) |

Ofis bilgisayari F1'de internete **acilmaz** (ADR-001). Port yonlendirme yapilmaz.

---

## 7. Uygulama guvenligi

- **Parametreli sorgu zorunlu**; SQL metni birlestirilerek kurulmaz.
- Girdi dogrulamasi **sunucuda** yapilir; istemci dogrulamasi kolayliktir.
- Cerez tabanli oturumda CSRF korumasi; CORS yalniz kendi alan adlarina acik.
- Giris, parola sifirlama, OTP ve dosya yukleme uclarinda hiz siniri.
- Guvenlik basliklari (icerik guvenlik politikasi, cerceve engeli, MIME koklama engeli).
- **Hata mesaji kullaniciya kisa ve anlasilir, ayrinti loga** (ENS kurali 13). Yigin izi,
  SQL metni, dosya yolu kullaniciya donmez.
- Bagimliliklar surumlu ve kilitli; her surumde acik taramasi (O86).

### 7.1 Dosya yukleme (fis, fatura, belge)

- Tur, uzantiya degil **icerige** bakilarak kontrol edilir; izinli turler: JPEG, PNG, HEIC, PDF.
- Boyut siniri; asan dosya reddedilir.
- Dosyalar **web kokunun disinda** saklanir, dosya adi sunucuda uretilir (kullanicinin
  verdigi ad yol olarak kullanilmaz).
- Erisim yalniz API uzerinden, yetki kontroluyle; paylasim gerekiyorsa kisa sureli imzali
  baglanti (spesifikasyon 9.1).
- Fis ve fatura goruntuleri **sirket disina cikmaz**; OCR kendi sunucumuzda yapilir (ADR-001).

---

## 8. Kisisel veri ve KVKK

### 8.1 Veri siniflari

| Sinif | Ornek | Kural |
|---|---|---|
| Kimlik | Ad, soyad, dogum tarihi | Rol bazli erisim |
| Kimlik numarasi | TCKN | **Alan bazinda sifreli** (spesifikasyon 4.2); listede maskeli (`123******90`) |
| Iletisim | Telefon, e-posta, adres | Rol bazli; loglarda maskeli |
| **Cocuk verisi** | Sporcu kaydi, gelisim | Veli yalniz kendi cocugu, antrenor yalniz kendi grubu |
| **Ozel nitelikli** | Saglik notlari | **Modul kapali** — bagimsiz guvenlik testi yapilana kadar acilmaz (O87); acildiginda alan bazinda ek sifreleme ve **okuma dahil** denetim kaydi |
| Finansal | Cari, tahsilat, kart | Kart numarasi **hic saklanmaz** (O25) |

### 8.2 Kurallar

- Gelistirme, test ve yapay zeka araclarinda gercek veri yoktur. Canli veri gelistirmeye
  indirilmez (ENS bunu yapiyordu; UUSport'ta yasaktir).
- Loglarda kisisel veri maskelenir; denetim kaydina gereksiz kisisel veri yazilmaz
  (standart bolum 8).
- Saklama ve imha sureleri avukatla belirlenir ve otomatik islenir (spesifikasyon 9.6).
- Riza kayitlari (kim, ne zaman, hangi metin surumu) F3 oncesi hazir olur.
- Push bildirimlerinin metni kisisel veri tasimaz (ADR-001 acik konu 6).

---

## 9. Denetim kaydi

- Append-only: uygulama rolunun audit tablosunda `UPDATE` ve `DELETE` hakki **yoktur**;
  bu hak veritabani duzeyinde kaldirilir, yalniz uygulama koduna guvenilmez.
- Kaydedilenler: finans ve stok hareketleri, fatura girisi ve iptali, yetki degisiklikleri,
  giris ve basarisiz giris, disa aktarimlar, Excel aktarimlari, donem kilidi acma, ayar
  degisiklikleri; saglik modulu acildiginda **okuma dahil** saglik verisi erisimi (O76).
- Her kayit: kiraci, kullanici, zaman (UTC), eylem, hedef tur/id, correlation id,
  once/sonra ozeti.
- Uygulama logu ile denetim kaydi ayri seylerdir; denetim kaydi is kanitidir.

---

## 10. Gelistirme ortami ve yapay zeka ajanlari

- Claude Code ve Codex **canli ortama dogrudan baglanmaz**. Canli bilgisi yalniz kullanicinin
  verdigi cikti veya kurulum raporuyla gelir.
- Canli surum iddiasi yalniz canlidaki surum dosyasi veya kurulum raporuyla yazilir; rapor
  yoklugu "kurulmadi" demek degildir (ENS dersi; denetim K3 hatasi).
- Kaynak Excel/PDF dosyalari repo ve Obsidian kasasi disinda durur (denetim K2).
- `.gitignore` yol bazli kurallara ek olarak uzanti bazli savunma icerir (`*.xlsx`, `*.xls`,
  `*.pdf` izinli klasorler disinda, `*.pem`, `*.p12`, `*.pfx`, `*.key`) ve commit oncesi
  buyuk/izlenmeyen dosya taramasi yapilir (denetim D2; ENS'te 723 MB canli musteri verisi
  neredeyse commit'e giriyordu).
- Riskli komutlarin (silme, tasima, SQL yazma, publish, kurulum) onay kurallari `AGENTS.md`'de
  tutulur.

---

## 11. Surum guvenlik tabani (O86)

Her surumde otomatik calisir; **biri basarisizsa surum durur**:

1. Yetki testleri: API ve RLS negatif testleri (bolum 3, 4.2)
2. Bagimlilik acigi taramasi
3. Kodda gizli bilgi taramasi
4. Finans regresyonu: ADR-004 senaryolari (S0-S10) ve mutabakat kontrolleri; kapanmis
   donemler yeniden hesaplandiginda sonuc degismemeli
5. Kulture bagli metin cagrisi analizi (ADR-003) ve parametresiz yuvarlama analizi (ADR-002)

---

## 12. Olay mudahalesi

- Veri ihlali mudahale plani avukatla hazirlanir (spesifikasyon bolum 12); en az: tespit,
  sinirlama, etki analizi, kanit koruma, KVK Kurulu'na ve ilgili kisilere bildirim,
  kok neden ve kalici onlem.
- Bildirim sureleri ve yukumlulukleri avukat tarafindan teyit edilir.
- Olaydan cikan ders teknik tuzak kaydina eklenir (ENS kurali: canli olayda bulunan hata
  yalniz sohbet notunda kalmaz).

---

## 13. Acik konular

| # | Konu | Kimden | Oneri |
|---|---|---|---|
| 1 | F1'de yerel ag HTTPS sertifikasi | Kullanici | `[KULLANICI KARARI]` Sirketin alan adi yok (26.09.2026). F1 icin **kendi ic sertifika otoritesi**: kok sertifika ofisteki bilgisayarlara ve muhasebecinin telefonuna bir kez yuklenir. F2 icin (internet, magaza uygulamalari) **sirket adina bir alan adi alinmasi zorunludur** (O83); RDP kiralamadan once alinmasi onerilir |
| 2 | Canlida gizli bilgi deposu | Kurulum sirasinda | Makineye bagli DPAPI veya ayri depo |
| 3 | Veli OTP icin SMS saglayicisi | F3 oncesi | Yurt ici saglayici, sirket adina hesap (O83) |
| 4 | KVKK metinleri, riza, saklama/imha sureleri, ihlal plani | Avukat | Spesifikasyon bolum 12 |
| 5 | VERBIS kaydi | Mali musavir / avukat | Spesifikasyon bolum 12 |
| 6 | Gelistirme makinesinde sandbox hesaplarinin veri klasoru okuma hakki | Kullanici | Bolum 4.2; bilincli kabul veya kaldirma |

---

## 14. Onay

- [x] Kullanici onayi - tarih: 26 Eylul 2026

**Onaydan sonra:** durum `[KARAR]` yapilir; ana hafiza bolum 3.3 bu belgeye baglanir;
`AGENTS.md` ve operasyon proseduru bu belgeye atif verir; celisen maddeler ayni calismada
giderilir.
