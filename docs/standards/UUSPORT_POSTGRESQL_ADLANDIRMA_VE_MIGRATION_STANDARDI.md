# UU Sport PostgreSQL Adlandirma ve Migration Standardi

**Durum:** `[KARAR]` Genel adlandirma ve guvenlik kurallari  
**Durum:** `[ACIK]` Migration araci teknoloji ADR'sinden sonra secilecek  
**Tarih:** 23 Eylul 2026

Bu standart PostgreSQL semasi olusturulmadan once uygulanacak ortak dili tanimlar.
Henuz tablo veya migration olusturma yetkisi vermez.

## 1. Genel adlandirma

- Tum PostgreSQL nesneleri ASCII `lower_snake_case` adlandirilir.
- Turkce karakter, bosluk, nokta, tire, buyuk harf ve cift tirnak gerektiren ad
  kullanilmaz.
- Tablo adlari tekil isimdir: `customer_account`, `ledger_entry`.
- Kolon adlari is anlamini aciklar; belirsiz `code1`, `value2` gibi adlar
  kullanilmaz.
- PostgreSQL ayirilmis kelimeleri nesne adi yapilmaz.
- Uygulama sorgulari nesneleri schema ile birlikte yazar:
  `finance.ledger_entry`.

## 2. Schema sinirlari

Ilk taslak schema ayrimi:

| Schema | Kapsam |
|---|---|
| `core` | tenant, tesis, genel parametre |
| `identity` | kullanici, rol, yetki |
| `academy` | sporcu, veli baglantisi, kayit, grup |
| `crm` | cari hesap ve iletisim |
| `finance` | cari hareket, tahsilat, avans, kasa/banka |
| `billing` | tarife, tahakkuk, fatura taslagi ve belge baglantisi |
| `inventory` | urun, depo, stok hareketi |
| `integration` | aktarim partisi, staging, dis sistem esleme |
| `audit` | append-only denetim olaylari |

Kesin schema listesi veri modeli incelemesinde onaylanir. Her servis hesabi yalniz
gereken schemalara erisir; `public` schema uygulama tablolari icin kullanilmaz.

## 3. Tablo ve kolon ornekleri

```text
core.tenant
identity.app_user
identity.role
academy.athlete
academy.enrollment
crm.customer_account
finance.ledger_entry
finance.payment
finance.payment_allocation
billing.invoice
billing.invoice_line
inventory.stock_item
inventory.stock_movement
integration.import_batch
integration.import_error
audit.audit_event
```

Standart kolonlar:

| Amac | Ad |
|---|---|
| Birincil anahtar | `id` |
| Tenant siniri | `tenant_id` |
| Yabanci anahtar | `<varlik>_id` |
| Olusturma zamani | `created_at` |
| Olusturan | `created_by_user_id` |
| Guncelleme zamani | `updated_at` |
| Guncelleyen | `updated_by_user_id` |
| Islem/olay zamani | `occurred_at` |
| Is kurali durumu | `status` |
| Dis sistem anahtari | `external_id` veya sistem adini belirten acik alan |

Birincil anahtar tipi `uuid` olur; UUID uretim yontemi teknoloji ADR'sinde
kesinlesir. Dis sistem anahtari birincil anahtar yapilmaz.

## 4. Veri turleri

- Para: `numeric(18,2)`; `real` veya `double precision` kullanilmaz.
- Para birimi: ISO 4217 kodu, ornegin `TRY`.
- Miktar: ihtiyaca gore `numeric(18,4)`.
- Tarih: yalniz gun gerekiyorsa `date`.
- Zaman: `timestamptz`; uygulama ve veritabani UTC saklar.
- Mantiksal alan: olumlu anlamli `is_active`, `is_locked`.
- Telefon, kimlik, belge numarasi gibi degerler sayisal hesap alani degildir;
  `text`/uygun uzunluklu karakter alaninda tutulur.
- JSON yalniz degisken dis sistem payload'u veya snapshot icin kullanilir; temel
  iliskisel alanlar JSON'a gomulmez.

## 5. Cari, belge ve resmi islem durumu

Tek bir `is_official` boolean alani yeterli degildir. Stok/hizmet ve tahsilat
akisinda su durumlar acikca modellenir:

- `pending_accounting`
- `draft_document`
- `documented`
- `partially_documented`
- `reversed`
- `cancelled`

Stok hareketi, hizmet satisi, tahsilat ve fatura ayri kayitlardir; baglanti
tablolariyla eslestirilir. Bir fatura birden cok cikisi, bir cikis de kismi olarak
birden cok belgeyi destekleyebilir.

Muhasebe ve stok hareketi silinmez. Duzeltme ilk kayda bagli ters hareketle yapilir.
Ana veri kartlari gerekiyorsa `is_active` ile kullanim disi birakilir.

## 6. Constraint ve index adlari

| Nesne | Kalip |
|---|---|
| Primary key | `pk_<table>` |
| Foreign key | `fk_<child>__<parent>` |
| Unique | `uq_<table>__<column_or_purpose>` |
| Check | `ck_<table>__<rule>` |
| Index | `ix_<table>__<columns_or_purpose>` |

Ornekler:

```text
pk_payment
fk_payment__customer_account
uq_import_batch__source_hash
ck_ledger_entry__amount_nonzero
ix_ledger_entry__tenant_account_occurred_at
```

Her foreign key icin sorgu ihtiyacina gore index degerlendirilir. Index eklemek
otomatik bir kural degildir; sorgu ve veri hacmiyle olculur.

## 7. Tenant ve yetki

- Tenant'a ait her satirda `tenant_id` bulunur.
- Unique constraintler tenant sinirini icerir.
- RLS politikalari varsayilan reddetme yaklasimiyla yazilir.
- Uygulama, tenant ve kullanici baglamini her transaction basinda guvenli bicimde
  ayarlar.
- Superuser veya migration rolu uygulama runtime hesabi olarak kullanilmaz.
- Yetki yalniz UI'da buton gizlemekle saglanmaz; API ve veritabaninda da uygulanir.

## 8. Audit ve izlenebilirlik

- Finans, stok, yetki, fatura ve import islemleri append-only audit olayi uretir.
- Audit satiri en az tenant, kullanici, zaman, eylem, hedef tur/id, correlation id
  ve once/sonra ozetini tutar.
- Parola, token, kart verisi ve gereksiz kisisel veri audit payload'una yazilmaz.
- Uygulama logu ile audit kaydi ayni sey degildir; audit is kanitidir.

## 9. Staging ve import

- Harici dosya dogrudan ana tablolara yazilmaz.
- `integration.import_batch` aktarim basligini tutar.
- Staging tablolari batch id ve kaynak satir numarasini saklar.
- Dogrulama hatalari `integration.import_error` altinda alan ve hata koduyla
  kaydedilir.
- Dry-run sonucu ile canli uygulama sonucu ayri tutulur.
- Mukerrerlik dosya hash'i ve is anahtariyla engellenir.
- `Sayfa1 (2)` disindaki benzer sayfalar otomatik birlestirilmez.

## 10. Migration standardi

- Migration'lar sirali, degistirilemez ve Git'te izlenir.
- Uygulanmis migration dosyasi duzeltilmez; yeni migration eklenir.
- Dosya adi secilen aracla uyumlu olur ve acik is amaci tasir.
- Her release su dort asamayi tasir:
  1. `01_precheck`
  2. `02_apply`
  3. `03_verify`
  4. `04_recovery`
- Precheck hedef ortam, mevcut migration seviyesi, bagimliliklar, veri hacmi ve
  cakisma kontrollerini yapar.
- Verify tablo/kolon varligi yaninda constraint, index, RLS, satir sayisi ve
  kritik mutabakatlari da kontrol eder.
- Yanlis veritabani veya yanlis tenant algilanirsa migration fail-fast durur.
- Destructive DDL varsayilan olarak ayni release'te yapilmaz; genislet-gecir-daralt
  uygulanir.

`[ACIK]` EF Core, Prisma, Flyway veya baska migration araci teknoloji ADR'siyle
belirlenecek. Arac secilene kadar elle SQL semasi baslatilmaz.

## 11. SQL yazim ve performans kurallari

- Parametreli sorgu zorunludur.
- `SELECT *` uygulama sorgularinda kullanilmaz.
- `search_path` varsayimina guvenilmez; schema adi yazilir.
- Siralama tekil degilse deterministik esitleyici alan eklenir.
- Sayfalama ve filtreleme sunucu tarafinda yapilir.
- N+1 sorgu, sinirsiz liste ve ekran acilisinda agir hesap engellenir.
- Kilit, transaction suresi, sorgu plani ve index etkisi gercekci veri hacmiyle
  olculur.
- Para ve stok mutabakati iki ondalik/dort ondalik hassasiyetle acik fark degeri
  dondurur; eksik kontrol basarili sayilmaz.

## 12. Canli oncesi SQL kontrol listesi

- Hedef veritabani ve ortam adi dogru
- Onayli tam yedek ve geri yukleme kaniti mevcut
- Migration checksum'lari degismemis
- Precheck basarili
- Aktif uzun transaction ve bloklayan oturum kontrol edildi
- Disk ve WAL icin yeterli bos alan var
- Apply sonucu kaydedildi
- Verify sonucu basarili
- RLS negatif testleri gecti
- Uygulama surumu migration seviyesiyle uyumlu
- Rollback/ileri duzeltme yolu ve sorumlu kisi belli
