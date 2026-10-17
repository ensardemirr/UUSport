# ADR-012 Coklu Sirket ve Kiraci Modeli

**Durum:** `[KARAR]` Kullanici tarafindan 17 Ekim 2026 tarihinde onaylandi (Secenek A ve
KU-8..KU-15 yanitlari). Uygulama ayrintilari (bolum 2.6-2.9) kullanicinin yanitlarindan
turetilmis `[ONERI]` niteligindedir; kod yazilirken itiraz edilirse degistirilir.  
**Tarih:** 17 Ekim 2026  
**Hazirlayan:** Claude Code (Fable 5.1)  
**Karar veren:** Kullanici  
**Iliskili:** `docs/designs/COKLU_SIRKET_YAPI_CALISMASI_2026-10-17.md`; ADR-004 (cari hareket
modeli); ADR-005 (kapsam); guvenlik standardi bolum 3-4; PostgreSQL standardi bolum 7;
danisma sentezi ADR-012 adayi (kiraci cozumleme ve SaaS kapilari)

---

## 1. Baglam

Surum 0.1 tek sirket (tek vergi kimlik numarasi) icin tasarlandi. Karar verici uc ayri
vergi kimlik numarali sirketi ayni programdan yonetmek istiyor: **UUSPORT**, **FITNESS**,
**TIYATRO**. Patron hepsini gorur; muhasebeciler sirket basina yetkilidir; bir sirketin
ortagi yalniz o sirketi gorur; ayni veli bir sirkette veli, digerinde musteri olabilir;
sirketler birbirine borc verir, fatura keser ve tahsilat yapar.

Kod incelemesi (yapi calismasi bolum 1): her is tablosunda `tenant_id` ve zorlanmis RLS
zaten var; eksik olan kullanici-sirket-rol iliskisi, istek basina sirket secimi, birlesik
okuma ve sirketler arasi islemlerdir.

---

## 2. Karar

### 2.1 Sirket = kiraci

> **Her vergi kimlik numarasi bir `core.tenant` satiridir.** Kullaniciya gorunen ad
> "Sirket"tir; kodda `Tenant` adi korunur. Tek kurulum, tek veritabani, tek yedek.

Her sirketin kendi carileri, katalogu, kasa/banka hesaplari, belge numaralari, hesap
kodlari ve denetim kaydi vardir. Bu, "her VKN ayri defter tutar" muhasebe gercegiyle
birebir ayni sinirdir. SaaS'a gecilirse kiracilarin ustune `core.tenant_group` eklenir;
RLS degismez.

### 2.2 Sirket basina rol

`identity.company_membership(user_id, tenant_id, role, is_active, ...)` kullanicinin bir
sirketteki rolunu tutar. Roller: **Yonetici**, **Muhasebe**, **Ortak** (yeni, bolum 2.4);
**Supervisor** ve **Hoca** Faz 2 akademi moduluyle acilir (`[KULLANICI KARARI]` KU-14),
yetki modeli onlari simdiden tasiyacak sekilde kurulur. `identity.app_user.is_group_admin`
patronu isaretler: tum sirketleri gorur, sirket/kullanici/uyelik yonetir.

Baslangic uyelikleri (`[KULLANICI KARARI]` KU-9, gercek ad ve VKN icermez):

| Kisi | UUSPORT | FITNESS | TIYATRO |
|---|---|---|---|
| Patron (grup yoneticisi) | tum | tum | tum |
| Muhasebeci 1 | Muhasebe | Muhasebe | - |
| Muhasebeci 2 | - | - | Muhasebe |
| Ortak (ornek) | - | Ortak | - |

### 2.3 Birlesik okuma, tekil yazma

RLS politikasi her korunan tabloda yenilenir:

```sql
USING      (tenant_id = ANY (string_to_array(NULLIF(current_setting('app.tenant_ids', true), ''), ',')::uuid[]))
WITH CHECK (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid)
```

Okuma secili sirket kumesini kapsar; **yazma tam olarak bir sirkete** yapilir. Istemci
secimi `X-UUSport-Companies` basligiyla gonderir; sunucu bunu kullanicinin aktif
uyelikleriyle keser (kesisim bos ise 403). Birlesik modda yazan uclar "Kayit icin tek
sirket secin" ile reddeder; veritabani da `WITH CHECK` ile reddeder.

### 2.4 Ortak rolunun gorunurlugu parametriktir

`[KULLANICI KARARI]` KU-10: Ortak'in ne gorecegi sabit degil, **grup yoneticisi veya o
sirketin Yonetici'si tarafindan acilip kapatilabilir**. Uygulama: kodda tanimli yetki
anahtarlari (orn. `parties.read`, `ledger.read`, `reports.summary`, `reports.cashflow`,
`payments.write`) ve rol sablonlari; uyelik satirinda `permission_overrides` (ver/kaldir)
ile sablon disina cikilir. Ortak sablonu varsayilan olarak ozet raporlar ve nakit akisi
acik, cari hareket ayrintisi kapali, **tum yazma anahtarlari kapali ve acilamaz** baslar.
Yonetim panelinde "etkin yetkiler" salt okunur gosterilir. Ayni mekanizma ileride her rol
icin ince ayar saglar; ekran basina tik kutusu matrisi yapilmaz (guvenlik standardi
"tek matris" kurali).

### 2.5 Sirketler arasi islemler (grup ici)

`[KULLANICI KARARI]` KU-11: sirketler birbirine borc verir, fatura keser, tahsilat yapar.

- Her sirkette karsi sirket icin bir cari acilir: `crm.party.related_tenant_id` dolu
  (`kind = Both`). Bu cari normal bir caridir; ADR-004 kurallari aynen gecerlidir.
- A, B'ye satis yaparsa A'da satis satiri (+ belge) B carisine yazilir. B tarafindaki
  karsi kayit (alis satiri) **otomatik yazilmaz**; A'nin kaydi B'nin "Muhasebe Bekleyen >
  Grup ici" kuyruguna **oneri** olarak duser, B'nin muhasebecisi onaylayinca B'de alis
  satiri olusur ve iki kayit `integration.intercompany_link` ile baglanir. Gerekce: tekil
  yazma kurali (2.3) ve gorev ayriligi; B'nin defterine B'nin yetkilisi yazar.
- Birlesik raporda bagli ciftler "Grup ici" olarak ayri gosterilir ve istege bagli elenir.
  Sessiz birlestirme yapilmaz.
- Bagli olmayan (karsi tarafi henuz onaylanmamis) grup ici kayitlar mutabakat gorunumunde
  "karsiligi bekliyor" olarak listelenir.

### 2.6 Ayni veli birden fazla sirkette; tek tahsilat, fazlanin aktarimi

`[KULLANICI KARARI]` KU-12: ayni kisi bir sirkette veli, digerinde musteri olabilir;
her sirket ayri fatura keser; **tahsilat tek olabilir** ve fazla tahsilat diger sirkete
odeme yapiliyormus gibi gorunur.

- Kisi her sirkette **ayri cari**dir; veri birlestirilmez. Birlesik gorunumde ayni kisi
  normalize telefonla gruplanir (`party.phone`); resmi bir "grup kisi" kaydi `[ACIK]`.
- Tahsilat, parayi fiilen alan sirkette (A) kaydedilir; o sirketin kasasina/bankasina
  girer. Veli A'ya 600, B'ye 400 borcluyken A'ya 1.000 oderse:
  1. A: tahsilat 1.000 -> veli carisi (ADR-004 normal tahsilat); 600 dagitilir, 400 avans.
  2. A: "Fazla tahsilati diger sirkete aktar" islemi -> `approved_adjustment` cifti:
     veli carisi borc 400 / B-sirketi carisi alacak 400 (A, B'ye 400 borclanir).
  3. B: ayni islem 2.5'teki kuyruktan onaylaninca `approved_adjustment` cifti: A-sirketi
     carisi borc 400 / veli carisi alacak 400 (velinin B borcu kapanir).
  4. A, B'ye parayi fiilen gonderince normal odeme/tahsilat kaydi iki tarafta da
     grup ici cariyi kapatir.
- `approved_adjustment` ADR-004'te zaten izinli bir cari kaynagidir; yeni kaynak turu
  eklenmez. Her aktarim onay ister ve denetim kaydina yazilir.
- Resmi kapsam (M2, ADR-009): aktarim kaydi belge degildir; fatura her sirkette kendi
  satirina kesilir. Velinin tek dekontu iki sirkete bolunmus sayilir; mali musavir teyidi
  `[ACIK]` (MM sorusu).

### 2.7 Mali musavir ve aktarim sirket basinadir

`[KULLANICI KARARI]` KU-13: musavirler su an ayri, ileride birlesebilir; programlari ve
formatlari farkli olabilir. Hesap kodlari (`account_code`) zaten sirket basinadir.
ADR-006 (hesap plani ve aktarim) **sirket basina aktarim profili**
(`integration.export_profile(tenant_id, format, mapping)`) ile yazilir; musavir birlesirse
iki profil ayni formati gosterir, baska degisiklik gerekmez.

### 2.8 Kodlarda sirket on eki ve tedarikci kodu eslemesi

`[KULLANICI KARARI]` KU-15: cari ve urun kodlarinda sirket on eki **istenir**. Kullanicinin
uyarisi: alis faturasindaki tedarikci urun kodu bizim kodla ayni degilse personel ayni
urunu farkli kodla ikinci kez acar.

- Sirket kisa kodu ASCII uc harf: `UUS`, `FIT`, `TIY` (`core.tenant.code`, `COLLATE "C"`,
  benzersiz). Sistem kodlari `UUS-C000001`, `FIT-U000001` bicimindedir. Kullanicinin elle
  verdigi kod on ek olmadan girilir, sistem on eki ekler.
- **Bizim kodumuz ile tedarikci kodu ayri kavramdir.** Tedarikcinin kodu bizim kodumuza
  esitlenmeye calisilmaz; eslenir:
  `inventory.supplier_item_code(tenant_id, item_id, supplier_party_id, supplier_code,
  barcode)` (benzersiz: `tenant_id, supplier_party_id, supplier_code`).
- Alis faturasi girisinde satir once tedarikci kodu veya barkod ile aranir; eslesme varsa
  bizim urunumuz gelir. Eslesme yoksa personel **once mevcut katalogdan secmeye** yonlendirilir
  (ad benzerligi `search_key` ile onerilir); "yeni urun ac" ikinci secenektir ve acilan
  urun ile tedarikci kodu otomatik eslenir. Bu, kullanicinin tarif ettigi mukerrer kart
  riskini kaynaginda keser.
- Barkod (GTIN) sirketler arasi ortak anahtardir; ayni urunun UUS ve FIT kartlari barkodla
  birlesik raporda gruplanabilir. Sirketler arasi ortak urun ana karti `[ACIK]`.

### 2.9 Arayuz

Ust cubukta sirket secici (rozet: kisa kod + erisilebilir renk, "Tumu", coklu secim;
tercih tarayicida hatirlanir). Birlesik modda listelere "Sirket" sutunu, toplamlar sirket
basina ve genel ayri; yazma dugmeleri pasif ve ipuclu. Genel Bakis sayaclari sirket
basina. "Yonetim > Sirketler" ve "Yonetim > Kullanicilar" (uyelik ve etkin yetkiler).
Apple Design Skill ilkeleri; Windows/web davranislari korunur.

---

## 3. Gerekce

- Mevcut kiraci siniri istegin dogrudan uzantisidir; 20 tabloya dokunulmaz, izolasyon
  veritabaninda kalir, mevcut 72 test ve sema koruma testleri gecerliligini korur.
- Muhasebe gercegi: her VKN ayri defter, ayri KDV ve gecici vergi beyani; birlesik
  gorunum yalniz yonetim raporudur, resmi beyan degildir.
- Tekil yazma kurali ve grup ici onay kuyrugu, gorev ayriligini ve "yanlis sirkete kayit"
  riskini veritabani seviyesinde kapatir.
- Secenek B (kiraci ustunde ayri sirket katmani) iki kat efor karsiliginda bugun kullanilmayan
  esneklik verirdi; gerekirse `tenant_group` ile ayni noktaya varilir.

---

## 4. Sonuclar

- Yeni migration: `core.tenant` kolonlari (`code`, `legal_name`, `short_name`, `tax_number`,
  `tax_office`, `is_active`, `sort_order`, `color_key`), `identity.company_membership`,
  `identity.app_user.is_group_admin`, `crm.party.related_tenant_id`,
  `integration.intercompany_link`, `inventory.supplier_item_code`, RLS v2 (yeni dondurulmus
  tablo listesi; eski listeler degismez).
- `TenantContext` okuma kumesi + yazma kiracisi; ara katman `UseAuthentication` sonrasi;
  `TenantResolver` yalniz kurulum ve anonim uclar icin.
- Politikalar sirket baglamli olur; `EndpointAuthorizationTests` her uc icin yetkisiz
  sirket durumunu, `MultiCompanyTests` capraz sirket ve birlesik-mod yazma reddini kanitlar.
- Guvenlik standardi bolum 3'e "sirket basina rol, birlesik okuma / tekil yazma, yetki
  anahtarlari" eklenir; PostgreSQL standardi bolum 7'ye RLS v2 politikasi islenir.
- Yol haritasi: bu is **M1.5** olarak M2'den once yapilir (S-21, S-23, S-25, S-26 ve M4
  sirket boyutunu kullanir). Tahmini 7-9 ajan gunu (grup ici kuyruk ve tedarikci kodu
  eslemesi ilk tahmine eklendi).
- Mevcut gelistirme veritabanindaki tek kiraci "UUSPORT" olur (`code = UUS`); FIT ve TIY
  sentetik veriyle acilir.

---

## 5. Acik konular

| # | Konu | Ne zaman |
|---|---|---|
| 1 | Ayni kisinin sirketler ustu "grup kisi" kaydi (telefonla gruplama disinda resmi bag) | Birlesik gorunum kullanildikca |
| 2 | Sirketler arasi ortak urun ana karti (barkod disinda) | Stok maliyeti (ADR-014) ile |
| 3 | Fazla tahsilatin diger sirkete aktariminin resmi/vergisel niteligi (MM sorusu) | M2 / ADR-009 oncesi |
| 4 | Grup ici fatura: A'nin kestigi e-Arsiv/e-Fatura B'de alis belgesi olarak otomatik mi? | e-belge entegratoru (Faz 2+) |
| 5 | Ortak sablonunun varsayilan anahtar listesi | Uygulama sirasinda kullaniciyla |
| 6 | SaaS'ta `tenant_group` ve platform yoneticisi | M8 |

---

## 6. Onay

`[KULLANICI KARARI]` 17 Ekim 2026: Secenek A onaylandi; sirketler UUSPORT / FITNESS /
TIYATRO; Ortak gorunurlugu parametrik; grup ici borc/fatura/tahsilat var; ayni veli
birden fazla sirkette, tek tahsilat ve fazlanin aktarimi; musavirler ayri (birlesebilir),
formatlar farkli olabilir; Hoca/Supervisor Faz 2; kodlarda sirket on eki istenir, tedarikci
kodu uyumsuzlugu riski belirtildi.
