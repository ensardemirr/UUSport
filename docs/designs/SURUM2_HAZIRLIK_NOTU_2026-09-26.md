# Surum 2 Hazirlik Notu

**Durum:** `[ONERI]` Tasarim asamasi; kod yazilmadi
**Tarih:** 26 Eylul 2026
**Kaynak:** Kullanicinin surum 0.1'i deneme sirasinda verdigi dort geri bildirim
**Iliskili:** ADR-004, ADR-005, `docs/decisions/` (yeni ADR bu notlardan turetilecek)

Bu not dort maddeyi kapsar. Madde 4 zaten calisir durumda oldugu icin kapatildi.
Digerleri surum 2 kapsamina alinmak uzere tasarlandi; kod yazilmadi.

---

## 1. Excel sablon indirme (aktarim onizleme sisteminin ustune)

`[KULLANICI ISTEGI]` Kullanici Excel sablonu indirebilmeli, doldurup geri
yuklemeli; sablonda zorunlu alanlar isaretli olmali.

### Mevcut durum

Surum 0.1'deki aktarim (`docs/decisions/ADR-005` madde 10) serbest sutun eslestirmesi
kullaniyor: kullanici herhangi bir Excel'i yukluyor, sutunlari elle eslestiriyor.
Ogrenci aktariminda kaynak sayfa `Sayfa1 (2)` ile sinirli (`[KULLANICI KARARI]`,
denetim raporu bolum 8).

### Onerilen ek (serbest eslestirmeyi degistirmez, ustune eklenir)

- Her aktarim turu (`ogrenci`, `cari`, `urun`) icin **/api/imports/template?kind=...**
  ucu, basliklari `ImportKinds.Fields` listesinden ureten bos bir `.xlsx` dosyasi doner.
- Sablon basliklarinda zorunlu alan **kalin** ve `*` ile isaretlenir; ikinci satirda
  aciklama/ornek deger bulunur (donmuş satir, silinebilir).
- Sablon indirilip doldurulduysa sistem basliklardan otomatik eslestirme onerir
  (mevcut `guess()` fonksiyonu zaten bunu yapiyor, sablon basliklariyla %100 eslesir).
- **Serbest yukleme kaldirilmaz.** Kullanici kendi Excel'ini de yukleyebilir, sutunlari
  elle eslestirir. Sablon, hatayi azaltan bir kisayoldur, zorunluluk degildir.

### Is

- `ImportService` icine `BuildTemplate(kind)` — `ClosedXML` ile bos workbook.
- `ImportEndpoints`'e `GET /api/imports/template` ucu.
- Web: "Sablon Indir" dugmesi 1. adima eklenir.

### Acik yok — dogrudan uygulanabilir.

---

## 2. Cari kodu, malzeme kodu ve hesap adlarinin mali musavirin mizaniyla uyumu

`[KULLANICI ISTEGI]` Cari kodlari, malzeme kodlari ve hesap isimleri mali musavirden
gelecek mizana uygun olmali; muhasebeye veri gonderiminde uyumsuzluk cikmamali.
Musavirden gecici mizan alinip hesap isimleri ona gore yazilacak.

### Bu, spesifikasyonun zaten bildigi bir acik konu

Spesifikasyon O34 "Hesap plani eslemesi (GMS.NET)" ve bolum 8.3 "Hesap plani kodlari
musavirin GMS.NET planindan alinir (esleme tablosu)" diyor. ADR-005 acik konu 6 da
GMS.NET aktarim sablonunu musavir girdisine birakiyor.

### `[ACIK]` Engelleyici: mizan dosyasi elimizde yok

Bu maddenin kod tarafi **mizan formatini gormeden** tasarlanamaz. Asagidaki sema
onerisi, mizan geldiginde doldurulacak bir iskelettir; mizan gelmeden migration
yazilmaz (AGENTS.md kurali: "Teknoloji yigini kesinlesmeden..." aynisi burada da
gecerli — "Hesap plani kesinlesmeden esleme tablosu doldurulmaz").

### Onerilen veri modeli (taslak)

```
integration.chart_of_accounts (
  id, tenant_id, account_code,      -- musavirin GMS.NET hesap kodu (orn. 120.01.001)
  account_name,                     -- musavirin mizanindaki isim
  account_type,                     -- cari | stok | gelir | gider | kasa_banka | ...
  is_active
)
```

- `crm.party` tablosuna `chart_of_account_id` (nullable, FK) eklenir. Cari kodu
  (`C000001` gibi) uygulama ici kimlik olarak **kalir**; `account_code` ayri alandir
  ve GMS.NET'e giderken kullanilir. Ikisini ayni alan yapmak, mizan degisince tum
  cari kodlarini yeniden yazmayi gerektirir — bundan kacinilir.
- `inventory.item` tablosuna ayni sekilde `chart_of_account_id`.
- Mizan Excel'i madde 1'deki aktarim altyapisiyla (`ImportKinds.ChartOfAccounts`)
  yuklenir: kod + isim + tur.
- Cari/urun olusturma ekraninda hesap kodu **zorunlu degil**, ama doluysa GMS.NET
  aktarim raporunda kullanilir; bos olanlar rapor oncesi uyari listesine duser.

### Sirada ne var

1. Kullanici mali musavirden **gecici mizan** ister (kendi cumlesi).
2. Mizan geldiginde hesap turu ve kod formati netlesir; yukaridaki sema kesinlesir.
3. ADR-006 "Hesap Plani Eslemesi" yazilir ve onaylanir.
4. Migration ve aktarim eklenir.

`[ONERI]` Bu madde icin simdiden yapilabilecek tek sey: cari ve urun tablosuna
`chart_of_account_id` (nullable) kolonunu **bos** olarak eklemek, boylece mizan
geldiginde veri kaybi olmadan doldurulur. Bu, ayri bir kucuk migration'dir ve mizan
beklemeden yapilabilir.

---

## 3. Odeme araclarinin genisletilmesi: kredi karti, kredi (taksit tablosu), cek portfoyu

`[KULLANICI ISTEGI]`
- Sirket kredi karti, tanimli banka hesabinin **altinda** olmali.
- Kredi cekilmisse, kredi de ilgili banka hesabinin altinda olmali; **taksit tablosu**
  olmali.
- Musteriden cek alinirsa **birden fazla cek** tek islemde girilebilmeli.
- "Seri cek" secenegi: ilk cek numarasi girilince, kac cek girilecekse **ardisik
  numaralarla** otomatik uretilmeli.

### Spesifikasyonla iliski

Spesifikasyon Ö37 "cek portfoy takibi: portfoyde -> tahsile_verildi -> tahsil_edildi |
ciro_edildi | karsiliksiz" zaten bu ihtiyaci tarif ediyor; ADR-005'te bilerek surum
1 disina birakilmisti ("Diger duzenli odemeler... Surum 2+").

### Onerilen veri modeli (taslak)

**Kasa/banka hesap hiyerarsisi:**

```
finance.cash_account (
  ...mevcut alanlar...,
  parent_account_id  uuid null references finance.cash_account(id)
)
```

- Kredi karti hesabi `kind = credit_card`, `parent_account_id` = ilgili banka hesabi.
- Kredi hesabi `kind = loan`, `parent_account_id` = ilgili banka hesabi (kredinin
  yattigi/odendigi hesap).
- `CashAccountKind` enum'una `CreditCard` ve `Loan` eklenir.
- Ust-alt iliskisi yalniz **gosterim ve raporlama** icindir (rapor bolum 6-7'deki
  "sirket kredi kartlari" ve "kredi odemeleri" gruplamasi); odeme kaydi mantigi
  (ADR-004) degismez, cash_account secimi genisler.

**Kredi taksit tablosu:**

```
finance.loan (
  id, tenant_id, cash_account_id,  -- loan turundeki hesap
  principal_amount, start_date, installment_count, ...
)
finance.loan_installment (
  id, tenant_id, loan_id, installment_no, due_date,
  principal_amount, interest_amount, fee_amount,  -- ADR-002: numeric(18,2)
  paid_amount, payment_id null  -- odendiginde finance.payment'a baglanir
)
```

- Taksit odendiginde normal bir `Payment` (Disbursement) olusur; `loan_installment.
  payment_id` o odemeye baglanir. **Cari hareket modeli degismez** — kredi hesabi
  bir cari degil, `cash_account`'tir; ADR-004 yalniz `Party` bazli hareketleri kapsar.
- Rapor bolum 7'deki "ana para odemesi ile faiz gideri ayri raporlanir" kurali bu
  tabloyla dogal karsilanir.

**Cek portfoyu:**

```
finance.check (
  id, tenant_id, party_id, direction,      -- received (musteriden) | issued (bizim verdigimiz)
  check_no, bank_name, due_date, amount,
  status,                                  -- portfoyde | tahsile_verildi | tahsil_edildi | ciro_edildi | karsiliksiz
  batch_id null                            -- seri girilen cekleri gruplamak icin
)
```

- **Seri cek girisi:** kullanici ilk cek numarasini, adedini ve (varsa) ortak tutar/
  vade deseni girer; sistem `check_no`'yu **ardisik** artirarak N satir onizler,
  kullanici her satiri (vade, tutar) tek tek duzeltebilir, sonra topluca kaydeder.
  `batch_id` ile iliskilendirilir; tek satirin geri alinmasi digerlerini etkilemez.
- **Nakit girisi ADR-004'e gore vade tarihinde sayilir** (spesifikasyon Ö37): cek
  `tahsil_edildi` oldugunda otomatik bir `Payment` (Receipt) uretilir; cek kaydinin
  kendisi cariye tutar yazmaz, ureteceği `Payment` yazar. Boylece K1'in cozdugu
  "tek yazma noktasi" kurali (ADR-004) burada da korunur: `finance.check` cari
  hareket kaynagi degildir, urettigi `payment` kaynaktir.
- Karsiliksiz cek: `Payment` yerine ayri bir uyari/iş kaydi olusur; cariye borc
  ters kayitla eklenir (ADR-004 deseni).

### Sirada ne var

1. Bu taslak kullaniciya sunulur, onaylanirsa **ADR-007 "Odeme Araclari Genislemesi"**
   olarak yazilir (kredi karti/kredi/cek).
2. Onaydan sonra migration ve API/ekran islenir.

`[ACIK]` Karsiliksiz cekte ne yapilacagi (yalniz uyari mi, otomatik ters kayit mi)
kullanici onayi ister — yukaridaki oneri varsayimdir.

---

## 4. Ayni faturada stok ve hizmet satirlarinin karismasi

`[KULLANICI ISTEGI]` Fatura keserken ayni faturada hem ticari mal hem hizmet
olabilmeli, birden fazla satir olabilmeli.

### Durum: zaten calisir durumda, ek is gerekmiyor

`AccountingService.EnterDocumentAsync` ve web'deki `DocumentDialog` bilesenini
kontrol ettim: fatura satir secimi yalniz su ikisini kisitliyor:

1. Secilen satirlarin cari (`PartyId`) ayni olmasi,
2. Secilen satirlarin yon (`Direction`: satis/alis) ayni olmasi.

**Kalem turu (`ItemKind`: stok/hizmet) hicbir yerde kisitlanmiyor.** Yani bugun bile
"Forma seti" (stok) ile "Ozel ders" (hizmet) satirlarini ayni faturada isaretleyip
tek fatura numarasiyla kaydedebilirsiniz. Coklu satir zaten destekleniyor
(`DocumentLinkInput` bir liste).

Bu madde icin **kapatildi** — kod degisikligi gerekmiyor.

---

## Ozet ve oncelik onerisi

| # | Konu | Hazir mi | Sonraki adim |
|---|---|---|---|
| 1 | Excel sablon indirme | Tasarlandi, engel yok | Onay verirseniz dogrudan yazilir |
| 2 | Hesap plani eslemesi | Tasarlandi, **mizan bekliyor** | Musavirden mizan gelince ADR-006 |
| 3 | Kredi karti / kredi / cek | Tasarlandi, onay bekliyor | Onaylarsaniz ADR-007, sonra kod |
| 4 | Karisik stok+hizmet fatura | **Zaten calisiyor** | Yok |

`[ONERI]` Madde 1 en hizli kazanc; mizan gelmeden yapilabilir. Madde 2'nin
`chart_of_account_id` kolonlarini bos olarak simdiden eklemek de dusuk riskli bir
hazirlik adimidir. Madde 3 en buyuk is; ADR-007 onayindan sonra baslanmasi onerilir.
