# ADR-005 Ilk Surum Kapsami ve Elle Fatura Kaydi

**Durum:** `[KARAR]` Kullanici tarafindan 26 Eylul 2026 tarihinde onaylandi
**Tarih:** 26 Eylul 2026
**Hazirlayan:** Claude Code (Opus 5.5)
**Karar veren:** Kullanici
**Iliskili:** ADR-001 (fazlar), ADR-002 (KDV kurali), ADR-004 (cari hareket modeli);
denetim raporu Y7 ve bolum 8
**Gecersiz kildigi:** Spesifikasyon O8 ("ozel entegrator ile e-Arsiv/e-Fatura -
MVP ilk modulu"); ADR-004 bolum 2.3'teki "belgelendi sayilmasi icin entegratorden basariyla
gonderilmis olmasi" sartinin ilk surumdeki uygulamasi (bolum 3)

---

## 1. Baglam

`[KULLANICI KARARI]` 26 Eylul 2026:

1. Su anda e-belge entegratoru yok; faturalar **elle** kesiliyor. Kesilen fatura on muhasebe
   programina (UUSport) **elle girilecek**.
2. Mali musavir surece yardimci olacak.
3. **Aidat tahakkuku ilk surumde olmayacak.**

Onceki kararlar: ilk calisan surumun onceligi Excel'den verileri alip muhasebeyi kullanmaya
baslamak (denetim raporu bolum 8, `[KULLANICI KARARI]`); Faz 1 yalniz yerel agda calisir
(ADR-001).

---

## 2. Karar: Surum 1 kapsami

### 2.1 Icinde

| # | Alan | Kapsam |
|---|---|---|
| 1 | Giris ve yetki | Kullanici girisi, iki rol (yonetici, muhasebe), iki adimli dogrulama (spesifikasyon O74), yetkinin API ve RLS'te uygulanmasi |
| 2 | Cari | Veli/musteri cari karti, bagli sporcular (ad ve temel bilgi duzeyinde) |
| 3 | Katalog ve stok | Urun ve hizmet katalogu, tek depo, stok miktari |
| 4 | Stok veya hizmet cikisi | Satis satiri, kullanici bazli iskonto siniri, stoktan dusme (ADR-004) |
| 5 | Tahsilat | Nakit, havale/EFT, kredi karti/POS; kasa ve banka hesabi secimi; faturaya/satira dagitim; avans |
| 6 | Elle fatura kaydi | Bolum 3 |
| 7 | Uc gorunum | Toplam / Belgeli-Resmi / Muhasebe Bekleyen (ADR-004 bolum 2.4) |
| 8 | Muhasebe Bekleyen listesi | Faturasi girilmemis satislar, dagitilmamis tahsilatlar, taslaklar |
| 9 | Denetim kaydi | Append-only; tum finans, stok, yetki ve aktarim islemleri |
| 10 | Excel aktarimi | Sutun eslestirme ve onizleme; satir dogrulama ve hata listesi; tekrarli aktarimi engelleme; acilis bakiyelerinin muhasebe onayi; aktarim kaydi ve ters kayitla kontrollu duzeltme. Ogrenci kaynagi yalniz `Sayfa1 (2)` `[KULLANICI KARARI]` |
| 11 | Temel raporlar | Cari ekstre, gunluk tahsilat, stoktan cikmis-faturasi girilmemis, dagitilmamis tahsilat, acik alacak |
| 12 | Alis faturasi ve gider girisi | `[KULLANICI KARARI]` 26.09.2026. Tedarikci carisi; alis faturasi ve perakende fisin elle girisi; stoklu kalemde stok girisi; tedarikciye odeme (kasa/banka cikisi). Tutarlar ADR-002 "gelen belge" kuralina gore **belgede yazdigi gibi** alinir. Model ADR-004'un aynasidir: cariye alis satiri ve odeme yazar, belge baglanti kaydidir. Stopajli belgeler (serbest meslek makbuzu, kira) bolum 6 acik konu 6'ya baglidir |

### 2.2 Disinda — sonraki surumlere

| Alan | Neden disarida | Ne zaman |
|---|---|---|
| **Aidat tahakkuku** (otomatik aylik borclandirma, paket, tarife, indirim motoru) | `[KULLANICI KARARI]` | Surum 2 adayi |
| **e-Belge entegratoru** | Su an entegrator yok; faturalar elle kesiliyor | Bolum 4'teki tetikleyicilerle |
| Mobil uygulama, fis fotografi | ADR-001: Faz 2, RDP gecisiyle | Faz 2 |
| Veli erisimi | ADR-001: Faz 3 | Faz 3 |
| KDV ve vergi simulasyonu, odeme takvimi raporlari | Rapor kapsami `[KARAR]`, fazlamasi bu ADR ile Surum 2+ | Surum 2+ |
| Banka ekstresi aktarimi ve otomatik eslestirme | Kapsam buyuk, ilk kullanim icin gerekli degil | Surum 2+ |
| Antrenor hakedisi, yoklama, performans | Muhasebe cekirdeginden sonra | Faz 2+ |
| GMS.NET aktarim dosyasi | Musavirle sablon netlesince | Musavir girdisine bagli |

---

## 3. Elle fatura kaydi

### 3.1 Akis

1. Satis satiri olusur (stok/hizmet cikisi). Cariye borc yazar, stok duser (ADR-004).
   Satir `pending_accounting` durumundadir ve Muhasebe Bekleyen'de gorunur.
2. Istenirse "Resmi cikis" secilir ve sistem bir **fatura hazirlik ozeti** gosterir: kalemler,
   matrah, oran bazinda KDV, toplam. Bu ozet, faturayi GIB portalinda keserken kullanilacak
   yardimci belgedir; **resmi belge degildir** ve `draft_document` durumunu uretir.
3. Kullanici faturayi **GIB portalinda** keser.
4. Kullanici UUSport'ta **"Kesilen faturayi gir"** ekranini acar ve sunlari girer:
   fatura no, fatura tarihi, belge turu (e-Arsiv / e-Fatura), satir matrahlari, oran bazinda
   KDV tutarlari, genel toplam; bagli satis satirlarini ve miktarlarini secer.
5. Kayit **ikinci bir goz** ile dogrulanir: sistem bagli satirlardan hesapladigi tutarlari
   girilen tutarlarla yan yana gosterir, fark varsa isaretler.
6. Kullanici onaylayinca belge **resmi belge** olarak kaydedilir; bagli satirlar
   `documented` veya `partially_documented` olur.

### 3.2 Kurallar

- **ADR-004 bolum 2.3'un ilk surumdeki karsiligi:** "Entegratorden basariyla gonderilmis"
  sartinin yerini **"GIB portalinda kesildikten sonra fatura no ve tarihiyle elle girilmis ve
  onaylanmis"** alir. Taslak/hazirlik ozeti hicbir zaman resmi belge sayilmaz (ADR-004 S7).
- **Tutarlar yazdigi gibi alinir.** Portalda kesilen fatura resmi belgedir; ADR-002'deki
  "gelen belge" kurali uygulanir: girilen KDV ve toplamlar aynen kaydedilir, sistem kendi
  hesabini yalniz kontrol icin gosterir, uzerine yazmaz.
- **Fatura cariye tutar yazmaz** (ADR-004). Elle girilen fatura yalniz belge kaydi ve satir
  baglantisidir.
- **Fatura numarasi benzersizdir** (belge turu + numara). Ayni fatura ikinci kez girilemez.
- **Yanlis girilen fatura silinmez.** Iptal edilir (ADR-004 S5) ve dogru bilgiyle yeniden
  girilir; ikisi de denetim kaydinda kalir.
- Kendi faturamizi elle girerken olusan fark (portal hesabi ile sistem hesabi) raporlanir;
  tekrarlayan fark, satis satirindaki fiyat veya oran hatasinin isaretidir.

### 3.3 Ekran etkisi

- Mockup'taki "Fatura taslagi onizlemesi" dialogu **"Fatura hazirlik ozeti"** olur ve
  "Taslak Faturayi Kaydet" yerine "Ozeti yazdir / Portalda kestim, faturayi gir" akisina
  baglanir.
- Yeni ekran: **Kesilen faturayi gir** (bolum 3.1 adim 4-6).
- Muhasebe Bekleyen listesine bir filtre: "Portalda kesilmesi bekleyen".

---

## 4. e-Belge entegratorune gecis tetikleyicileri

Entegrator su durumlardan biri olusunca yeniden degerlendirilir:

- Aidat tahakkuku devreye girdiginde (spesifikasyon: yilda 3.000'den fazla aidat faturasi
  elle kesilmesi surdurulemez).
- Elle fatura girisi ayda belirli bir sayiyi astiginda (esik musavirle belirlenir).
- Mali musavir e-Defter hazirligi (1 Ocak 2027) icin entegrator gerektirirse.

Gecis tasarimi hazirdir: ADR-004 zaten belgeyi satirdan ayri bir kayit olarak tuttugu icin
entegrator geldiginde yalniz belgenin **nereden geldigi** degisir (elle giris yerine
entegrator); cari, stok ve uc gorunum etkilenmez. Entegrator secim kriteri spesifikasyondaki
gibi GMS.NET ile anlasmali olmaktir (O88).

---

## 5. Sonuclar

**Olumlu:** Ilk surum entegrator maliyeti ve bagimliligi olmadan calisir; kullanicinin bugunku
is akisini (portalda elle kesme) degistirmez, yalniz kayda baglar; kapsam daralir, Excel'den
kullanima gecis hizlanir.

**Olumsuz:** Fatura bilgisi iki kez yazilir (portal ve UUSport); elle giris hatasi riski
vardir, bolum 3.1 adim 5'teki karsilastirma bu riski azaltir ama sifirlamaz. Aidat olmadigi
icin veli borclari ilk surumde yalniz elle girilen satislar ve Excel acilis bakiyelerinden
olusur.

---

## 6. Acik konular — mali musavirle

| # | Konu |
|---|---|
| 1 | Elle girilen faturada hangi alanlarin zorunlu tutulacagi (ETTN dahil mi) |
| 2 | e-Arsiv / e-Fatura ayrimi: alicinin e-Fatura mukellefi olup olmadiginin nasil izlenecegi |
| 3 | Elle fatura sayisi icin entegrator gecis esigi |
| 4 | Acilis bakiyelerinin kaynagi ve onay yontemi (Excel mi, mizan mi — spesifikasyon O30) |
| 5 | Teslimden sonra fatura duzenleme suresi ve Muhasebe Bekleyen uyari esigi (ADR-004 acik konu 1) |
| 6 | Alis faturasi ve gider girisi ilk surume alindi (bolum 2.1 madde 12). Acik kalan: stopajli belgelerin (serbest meslek makbuzu, kira stopaji, gider pusulasi) ilk surumde mi, Surum 2'de mi girecegi ve belge turune gore vergi davranisi (spesifikasyon 6.1) |

---

## 7. Onay

- [x] Kullanici onayi - tarih: 26 Eylul 2026

**Onaydan sonra:** bu belge `[KARAR]` yapilir; ana hafiza bolum 4.2 (MVP adayi) bu kapsamla
degistirilir ve `[ACIK]` MVP maddesi kapatilir; ADR-004'e bu ADR'ye atif veren kisa bir not
eklenir; akis belgesi ve mockup revizyon listesine bolum 3.3 eklenir.
