# ADR-001 Teknoloji Yigini ve Istemci Mimarisi

**Durum:** `[KARAR]` Kullanici tarafindan 26 Eylul 2026 tarihinde onaylandi
**Tarih:** 26 Eylul 2026
**Hazirlayan:** Claude Code (Opus 5.5), kullaniciyla 25-26 Eylul 2026 gorusmelerine dayanarak
**Karar veren:** Kullanici
**Iliskili:** `docs/reviews/CLAUDE_CODE_UUSPORT_DENETIM_RAPORU_2026-09-25.md` (Y6, Y3, Y4, Y7, O2)
**Gecersiz kildigi:** Surum 0.11 spesifikasyonu bolum 2.1'deki Node.js + TypeScript
arka uc tercihi ve ana hafiza bolum 4.1'deki `[ACIK]` teknoloji yigini maddesi

---

## 1. Baglam

### 1.1 Kullanicinin urun vizyonu

`[KULLANICI KARARI]` 26 Eylul 2026 gorusmesinde belirtildi:

- Program **web tabanli** olacak, tarayicidan kullanilacak. ENS_Stp.App'teki gibi masaustu
  (WinForms) ekranlari olmayacak.
- **Faz 1:** Canli, ofisteki normal Windows bilgisayarda calisir. Muhasebe ve yonetici
  tarayicidan kullanir.
- **Faz 2:** Canli, kiralanacak RDP'ye tasinir. Hocalar ve yoneticiler icin App Store ve
  Google Play'den indirilen **gercek mobil uygulama** yapilir. Muhasebeci alis faturasinin
  veya fisin fotografini cekip programa isleyebilir.
- **Faz 3:** Veliler acilir. Veli cocugunun gelisimini ve carisini gorur.
- Mobil uygulamada surum gecisi icin **havadan (OTA) guncelleme** istenir.
- Fis ve fatura goruntuleri **sirket disina cikmayacak**.
- Veritabani **PostgreSQL** olacak (onceki kullanici karari).

### 1.2 Kisitlar

- Spesifikasyon: yurt ici barindirma zorunlu (O5, O72); istemciler veritabanina dogrudan
  baglanmaz; yetki API ve RLS katmaninda uygulanir (O73); rol bazli kimlik dogrulama (O74);
  tum hesaplar sirket adina acilir (O83); sistem devredilebilir olmali (O83-O85).
- Tek gelistirici + yapay zeka ajanlari ile calisilir; maliyet hassasiyeti yuksektir.
- Gelistirme ortami Windows'tur (`BAKON-DEV01`). Mac bulunup bulunmadigi bilinmiyor.
- Personelin iPhone / Android dagilimi bilinmiyor.

### 1.3 ENS_Stp.App deneyiminden cikan iki ders

ENS_Stp.App bir .NET + WinForms + SQL Server uygulamasidir ve Haziran 2026'dan bu yana
`0.1.50` surumune gelmistir. Belgeleri (`C:\ENS_Stp.App\Docs\Wiki\Kural\`) iki ayri ders verir:

1. **.NET birikimi korunmalidir.** Windows uzerinde .NET derleme, yayinlama, SHA256 ile
   dogrulama, PowerShell kurulum betikleri ve geri alma konusunda bedeli odenmis bir deneyim
   ve seksenin uzerinde maddelik bir teknik tuzak kaydi vardir.
2. **Masaustu istemci modeli tekrarlanmamalidir.** ENS'teki olaylarin buyuk bolumu bu modelden
   dogmustur: tek ornek kilidi olmadigi icin canlida bes ornek; dagitimda dosyalarin kilitli
   kalmasi ve kopyalamanin yarim kalmasi; dagitim icin uygulamanin tum kullanici oturumlarinda
   kapatilmasi zorunlulugu; istemciden veritabanina dogrudan baglanti yuzunden biriken basarisiz
   girisimler ve sunucunun IP'yi bloke ederek Logo dahil tum sistemi dusurmesi.

---

## 2. Degerlendirilen secenekler

### 2.1 Arka uc

| Secenek | Arti | Eksi | Sonuc |
|---|---|---|---|
| **ASP.NET Core (C#)** | `decimal` dilin parcasi, PostgreSQL `numeric` ile kayipsiz eslesir; ENS birikimi aktarilir; Windows servisi olarak kurulur; EF Core migration paketi runtime'dan ayri uretilebilir; Npgsql ile RLS sorunsuz | Web ve mobil TypeScript oldugu icin projede iki dil bulunur | **Secildi** |
| Node.js + TypeScript | Tum katmanlarda tek dil | JavaScript'te ondalik tip yok, para icin kati disiplin gerekir; ENS birikimi aktarilmaz | Reddedildi |

### 2.2 Web istemcisi

| Secenek | Sonuc |
|---|---|
| **React + TypeScript** | **Secildi.** Tek aday; spesifikasyonla ve onceki gorusmelerle uyumlu |

### 2.3 Masaustu istemcisi

| Secenek | Sonuc |
|---|---|
| WPF / WinForms | **Reddedildi.** Her muhasebe ekraninin ikinci kez yazilmasini gerektirir; ENS'teki masaustu dagitim sorunlarini geri getirir; kullanici tarayici tabanli calisma istemistir |

### 2.4 Mobil istemci

| | PWA | Capacitor | **React Native (Expo)** | .NET MAUI | Flutter |
|---|---|---|---|---|---|
| Magaza uygulamasi | Yok | Var | **Var** | Var | Var |
| Havadan guncelleme | Dogal | Ucuncu taraf hizmet | **Yerlesik (EAS Update)** | Yok | Ucuncu taraf |
| Mac olmadan iOS derleme | Gerekmez | Gerekir veya ucretli macOS bulut derleyici | **Bulutta derlenir** | Gerekir | Gerekir veya bulut |
| Cevrimdisi calisma | Kisitli (ozellikle iOS) | Iyi | **Guclu** | Guclu | Guclu |
| Dil | TypeScript | TypeScript | **TypeScript** | C# | Dart |
| Web ile paylasim | Tam | Cok yuksek | Tipler, API istemcisi, dogrulama | Yok | Yok |
| Sonuc | Magaza sarti nedeniyle eler | Ikinci aday | **Secildi** | Reddedildi | Reddedildi |

---

## 3. Karar

### 3.1 Katmanlar

| Katman | Secim |
|---|---|
| Arka uc | **ASP.NET Core (C#)**, .NET 10 LTS — tek API, tum istemciler buna baglanir |
| Veritabani | **PostgreSQL 18 + RLS** |
| Web | **React + TypeScript** |
| Mobil | **React Native (Expo)** |
| Masaustu istemci | **Yok** |
| Ortak kod | TypeScript veri tipleri, API istemcisi ve dogrulama kurallari tek pakette; web ve mobil ayni paketi kullanir. API istemcisi, arka ucun OpenAPI tanimindan uretilir |

### 3.2 Dagitim birimi

Faz 1'de **tek dagitilabilir uygulama**: ASP.NET Core uygulamasi hem API'yi hem derlenmis
React web arayuzunu statik dosya olarak sunar. Tek surec, tek port, tek HTTPS sertifikasi,
tek surum. Canli makinede Node.js, .NET SDK veya Visual Studio kurulmaz; yalniz calisma
zamani gerekir (operasyon proseduru bolum 13 ile uyumlu).

Ayirma, olculmus bir ihtiyac ortaya ciktiginda yapilir.

### 3.3 Mobil uygulamalar

- **Faz 2 — Personel uygulamasi:** hoca, yonetici ve muhasebe tek uygulamada, role gore
  farkli ekranlar (bugunku seanslar, yoklama, ozetler, fis cekme).
- **Faz 3 — Veli uygulamasi:** ayri uygulama. Farkli gizlilik kurallari, farkli magaza
  listelemesi ve farkli kullanici kitlesi nedeniyle personel uygulamasindan ayrilir
  (spesifikasyon bolum 10.6 ve 10.7 ile uyumlu).
- Iki uygulama ayni depoda durur ve ortak paketleri kullanir.

### 3.4 Havadan guncelleme (OTA)

Expo Updates kullanilir. Kurallar:

1. OTA **yalniz** ekran, is mantigi, metin ve gorsel degisikliklerini tasir. Yeni izin,
   yeni yerel kutuphane veya Expo SDK yukseltmesi magaza surumu gerektirir.
2. Uygulamanin temel amacini veya niteligini degistiren degisiklik OTA ile yapilmaz.
3. OTA bir **dagitim kanalidir**; operasyon prosedurundeki surum, commit, dogrulama ve
   geri alma disiplini aynen uygulanir.
4. Once test kanali, sonra kademeli yayin. Geri alma yolu yayindan once hazirdir.
5. Sahada eski surumler bulunacagi kabul edilir. API geriye uyumlu ilerler; desteklenen en
   eski istemci surumu tanimlanir, daha eskisine "zorunlu guncelleme" ekrani gosterilir.

### 3.5 Fis ve fatura okuma

`[KULLANICI KARARI]` Goruntuler sirket disina cikmaz. Buna gore:

1. Gelen e-faturalar entegratorden otomatik alinir (spesifikasyon O44); fotograf akisi
   kagit fis ve basili fatura icindir.
2. Basili e-Fatura ve e-Arsiv uzerindeki **GIB karekodu** once okunur. Karekod varsa
   OCR kullanilmaz. `[ONERI]` Karekod iceriginin ornek faturalarla dogrulanmasi gerekir.
3. Karekodu olmayan belgeler icin **kendi sunucumuzda** calisan acik kaynak bir OCR motoru
   kullanilir. Bulut OCR veya yapay zeka servisi kullanilmaz.
4. Okunan her deger kullanici tarafindan **onaylanmadan** kayda donusmez.
5. Goruntu telefondan yalniz kendi API'mize gider ve kendi dosya depomuzda durur.
6. Gelistirme ve testte gercek fis kullanilmaz; sentetik ornekler uretilir.

### 3.6 Fazlar ve barindirma eslemesi

| Faz | Barindirma | Kullanicilar | Istemci | Erisim |
|---|---|---|---|---|
| 1 | Ofisteki normal bilgisayar | Muhasebe, yonetici | Web | **Yalniz yerel ag**, HTTPS ile |
| 2 | Kiralik RDP, Turkiye'de | + Hoca, yonetici (sahada), muhasebe (fis) | + Personel uygulamasi | Internet, HTTPS |
| 3 | Ayni RDP | + Veli | + Veli uygulamasi | Internet, HTTPS |

**Mobil uygulama Faz 2'de, RDP gecisiyle birlikte acilir.** Faz 1'de ofis bilgisayari
internete acilmaz. Istisna: muhasebeci ofis Wi-Fi'ina bagliyken fis cekme islemi yerel ag
uzerinden calisabilir; bunun icin yerel agda da HTTPS zorunludur.

---

## 4. Gerekce

### 4.1 Neden ASP.NET Core

- **Para dogrulugu:** Muhasebe ve vergi simulasyonu yapan bir sistemde ondalik tipin dilin
  parcasi olmasi belirleyicidir. C# `decimal` ile PostgreSQL `numeric(18,2)` kayipsiz eslesir;
  denetim raporundaki Y3 bulgusu (spesifikasyonda `bigint` kurus, standartta `numeric(18,2)`)
  bu secimle dogal olarak kapanir. Ayrintisi ADR-002'de karara baglanir.
- **ENS birikimi:** .NET yayin kurallari, SHA256 manifest dogrulamasi, PowerShell kurulum
  betikleri ve ilgili teknik tuzaklar dogrudan gecerlidir.
- **Windows'ta calisma:** Tek bir Windows servisi olarak kurulur; hem ofis bilgisayarinda
  hem RDP'de ayni sekilde calisir.
- **Migration:** EF Core migration'lari calisma zamanindan ayri, tek basina calisan bir
  paket olarak uretilebilir; operasyon prosedurundeki "runtime ve migration paketi ayri"
  kuralinin karsiligidir.

### 4.2 Neden masaustu istemci yok

- Kullanici tarayici tabanli calisma istemistir.
- ENS'teki masaustu kaynakli olaylar (bolum 1.3) web modelinde olusmaz: tek sunucu sureci,
  tek ayar, tek dagitim; istemci veritabanina hic baglanmaz.
- Kullanicilarin cogunlugu (hoca, veli) telefondadir; masaustu istemci onlara ulasamaz.

### 4.3 Neden React Native (Expo), neden Capacitor degil

Kullanicinin uc cevabi belirleyicidir:

1. Hocalar icin Faz 2'den itibaren **magaza uygulamasi** isteniyor.
2. **Havadan guncelleme** isteniyor; Expo'da yerlesik, Capacitor'da ucuncu tarafa bagli.
3. **Mac bilgisi yok**; Expo iOS surumunu bulutta derler.

Ek olarak cevrimdisi hoca yoklamasinda React Native daha saglamdir.

Capacitor'un temel ustunlugu web koduyla paylasimdir. Bu projede zayiftir: mobil ekranlar
(seanslar, yoklama, fis cekme) ile web ekranlari (cari tablolari, raporlar) zaten farklidir.
Paylasilmasi gereken katman — tipler, API istemcisi, dogrulama — Expo'da da ortak paket
olarak paylasilir.

### 4.4 Neden .NET MAUI ve Flutter degil

- **.NET MAUI:** Arka uc ile ayni dil cazip gorunur, ancak web zaten TypeScript oldugu icin
  bu kazanc olusmaz. Ekranlar web ile paylasilamaz, ekosistem React Native'den dardir,
  iOS derlemesi Mac gerektirir.
- **Flutter:** Ucuncu bir dil (Dart) ekler, karsiliginda bir kazanc saglamaz.

---

## 5. Sonuclar

### 5.1 Olumlu

- Tek arka uc, tum istemciler icin tek yetki ve is kurali noktasi.
- Faz 1'de tek dagitilabilir birim; ENS'in masaustu dagitim sorunlari olusmaz.
- Para tipi sorunu dil duzeyinde cozulur.
- Mobil icin magaza, OTA ve Mac'siz iOS derleme ayni aracla karsilanir.
- Istemci degisikligi arka ucu etkilemez; ileride Capacitor'a veya baska bir istemciye
  gecis yalniz o istemciyi etkiler.

### 5.2 Olumsuz ve maliyetler

- Projede iki dil bulunur: C# (arka uc) ve TypeScript (web + mobil).
- Web ve mobil ekranlari ayri yazilir.
- Apple gelistirici hesabi yillik, Google Play hesabi tek seferlik ucretlidir.
- Expo'nun bulut hizmetlerinin ucretsiz katmani ve ucretli planlari vardir; kullanim
  buyudukce maliyet dogabilir.
- Acik kaynak OCR, bulut servislerinden daha az isabetlidir; kullanici dogrulamasi zorunlu
  oldugu icin veri kalitesi korunur ama muhasebecinin is yuku artar.

### 5.3 Riskler

- **Kotu bir OTA guncellemesi tum telefonlara ayni anda gider.** Bolum 3.4'teki test kanali,
  kademeli yayin ve geri alma kurallari bu riske karsidir.
- **Eski istemci surumleri:** API geriye uyumlulugu bozulursa sahadaki uygulamalar calismaz.
- **Magaza politikalari degisebilir:** OTA kurallari ve inceleme sureleri uretici kararidir.

---

## 6. Acik konular

Asagidakiler bu ADR'nin onayini engellemez; ilgili faz baslamadan once kapatilir.

| # | Konu | Ne zaman | Oneri |
|---|---|---|---|
| 1 | Personelin iPhone / Android dagilimi | Faz 2 oncesi | Hoca ve yoneticilere kisa bir yoklama |
| 2 | Apple ve Google gelistirici hesaplari | **Simdi baslatilabilir** | Sirket adina (O83). Apple sirket hesabi icin D-U-N-S numarasi ister; alinmasi gunler veya haftalar surebilir |
| 3 | Personel uygulamasinin magazada listelenmesi | Faz 2 oncesi | Apple, yalniz bir kurumun calisanlarina yonelik uygulamalari genel listelemede reddedebilir; listelenmemis dagitim secenegi basvuru aninda degerlendirilir |
| 4 | Bulut derleme sirasinda kaynak kodun Expo sunucularina gitmesi | Faz 2 oncesi | Veri gitmez, kod gider. Kabul edilmezse kiralik bir macOS derleme ortami degerlendirilir |
| 5 | OTA paketlerinin barindirilmasi | Faz 2 oncesi | Expo bulutu (kolay) veya protokol acik oldugu icin kendi sunucumuz (Turkiye'de). Paketler kod icerir, kisisel veri icermez |
| 6 | Bildirim icerigi | Faz 2 oncesi | Bildirimler Apple ve Google sunucularindan gecer; bildirim metni kisisel veri tasimaz ("Yeni bildiriminiz var") |
| 7 | OCR motoru secimi ve isabet olcumu | Fis ozelligi oncesi | Sentetik fislerle olculur |
| 8 | GIB karekod formatinin dogrulanmasi | Fis ozelligi oncesi | Ornek e-Fatura ve e-Arsiv ciktilariyla |
| 9 | Cevrimdisi yoklamanin senkron tasarimi | Faz 2 oncesi | Cakisma kurallari ayrica tasarlanir |
| 10 | Migration araci | Sema tasarimindan once | `[ONERI]` EF Core migration'lari; gozden gecirilebilir SQL betigi olarak uretilir. RLS politikalari ve ozel nesneler migration icinde ham SQL ile yazilir. PostgreSQL standardi bolum 10'daki `[ACIK]` madde bununla kapatilir |
| 11 | Kimlik dogrulama akislari | Sema tasarimindan once | Web icin oturum cerezi, mobil icin token, veli icin telefon + tek kullanimlik kod (O74). API ilk gunden uc akisi destekleyecek sekilde tasarlanir |
| 12 | Dosya deposu soyutlamasi | Fis ozelligi oncesi | Ilk etapta sunucu diski, yedekli; RDP'ye tasinabilir olmali |

---

## 7. Gozden gecirme tetikleyicileri

Bu karar su durumlardan biri olusursa yeniden degerlendirilir:

- Cevrimdisi yoklama veya baska bir mobil ihtiyac Expo ile karsilanamazsa.
- Expo'nun lisans veya fiyat politikasi projeyi olumsuz etkileyecek bicimde degisirse.
- Apple veya Google'in OTA kurallari degisirse.
- Faz 1'de tek dagitilabilir birim olculmus bir performans veya guvenlik sorunu uretirse.

---

## 8. Onay

- [x] Kullanici onayi - tarih: 26 Eylul 2026

**Onaydan sonra yapilacaklar:**

1. Bu belgenin durumu `[KARAR]` olarak guncellenir.
2. Ana hafiza bolum 0 ve 4.1'deki `[ACIK]` teknoloji yigini maddesi `[KARAR]` yapilir.
3. Ana hafizaya surum 0.11 spesifikasyonu bolum 2.1'in bu ADR ile gecersiz kilindigi not edilir.
4. PostgreSQL standardi bolum 10'daki migration araci maddesi, acik konu 10 kapatildiginda guncellenir.
5. Operasyon prosedurune OTA dagitim kurallari (bolum 3.4) eklenir.
