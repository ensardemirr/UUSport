# UU Sport - Coklu Sirket (Birden Fazla Vergi Kimlik Numarasi) Yapi Calismasi

**Durum:** `[ONERI]` - kullanici karari bekliyor; kod degisikligi yok  
**Tarih:** 17 Ekim 2026  
**Yazan:** Claude Code  
**Ilgili:** ADR-012 adayi (kiraci cozumleme), guvenlik standardi bolum 3-4, PostgreSQL standardi bolum 7, danisma sentezi M2-M4

## 0. Istek ozeti

Bugune kadar tek vergi kimlik numarali tek sirket icin planlandi. Karar verici toplam
**uc ayri vergi kimlik numarali sirketi** (A, B, C) ayni programdan yonetmek istiyor:

- Patron (asil yonetici) uc sirketin tum gelir/giderini gorur.
- Ayni ekranda dugmeyle "yalniz A", "yalniz B", "B ve C" gibi secimler yapar.
- Muhasebeci 1 yalniz A ve B'ye bakar ve yazar; muhasebeci 2 yalniz C'ye bakar.
- Bir sirkette ortak varsa o ortak yalniz o sirketin yonetim verilerini gorur.
- Finansal simulasyon (borc/alacak, gelecek ay/yil nakit akisi) sirket bazinda ve
  secilen sirketler toplaminda calisir.
- Patron, kimin hangi ekrani gorecegini yonetim panelinden belirler ve kullanici
  yetkilerini gorur.
- Sirketlerde ogrenci -> hoca -> supervisor hiyerarsisi var (akademi modulu, Faz 2).

## 1. Mevcut durumun tespiti (kod incelemesi)

`[DOGRU]` Asagidakiler 17.10.2026'da `master` (49ccb00) uzerinde dogrulandi.

| Konu | Mevcut durum | Kaynak |
|---|---|---|
| Kiraci (tenant) tablosu | `core.tenant(id, name, created_at)`; tek satir, kurulumda olusur | `Domain/Entities.cs`, `AuthEndpoints.cs` setup |
| Veri siniri | 20 is tablosunun **hepsinde** `tenant_id`; RLS acik ve zorlanmis (FORCE); politika `tenant_id = current_setting('app.tenant_id')` | `Data/MigrationSecurity.cs` |
| Kiraci secimi | `TenantResolver` ilk kiraciyi okur ve **onbellekler**; her `/api` istegi ayni kiraciyi alir | `Data/TenantContext.cs`, `Program.cs` |
| Baglantiya aktarim | Baglanti acilisinda `set_config('app.tenant_id', ...)` (oturum duzeyi) | `TenantConnectionInterceptor` |
| Sorgularda kiraci filtresi | Kod RLS'e guvenir: `TenantId ==` filtresi yalniz 1 yerde, `tenant.Require()` 15 yerde (yazimda) | `Features/*` |
| Kullanici-kiraci | `AppUser.TenantId` tek deger; kullanici tek kiraciya baglidir | `Entities.cs` |
| Roller | Iki sabit rol: `Yonetici`, `Muhasebe`; kullanici basina **tek rol**; Identity rolleri kiracidan bagimsiz | `Roles`, `UserEndpoints.cs` |
| Yetki politikalari | `Staff` (iki rol + MFA), `Admin` (Yonetici + MFA); ekran/uc bazli ince yetki yok | `Program.cs` 146-150 |
| Benzersizlik | Cari kodu, urun kodu, fatura no, aktarim hash'i `(tenant_id, ...)` ile benzersiz | `AppDbContext.cs` |
| Denetim kaydi | `audit.audit_event.tenant_id` var | `AuditWriter.cs` |
| Arayuz | Ust cubukta kiraci/sirket secici yok; menu rol bazli (Yonetim grubu yalniz Yonetici) | `Web/src/Layout.tsx` |
| Testler | `TestWorld` her testte yeni kiraci olusturur; `EndpointAuthorizationTests` tum `/api` uclarini rol bazli dogrular; **capraz kiraci testi yok** | `tests/UUSport.Tests` |

**Sonuc:** Veri modeli zaten kiraci sinirli tasarlanmis; eksik olan (1) kullanici-sirket-rol
iliskisi, (2) istek basina sirket secimi, (3) birden fazla sirketi ayni anda okuyabilen
birlesik gorunum, (4) ince yetki matrisi ve yonetim ekrani, (5) raporlarda sirket boyutu.

Danisma raporu (Claude, 27.09, satir 558-573) zaten "kiraci bilgisi girişte claim'e
yazilsin, TenantResolver yalniz kurulum yardimcisi olsun, capraz kiraci kullanici testleri
eklensin" demisti; bu calisma o oneriyi coklu sirket ihtiyaciyla birlestirir.

## 2. Temel soru: "sirket" ile "kiraci" ayni sey mi?

Uc secenek degerlendirildi.

### Secenek A - Sirket = kiraci (her vergi kimlik numarasi bir `core.tenant` satiri)

Mevcut RLS, benzersizlik ve denetim yapisi **oldugu gibi** kullanilir. Eklenenler:
kullanici-sirket uyelik tablosu, istek basina sirket secimi, coklu okuma politikasi.

- Arti: 20 tabloya dokunulmaz; izolasyon veritabaninda zaten var; her sirketin ayri
  cari, kasa/banka, katalog ve belge numarasi olmasi **muhasebe gercegiyle** uyumlu (her
  vergi kimlik numarasi ayri defter tutar, ayri KDV beyani verir).
- Eksi: Ayni tedarikci veya ayni veli iki sirkette iki ayri cari karti olur (muhasebede
  zaten boyledir; birlesik raporda ad/telefonla "ayni kisi" olarak gruplanabilir, veri
  birlestirilmez). Ileride SaaS'ta "musteri = sirket grubu" kavrami icin kiracinin ustune
  `core.tenant_group` eklenmesi gerekir; bu kucuk bir ektir, RLS degismez.
- Efor: orta (tahmini 5-7 ajan gunu, test dahil).

### Secenek B - Kiraci ustte kalir, altina `core.company` eklenir

Kiraci "kurulum/musteri" olur; her is tablosuna `company_id` eklenir; RLS iki kademeli olur.

- Arti: Kavramsal olarak en temiz; SaaS'a hazir; ortak kullanici havuzu ve istenirse
  sirketler arasi ortak katalog mumkun.
- Eksi: 20 tabloya kolon + tum benzersiz indekslerin yeniden kurulmasi + 15 yazim
  noktasi + tum testler; sifir veritabaninda migration zinciri yeniden denenir. Mevcut
  gelistirme verisi (sentetik) tasinir. ADR-004 ve mevcut sema belgeleri guncellenir.
- Efor: yuksek (tahmini 12-16 ajan gunu). Kazanci su an icin A'ya gore kucuk.

### Secenek C - Uc ayri kurulum / uc veritabani

- Arti: Sifir kod degisikligi.
- Eksi: Birlesik gorunum ve ortak kullanici yonetimi yok; uc kez yedek, uc kez
  guncelleme, uc kez giris. Istegi karsilamaz.

### Oneri

`[ONERI]` **Secenek A.** Gerekce: istek, mevcut kiraci sinirinin dogrudan uzantisidir;
muhasebe gercegi ("her VKN ayri defter") ile birebir orusur; RLS ve testler yeniden
yazilmaz; ileride B'ye gecis gerekirse `tenant_group` ile ayni sonuca varilir.
Kullaniciya gorunen ad "Sirket" olur; kodda `Tenant` adi korunur (yeniden adlandirma
riske degmez), belgelere "kiraci = sirket (tuzel kisilik)" notu dusulur.

## 3. Hedef model (Secenek A)

### 3.1 Sema degisiklikleri (tek migration, standart bolum 7-10)

```text
core.tenant
  + legal_name      text          -- unvan (orn. "A Spor Egitim Ltd. Sti.")
  + short_name      text          -- ekranda rozet (orn. "A")
  + tax_number      text collate "C" null   -- VKN; gercek deger yalniz canlida girilir
  + tax_office      text null
  + is_active       bool default true
  + sort_order      smallint default 0
  + color_key       text null     -- arayuzde sirket rengi (erisilebilir palet anahtari)

identity.company_membership            -- kullanicinin bir sirketteki rolu
  id uuid pk, user_id uuid fk, tenant_id uuid fk,
  role text collate "C" not null,      -- 'yonetici' | 'muhasebe' | 'ortak' | (faz 2: 'supervisor','hoca')
  is_active bool default true,
  granted_by_user_id uuid, granted_at timestamptz, revoked_at timestamptz null
  unique (user_id, tenant_id) where is_active

identity.app_user
  + is_group_admin  bool default false   -- patron: tum sirketleri gorur, uyelik yonetir
  tenant_id -> "ev sirketi" olarak kalir (varsayilan secim); zorunlu olmaktan cikar
```

RLS politikasi her korunan tabloda yenilenir (yeni dondurulmus liste
`TenantTablesAsOf20261017` ile; eski listelere dokunulmaz, bolum 5.6 kurali):

```sql
USING      (tenant_id = ANY (string_to_array(NULLIF(current_setting('app.tenant_ids', true), ''), ',')::uuid[]))
WITH CHECK (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid)
```

Okuma: secili sirket kumesi. Yazma: **tam olarak bir** sirket. Boylece "birlesik
gorunumde yanlis sirkete kayit" veritabani seviyesinde imkansiz olur.

`identity.company_membership` tablosu da RLS'e girer mi? Girmez (kullanici yonetimi
grup duzeyindedir); erisimi yalniz `is_group_admin` veya ilgili sirketin Yonetici'si
API katmaninda alir ve `EndpointAuthorizationTests` ile kanitlanir.

### 3.2 Istek basina sirket baglami

1. Giris sonrasi `/api/auth/me` kullanicinin uyeliklerini doner:
   `companies: [{id, shortName, legalName, role}]`, `isGroupAdmin`.
2. Istemci secimi `X-UUSport-Companies: <id>,<id>` basligiyla gonderir (CSRF basligi gibi
   yazan ve okuyan her istekte). Baslik yoksa ev sirketi.
3. Ara katman `UseAuthentication`'dan **sonra** calisir: istenen kumeyi kullanicinin
   aktif uyelikleriyle **keser**; kesisim bos ise 403. `TenantContext.ReadTenantIds`
   ve (kume tek elemanli ise) `TenantContext.TenantId` dolar.
4. Yazan uclar (`POST/PUT`) `tenant.Require()` cagirir; kume birden fazla sirket iceriyorsa
   `DomainException("company", "Kayit icin tek sirket secin.")`.
5. `TenantResolver` yalniz ilk kurulum ve anonim uclar icin kalir (danisma onerisi).
6. `TenantConnectionInterceptor` iki ayari birden yazar. Danisma raporunun
   "transaction basinda, yerel" onerisi (ACC-06/UnitOfWork) bu isle birlestirilebilir
   ama zorunlu degildir.

### 3.3 Rol ve yetki modeli ("uc yer kurali"na uygun tek matris)

Mevcut iki global rol, **sirket basina rol** olur. Yetkiler kod icinde tek listede
tanimlanir ve API politikasi, RLS yazma kurali ve arayuz menusu ayni listeden beslenir
(guvenlik standardi bolum 3).

| Rol (sirket basina) | Gorur | Yazar | Yonetir |
|---|---|---|---|
| Grup yoneticisi (`is_group_admin`, patron) | Tum sirketler, tum ekranlar, birlesik gorunum | Secili tek sirkete her islem | Sirketler, kullanicilar, uyelikler, yetki matrisi |
| Yonetici | O sirketin tum ekranlari | O sirkette her islem | O sirketin kullanici uyelikleri (grup yoneticisi disinda) |
| Muhasebe | O sirketin is ekranlari | Satis/alis satiri, tahsilat, fatura girisi, dagitim; iskonto sinirina tabi | - |
| Ortak (yeni) | O sirketin yonetim ve rapor ekranlari, cari bakiyeleri, nakit akisi | **Hicbir yazma** | - |
| Supervisor, Hoca (Faz 2, akademi) | Kendi grubu/sporculari; **borc bilgisi gorunmez** (O27) | Yoklama, gelisim notu (Faz 2) | - |

Ince yetki icin oneri: ekran basina tik kutusu matrisi **degil**, kod ile tanimli
"yetki anahtarlari" (orn. `parties.read`, `payments.write`, `reports.cashflow`,
`users.manage`) ve bunlari gruplayan rol sablonlari. Yonetim panelinde patron:

- Kullanici x sirket tablosunda rol atar/kaldirir.
- "Etkin yetkiler" sekmesinde kullanicinin her sirkette hangi anahtarlara sahip oldugunu
  **salt okunur** gorur (bu, istenen "kullanici yetkilerini gorebilme" ihtiyacidir).
- Ozel rol (anahtar secerek) ikinci asamaya birakilir; `[ACIK]`.

Kurallar:
- Kendi grup yoneticisi yetkisini veya son grup yoneticisini kaldirma engellenir
  (mevcut "tek yonetici" uyarisi grup duzeyine tasinir).
- Uyelik degisikligi `SecurityStamp` yeniler; acik oturum en gec 1 dakikada guncellenir.
- Her uyelik degisikligi denetim kaydina (`membership.grant/revoke`) yazilir; kayit hem
  hedef sirkete hem de (grup islemi oldugu icin) islemi yapanin baglamina dusmez, **hedef
  sirket** kiracisina yazilir.

### 3.4 Birlesik gorunum (patron ekrani)

- Ust cubukta sirket secici: her sirket icin bir rozet (kisa ad + renk), "Tumu" dugmesi,
  coklu secim. Secim tarayicida hatirlanir (`localStorage`, yalniz tercih).
- Birlesik modda her listeye "Sirket" sutunu eklenir; toplamlar sirket basina ve genel
  olarak ayri gosterilir (**sessizce birlestirilmez**, rapor ilkesiyle uyumlu).
- Birlesik modda yazma dugmeleri pasiflesir ve "Kayit icin tek sirket secin" ipucu cikar;
  sunucu da reddeder (3.1 WITH CHECK).
- Genel Bakis sayaclari (`/api/dashboard`) sirket bazinda doner; kart basina rozet.

### 3.5 Raporlar ve finansal simulasyon

- Mevcut raporlar (`/api/reports/*`) birlesik modda `tenant_id` ile gruplanir; cikti
  `rows[].company` + `totalsByCompany` + `grandTotal`.
- Nakit akisi/simulasyon (M4, ADR-010 plan tablolari) zaten `tenant_id` tasir; sirket
  bazli simulasyon dogal olarak calisir. Birlesik simulasyon yalniz **nakit toplamidir**;
  KDV ve gecici vergi simulasyonu her VKN icin **ayri** hesaplanir ve ayri gosterilir
  (beyanlar ayridir, toplanmaz).
- `[ACIK]` **Grup ici islemler:** A sirketi B'nin giderini oderse veya B'ye borc verirse,
  her sirkette karsi sirket icin bir cari acilir (`party.is_group_company = true` veya
  `party.related_tenant_id`). Birlesik raporda bu satirlar "Grup ici" olarak ayri
  gosterilir ve istege bagli olarak elenir. Bu, ADR-004 modelini degistirmez.

### 3.6 Diger etkiler

| Alan | Etki |
|---|---|
| Ilk kurulum | Ilk sirketi ve grup yoneticisini olusturur (bugunku akis). "Yonetim > Sirketler" ekrani ikinci ve ucuncu sirketi ekler; her yeni sirkette varsayilan "Merkez Kasa/Banka/POS" acilir |
| Excel aktarimi | Tek sirkete yapilir; aktarim ekrani secili tek sirketi gosterir; hash benzersizligi zaten sirket basina |
| Cari/urun kodlari | Her sirkette `C000001`'den baslar. Birlesik listede sirket rozeti ile ayrisir. `[ACIK]` Istenirse sirket on eki (`A-C000001`) |
| Fatura numaralari | Zaten sirket basina benzersiz; VKN farkli oldugu icin dogru |
| Denetim kaydi | Hangi sirket kumesiyle okundugu `summary`'ye eklenir; grup yoneticisinin birlesik okumalari izlenir |
| Yedek/geri yukleme | Tek veritabani, degismez. Sirket bazli disa aktarim (danisma: `tools/tenant-export`) ileride tek sirketi ayirmaya yarar |
| Mali musavir | Uc sirketin musaviri ayni olsa da hesap plani ve GMS.NET aktarimi sirket basinadir (ADR-006 adayi) |
| Guvenlik standardi | Bolum 3'e "sirket basina rol" ve "birlesik okuma / tekil yazma" kurali eklenir; RLS negatif testlerine capraz sirket senaryolari eklenir |
| ADR-005 kapsami | Degismez; coklu sirket **yapisal** bir degisikliktir, ozellik degil |

## 4. Test plani (is bitmis sayilmasi icin)

1. `MultiCompanyTests`: uc sirket, dort kullanici (patron, muhasebe A+B, muhasebe C,
   ortak B). Her biri yalniz izinli sirketleri gorur; muhasebe C, A'nin carisine
   `GET` -> 404, `PUT` -> 404; ortak B her yazma ucunda 403.
2. Birlesik okumada (`A,B`) yazma -> 400 "tek sirket secin"; RLS `WITH CHECK` dogrudan
   SQL ile de reddeder (veritabani seviyesi kanit).
3. Istenmeyen sirket basligi (`X-UUSport-Companies: <C>` muhasebe A+B'den) -> 403.
4. `EndpointAuthorizationTests` genisler: her `/api` ucu icin "yetkisiz sirket" durumu.
5. `SchemaGuardTests` yeni politika metnini (ANY + tekil WITH CHECK) dogrular; sifir
   veritabaninda 5 migration zinciri bastan calisir.
6. Birlesik rapor toplamlari = sirket toplamlarinin toplami (sessiz birlestirme yok).
7. Ekran goruntusu: sirket secici, birlesik liste, pasif yazma dugmesi, yetki matrisi.

## 5. Yol haritasina etkisi

`[ONERI]` Bu is **M2'den once** yapilmali ve "M1.5 - Coklu sirket" olarak eklenmeli.
Gerekce: S-21 (sunucu ekstre), S-23 (acilis bakiyesi), S-25 (donem kilidi), S-26
(mutabakat) ve M4 (nakit akisi) sirket boyutunu dogrudan kullanir; once yapilmazsa her
biri iki kez yazilir. ADR-012 (kiraci cozumleme ve SaaS kapilari) bu kararla birlikte
yazilir ve "kiraci = sirket (VKN)" tanimini sabitler.

Tahmini sira ve efor (Sonnet, orta efor; sema ve yetki tasarimi bu belgeyle Opus
tarafindan yapilmis olur):

| Adim | Icerik | Efor |
|---|---|---|
| 1 | ADR-012 yazimi ve onayi | 0,5 gun |
| 2 | Migration (tenant kolonlari, uyelik tablosu, RLS v2), `MigrationSecurity` yeni dondurulmus liste | 1 gun |
| 3 | `TenantContext` / ara katman / `me` ucu / politikalar / uyelik uclari | 1,5 gun |
| 4 | Web: sirket secici, birlesik mod, yonetim > sirketler ve uyelikler, etkin yetkiler | 1,5-2 gun |
| 5 | Raporlar ve genel bakista sirket boyutu | 0,5 gun |
| 6 | Testler (bolum 4), ekran goruntuleri, ana hafiza ve standart guncellemesi | 1 gun |

Toplam 6-7 ajan gunu. M1 kalanlari (yedek gorevi, S-18 paketleme) bundan bagimsizdir
ve paralel yurutulebilir.

## 6. Kullanici karari bekleyen sorular

- **KU-8** Secenek A (sirket = kiraci, tek kurulum, tek veritabani) onaylaniyor mu?
- **KU-9** Sirket listesi: uc sirketin kisa adi ve hangi sektorde oldugu (VKN ve unvan
  gelistirmeye girmez; canlida girilir). Baslangic uyelikleri: kim hangi sirkette hangi
  rol? (Gercek ad gerekmez; "Muhasebeci 1: A+B" yeter.)
- **KU-10** "Ortak" rolu neyi gormeli: tum cari bakiyeleri ve hareketleri mi, yalniz
  ozet raporlar ve nakit akisi mi? Yazma hakki olmamasi onerilir.
- **KU-11** Sirketler arasi para hareketi var mi (biri digerinin giderini oduyor, borc
  veriyor)? Varsa 3.5'teki "grup ici cari" tasarimi ilk surume girer.
- **KU-12** Ayni veli/ogrenci birden fazla sirkette hizmet alabiliyor mu? (Oneri: her
  sirkette ayri cari; birlesik gorunumde telefonla gruplanir, veri birlestirilmez.)
- **KU-13** Uc sirketin mali musaviri ayni mi? (Hesap plani ve aktarim sirket basina
  kalir; cevap yalniz ADR-006 planlamasini etkiler.)
- **KU-14** Hoca ve supervisor rolleri Faz 2 akademi moduluyle mi acilsin (oneri: evet;
  yetki modeli simdi onlari tasiyacak sekilde kurulur, ekranlari sonra gelir)?
- **KU-15** Cari/urun kodlarinda sirket on eki istenir mi, yoksa rozet yeterli mi?

## 7. Bu calismada yapilmayanlar

- Kod, migration veya test degisikligi yapilmadi.
- Ana hafizaya yalniz bu belgeye baglanti ve `[ONERI]` notu eklendi; karar olarak
  islenmedi.
- Spesifikasyon 0.11 masaustunde oldugu icin okunamadi; coklu sirketle ilgili bir madde
  iceriyorsa ADR-012 yazilirken karsilastirilacak.
