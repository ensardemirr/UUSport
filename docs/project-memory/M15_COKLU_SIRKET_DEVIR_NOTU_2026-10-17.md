# M1.5 Coklu Sirket - Devir Notu (17 Ekim 2026)

**Amac:** Bu belge, ADR-012 (coklu sirket) uygulamasinin yarida kalmamasi icin yazildi. Ne
tasarlandi, ne yapildi, ne kaldi, nelere dikkat edilmeli; tek yerde. Bir sonraki oturum (Claude
Code veya Codex) once `AGENTS.md`, ana hafiza ve bu notu okuyup **bolum 5'ten** devam eder.

Durum etiketleri ana hafizadaki gibidir: `[DOGRU]` dogrulandi, `[KARAR]` kullanici onayi,
`[ONERI]` onaysiz, `[ACIK]` karar bekliyor.

---

## 1. Ne tasarladik (ozet; ayrintisi ADR-012)

Kaynaklar: `docs/decisions/ADR-012_COKLU_SIRKET_VE_KIRACI_MODELI.md` (`[KARAR]`),
`docs/designs/COKLU_SIRKET_YAPI_CALISMASI_2026-10-17.md` (calisma ve secenekler).

- `[KARAR]` **Sirket = kiraci.** Her vergi kimlik numarasi bir `core.tenant` satiri. Uc sirket:
  **UUSPORT (UUS), FITNESS (FIT), TIYATRO (TIY)**. Tek kurulum, tek veritabani, tek yedek.
- `[KARAR]` **Sirket basina rol**: `identity.company_membership(user_id, tenant_id, role, granted_keys,
  denied_keys)`. Roller: Yonetici, Muhasebe, **Ortak** (salt okunur, gorunurlugu parametrik).
  Hoca/Supervisor Faz 2. Patron = `app_user.is_group_admin` (tum sirketler, kullanici/uyelik yonetimi).
- `[KARAR]` **Birlesik okuma, tekil yazma (RLS v2):** okuma `app.tenant_ids` kumesi (ANY), yazma tek
  `app.tenant_id` (WITH CHECK). Birlesik gorunumde yanlis sirkete kayit veritabaninda imkansiz.
- `[KARAR]` **Yetki anahtarlari** (`write, parties.read, ledger.read, documents.read, catalog.read,
  cash.read, reports.summary, reports.detail, users.manage, audit.read`) + rol sablonlari + uyelik
  basina ver/kaldir. Ekran basina tik kutusu matrisi YOK. Ortak icin `write` ve `users.manage` asla acilmaz.
- `[KARAR]` **Grup ici islemler** (A, B'ye fatura/borc/tahsilat): karsi sirkette otomatik yazilmaz;
  "Muhasebe Bekleyen > Grup ici" kuyruguna oneri duser, karsi sirketin muhasebecisi onaylar,
  `integration.intercompany_link` ile baglanir. Birlesik raporda "Grup ici" ayri gosterilir.
- `[KARAR]` **Ayni veli iki sirkette** ayri cari; tek tahsilat parayi alan sirkette; fazla tutar
  `approved_adjustment` ciftiyle diger sirkete aktarilir (ADR-004 kaynak turu degismez).
- `[KARAR]` **Kod on eki:** `UUS-C000001`, `UUS-U000001`. Tedarikci kodu bizim kodla esitlenmez,
  `inventory.supplier_item_code` ile eslenir; alis faturasinda once tedarikci kodu/barkodla aranir.
- `[KARAR]` Mali musavir/aktarim profili sirket basina (ADR-006 yazilirken).
- Yol haritasi: bu is **M1.5**, M2'den once. Tahmin 7-9 ajan gunu; bugun ~2 gunluk kismi bitti.

## 2. Ne yaptik (bu oturum, 17.10.2026) - `[DOGRU]`

Dal: `claude/uusport-durumu-102mgc` (GitHub `ensardemirr/UUSport`). `master`'a birlestirilmedi.

### 2.1 Belgeler (commit 333134d, 9637b63)
- Yapi calismasi, ADR-012, ana hafiza 3.0 -> 3.1.

### 2.2 Linux'ta derleme ve test (bu commit)
- `.NET 10 SDK` + PostgreSQL 16 ile bulut oturumunda **72 test kosuldu ve gecti** (once degisiklik
  oncesi, sonra degisiklik sonrasi).
- `Infrastructure/DpapiSecrets.cs`: gizli bilgi once ortam degiskeninden okunur
  (`UUSPORT_SECRET_UUSPORT_DB_APP_DPAPI`, `UUSPORT_SECRET_UUSPORT_DB_OWNER_DPAPI`); yoksa Windows'ta
  DPAPI. Windows disinda DPAPI yoksa acik hata. **Canli davranisi degismedi** (Windows'ta ortam
  degiskeni tanimlanmaz). `Program.cs`: Data Protection anahtari yalniz Windows'ta DPAPI ile sarilir.
- Linux test ortami kurulumu (tekrar gerekirse): PG rolleri `uusport_owner`/`uusport_app`
  (`NOBYPASSRLS`), veritabanlari `uusport`, `uusport_test` ICU `tr-TR`; `tools/setup-uusport-dev-db.ps1`
  ile ayni SQL. Parolalar yalniz test icin, ortam degiskeniyle verilir.

### 2.3 Sema ve migration `20261017050655_M15MultiCompany`
- `core.tenant`: `code` (benzersiz, collate C), `legal_name`, `tax_number`, `tax_office`,
  `is_active` (varsayilan true), `sort_order`, `color_key`.
- `identity.company_membership` (yukarida), `identity.app_user.is_group_admin`,
  `crm.party.related_tenant_id` (+ `uq_party__tenant_related_tenant`),
  `inventory.supplier_item_code` (RLS'li).
- **Veri tasima:** en eski kiraci `UUS` kodu alir, digerleri `T00001..`; Identity rolleri uyelige
  donusur; her eski Yonetici **grup yoneticisi** olur; mevcut cari/urun kodlarina on ek eklenir
  (migration rolu FORCE RLS altinda satir goremedigi icin `NO FORCE` gecici kaldirilip `ApplyRlsV2`
  ile geri kurulur).
- `MigrationSecurity.TenantTablesAsOf20261017` (dondurulmus 20 tablo) + `ApplyRlsV2`
  (`ReadPolicySql`, `WritePolicySql`). Eski listeler ve `ApplyRls` (v1) degistirilmedi.
- `Down()` yalniz semayi geri alir; kodlar ve politika v1'e otomatik donmez (yedekten geri yukleme).

### 2.4 Uygulama katmani
- `Data/TenantContext.cs`: `TenantId` (yazma), `ReadTenantIds` (okuma kumesi), `TenantCode`,
  `IsConsolidated`, `Require()` birlesik modda `DomainException("company", "Kayit icin tek sirket secin.")`,
  `RequireCodeAsync(db)`. Interceptor `app.tenant_id` **ve** `app.tenant_ids` ayarlar.
- `Features/Auth/Permissions.cs`: anahtarlar, `Template(role)`, `Effective(...)`, `Grantable`.
- `Features/Auth/CompanyAccess.cs`: `CompanyAccess` (scoped), `CompanyContextMiddleware`
  (**UseAuthentication sonrasi, UseAuthorization oncesi**; `X-UUSport-Companies: id,id | all`
  basligi uyeliklerle kesistirilir, bos kesisim 403; Ortak yazma istegi 403 `read_only_role`),
  `CompanyRequirement` (Member/Admin/GroupAdmin), `PermissionRequirement`, `RequirePermission(...)`.
- `Program.cs`: politikalar `Staff` (secili her sirkette uyelik + MFA), `Admin` (secili her sirkette
  Yonetici veya grup yoneticisi), `GroupAdmin`, `perm:<anahtar>`. Identity rol claim'leri artik
  yetki icin KULLANILMIYOR (tablo duruyor).
- `Features/Auth/AuthEndpoints.cs`: kurulum ilk kullaniciyi grup yoneticisi + Yonetici uyeligi
  yapar, sirket kodu `CompanyCode` veya addan turetilir; `/api/auth/me` artik `isGroupAdmin, isAdmin,
  canWrite, permissions, companies[], selectedCompanyIds` doner; `onlyAdmin` = tek grup yoneticisi.
- `Features/Users/UserEndpoints.cs` yeniden yazildi: uyelik listesi, `memberships[]` ile olusturma /
  guncelleme (eski `role` alani geriye uyumlu: secili sirkete uyelik), sirket Yonetici'si yalniz
  yonettigi sirketlerdeki uyelikleri gorur/degistirir, baskasinin kullanicisi 404; son grup yoneticisi
  kaldirilamaz; uyelik degisikligi `SecurityStamp` yeniler. `GET /api/users/permission-keys`.
- `Features/Companies/CompanyEndpoints.cs` (yeni): `GET /api/companies` (izinli sirketler),
  `GET /api/companies/all`, `POST`, `PUT` (yalniz grup yoneticisi; kod sonradan degismez; yeni
  sirkete varsayilan Merkez Kasa/Banka/POS). `CompanyCodes.cs`: `Normalize`, `Derive`, `WithPrefix`.
- Kod on eki: `PartyEndpoints`, `CatalogEndpoints`, `ImportService` (dosyadaki on eksiz kod da eslesir).
- Okuma gate'leri: parties -> `parties.read`; items -> `catalog.read`; cash-accounts/loans/checks ->
  `cash.read`; ledger, allocations, pending-work -> `ledger.read`; documents -> `documents.read`;
  reports grubu -> `reports.summary`, open-balances/uninvoiced/unallocated -> `reports.detail`,
  stock -> `catalog.read`; imports -> `write`; audit -> Admin + `audit.read`.

### 2.5 Testler
- `TestSupport.cs`: test kiracisi benzersiz `Code` alir (Guid v7'nin **son** 5 hex'i; ilk hex'ler
  zaman damgasidir ve cakisir - bu hata bir kez yasandi).
- `EndpointAuthorizationTests`: `GroupAdmin` politikasi da yonetici-ozel sayilir; kataloga
  `/api/users/permission-keys`, `/api/companies/all`, `POST/PUT /api/companies` eklendi.
- Sonuc: **72/72 gecti** (yeni migration uygulanmis `uusport_test` + sifir veritabani zinciri).

## 3. Ne kaldi (sirayla) - yapilmasi gerekenler

1. **MultiCompanyTests (yeni dosya, oncelik 1).** Uc sirket, dort kullanici (patron, muhasebe A+B,
   muhasebe C, ortak B) - `WebApplicationFactory` ile (EndpointAuthorizationTests kalibi):
   - muhasebe C -> A carisine GET/PUT 404; ortak B her yazma ucunda 403 `read_only_role`;
   - baslik `A,B` ile POST -> 400 `company`; dogrudan SQL ile `WITH CHECK` reddi (RLS kaniti);
   - muhasebe A+B'den baslik `C` -> 403; `GET /api/users` baska sirketin kullanicisini listelemez;
   - Ortak'a `ledger.read` verilince `/api/ledger` 200, alininca 403;
   - birlesik rapor toplamlari = sirket toplamlarinin toplami.
2. **SchemaGuardTests**: politika metninin `app.tenant_ids` icerdigini ve `WITH CHECK`'in tekil
   oldugunu `pg_policies.qual / with_check` ile dogrula; `supplier_item_code` RLS listesinde.
3. **Birlesik modda liste satirlarina sirket kodu.** `AccountingQueries` satir kayitlarina
   (`LedgerListRow`, `PaymentRow`, `DocumentRow`, `LineRow`) ve cari listesine `CompanyCode` ekle
   (`db.Tenants` kodlari bir kez sozluge al; RLS yok). Dashboard ve raporlar `rows[].company`,
   `totalsByCompany`, `grandTotal` (sessiz birlestirme yok).
4. **Web (hic baslanmadi).** `api.ts`: `X-UUSport-Companies` basligi (modul degiskeni), `Me` tipi
   genisler. `auth.tsx`: `companies`, `selected` (localStorage), `isAdmin = me.isAdmin`,
   `canWrite = me.canWrite`, `isGroupAdmin`. `Layout.tsx`: ust cubukta rozetler (kisa kod + renk),
   "Tumu", coklu secim; birlesik modda banner "Birlesik gorunum: kayit icin tek sirket secin" ve
   yazma dugmeleri pasif. `UsersPage` uyelik duzenleme (sirket x rol tablosu, Ortak icin anahtar
   tikleri, "etkin yetkiler" salt okunur). Yeni `CompaniesPage` (`/yonetim/sirketler`, yalniz grup
   yoneticisi). Listelerde birlesik modda "Sirket" sutunu. `npm run build` + ekran goruntusu.
5. **Grup ici islemler** (ADR-012 2.5): `integration.intercompany_link`, "Grup ici cari ac"
   (`party.related_tenant_id`), oneri kuyrugu ve onay ucu, birlesik raporda "Grup ici" ayrimi.
6. **Fazla tahsilat aktarimi** (ADR-012 2.6): `approved_adjustment` cifti + karsi sirkette onay.
7. **Tedarikci kodu eslemesi uclari** (tablo var, uc yok): `GET/POST /api/items/{id}/supplier-codes`,
   alis satiri girisinde tedarikci kodu/barkodla arama, "once mevcut katalogdan sec" akisi.
8. **Belgeler:** guvenlik standardi bolum 3 (sirket basina rol, birlesik okuma/tekil yazma, yetki
   anahtarlari), PostgreSQL standardi bolum 7 (RLS v2 metni), ana hafiza 3.2, ADR-012 "acik konular".
9. Windows'ta dogrulama: `tools/build-uusport.ps1` (web + API + testler), masaustu kisayoluyla
   calistirma, kurulum ekrani -> UUS; `Yonetim > Sirketler` ile FIT ve TIY acma; ekran goruntusu.

## 4. Nelere dikkat edilmeli

- **Ara katman sirasi degismez:** `UseAuthentication -> CompanyContextMiddleware -> UseAuthorization`.
  Oturumsuz uclar (kurulum, giris) ilk kiraciyi `TenantResolver` ile alir; bu yalniz onlar icindir.
- **Yazma her zaman tek sirket.** `tenant.Require()` birlesik modda `DomainException` atar; yeni uc
  yazarken `TenantId = tenant.Require()` deseni korunur. Baska sirkete yazmak gerekiyorsa (sirket acma
  gibi) `tenant.TenantId/ReadTenantIds/TenantCode` acikca o sirkete cevrilir (bkz. `CompanyEndpoints`).
- **Denetim kaydi hedef sirkete yazilir** (`audit.Add(..., tenantOverride: tenantId)`), grup
  islemlerinde bile.
- **Migration kurali:** yeni RLS'li tablo icin `ApplySchemaGrants(mb, AppendOnlyTablesAsOf20260927)` +
  `ApplyRlsV2(mb, "<yeni tablo>")`. `TenantTablesAsOf20261017` ve eski listeler DEGISTIRILMEZ.
  Yeni append-only tablo gelirse yeni dondurulmus liste acilir. Zincir sifir veritabaninda denenir
  (`SchemaGuardTests.TemizKurulum_SifirVeritabani`).
- **Migration rolu FORCE RLS altinda satir goremez.** Veri tasima SQL'i yazarken `NO FORCE` gecici
  kaldirilir ve ayni migration icinde `ApplyRlsV2` ile geri kurulur (bu migrationda yapildi).
- **Sirket kodu sonradan degismez** (cari/urun kodlarinin on eki). Kullanici girdigi kod on eksiz
  olabilir; `CompanyCodes.WithPrefix` ekler. Excel'deki `cari_kodu`/`kod` da on eksiz eslesir.
- **Identity rolleri yetki kaynagi degil.** `IsInRole("Yonetici")` kullanan yeni kod yazilmaz;
  `CompanyAccess` kullanilir. `ICurrentUser.IsInRole` hala claim okur (eski kod icin); iskonto/eksi
  stok yetkisi (`AccountingService` 666) bunu kullanir; `HttpCurrentUser.IsInRole` artik `CompanyAccess`'ten okur (yapildi).
- **Ortak rolu:** `write` ve `users.manage` ASLA verilemez (`Permissions.Grantable` disinda). Varsayilan
  yalniz `reports.summary`; yonetici uyelikten `ledger.read`, `reports.detail` vb. acar.
- **Baslik yoksa ev sirketi** (`app_user.tenant_id`), o yoksa ilk izinli sirket. Istemci secimi her
  istekte gondermeli (CSRF basligi gibi `api.ts` icinde merkezi).
- **Gercek veri yok.** Sirket adlari ve kodlar disinda VKN, unvan girilmez; `tax_number` canlida dolar.
- **Test kiraci kodu**: Guid v7'nin ilk hex'leri zaman damgasidir, kod icin SON karakterler kullanilir.
- `CLAUDE.md`'deki "25 Eylul ara / dosya degistirme" satiri eskidir; kullanici onayiyla temizlenecek.
- Birlesik gorunum raporlari **yonetim raporudur**, resmi beyan degildir; KDV/vergi simulasyonu VKN
  basina ayri hesaplanir, toplanmaz (ADR-012 3).

## 5. Bir sonraki oturumun ilk adimlari

1. `git fetch origin claude/uusport-durumu-102mgc && git checkout claude/uusport-durumu-102mgc`.
2. Windows'ta: `tools/build-uusport.ps1` (test veritabanina migration uygulanir, 72 test). Hata varsa
   once bu nota bak (bolum 4).
3. Bolum 3 sirasinda devam et: 1 (MultiCompanyTests) -> 3 (sirket sutunu) -> 4 (web) -> 8 (belgeler).
4. Her adimda ana hafiza degisiklik gecmisine satir ekle; `[ONERI]`'yi `[KARAR]` gibi uygulama.
5. Is bittiginde `master`'a birlestirme kullanici onayiyla; canliya alma yok (canli ortam yok).

## 6. Acik sorular (kullaniciya)

- `[ACIK]` Ortak sablonunun varsayilan anahtar listesi (su an yalniz `reports.summary`).
- `[ACIK]` Fazla tahsilat aktariminin vergisel niteligi (mali musavir).
- `[ACIK]` Grup ici faturanin karsi tarafta otomatik alis belgesi olmasi (e-belge entegratoru ile).
- `[ACIK]` Sirket renkleri ve sira (UUS, FIT, TIY) - arayuz tasariminda.
