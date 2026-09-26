# ADR-003 Harmanlama (Collation) ve Turkce Metin Kurallari

**Durum:** `[KARAR]` Kullanici tarafindan 26 Eylul 2026 tarihinde onaylandi
**Tarih:** 26 Eylul 2026
**Hazirlayan:** Claude Code (Opus 5.5)
**Karar veren:** Kullanici
**Iliskili:** ADR-001; denetim raporu O2; PostgreSQL standardi bolum 1 ve 4

---

## 1. Baglam

- Gelistirme betikleri PostgreSQL kumesini `--locale-provider=icu --icu-locale=tr-TR` ile
  kuruyor; PostgreSQL standardi harmanlama hakkinda hicbir sey soylemiyor (denetim O2).
- UUSport veritabani **henuz olusturulmadi**; harmanlama bugun bedelsiz secilebilir.
- Turkcede `I/i` ve `İ/ı` dort ayri harftir. Turkce kurallarla `'I'` kucuk harfe `'ı'`,
  `'i'` buyuk harfe `'İ'` olur. Ingilizce/ASCII kurallarla `'I'` -> `'i'` olur.
- **Yasanmis olay (ENS_Stp.App, 21.09.2026):** Canli gorunumde kolon adi `TEVKİFAT`, program
  `TEVKIFAT` okuyordu. `Turkish_CI_AS` harmanlamasinda `I` ile `İ` esit sayilmadigi icin
  senkron duracakti; dogrulama betigi son anda yakaladi.

---

## 2. Karar

### 2.1 Veritabani varsayilani

UUSport veritabani migration ile **acikca** olusturulur, kumenin varsayilanina guvenilmez:

```sql
CREATE DATABASE uusport
  TEMPLATE template0
  ENCODING 'UTF8'
  LOCALE_PROVIDER icu
  ICU_LOCALE 'tr-TR'
  LOCALE 'C';
```

Varsayilan harmanlama Turkce'dir; boylece ad, soyad, unvan ve aciklama gibi insan metinleri
dogru siralanir (`c, ç, g, ğ, ı, i, o, ö, s, ş, u, ü`).

### 2.2 Sutun turleri

| Sutun turu | Ornek | Harmanlama | Buyuk/kucuk harf |
|---|---|---|---|
| **Insan metni** | ad, soyad, unvan, aciklama, urun adi | Varsayilan (`tr-TR`) | Turkce kurallar |
| **Kod ve anahtar** | cari kodu, stok kodu, belge no, e-posta, kullanici adi, VKN/TCKN, dis sistem anahtari, dosya hash'i, durum kodu | **`COLLATE "C"`** acikca yazilir | ASCII kurallar |
| **Arama anahtari** | `arama_metni` gibi turetilmis sutun | `COLLATE "C"` | Normalize edilmis (bolum 2.3) |

Kod ve anahtar sutunlarinda `COLLATE "C"` migration'da **her seferinde acikca** yazilir.
Unutulmasi, sutunun sessizce Turkce kurallara dusmesi demektir.

### 2.3 Arama

Buyuk-kucuk harf ve Turkce karakter duyarsiz arama icin veritabani fonksiyonlarina
(`lower`, `ILIKE`) guvenilmez. Bunun yerine:

- Aranacak her tabloda uygulama tarafindan uretilen bir **arama anahtari** sutunu tutulur.
- Normalizasyon **tek bir C# fonksiyonunda** yapilir: Turkce kucuk harfe cevir, sonra
  `ı->i`, `ğ->g`, `ü->u`, `ş->s`, `ö->o`, `ç->c` katla, fazla bosluklari temizle.
- Arama metni de ayni fonksiyondan gecer. Boylece "ISIK", "Işık", "isik", "IŞIK" ayni
  kaydi bulur.
- Sonuclar gosterilirken siralama insan metni sutunu uzerinden (Turkce) yapilir.

### 2.4 E-posta ve kullanici adi

- Kayit aninda **ASCII kurallariyla** kucuk harfe cevrilir (`ToLowerInvariant`), `COLLATE "C"`
  sutunda saklanir, benzersiz indeks bu sutunda olur.
- Turkce kurallarla kucuk harfe cevirmek `INFO@...` adresini `ınfo@...` yapar; bu yasaktir.

### 2.5 Uygulama kodu kurallari

**C#:**

- Parametresiz `ToLower()`, `ToUpper()`, `string.Compare(a, b)`, `StartsWith(x)` gibi
  kulture bagli cagrilar **yasaktir**. Turkce Windows'ta `"ID".ToLower()` sonucu `"ıd"` olur.
- Kod/anahtar icin: `ToLowerInvariant()`, `StringComparison.Ordinal`.
- Insan metni icin: `CultureInfo.GetCultureInfo("tr-TR")` acikca verilir.
- Kulture bagli cagrilari yakalayan kod analizi kurallari (CA1304, CA1309, CA1310, CA1311)
  **hata** seviyesinde acilir.

**TypeScript (web ve mobil):**

- Insan metni: `toLocaleLowerCase('tr-TR')`, `localeCompare(x, 'tr-TR')`.
- Kod/anahtar: `toLowerCase()`.

### 2.6 Adlandirma

PostgreSQL nesne adlari zaten ASCII `lower_snake_case` (standart bolum 1). Bu ADR bunu
teyit eder: nesne, kolon, JSON alan ve API alan adlarinda Turkce karakter kullanilmaz.
ENS'teki `TEVKİFAT` olayi bu kuralin gerekcesidir.

### 2.7 ICU surumu ve tasima

ICU tabanli harmanlamanin siralama kurallari ICU surumuyle degisebilir. Bu, ofis
bilgisayarindan RDP'ye tasimada ve PostgreSQL/isletim sistemi yukseltmelerinde onemlidir:

- Precheck adimi veritabaninin harmanlama surumunu kaydeder ve hedefteki surumle
  karsilastirir.
- Surum farkliysa geri yukleme sonrasi metin indeksleri yeniden olusturulur (`REINDEX`) ve
  `ALTER DATABASE ... REFRESH COLLATION VERSION` calistirilir.
- Bu adim operasyon prosedurunun tasima ve yukseltme kontrol listesine girer.

---

## 3. Gerekce

- **Turkce varsayilan:** Kullaniciya gosterilen listelerin cogunlugu insan adidir; yanlis
  siralama gunluk kullanimda hemen fark edilir.
- **Kodlarda `"C"`:** Kod ve anahtar alanlarinda dil kurali istenmez; benzersizlik ve
  karsilastirma bayt bazinda ongorulebilir olmalidir. ENS olayi tam olarak bu ayrimin
  yapilmamasindan dogdu.
- **Arama anahtari:** Veritabani fonksiyonlariyla Turkce duyarsiz arama hem yavas hem
  hataya aciktir; normalize edilmis sutun indekslenebilir ve tek noktadan test edilir.
- **Acik `CREATE DATABASE`:** Gelistirme kumesinin yerel ayari dogrulanamadi (denetim O2);
  canli makinede farkli olabilir. Veritabani kendi harmanlamasini tasir.

---

## 4. Sonuclar

**Olumlu:** Siralama dogru, benzersizlik ongorulebilir, arama tutarli; ENS'teki I/İ olayi
tekrarlanmaz; tasimada harmanlama farki yakalanir.

**Olumsuz:** Her kod sutununda `COLLATE "C"` yazmak disiplin ister; arama anahtari sutunlari
ek alan ve ek guncelleme mantigi getirir.

---

## 5. Acik konular

| # | Konu | Not |
|---|---|---|
| 1 | Mevcut gelistirme kumesinin gercek yerel ayari | Sunucuya baglanarak dogrulanmali (denetimde dogrulanamadi). Bu ADR veritabanini acikca olusturdugu icin sonucu engellemez |
| 2 | Tam metin arama (`tsvector`) gerekip gerekmedigi | Ilk surumde gerekmez; arama anahtari yeterli. Gerekirse PostgreSQL'in Turkce sozlugu degerlendirilir |

---

## 6. Onay

- [x] Kullanici onayi - tarih: 26 Eylul 2026

**Onaydan sonra:** bu belge `[KARAR]` yapilir; PostgreSQL standardina harmanlama bolumu ve
bolum 2.2 tablosu eklenir; operasyon prosedurunun tasima kontrol listesine bolum 2.7
eklenir; ana hafizaya baglanti verilir.
