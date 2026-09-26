# ADR-002 Para Tipi ve Yuvarlama

**Durum:** `[KARAR]` Kullanici tarafindan 26 Eylul 2026 tarihinde onaylandi
**Tarih:** 26 Eylul 2026
**Hazirlayan:** Claude Code (Opus 5.5)
**Karar veren:** Kullanici
**Iliskili:** ADR-001; denetim raporu Y3; PostgreSQL standardi bolum 4
**Gecersiz kildigi:** Surum 0.11 spesifikasyonu bolum 14'teki "Para birimi:
tamsayi kurus olarak saklanir (`bigint`)" kurali

---

## 1. Baglam

- Spesifikasyon parayi `bigint` kurus olarak tanimliyor; PostgreSQL standardi
  `numeric(18,2)` diyor. Denetim raporu Y3 bu celiskiyi ve yuvarlama politikasinin
  hicbir belgede olmadigini tespit etti.
- ADR-001 ile arka uc ASP.NET Core (C#) secildi. C# `decimal` tipi PostgreSQL `numeric`
  ile kayipsiz eslesir. Web ve mobil TypeScript'tir; JavaScript'te ondalik tip yoktur.
- ENS_Stp.App tecrubesi: kaynakta `float`, gelistirmede `decimal` tanimli kolonlar canlida
  `Unable to cast Double to Decimal` hatasi uretti ve gelistirmede gorulmesi imkansizdi.

### 1.1 Kullanici karari

`[KULLANICI KARARI]` 26 Eylul 2026: "KDV satirinda yuvarlama yapilmasin, ne yaziyorsa o
uygulansin."

`[CIKARIM]` Bu karar iki durum icin su sekilde uygulanmistir; yorum kullanici tarafindan
26 Eylul 2026 tarihinde onaylanmistir:

- **Gelen belge** (alis faturasi, fis, gider belgesi): belgede yazan KDV tutari aynen
  alinir. Sistem yeniden hesaplamaz, yuvarlamaz, uzerine yazmaz.
- **Kendi duzenledigimiz belge** (satis faturasi taslagi): satir KDV'si tam hassasiyette
  tutulur, satirda yuvarlanmaz. Yuvarlama yalniz belge toplaminda, bir kez yapilir.

---

## 2. Karar

### 2.1 Veri tipleri

| Deger | PostgreSQL | C# | API (JSON) |
|---|---|---|---|
| Kesinlesmis para tutari (belge toplami, tahsilat, odeme, cari hareket, bakiye) | `numeric(18,2)` | `decimal` | **metin** (`"1234.50"`) |
| Satir KDV'si ve ara hesaplar (kendi belgemizde, yuvarlanmamis) | `numeric(18,6)` | `decimal` | metin |
| Miktar | `numeric(18,4)` | `decimal` | metin |
| Oran (KDV, iskonto, stopaj yuzdesi) | `numeric(9,4)` | `decimal` | metin |
| Doviz kuru | `numeric(18,6)` | `decimal` | metin |
| Para birimi | `char(3)` ISO 4217 (`TRY`) | `string` | metin |

Kesin kurallar:

- `real`, `double precision`, `float`, `money` tipleri para ve miktar icin **yasaktir**.
  (PostgreSQL `money` tipi yerel ayara baglidir.)
- `bigint` kurus kullanilmaz. Spesifikasyondaki kural bu ADR ile gecersizdir.
- API'de para, miktar ve oran JSON **sayisi olarak gonderilmez, metin olarak gonderilir**.
  JavaScript sayilari ikili kayan noktalidir; `0.1 + 0.2` bile `0.3` etmez.
- Kolon tipleri elle yazilmaz; migration'dan ve veri sozlesmesinden uretilir.

### 2.2 Yuvarlama yontemi

- Tek yontem: **yarim yukari** (`0,005` -> `0,01`; `-0,005` -> `-0,01`). Ticari ve vergisel
  uygulamadaki genel yontemdir.
- C#'ta her yuvarlama **acikca** `Math.Round(x, 2, MidpointRounding.AwayFromZero)` ile
  yapilir. .NET'in varsayilan `Math.Round` yontemi **banker yuvarlamasidir** (`0,125` ->
  `0,12`) ve PostgreSQL `round()` ile farkli sonuc verir. Parametresiz `Math.Round` para
  kodunda yasaktir; bir analiz kurali ile derlemede yakalanir.
- PostgreSQL'de `round(numeric, 2)` zaten yarim yukari calisir.
- Yuvarlama tek bir yardimci fonksiyondan gecer; kod icinde daginik yuvarlama yapilmaz.

### 2.3 KDV

**Gelen belge:**

1. Belgedeki satir tutarlari, KDV tutarlari ve toplamlar **belgede yazdigi gibi** kaydedilir.
2. Sistem kendi hesabini yalniz **kontrol** icin yapar. Fark varsa kullaniciya gosterir ve
   raporlar; belgedeki degeri degistirmez.
3. Karekoddan okunan degerler de ayni kurala tabidir.

**Kendi duzenledigimiz belge:**

1. Satir KDV'si = satir matrahi x oran; `numeric(18,6)` hassasiyetinde tutulur, **satirda
   yuvarlanmaz**.
2. Belge KDV toplami = satir KDV'lerinin toplami, **bir kez** yarim yukari 2 haneye yuvarlanir.
3. Belge genel toplami = yuvarlanmis matrah toplami + yuvarlanmis KDV toplami.
4. Farkli KDV oranlari (%10 egitim, %20 malzeme) belge uzerinde oran bazinda ayri toplanir;
   yuvarlama her oran toplaminda bir kez yapilir.

### 2.4 Bolme ve dagitim — kurus farki

Taksitlendirme, iskonto dagitimi, tahsilatin birden cok borca dagitimi ve masraf merkezi
dagitimi gibi islemlerde toplam **asla kaybolmaz veya artmaz**:

- Parcalar yuvarlanir, kalan kurus farki **son parcaya** eklenir.
- Ornek: 100,00 TL uc taksit -> 33,33 + 33,33 + 33,34.
- Her dagitimdan sonra "parcalar toplami = asil tutar" kontrolu calisir; tutmazsa islem
  kaydedilmez.

### 2.5 Istemcilerin rolu

- Web ve mobil istemciler **otoriter para hesabi yapmaz**. Toplam, KDV, iskonto ve bakiye
  sunucuda hesaplanir; istemci gosterir.
- Istemcide anlik onizleme gerekiyorsa (ornegin iskonto girilirken net tutar) ya bir
  ondalik kutuphanesi kullanilir ya da sunucudan onizleme istenir. Kaydedilen deger her
  zaman sunucunun hesabidir.

### 2.6 Imkansiz deger kontrolleri

ENS dersi: bir hesabin ciktisi fiziksel olarak imkansiz olabiliyorsa o kontrol yazilir.

- Negatif stok miktari (yetkiyle izin verilmedikce)
- Sifir veya negatif tutarli satis/tahsilat satiri
- Belge toplami ile satir toplamlari arasinda kurus farkinin 1 kurustan buyuk olmasi
  (kendi belgemizde)
- Dagitim sonrasi parca toplaminin asil tutara esit olmamasi

---

## 3. Gerekce

- **`numeric(18,2)` ve `decimal`:** Ondalik degerleri tam tutar, C# ile kayipsiz eslesir;
  PostgreSQL standardi zaten bunu yaziyordu. `bigint` kurus da dogrudur ancak her katmanda
  100'e bolup carpmayi gerektirir; hata yuzeyi buyuktur ve C# `decimal` varken kazanci yoktur.
- **Satir KDV'sinin yuvarlanmamasi:** Kullanici karari. Ayrica satir bazinda yuvarlama,
  cok satirli belgelerde toplamda kurus sapmasi uretir.
- **Gelen belgenin aynen alinmasi:** Resmi belge karsi tarafin duzenledigi belgedir;
  muhasebe kaydi belgedeki tutarla uyumlu olmalidir. Sistemin kendi hesabiyla uzerine
  yazmasi GMS.NET ve beyan ile uyumsuzluk yaratir.
- **Acik yuvarlama yontemi:** .NET varsayilani ile PostgreSQL farkli davranir; bu fark
  sessizce kurus farki uretir ve mutabakatta ortaya cikar.
- **API'de metin:** JavaScript sayilarinin ikili kayan noktali olmasi para icin guvenli
  degildir.

---

## 4. Sonuclar

**Olumlu:** Para tipi celiskisi kapanir; gelen belgeler muhasebe kaydi ile birebir uyumlu
olur; kurus farki kaybolmaz; yuvarlama tek noktadan denetlenir.

**Olumsuz:** Kendi belgemizde satir KDV'si 6 haneli tutuldugu icin ekranda gosterilen
satir KDV'si (2 hane) ile toplam arasinda kullanicinin gozunde 1 kurusluk fark gorulebilir;
ekran bunu aciklamalidir. Istemcilerde para icin metin tasima ek dikkat ister.

---

## 5. Acik konular

| # | Konu | Kimden | Etki |
|---|---|---|---|
| 1 | e-Fatura / e-Arsiv UBL formatinda satir KDV tutarinin kac haneyle gonderilmesi gerektigi | Entegrator + mali musavir | Satir KDV'si gonderimde 2 haneye yuvarlanmak zorundaysa yalniz **gonderilen gorunum** yuvarlanir; ic kayit ve belge toplami bu ADR'ye gore kalir. Toplam ile satirlar arasindaki kurus farkinin formatta nasil ele alinacagi entegratorle netlestirilir |
| 2 | Gelen belgede sistem hesabiyla belge arasindaki farkin hangi esikten sonra onaya dusecegi | Mali musavir | Varsayilan: her fark gosterilir, kayit engellenmez |
| 3 | Doviz kullanilip kullanilmayacagi | Kullanici | Kullanilmazsa `TRY` sabit, kur alanlari bos kalir |
| 4 | Stopajli belgelerde (serbest meslek, kira) yuvarlama noktasi | Mali musavir | Ayni ilke uygulanir: gelen belgede yazan esas |

---

## 6. Onay

- [x] Kullanici onayi - tarih: 26 Eylul 2026
- [x] Bolum 1.1'deki yorumun onayi (gelen belge aynen / kendi belgemizde satirda yuvarlama yok)

**Onaydan sonra:** bu belge `[KARAR]` yapilir; PostgreSQL standardi bolum 4'e satir KDV'si
icin `numeric(18,6)`, oran icin `numeric(9,4)` ve API metin kurali eklenir; ana hafiza
bolum 8.3'e baglanti verilir; spesifikasyon bolum 14'un para kuralinin gecersiz kilindigi
not edilir.
