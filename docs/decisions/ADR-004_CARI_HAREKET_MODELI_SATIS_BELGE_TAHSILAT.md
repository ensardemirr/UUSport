# ADR-004 Cari Hareket Modeli: Satis, Belge ve Tahsilat

**Durum:** `[KARAR]` Kullanici tarafindan 26 Eylul 2026 tarihinde onaylandi
**Tarih:** 26 Eylul 2026
**Hazirlayan:** Claude Code (Opus 5.5)
**Karar veren:** Kullanici
**Iliskili:** Denetim raporu K1; `docs/designs/MUHASEBE_CARI_STOK_BELGE_AKISI_TASLAK.md`;
PostgreSQL standardi bolum 5; ADR-002 (para ve yuvarlama)

---

## 1. Baglam

Denetim K1: akis belgesi satis satirinin cariye borc yazdigini, standart ise satis, stok,
tahsilat ve faturanin ayri kayitlar oldugunu soyluyor; mockup'ta fatura cari tabloda ayri
bir borc satiri olarak gorunuyor. **Hicbir belge faturanin cariye tutar yazip yazmadigini
soylemiyor.** Bu belirsizlik, sonradan faturalanan bir satisin cariye iki kez borc
yazilmasina yol acabilir.

Bu calisan bir hata degil, kod yazilmadan kapatilmasi gereken bir tasarim acigidir.

### 1.1 Kullanicinin kabul senaryosu

`[KULLANICI KARARI]` (capraz degerlendirme, 25-26 Eylul 2026):

1. 2.500 TL urun teslimi -> cariye 2.500 TL borc; stok duser.
2. Ayni teslimin sonradan faturalanmasi -> borc **yine 2.500 TL**; stok **ikinci kez dusmez**.
3. 1.000 TL tahsilat -> kalan borc 1.500 TL.

Ayrica: kismi faturalama, toplu fatura, iskonto/fiyat farki, iade, iptal ve tekrar gonderim
senaryolari tanimlanmali; **taslak fatura duzenlenmis resmi belge sayilmamalidir**.

---

## 2. Karar

### 2.1 Temel kural

> **Cariye tutar yalniz iki yerden yazilir: satis satiri ve tahsilat.**
> **Belge (fatura) ve dagitim (eslestirme) kayitlari cariye tutar yazmaz.**

| Kayit | Cariye etkisi | Stoka etkisi | Rolu |
|---|---|---|---|
| **Satis satiri** (stok cikisi, hizmet satisi, aidat tahakkuku) | **Borc yazar** | Stoklu kalemse **stok dusurur** | Ekonomik olayin kendisi |
| **Tahsilat** | **Alacak yazar** | Yok | Paranin gelisi |
| **Belge** (e-Arsiv, e-Fatura, iade faturasi) | **Yok** | **Yok** | Satis satirlarini resmilestiren kayit |
| **Belge baglantisi** (belge <-> satis satiri, miktar ve tutar) | Yok | Yok | Hangi satirin ne kadarinin belgelendigi |
| **Tahsilat dagitimi** (tahsilat <-> satis satiri/belge) | Yok | Yok | Hangi paranin hangi borcu kapattigi |
| **Ters kayit** (iade, duzeltme, tahsilat iadesi) | Kaynaginin tersini yazar | Stoklu kalemse stogu geri alir | Silme yerine duzeltme |
| **Acilis bakiyesi** | Borc veya alacak | Yok | Yalniz muhasebe onayiyla (ana hafiza bolum 3.1) |

### 2.2 Veri modeli ilkeleri

- `finance.ledger_entry` (cari hareket) cariyi etkileyen **tek** tablodur. Bakiye yalniz
  bu tablodan hesaplanir.
- Her cari hareketin bir **kaynagi** vardir: `source_type` + `source_id`.
- `source_type` yalniz su degerleri alabilir: `sales_line`, `sales_line_reversal`, `payment`,
  `payment_reversal`, `opening_balance`, `approved_adjustment`. **`document` bu listede yoktur.**
  Kural `CHECK` kisitiyla veritabaninda uygulanir.
- `(source_type, source_id)` **benzersizdir**: ayni kaynak cariye ikinci kez yazamaz.
- Stok hareketi yalniz satis satirindan veya onun tersinden uretilir; belge stok hareketi
  uretemez. Ayni kurallar `inventory.stock_movement` icin de gecerlidir.
- Satis satirinda `documented_quantity` tutulur; `documented_quantity <= quantity` kisiti
  vardir. Belge baglantilari bu alani besler.
- Tahsilat dagitimlarinin toplami tahsilat tutarini, bir satis satirina yapilan dagitimlarin
  toplami o satirin brut tutarini asamaz.

### 2.3 Belge durumlari

PostgreSQL standardi bolum 5'teki durum adlari kullanilir:

| Durum | Anlami | Satis satirini "belgelendi" yapar mi |
|---|---|---|
| `pending_accounting` | Belge yok, belge bekliyor | - |
| `draft_document` | Taslak belge var, gonderilmedi | **Hayir** |
| `partially_documented` | Satirin bir kismi resmi belgeye baglandi | Kismen |
| `documented` | Satirin tamami resmi belgeye baglandi | **Evet** |
| `reversed` | Satir ters kayitla kapatildi | - |
| `cancelled` | Belge iptal edildi | Baglantilar pasiflesir |

**Taslak belge resmi belge degildir.** Bir satirin `documented` sayilmasi icin baglandigi
belgenin entegratorden **basariyla gonderilmis** olmasi gerekir.

### 2.4 Uc gorunumun tanimi

| Gorunum | Icerik |
|---|---|
| **Toplam** | Carinin tum `ledger_entry` kayitlari. Bakiye budur |
| **Belgeli / Resmi** | Resmi belgeye baglanmis satis satirlari (belgelenen miktar/tutar kadar) ve bunlara dagitilmis tahsilatlar |
| **Muhasebe Bekleyen** | Tamami belgelenmemis satis satirlari, tamami dagitilmamis tahsilatlar, taslakta bekleyen ve hata alan belgeler |

Uc gorunum ayni veriden hesaplanir; ayri defter tutulmaz.

---

## 3. Senaryolar

Her senaryo otomatik test olarak yazilir ve her surumde calisir.

### S0 — Kullanicinin kabul senaryosu

| Adim | Islem | Cari bakiye | Stok | Belge durumu |
|---|---|---:|---:|---|
| 1 | 1 adet urun teslimi, 2.500 TL | **2.500 borc** | -1 | `pending_accounting` |
| 2 | Ayni teslim faturalanir, gonderim basarili | **2.500 borc** (degismez) | Degismez | `documented` |
| 3 | 1.000 TL tahsilat, faturaya dagitilir | **1.500 borc** | Degismez | `documented` |

`ledger_entry` sayisi: **2** (satis satiri + tahsilat). Fatura cari hareket uretmez.

### S1 — Kismi faturalama

10 adet teslim; 4 adet faturalanir. Satis satiri `documented_quantity = 4`, durum
`partially_documented`. Cari bakiye degismez. Kalan 6 adet Muhasebe Bekleyen'de kalir.
Kalan faturalandiginda durum `documented` olur.

### S2 — Toplu fatura

Bir belge birden cok satis satirini (farkli gunlerdeki teslimler) toplar. Belge tutari,
baglanan satirlarin belgelenen tutarlari toplamina esittir. Cari bakiye degismez.

### S3 — Iskonto veya fiyat farki

Fatura kesilirken fiyat degisirse satis satirinin uzerine **yazilmaz**. Fark, gerekcesi ve
onaylayan kullanicisiyla ayri bir **fiyat farki satis satiri** (eksi veya arti) olarak girer,
cariye yazar ve ayni belgeye baglanir. Iskonto siniri kullanici yetkisine tabidir
(akis belgesi bolum 3.1).

### S4 — Iade

Ilk satisa bagli bir **ters satis satiri** (eksi miktar, eksi tutar) olusur: cariye alacak
yazar, stoklu kalemse stogu geri alir. Iade faturasi ayri bir belgedir ve ters satira baglanir.
Ilk satis ve ilk fatura degismez.

### S5 — Fatura iptali

Belge `cancelled` olur, baglantilari pasiflesir, satis satirlari tekrar `pending_accounting`
(veya kismi) durumuna doner. **Stok ve cari degismez** — teslim gerceklesmistir, yalniz
belgesi iptal olmustur. Tekrar faturalanabilir.

### S6 — Tekrar gonderim

Belge gonderimi idempotent bir anahtarla yapilir. Ayni belgenin ikinci kez gonderilmesi
(zaman asimi sonrasi yeniden deneme dahil) yeni belge, yeni baglanti veya yeni cari hareket
uretmez.

### S7 — Taslak resmi degildir

Taslak belge olusturulur, gonderilmez. Satis satiri `draft_document` durumundadir;
Belgeli/Resmi gorunumde **yer almaz**, Muhasebe Bekleyen'de "taslakta" olarak gorunur.

### S8 — Faturadan once tahsilat (avans)

Tahsilat alinir, acik borc yok. Cariye alacak yazar; dagitilmamis tahsilat (avans) olarak
Muhasebe Bekleyen'de gorunur. Sonraki satis satirina dagitildiginda **yeni cari hareket
olusmaz**, yalniz dagitim kaydi olusur.

### S9 — Dagitimin geri alinmasi

Yanlis dagitim silinmez; ters dagitim kaydiyla kapatilir. Tahsilat ve cari bakiye degismez.

### S10 — Tahsilat iadesi veya hatali tahsilat

Ters tahsilat hareketi (`payment_reversal`) cariye borc yazar. Ilk tahsilat silinmez.

---

## 4. Mutabakat kontrolleri

Her migration'in `03_verify` adiminda ve gunluk kontrol isinde calisir; herhangi biri
sifirdan farkli sonuc verirse **basarisiz** sayilir:

1. Kaynagi `document` olan cari hareket sayisi = 0
2. Ayni `(source_type, source_id)` ile birden fazla cari hareket = 0
3. Her cari icin: bakiye - (satis satiri brut toplami - tahsilat toplami +/- ters kayitlar + acilis) = 0
4. `documented_quantity > quantity` olan satir sayisi = 0
5. Tahsilat tutarini asan dagitim toplami = 0
6. Belgenin kendi toplami ile bagli satir tutarlari toplami arasindaki fark > 0,01 TL
   olan resmi belge sayisi = 0 (kendi duzenledigimiz belgeler icin; ADR-002)
7. Stoklu satis satiri olup stok hareketi olmayan veya birden fazla stok hareketi olan
   satir sayisi = 0

---

## 5. Gerekce

- **Tek yazma noktasi:** Cariye yazan kaynaklar sayili ve veritabani kisitiyla sinirli
  oldugunda cift sayim kod hatasiyla bile olusamaz; kisit reddeder.
- **Ekonomik olay ile belge ayrimi:** Teslim ve hizmet ekonomik olaydir; fatura onu
  belgeler. Tahsilat faturadan once, stok cikisi faturadan once olabilir (kullanici is
  ihtiyaci). Tutari olaya baglamak bu esnekligi korur.
- **GMS.NET ile uyum:** Spesifikasyon bolum 8.3: faturalar GMS.NET'e entegrator uzerinden
  girer; sistem yalniz fatura disi hareketleri aktarir. Faturanin sistemde cari hareket
  uretmemesi bu ayrimla dogal olarak ortusur.
- **Silme yok:** Iade, iptal ve duzeltmelerin tamami ters kayitla yapilir (ana hafiza 3.3).

---

## 6. Sonuclar

**Olumlu:** Cift sayim veri modeli duzeyinde imkansizlasir; uc gorunum tek veriden
hesaplanir; her senaryo test edilebilir; denetim kaydi dogal olarak olusur.

**Olumsuz ve etkilenen belgeler:**

- `docs/designs/MUHASEBE_CARI_STOK_BELGE_AKISI_TASLAK.md` bolum 3'teki hareket tablosunda
  "fatura" ayri bir islem turu olarak listelenmis; onaydan sonra kaldirilir. Belge, satirin
  bir **sutunu ve durumu** olarak gosterilir.
- `docs/designs/cari-hesap-mockup.html` icindeki `e-Arsiv Fatura` borc satiri modele aykiridir.
  Dogrusu: "Aylik futbol egitim paketi" satis satiri, belge sutununda `EAR-2026-00041`,
  durum `Belgelendi`. Mockup revizyonunda duzeltilir.
- Fiyat farki ve iade icin ayri satir acmak, satirin uzerine yazmaktan daha fazla kayit uretir.

---

## 7. Acik konular

| # | Konu | Kimden |
|---|---|---|
| 1 | Tesliminden sonra faturanin en gec kac gun icinde duzenlenmesi gerektigi ve Muhasebe Bekleyen'de uyari esigi | Mali musavir |
| 2 | e-Arsiv / e-Fatura iptal ve iade proseduru (iptal mi, iade faturasi mi, sure siniri) | Mali musavir + entegrator |
| 3 | Pesin odemelerin avans olarak tutulup aylik gelire donusturulmesi (spesifikasyon O33) ile bu modelin baglantisi | Mali musavir; aidat tahakkuku tasariminda |
| 4 | Donem kilidi sonrasi duzeltmelerin hangi doneme yazilacagi | Mali musavir |
| 5 | Kabul senaryosunun yaninda gercek is akislarindan ek senaryolar | Kullanici |

---

## 8. Onay

- [x] Kullanici onayi - tarih: 26 Eylul 2026

**Onaydan sonra:** bu belge `[KARAR]` yapilir; akis belgesi bolum 3 ve 5, PostgreSQL
standardi bolum 5 bu ADR'ye gore guncellenir; bolum 4'teki mutabakat kontrolleri operasyon
prosedurundeki verify adimina eklenir; ana hafiza bolum 4.4'e baglanti verilir.
