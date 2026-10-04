# UUSPORT_DANISMA_CLAUDE_2026-09-27

## 0 META
arac: Claude (yapılandırılmış model claude-opus-5-5), Claude uygulaması; kullanıcının bilgisayarına bağlı oturum, depo salt okunur (docs, src, tests, deployment); `uu sport ltd/`, secrets ve veritabanı açılmadı
okunan_ekler: [DIS_YZ_STRATEJI_DANISMA_PROMPTU_2026-09-27.md, 01-genel-bakis-ve-menu.png, 02-cari-detay.png, 03-cari-detay-koyu-gorunum.png, 04-ekstre-toplam.png, 05-ekstre-resmi-cari.png, 06-hesap-hareketleri.png, 07-tahsilat-penceresi.png, 08-muhasebe-bekleyen.png, 09-kasa-banka-hiyerarsi.png, 10-kredi-taksitler.png, 11-cek-portfoyu.png, 12-raporlar.png, 13-excel-aktarim.png, 14-ekstre-pdf-ciktisi.pdf (içerik: Hesap Hareketleri sayfasının çıktısı, cari ekstre değil), ADR-001..005, UUSPORT_GUVENLIK_STANDARDI.md, UUSPORT_POSTGRESQL_ADLANDIRMA_VE_MIGRATION_STANDARDI.md (başlıklar), MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md, SURUM2_HAZIRLIK_NOTU_2026-09-26.md, UUSPORT_GELISTIRME_VERI_VE_CANLIYA_ALMA_PROSEDURU.md (bölüm 9-10), UUSPORT_ANA_HAFIZA.md (bölüm 0, 5.6, 5.7), CLAUDE_CODE_UUSPORT_DENETIM_RAPORU_2026-09-25.md (başlıklar), Entities.cs, AppDbContext.cs, TenantContext.cs, MigrationSecurity.cs, Program.cs, appsettings.json, WebPipeline.cs, CurrentUser.cs, AuditWriter.cs, AccountingQueries.cs, AccountingService.cs, AccountingEndpoints.cs, AuthEndpoints.cs, UserEndpoints.cs (seçme), PartyEndpoints.cs, CatalogEndpoints.cs, ReportEndpoints.cs, CheckService.cs, LoanService.cs, ImportService.cs (seçme), 2 migration (güvenlik çağrıları), Web: PartyPages.tsx, WorkPages.tsx, Dialogs.tsx, CashDetailPages.tsx, components.tsx, styles.css (seçme), tests/*.cs (adlar + LedgerOfficialFlagTests)]
varsayimlar:
  - A1: Şirket Ltd. Şti.; kurumlar vergisi ve KDV mükellefi; satış faturalarını GİB portalında e-Arşiv olarak düzenliyor, e-Fatura mükellefi değil (MM teyidi).
  - A2: 150-600 aktif sporcu, ayda 300-1.500 hareket; Faz 1'de eşzamanlı kullanıcı ≤ 3.
  - A3: Tek para birimi TRY; döviz hesabı ve dövizli cari yok.
  - A4: MM, GMS.NET'e Excel/XML muhasebe fişi içe aktarabiliyor; GMS.NET API lisansı yok.
  - A5: MM banka hareketlerini banka ekstresinden kendisi işliyor; e-Arşiv faturalarını GİB/UBL üzerinden alıyor.
  - A6: Personel ≤ 15; bordro MM'de hazırlanıyor, UU Sport'a yalnız toplamlar gelecek.
  - A7: İşyeri kirası gerçek kişiye ödeniyorsa kira stopajı var (MM teyidi).
  - A8: Ekran görüntüleri 26-27.09 sürümünden; ekran ile kod çeliştiğinde 27.09 tarihli kod esas alındı.
  - A9: "Resmi" = MM'nin GMS.NET defterine girmiş veya girecek olan; UU Sport resmi defter değildir.
  - A10: Canlıya geçiş tarihi bir ay sonu olacak (ör. 31.10.2026).

## 1 OZET
- Çekirdek sağlam: ADR-004 tek yazma noktası, append-only, CHECK kısıtları, RLS+FORCE, sunucuda para. Bu temel korunmalı.
- En büyük açık "Resmi" kavramı: üç yerde üç farklı tanım; resmi ekstre PDF'i kendi içinde tutarsız ve belgesiz hareketlerin varlığını ele veriyor (ACC-01..03).
- Tahsilatın resmiliği dağıtıma değil kanala bağlanmalı: banka/POS/çek MM'ye zaten ekstreyle gider (ACC-03, INT-02).
- Canlı öncesi zorunlu: yedek+geri yükleme provası, HTTPS/LAN, gerçek Excel'in depodan çıkması, açılış bakiyesinin tarih+kapsamla yeniden tasarımı, kasa/banka hareket defteri, geçiş planı (P0'lar).
- Çek ve kredi modelleri Türk ön muhasebe pratiğine aykırı: alınan/ciro edilen çek cariyi kapatmıyor; kredi kullandırımı yok, banka carisi ters işaretli (ACC-09, ACC-10) → ADR-007 revize.
- Eşzamanlılık kilidi ve çok adımlı işlemlerde atomiklik yok; ADR-004 §4 mutabakatının 7 kontrolünden 2'si test (ACC-04..06).
- Nakit akışının ön koşulu kasa/banka defteri + carisiz fiş (CSH-01); sonra plan kalemi, tekrar kuralı, tarih aralıklı vergi parametresi; motor deterministik, açıklanabilir, senaryo = düzeltme katmanı (CF, TAX).
- Aidat tahakkuku dışarıda kalabilir ama cariye yazmayan "tahsilat planı" beklenen tahsilatın tek makul kaynağı (CF-02).
- Hukuki risk: belgesiz satışın kalıcı ayrı katmanda tutulması ve "Gayri resmi" dili; ürün "belgelenme kuyruğu"na çevrilmeli, hedef Toplam = Resmi (ACC-14). AV/MM teyidi şart.
- GMS.NET: önce "aktarım sahipliği matrisi", sonra Excel/XML fiş; API en son (INT-02).
- SaaS: paylaşımlı şema+RLS doğru; ucuz kapılar şimdi: kiracı claim'den, identity uçlarında kiracı filtresi, şema koruma testi (SAAS-01..04).
- Tasarım tutarlı; eksik: ortak yazdırma düzeni, geliştirici jargonunun temizlenmesi, klavye-öncelikli giriş (UX-01..03).

## 2 BULGULAR
- id: ACC-01
  oncelik: P0
  tur: risk
  iddia: "Resmi Cari" ekstresinde Bakiye sütunu ve dip bakiye Toplam defterden geliyor; PDF kendi içinde tutarsız (Borç 2.500 − Alacak 2.500, Bakiye 2.500 borçlu) ve belgesiz hareketlerin varlığını ele veriyor.
  gerekce: Filtre istemcide satır gizliyor, yürüyen bakiye sunucuda tüm defterden hesaplanıyor; dipnot "Bakiye sütunu güncel toplam cari bakiyedir" basılı belgeye düşüyor; istemci para topluyor (ADR-002 ihlali).
  oneri: Ekstreyi sunucuya taşı: kapsam (total|official) ve tarih aralığı parametreli; kapsamın kendi devir satırı, yürüyen bakiyesi ve dip toplamı sunucuda; resmi kapsamda "Resmi mi?" sütunu ve toplam bakiye hiç dönmez; istemci yalnız çizer.
  somut: GET /api/parties/{id}/statement?scope=total|official&from=&to= → {opening, rows[{date,description,debit,credit,balance}], totals}; AccountingQueries.StatementAsync; PartyPages.tsx StatementDialog yalnız render + tarih alanları.
  kabul: StatementTests.ResmiEkstre_KendiBakiyesiniYurutur (belgeli 2.500 satış + belgesiz 2.500 satış + belgeli satıra 2.500 havale → official kapanış 0, total 2.500); PDF metninde "Gayri" ve toplam bakiye yok; PartyPages.tsx'te Number()/reduce ile para toplamı yok.
  bagimlilik: [ACC-02, ACC-03]
  efor: M
  adr: yok
  kanit: src/UUSport.Web/src/pages/PartyPages.tsx:335-338, :372, :381, :389; ekran 05-ekstre-resmi-cari.png
  guven: yuksek
- id: ACC-02
  oncelik: P1
  tur: risk
  iddia: "Resmi" üç yerde üç farklı tanımla hesaplanıyor; ADR-004 §2.4 ise "belgelenen miktar/tutar kadar" diyor.
  gerekce: Özet kartı orantılı (documented/qty), defter bayrağı hep-ya-hiç (yalnız tam belgeli), cari sekmesi ve /lines?view=official "documentedQuantity≠0" (kısmi de resmi). Aynı cari için kart, sekme ve ekstre farklı resmi tutar gösterir (ekran 02: sekme "Belgeli/Resmi 1", ekstre 2 resmi satır).
  oneri: Tek OfficialScopeService: her defter kaydı için bool yerine official_amount (decimal). Satır: gross × documented_qty/qty (kuruş farkı son belgelemede kapanır). Ekstrede kısmi satır ikiye bölünür (belgelenen / belge bekleyen). IsOfficial yalnız official_amount = amount türevi.
  somut: AccountingQueries.BuildOfficialFlagsAsync → BuildOfficialAmountsAsync; LedgerRow.OfficialAmount; PartyEndpoints /lines?view=official ve PartyPages.tsx:166 aynı servisi kullanır; LedgerOfficialFlagTests güncellenir.
  kabul: OfficialScopeTests.S1_KismiBelge_4te10: özet kartı, sekme, ekstre ve /api/ledger aynı resmi tutarı (gross × 0,4) döndürür.
  bagimlilik: []
  efor: M
  adr: yeni ADR: ADR-009 Resmi kapsam tanımı
  kanit: src/UUSport.Api/Features/Accounting/AccountingQueries.cs:46-47, :119, :124; Features/Parties/PartyEndpoints.cs:148; PartyPages.tsx:166; docs/decisions/ADR-004:89
  guven: yuksek
- id: ACC-03
  oncelik: P1
  tur: risk
  iddia: Tahsilatın resmiliğini "belgeli satıra dağıtıldı mı" sorusuna bağlamak MM defteriyle uyuşmaz; kod ayrıca geri alınmış ve kısmi dağıtımı yanlış sayıyor.
  gerekce: Havale/POS/çek bankadan geçer, MM ekstreden dağıtımdan bağımsız kaydeder; nakit ancak makbuz/faturayla deftere girer. Kod `a.Amount > 0` ile ters dağıtımı yok sayar (geri alınan dağıtım resmi kalır); 10.000 TL'nin 100 TL'si dağıtılsa tamamı resmi olur.
  oneri: Tahsilat resmi tutarı = kanal kuralı (kiracı ayarı, MM teyidi): banka/POS/çek → tamamı; nakit → belgeli satırlara NET dağıtım toplamı, receipt_no (tahsilat makbuzu) girilmişse tamamı. Net dağıtım (payment, line) bazında toplanır.
  somut: finance.payment +receipt_no text null; core.tenant_setting 'official.payment_rule'; AccountingQueries dağıtım sorgusu GroupBy(PaymentId, TradeLineId).Sum(Amount) > 0.
  kabul: LedgerOfficialFlagTests: DagitimGeriAlininca_NakitResmiSifir, KismiDagitim_NakitResmi100, Havale_KanalKurali_TamamiResmi.
  bagimlilik: [ACC-02]
  efor: S
  adr: yeni ADR: ADR-009 Resmi kapsam tanımı
  kanit: AccountingQueries.cs:138-143
  guven: orta
- id: ACC-04
  oncelik: P1
  tur: eksik
  iddia: ADR-004 §4'teki 7 mutabakat kontrolünden yalnız 2'si test; günlük/sürüm "verify" işi yok; türetilmiş alanlar için tutarlılık kontrolü yok.
  gerekce: documented_quantity değişebilir önbellek alan; dağıtım ve stok sınırları yalnız uygulama kodunda; tutarsızlık sessiz birikir.
  oneri: Kontroller SQL görünümü: ADR-004 §4'ün 7'si + documented_quantity = Σ aktif document_link.quantity + satır başına net dağıtım ∈ [0, gross] + tahsilat başına net dağıtım ∈ [0, amount] + (CSH-01 sonrası) payment ↔ cash_movement 1:1. `verify` komutu, Yönetici ucu, zamanlanmış görev; sonuç audit'e.
  somut: audit.v_integrity_* (owner rolü tanımlar, app'e SELECT); UUSport.Api.exe verify --database=; GET /api/admin/integrity; IntegrityTests (bozuk fikstür).
  kabul: Temiz test veritabanında tüm görünümler 0 satır; bilinçli bozulmuş fikstürde ilgili görünüm ≥ 1 satır ve verify çıkış kodu ≠ 0.
  bagimlilik: []
  efor: M
  adr: yok
  kanit: docs/decisions/ADR-004:167-180; tests/UUSport.Tests/CariHareketModeliTests.cs:228, :242
  guven: yuksek
- id: ACC-05
  oncelik: P1
  tur: risk
  iddia: Dağıtım, fatura bağlama ve eksi stok kontrolü kilitsiz "oku-kontrol-yaz" deseniyle çalışıyor; eşzamanlı iki istek sınırları aşar, documented_quantity kaybolan güncelleme yaşar.
  gerekce: Varsayılan READ COMMITTED; trade_line ve payment'ta concurrency token yok; Faz 2 mobil ile eşzamanlılık artar.
  oneri: trade_line ve payment'a xmin concurrency token; dağıtım/fatura/stok işlemlerinde açık transaction içinde SELECT … FOR UPDATE (payment + ilgili trade_line; stokta item_cost_state/item); DbUpdateConcurrencyException → 409 "Kayıt başka kullanıcı tarafından değiştirildi, yenileyin".
  somut: Entities TradeLine/Payment `uint Version` + IsRowVersion (Npgsql xmin); AccountingService.AllocateInternal, EnterDocumentAsync, AddStockMovement; ErrorHandlingMiddleware 409 eşlemesi.
  kabul: ConcurrencyTests: iki paralel görev aynı satırın tüm açık tutarını dağıtır → biri 409, net dağıtım ≤ gross; iki paralel fatura aynı kalan miktara → biri reddedilir.
  bagimlilik: [ACC-06]
  efor: M
  adr: yok
  kanit: src/UUSport.Api/Features/Accounting/AccountingService.cs:255-287, :418-422, :532-537
  guven: yuksek
- id: ACC-06
  oncelik: P1
  tur: risk
  iddia: Kredi taksiti ödeme ve çek tahsil/karşılıksız işlemleri iki ayrı SaveChanges ile yapılıyor; ikinci adım hata verirse ödeme kalır, taksit/çek ödenmemiş görünür ve tekrar ödenebilir.
  gerekce: CreatePaymentAsync kendi SaveChanges'ini çağırıyor, çağıran servis ikinci kez kaydediyor; açık transaction yok.
  oneri: IUnitOfWork.ExecuteAsync (execution strategy + BeginTransaction); iç servisler SaveChanges çağırmaz, uç tek commit yapar.
  somut: Infrastructure/UnitOfWork.cs; AccountingService.CreatePaymentAsync → CreatePaymentCoreAsync (SaveChanges'siz); LoanService.PayInstallmentAsync; CheckService.CollectAsync/BounceAsync.
  kabul: AtomicityTests: ikinci adıma enjekte edilen hata sonrası payment ve ledger_entry sayısı değişmez.
  bagimlilik: []
  efor: S
  adr: yok
  kanit: src/UUSport.Api/Features/Cash/LoanService.cs:105-112; Features/Cash/CheckService.cs:116-123, :134-140; AccountingService.cs:231
  guven: yuksek
- id: ACC-07
  oncelik: P1
  tur: eksik
  iddia: Dönem kilidi yok; her kayıt geçmiş tarihe atılabiliyor, muhasebeye gönderilmiş dönem sessizce değişebilir.
  gerekce: Resmi tarafın MM ile tutarlılığı ve "kapanmış dönem yeniden hesaplanınca değişmez" kuralı (standart §11.4) buna bağlı; ADR-004 açık konu 4.
  oneri: finance.period_lock; kilit tarihine ≤ tarihli satır, tahsilat, belge, açılış, dağıtım reddedilir; yalnız Yönetici gerekçeyle geçici açar (audit); GMS aktarım partisi kilidi otomatik ilerletir; ters kayıtlar bugünün tarihine yazılır (mevcut davranış).
  somut: finance.period_lock(tenant_id, locked_through date, locked_by, reason, created_at); AccountingService.EnsureOpenPeriod(date); POST /api/period-locks; Yönetim > Dönem Kilidi.
  kabul: PeriodLockTests: kilit 30.09.2026 → 15.09.2026 tarihli satış/tahsilat/fatura 400 "Dönem kapalı"; kilit açma audit'te.
  bagimlilik: []
  efor: S
  adr: yeni ADR: ADR-013 Dönem kilidi
  kanit: AccountingService.cs:74, :204, :399; docs/decisions/ADR-004:222
  guven: yuksek
- id: ACC-08
  oncelik: P0
  tur: risk
  iddia: Açılış bakiyesi onay gününün tarihiyle deftere yazılıyor ve her zaman "resmi" sayılıyor; Excel bakiyeleri doğrulanmış kayıt değil, bu yüzden canlıda yaşlandırma da resmi mutabakat da baştan bozulur.
  gerekce: Geçiş tarihi ile onay tarihi farklı; MM'nin cari bakiyesi (resmi) ile Excel'deki toplam bakiye iki ayrı sayıdır.
  oneri: opening_balance'a as_of_date (zorunlu, geçiş tarihi), due_date (yaşlandırma), scope (official|unofficial). Resmi açılış yalnız MM'nin cari bakiye listesi/mizanından; Excel toplamıyla fark "unofficial" satır. Ledger tarihi = as_of_date.
  somut: finance.opening_balance +as_of_date date not null, +due_date date null, +scope text check in ('official','unofficial'); öğrenci aktarımında scope='unofficial'; ACC-02 servisi scope'a bakar.
  kabul: OpeningBalanceTests: 01.11'de onaylanan 31.10 tarihli açılış ledger'da 31.10; unofficial satır resmi ekstrede yok; resmi ekstre açılışı MM listesindeki tutara eşit.
  bagimlilik: [ACC-02]
  efor: S
  adr: ADR-004 değişir
  kanit: AccountingService.cs:515; AccountingQueries.cs:73; tests/UUSport.Tests/LedgerOfficialFlagTests.cs:97; belge bölüm 4 (23.09)
  guven: yuksek
- id: ACC-09
  oncelik: P1
  tur: itiraz
  iddia: Çek kaydının cariye etki etmemesi ön muhasebe pratiğine ve MM defterine aykırı: çek veren veli vade gününe kadar borçlu görünür, ciro ne müşteriyi ne tedarikçiyi etkiler.
  gerekce: Pratikte çek girişi carinin borcunu kapatır (portföy kıymeti), ciro tedarikçi borcunu kapatır, karşılıksız cariye geri borç yazar. Mevcut kodda ciro edilen çekin müşterisi süresiz borçlu kalır.
  oneri: Çek girişi = Payment(method=check) giriş tarihinde, yeni hesap türü CheckPortfolio'ya → cariye alacak. Tahsil = portföy→banka virmanı (carisiz, CSH-01). Ciro = tedarikçiye Payment(disbursement, check_endorse) portföyden. Karşılıksız = payment_reversal (cariye borç) + protesto masrafı borç dekontu (ACC-11). Nakit akışı çeki vade + takas gününde nakit sayar (spesifikasyon O37 korunur).
  somut: CashAccountKind.CheckPortfolio; check_note +receipt_payment_id, +endorse_payment_id, +endorsed_party_id; CheckService yeniden yazımı; Çek Portföyü'nde ciro için cari seçimi; portföy raporları (vade dağılımı, müşteri başına çek riski).
  kabul: CheckFlowTests: giriş → cari 0; ciro → tedarikçi borcu düşer, müşteri değişmez; karşılıksız → müşteri yeniden borçlu; portföy hesabı bakiyesi = portföydeki çek toplamı.
  bagimlilik: [CSH-01, ACC-06]
  efor: M
  adr: yeni ADR: ADR-007 Ödeme araçları (çek/kredi/kart/POS) revize
  kanit: CheckService.cs:143-152; src/UUSport.Api/Domain/Entities.cs:344-348; ekran 11-cek-portfoyu.png
  guven: yuksek
- id: ACC-10
  oncelik: P1
  tur: itiraz
  iddia: Kredi modeli kullandırımı kaydetmiyor ve taksiti kredi veren bankanın carisine ödeme yazıyor; banka hesabı eksiye düşüyor, kredi veren cari "bize borçlu" görünüyor, anapara ve faiz aynı tutarda karışıyor.
  gerekce: Ekran 09'da Ziraat −10.500, kredi hesabı ₺0, kalan anapara 20.000. Sürüm 2 notu "kredi bir cari değil cash_account'tur" diyordu, kod Party'ye bağladı.
  oneri: Kredi cariden çıkar, yükümlülük hesabı olur: kullandırım = cash_movement(+banka) ve kredi borcu; taksit = cash_movement(−banka) + bölünmüş satırlar (anapara → kredi borcu azalır; faiz, BSMV, KKDF, masraf → gider kategorileri). party_id opsiyonel bilgi.
  somut: finance.loan +disbursed_on, +disbursement_account_id, party_id nullable; loan_installment +bsmv_amount, +kkdf_amount; finance.cash_movement_split; LoanService.DisburseAsync/PayInstallmentAsync; Krediler ekranında "Kullandırım" alanı.
  kabul: LoanTests: 30.000 kullandırım + 1. taksit → banka +30.000/−10.500, kredi borcu 20.000, faiz gideri 500, banka carisi etkilenmez.
  bagimlilik: [CSH-01]
  efor: M
  adr: yeni ADR: ADR-007 Ödeme araçları (çek/kredi/kart/POS) revize
  kanit: LoanService.cs:43-44, :105-108; Entities.cs:310-314; docs/designs/SURUM2_HAZIRLIK_NOTU_2026-09-26.md:149-151; ekran 09, 10
  guven: yuksek
- id: ACC-11
  oncelik: P2
  tur: eksik
  iddia: Cari hesap fişleri (borç/alacak dekontu, cari virman) yok; approved_adjustment kaynak türü tanımlı ama kullanılmıyor.
  gerekce: Protesto masrafı, kardeş indirimi, iki veli carisinin birleştirilmesi, yanlış cariye girilmiş tahsilatın taşınması bugün satır/ödeme kötüye kullanılarak yapılır.
  oneri: finance.adjustment_voucher (debit_note | credit_note | party_transfer; gerekçe; onay). Dekont → tek ledger satırı (approved_adjustment). Virman → iki ledger satırı (party_transfer_out/in kaynak türleri). Eşik üstü ikinci kullanıcı onayı (SEC-11).
  somut: LedgerSources +PartyTransferOut/In (CHECK güncellenir); POST /api/adjustments; cari detay "⋯" menüsü.
  kabul: AdjustmentTests: virman sonrası iki carinin toplamı değişmez; onaysız dekont cariye yazmaz; S0-S10 yeşil.
  bagimlilik: [SEC-11]
  efor: M
  adr: ADR-004 değişir
  kanit: Entities.cs:252 (ApprovedAdjustment; üreten kod yok)
  guven: yuksek
- id: ACC-12
  oncelik: P2
  tur: eksik
  iddia: Vade, sunucu tarafı kapama stratejisi ve yaşlandırma yok; "En eski borçtan dağıt" yalnız istemcide doldurma düğmesi.
  gerekce: Açık alacağın yaşı, veli bazında gecikme ve tahsilat tahmini (CF-02) vade verisi olmadan kurulamaz.
  oneri: trade_line.due_on (varsayılan occurred_on + party.payment_terms_days); sunucu tarafı otomatik kapama (fifo | documented_first) ve toplu kapama; yaşlandırma (0-30/31-60/61-90/90+) Toplam ve Resmi kapsam için ayrı.
  somut: crm.party +payment_terms_days int default 0; billing.trade_line +due_on date; POST /api/parties/{id}/auto-allocate?strategy=; GET /api/reports/aging?scope=&asOf=.
  kabul: AgingTests: kova toplamları = açık tutar toplamı; fifo ve documented_first farklı satırı kapatır.
  bagimlilik: [ACC-02, ACC-05]
  efor: M
  adr: yok
  kanit: src/UUSport.Web/src/pages/Dialogs.tsx:190, :246
  guven: yuksek
- id: ACC-13
  oncelik: P2
  tur: eksik
  iddia: Girilen fatura toplamı ile bağlı satırların sistem toplamı arasındaki fark yalnız anlık yanıtta dönüyor, saklanmıyor; ADR-004 S3 "fiyat farkı satırı" akışı yok.
  gerekce: Resmi görünüm satır tutarını kullanır; belge farklıysa resmi cari MM'nin gördüğü fatura tutarından sapar.
  oneri: document.system_gross ve difference saklanır; |fark| > 0,01 → review_status=needs_review, Muhasebe Bekleyen'de "Tutar farkı" sekmesi; tek tıkla fiyat farkı satırı oluşturulup belgeye bağlanır.
  somut: billing.document +system_gross, +difference, +review_status; AccountingService.EnterDocumentAsync; POST /api/documents/{id}/price-difference-line.
  kabul: DocumentDiffTests: fark 3,00 TL → needs_review; fark satırı sonrası resmi tutar = belge GrossTotal.
  bagimlilik: [ACC-02]
  efor: S
  adr: yok
  kanit: AccountingService.cs:444-446; docs/decisions/ADR-004:121-126
  guven: yuksek
- id: ACC-14
  oncelik: P1
  tur: risk
  iddia: Belgesiz satışların sistematik ve kalıcı olarak ayrı "gayri resmi" katmanda tutulması vergi incelemesinde aleyhe delil olabilir; ürün dili bu riski büyütüyor.
  gerekce: VUK 231/5 teslimden itibaren azami 7 gün içinde fatura ister (doğrulanmalı); belgesiz satışı düzenli izleyen kayıtların hukuki niteliği (VUK 359 kapsamı dahil) AV/MM değerlendirmesi ister; "Gayri resmi" ibaresi basılı çıktıya giriyor.
  oneri: Kavram "belgelenme kuyruğu"; hedef Toplam = Resmi. Etiketler "Belgelendi / Belge bekliyor / Süresi geçti"; 7 gün eşiği parametre; Genel Bakış'ta belgelenme oranı ve süresi geçmiş belgesiz tutar; resmi kapsamlı çıktı başlığı "Belgelenmiş hareketler dökümü — tüm hareketleri içermez". "Gayri resmi" kod ve arayüzden kalkar.
  somut: core.tax_parameter 'invoice.max_days_after_delivery'; LineRow.AgeDays, IsOverdue; WorkPages.tsx:235, PartyPages.tsx:369 metinleri; /api/dashboard +documentationRate, +overdueUndocumentedAmount.
  kabul: Depoda "Gayri resmi|gayri resmi" araması 0; 8 günlük belgesiz satış Muhasebe Bekleyen'de "Süresi geçti" rozetiyle en üstte.
  bagimlilik: [ACC-01]
  efor: S
  adr: yeni ADR: ADR-009 Resmi kapsam tanımı
  kanit: src/UUSport.Web/src/pages/WorkPages.tsx:235; PartyPages.tsx:369; belge bölüm 2
  guven: orta
- id: ACC-15
  oncelik: P2
  tur: fikir
  iddia: Logo Tiger 3'ten alınacak minimum set ve alınmayacaklar yazılı olmazsa kapsam Logo'yu kopyalamaya kayar.
  gerekce: Akademi ölçeğinde değer cari/kasa/banka/çek/kredi/kart/POS döngüsünde ve muhasebe bağlantısında; sipariş zinciri, üretim ve çoklu ambar maliyet üretir.
  oneri: AL: cari fişleri (dekont, virman), vade/ödeme planı, çek-senet bordroları (giriş, ciro, tahsile verme, karşılıksız), kasa ve banka fişleri (virman, masraf, gelen/giden havale), kredi kartı hesabı + POS geri ödeme planı, banka kredi takibi, kural bazlı muhasebe bağlantı kodları, yaşlandırma, cari mutabakat. ALMA: sipariş/teklif/irsaliye zinciri, üretim, lot/seri, çoklu ambar transferi, sabit kıymet/amortisman (MM'de), döviz ve kur farkı (A3), bütçe modülü (nakit planı karşılar), e-Defter (MM'de).
  somut: ADR-005 kapsam tablosuna "Logo eşleme" eki; her AL maddesi bir bulguya bağlı (ACC-09..12, CSH-01..05, INT-01).
  kabul: Kullanıcı onaylı kapsam tablosu ana hafızada.
  bagimlilik: []
  efor: S
  adr: ADR-005 değişir
  kanit: belge bölüm 3.1, 6
  guven: orta
- id: ACC-16
  oncelik: P2
  tur: itiraz
  iddia: "Resmi sorgu anında hesaplanır, saklanmaz" kuralı aktarım öncesi için doğru, aktarım sonrası için yanlış; gönderilmiş kapsam dondurulmalı.
  gerekce: Aktarımdan sonra dağıtım geri alma veya belge iptali, gönderilmiş fişi geriye dönük "resmi değil" yapar; MM defteriyle kalıcı fark doğar.
  oneri: Canlı hesap (ACC-02) korunur; GMS aktarımında üretilen journal_line'lar değişmez saklanır, kaynak kayıtlar export_link ile partiye bağlanır; aktarılmış kaydı etkileyen değişiklik bir sonraki partiye ters/düzeltme fişi olarak girer; dönem kilidi otomatik ilerler.
  somut: integration.journal_batch, journal_line, export_link (bölüm 3); ACC-07 kilidi.
  kabul: ExportFreezeTests: aktarılmış belge iptal edilince önceki parti değişmez, sonraki partide ters fiş oluşur.
  bagimlilik: [INT-01, ACC-07]
  efor: M
  adr: yeni ADR: ADR-009 Resmi kapsam tanımı
  kanit: belge bölüm 5.3
  guven: yuksek
- id: ACC-17
  oncelik: P3
  tur: iyilestirme
  iddia: Tedarikçi ve kurumsal müşterilerle cari mutabakat mektubu ve Ba/Bs ön kontrolü ürün farkı yaratır ama şimdi gerekmez.
  gerekce: Ba/Bs belge bazlı ve eşiklidir (5.000 TL, doğrulanmalı); resmi kapsam doğru kurulunca rapor ucuzdur.
  oneri: Resmi kapsamdan dönem sonu cari bakiye mektubu (PDF) ve Ba/Bs ön kontrol listesi (VKN'li cariler, eşik üstü belgeler).
  somut: GET /api/reports/ba-bs?month=; GET /api/parties/{id}/reconciliation-letter?asOf=.
  kabul: Eşik parametreden okunur; fikstürde eşik üstü 2 belge listeye düşer.
  bagimlilik: [ACC-02, TAX-01]
  efor: S
  adr: yok
  kanit: yok
  guven: orta
- id: CSH-01
  oncelik: P0
  tur: eksik
  iddia: Kasa/banka hareket defteri yok: bakiye yalnız tahsilat/ödeme toplamı; açılış, virman, banka masrafı, faiz, vergi/SGK/maaş gibi carisiz hareket kaydedilemiyor; her ödeme bir cari istiyor.
  gerekce: Günlük kasa sayımı ve banka mutabakatı yapılamaz; ekran 09'daki −10.500 gibi bakiyeler canlıda güveni bitirir; nakit akışının açılış noktası bu defterdir.
  oneri: ADR-004'ün aynası ADR-008: finance.cash_movement tek yazma noktası (append-only). Kaynaklar: payment (otomatik bacak), transfer (iki bacak), cash_voucher (carisiz gelir/gider, kategori zorunlu), opening, kredi/kart/POS/çek olayları. Hesap bakiyesi = Σ cash_movement. Mevcut ödemeler için geriye dönük bacak migration'da üretilir.
  somut: finance.cash_movement, cash_voucher, transfer, cash_category (bölüm 3); POST /api/cash-vouchers, /api/transfers, /api/cash-accounts/{id}/opening; CatalogEndpoints bakiye sorgusu cash_movement'a geçer.
  kabul: CashLedgerTests: Σ cash_movement(source=payment) = Σ işaretli payment; virman iki hesabı ters etkiler; SGK ödemesi cari açmadan girilir; açılış sonrası kasa bakiyesi sayımla eşit.
  bagimlilik: [ACC-06]
  efor: L
  adr: yeni ADR: ADR-008 Kasa/banka hareket defteri
  kanit: src/UUSport.Api/Features/Catalog/CatalogEndpoints.cs:98-105; AccountingService.cs:196-197; ekran 09-kasa-banka-hiyerarsi.png
  guven: yuksek
- id: CSH-02
  oncelik: P1
  tur: risk
  iddia: Ödeme aracı–hesap türü eşleşmesi yönden bağımsız; müşteri tahsilatı şirket kredi kartı hesabına yazılabiliyor; ana hafıza "gerçek ödeme yalnız Cash/Bank/Pos'tan çıkar" diyor, kod CreditCard'a izin veriyor.
  gerekce: Card → Pos | CreditCard kuralı hem tahsilat hem ödeme için geçerli; Entities yorumuyla ve ana hafıza §5.6 ile çelişki.
  oneri: (yön, araç, hesap türü) tablosu. Tahsilat: nakit→Cash, havale→Bank, kart→Pos, çek→CheckPortfolio. Ödeme: nakit→Cash, havale→Bank, kart→CreditCard (yükümlülük), kendi çekimiz→Bank, ciro→CheckPortfolio.
  somut: AccountingService.EnsureMethodMatchesAccount(direction, method, kind); PaymentMethod +CheckEndorse; Theory testi tüm kombinasyonlar.
  kabul: PaymentMatrixTests: yalnız tablodaki kombinasyonlar geçer.
  bagimlilik: [ACC-09]
  efor: S
  adr: yeni ADR: ADR-007 Ödeme araçları (çek/kredi/kart/POS) revize
  kanit: AccountingService.cs:580-592; Entities.cs:182-187; docs/project-memory/UUSPORT_ANA_HAFIZA.md:394-395
  guven: yuksek
- id: CSH-03
  oncelik: P1
  tur: iyilestirme
  iddia: Hesap ilişkisi "hiyerarşi" değil Logo'daki gibi "tür + bağlantı + profil" olmalı; yükümlülük hesapları bakiyeyi borç olarak göstermeli.
  gerekce: parent_account_id tek anlam taşıyor; POS'un hangi bankaya yattığı, kartın hangi hesaptan ödendiği, kredinin hangi hesaba kullandırıldığı ayrı ilişkiler. Ekranda İşletme Kredisi ₺0, kalan borç 20.000.
  oneri: Türler: Cash, BankCurrent, Pos (settles_to→Bank), CreditCard (paid_from→Bank, yükümlülük), Loan (yükümlülük), CheckPortfolio (varlık). parent_account_id → linked_account_id + link_role; profil tabloları (pos_profile, card_profile). Liste Varlıklar/Yükümlülükler gruplu.
  somut: finance.cash_account +linked_account_id, +link_role, +is_liability (türetilmiş); CatalogEndpoints.ValidateHierarchy → ValidateLinks.
  kabul: Kredi satırı kalan anaparayı "Borç" etiketiyle gösterir; POS'un settles_to'su banka dışı hesaba bağlanamaz.
  bagimlilik: [CSH-01, ACC-10]
  efor: M
  adr: yeni ADR: ADR-007 Ödeme araçları (çek/kredi/kart/POS) revize
  kanit: CatalogEndpoints.cs:143-157; ekran 09, 10
  guven: yuksek
- id: CSH-04
  oncelik: P1
  tur: eksik
  iddia: POS tahsilatı brüt tutarla POS hesabında birikiyor; komisyon, valör, blokaj, taksitli satış ve bankaya geçiş izlenmiyor.
  gerekce: Nakit akışında POS parası valör günü ve net tutarla gelir; komisyon gideri ve banka mutabakatı bunsuz kurulamaz (Logo "geri ödeme planı" karşılığı).
  oneri: pos_profile (tarih aralıklı; taksit sayısına göre komisyon oranı ve blokaj günü; settles_to). Her kart tahsilatı pos_settlement_expected satırları üretir (tarih, brüt, komisyon, net). Banka girişiyle eşleşince POS→banka virmanı (net) + komisyon gider fişi.
  somut: finance.pos_profile, pos_settlement_expected (bölüm 3); payment +installment_count smallint default 1; GET /api/pos/settlements?status=pending; eşleştirme ekranı.
  kabul: PosTests: 10.000 TL, 3 taksit, %2,5 komisyon, 30 gün blokaj → beklenen net 9.750, vade +30 gün; eşleşme sonrası POS bakiyesi 0, komisyon gideri 250.
  bagimlilik: [CSH-01, CSH-03]
  efor: M
  adr: yeni ADR: ADR-007 Ödeme araçları (çek/kredi/kart/POS) revize
  kanit: belge bölüm 5.6 madde 7; docs/designs/MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md:130-137
  guven: yuksek
- id: CSH-05
  oncelik: P1
  tur: eksik
  iddia: Şirket kredi kartının hesap kesim/son ödeme döngüsü, taksitli harcama ve asgari ödeme modeli yok.
  gerekce: Kart harcaması nakit çıkışı değildir; nakit etkisi ekstre son ödeme gününde olur; taksitler gelecek ekstrelere dağılır.
  oneri: card_profile (kesim günü, son ödeme ofseti, limit); kart harcaması = ödeme veya carisiz fiş (hesap=CreditCard) + taksit sayısı → card_installment (düştüğü ekstre dönemi); card_statement (dönem, kesim, son ödeme, toplam, asgari, ödenen); ekstre ödemesi = banka→kart virmanı; faiz/gecikme = kart üzerinde gider fişi.
  somut: finance.card_profile, card_installment, card_statement (bölüm 3); GET /api/cards/{id}/statements; kesim gününde ekstre kapatan zamanlanmış iş.
  kabul: CardTests: kesim 15, son ödeme +10; 16.10 tarihli 3 taksitli 3.000 TL → Kasım/Aralık/Ocak ekstrelerine 1.000'er, ilk nakit çıkışı 25.11.
  bagimlilik: [CSH-01, CSH-03]
  efor: M
  adr: yeni ADR: ADR-007 Ödeme araçları (çek/kredi/kart/POS) revize
  kanit: belge bölüm 5.6 madde 7
  guven: yuksek
- id: STK-01
  oncelik: P2
  tur: eksik
  iddia: Maliyet fiyatı ve maliyet yöntemi yok; satış kârlılığı hesaplanamıyor.
  gerekce: Stok az çeşitli (forma, çorap, top), alış seyrek; FIFO katmanı gereksiz karmaşa; MM dönem sonu maliyetini ayrıca hesaplar.
  oneri: Hareketli ağırlıklı ortalama. Alış maliyeti = KDV hariç iskontolu net/miktar; satış çıkışında o anki ortalama hareket üzerine yazılır (değişmez); iade orijinal çıkış maliyetiyle. Kârlılık = satış neti − çıkış maliyeti.
  somut: inventory.stock_movement +unit_cost numeric(18,6), +total_cost numeric(18,2); inventory.item_cost_state (FOR UPDATE ile güncellenir); GET /api/reports/profitability?from&to.
  kabul: CostTests: 10×100 + 10×120 alış, 5 satış → çıkış maliyeti 550; iade sonrası ortalama değişmez.
  bagimlilik: [ACC-05]
  efor: M
  adr: yeni ADR: ADR-014 Stok maliyeti
  kanit: Entities.cs:80-104
  guven: yuksek
- id: STK-02
  oncelik: P2
  tur: eksik
  iddia: Sayım farkı, fire, hediye/numune ve "kayıt paketiyle verilen forma" gibi satışsız stok çıkışları kaydedilemiyor.
  gerekce: ADR-004 stok hareketini yalnız satış satırına bağlıyor; sayım yapılamadığı için stok bakiyesi sürekli sapar.
  oneri: inventory.stock_adjustment (count | waste | gift | kit_included | internal_use; onaylayan); stock_movement.source_type='stock_adjustment'; cariye etkisi yok; maliyetli (STK-01).
  somut: POST /api/stock-adjustments; Ürün ve Hizmetler'de "Stok düzeltme"; sayım Excel şablonu (mevcut aktarım altyapısı).
  kabul: StockAdjustmentTests: sayım −2 → stok −2, ledger değişmez, audit kaydı var.
  bagimlilik: [STK-01]
  efor: S
  adr: ADR-004 değişir
  kanit: Entities.cs:96; docs/decisions/ADR-004:61-62
  guven: yuksek
- id: STK-03
  oncelik: P3
  tur: fikir
  iddia: Çoklu depo, varyant (beden/renk) ve paket (bundle) şimdi gereksiz ama kapıları ucuz.
  gerekce: Satılabilir üründe şube/tesis ve forma bedeni gerçek ihtiyaç olacak.
  oneri: Şimdi yalnız nullable kolonlar; UI ve iş kuralı yok.
  somut: inventory.stock_movement +location_id uuid null; inventory.item +parent_item_id uuid null.
  kabul: Migration sıfır veritabanında çalışır; mevcut testler yeşil.
  bagimlilik: []
  efor: S
  adr: yok
  kanit: yok
  guven: orta
- id: CF-01
  oncelik: P1
  tur: eksik
  iddia: Planlanan/tahmini nakit kalemleri için hiçbir varlık yok (maaş, kira, SGK, vergi, abonelik, tedarikçi vadesi).
  gerekce: Nakit akışı öngörüsünün girdisi; bugün yalnız gerçekleşen kayıt var.
  oneri: plan.cash_item (tek kalem: yön, kategori, vade, tutar, certainty, doc_scope, status, kaynak) + plan.recurring_rule (sıklık, tutar türü, artış, iş günü kaydırma) + plan.settlement_match (planlı kalem ↔ gerçekleşen hareket). Kural motoru ufuk kadar kalemi idempotent (rule_id + period_key) üretir.
  somut: bölüm 3 plan şeması; PlanService.MaterializeAsync(horizonEnd); /api/plan/rules, /api/plan/items, POST /api/plan/items/{id}/match.
  kabul: PlanTests: "Kira 45.000, her ayın 5'i, hafta sonu → sonraki iş günü" → 12 kalem, Cumartesi'ye düşen ay Pazartesi; ödeme eşleşince status=settled; motor iki kez çalışınca kalem sayısı değişmez.
  bagimlilik: [CSH-01]
  efor: L
  adr: yeni ADR: ADR-010 Nakit akışı plan ve senaryo modeli
  kanit: belge bölüm 5.6 madde 10
  guven: yuksek
- id: CF-02
  oncelik: P1
  tur: itiraz
  iddia: Aidat tahakkuku dışarıdayken beklenen öğrenci tahsilatının kaynağı yok; cariye yazmayan "tahsilat planı" şimdi gerekli, aidat tahakkuku kararı M5'te yeniden açılmalı.
  gerekce: Gelirin çoğu aidat; beklenen tahsilat olmadan nakit akışı yalnız gider listesidir. Tahsilat planı cariye yazmadığı için ADR-004/005'i bozmaz. Veli uygulaması (Faz 3), gecikme takibi ve e-belge tetikleyicisi uzun vadede tahakkuku gerektirir.
  oneri: academy.billing_plan (sporcu, cari, paket kalemi, tutar, sıklık, gün, başlangıç/bitiş, durum) → plan.cash_item (in, planned). Tahmin katmanı: carinin son 6 ay tahsilat oranı ve medyan gecikmesi, yoksa kiracı varsayılanı. Aylık "toplu satış satırı oluştur" (kullanıcı onaylı tahakkuk benzeri) ayrı ADR ile sonra.
  somut: academy.billing_plan (bölüm 3); GET /api/cashflow/receivable-forecast; sporcu kartında "Ödeme planı" bölümü; ödeme planı Excel aktarım türü.
  kabul: BillingPlanTests: 20 sporcu × 3.800 → gelecek ay planlanan 76.000; tarihsel oran 0,9 ise base senaryo beklenen tutarı 68.400 ayrı kolonda.
  bagimlilik: [CF-01]
  efor: M
  adr: yeni ADR: ADR-015 Tahsilat planı ve aidat
  kanit: belge bölüm 5.3 (ADR-005 dışarıda) ve 5.6 madde 10
  guven: orta
- id: CF-03
  oncelik: P1
  tur: eksik
  iddia: Hesaplama motoru deterministik, günlük ızgaralı ve her rakamı kaynağına kadar açıklayan biçimde kurulmalı; Monte Carlo ilk sürümde gereksiz.
  gerekce: Kullanıcı yazılımcı değil; "bu rakam nereden geldi" güveni belirler; küçük veride olasılıksal dağılım sahte kesinlik üretir.
  oneri: Kaynaklardan lineage'lı cf_item'lar → senaryo düzeltmeleri → günlük bakiye → en düşük nakit noktası ve eşik altı günler → hafta/ay/çeyrek/yıl kovaları. Sonuç istek anında hesaplanır, saklanmaz (CF-06 anlık görüntüsü hariç). Ayrıntı bölüm 4.
  somut: Features/CashFlow/ForecastEngine.cs; GET /api/cashflow/forecast?granularity=week|month|quarter|year&horizon=&scenario=&asOf=; GET /api/cashflow/items/{id}/explain.
  kabul: ForecastTests: açılış 100.000; 5'inde kira 45.000; 10'unda planlı tahsilat 60.000; 26'sında SGK 30.000 → en düşük nokta 5'inde 55.000; kova toplamı = kalem toplamı; explain formülü ve parametre sürümünü döndürür.
  bagimlilik: [CF-01, CSH-01, TAX-01]
  efor: L
  adr: yeni ADR: ADR-010 Nakit akışı plan ve senaryo modeli
  kanit: yok
  guven: yuksek
- id: CF-04
  oncelik: P2
  tur: eksik
  iddia: Senaryo motoru veriyi kopyalamadan temel plana uygulanan "düzeltme katmanları" olarak kurulmalı.
  gerekce: Kopya senaryo tabloları senkron sorunu yaratır; düzeltme listesi açıklanabilir ve karşılaştırılabilir.
  oneri: plan.scenario (base | optimistic | conservative | custom) + plan.scenario_adjustment (seçici: kategori/cari/kural/kaynak türü/yön; işlem: scale, shift_days, add, remove; tarih aralığı). Hazır: iyimser (tahsilat oranı 1,0, gecikme 0), temkinli (tahsilat × 0,85, +15 gün, gider +%5). "500 bin kredi" = add giriş + add taksit serisi; "20 öğrenci ayrılır" = billing_plan seçicisinde scale; "kira %40" = kural seçicisinde tarih aralıklı scale.
  somut: plan.scenario, plan.scenario_adjustment (bölüm 3); POST /api/cashflow/scenarios; GET /api/cashflow/compare?base=&scenario=.
  kabul: ScenarioTests: "kira +%40 Ocak'tan" yalnız kira kalemlerini değiştirir; explain çıktısında düzeltme id'si var; temel plan satırları değişmez.
  bagimlilik: [CF-03]
  efor: M
  adr: yeni ADR: ADR-010 Nakit akışı plan ve senaryo modeli
  kanit: yok
  guven: yuksek
- id: CF-05
  oncelik: P1
  tur: risk
  iddia: Gerçekleşen/Planlanan/Tahmini ile Resmi/Muhasebe Bekleyen iki ayrı eksen; tek "durum" alanına sıkıştırılırsa gruplar sessizce toplanır.
  gerekce: Bir tahsilat hem gerçekleşen hem belge bekleyen olabilir; kira kalemi planlı ve belge kapsamı dışı.
  oneri: Her kalemde iki enum: certainty (realized | planned | forecast) ve doc_scope (official | pending | none). API toplamları iki eksenli matris olarak döndürür; tek toplam yalnız etiketli "Genel toplam" satırında. Grafikte kesinlik desen + etiketle ayrılır.
  somut: ForecastEngine çıktı şeması; CashflowCell{certainty, docScope, amount}; ekranda "Bu toplam neleri içeriyor" satırı.
  kabul: Sözleşme testi: forecast yanıtında etiketli genel toplam dışında eksensiz toplam alanı yok.
  bagimlilik: [CF-03, ACC-02]
  efor: S
  adr: yeni ADR: ADR-010 Nakit akışı plan ve senaryo modeli
  kanit: docs/designs/MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md:240-249
  guven: yuksek
- id: CF-06
  oncelik: P3
  tur: iyilestirme
  iddia: Tahmin doğruluğu ölçülmezse motor kalibre edilemez.
  gerekce: Nakit planı araçlarının güveni plan-gerçekleşen karşılaştırmasından gelir.
  oneri: Her ay sonu base senaryonun sonraki 3 ay kovaları plan.forecast_snapshot'a yazılır; gerçekleşince kategori bazlı sapma ve en büyük 5 sapma kalemi raporlanır.
  somut: plan.forecast_snapshot; GET /api/cashflow/accuracy.
  kabul: 2 ay sonra rapor kategori bazlı sapma yüzdesi üretir.
  bagimlilik: [CF-03]
  efor: S
  adr: yok
  kanit: yok
  guven: orta
- id: TAX-01
  oncelik: P1
  tur: eksik
  iddia: Vergi/SGK oran ve tarihleri için tarih aralıklı, kaynak ve teyit bilgili parametre tablosu yok.
  gerekce: Oranlar ve günler değişir; koda sabit yazılırsa geçmiş dönem yeniden hesaplandığında sonuç değişir (standart §11.4).
  oneri: core.tax_parameter (küresel, sürümlü) + core.tenant_parameter (kiracı istisnası); her değerde valid_from/valid_to, kaynak, verified_by (MM), verified_at. Teyitsiz değerle hesaplanan her rakam "teyit bekliyor" rozetli. core.holiday iş günü kaydırması için. Anahtarlar bölüm 4.4.
  somut: ParameterService.Get(key, date, tenant); GET/POST /api/tax-parameters (Yönetici; SaaS'ta küresel olanlar yalnız platform).
  kabul: ParameterTests: 2026-06 hesabı eski değeri, 2026-08 yeni değeri kullanır; verified_at boş parametreyle üretilen kalemde needsVerification=true.
  bagimlilik: []
  efor: M
  adr: yeni ADR: ADR-011 Vergi ve mevzuat parametreleri
  kanit: belge bölüm 1 madde 3
  guven: yuksek
- id: TAX-02
  oncelik: P1
  tur: eksik
  iddia: Aylık KDV tahmini resmi belgelerden hesaplanabilir; belge bekleyen satırların KDV etkisi ayrı gösterilmeli.
  gerekce: Veri var (document_vat_line); bekleyen satışlar faturalandığında doğacak KDV karar için kritik ve belgelenmeyi teşvik eder.
  oneri: Hesaplanan_m − İndirilecek_m − Devreden_{m−1} (formüller bölüm 4.3). Vade izleyen ay, gün parametre. "Belge bekleyen KDV etkisi" ayrı satır, toplamlara girmez. Tevkifat ve istisna v1 dışı.
  somut: TaxEstimator.VatAsync(month); GET /api/tax/vat?from&to; plan.cash_item source_type='tax_estimate'.
  kabul: VatTests: Ekim H=10.000, İ=4.000, devreden 1.000 → ödenecek 5.000; Kasım İ > H → 0, devreden aktarılır.
  bagimlilik: [TAX-01, ACC-13]
  efor: M
  adr: yeni ADR: ADR-011 Vergi ve mevzuat parametreleri
  kanit: docs/designs/MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md:49-67
  guven: yuksek
- id: TAX-03
  oncelik: P2
  tur: eksik
  iddia: Geçici ve kurumlar vergisi tahmini düşük güvenli; v1'de MM'nin tahmini tutarı planlı kalem olarak girilmeli, formül v2'de.
  gerekce: Matrah KKEG, amortisman, finansman gideri kısıtlaması gibi MM'deki verilere bağlı; yanlış otomatik rakam yanlış güven üretir.
  oneri: v1: dönem başına "MM tahmini" kalemi (planned, source=mm_estimate). v2: kümülatif resmi gelir − resmi gider (belge netleri) + KKEG − amortisman (elle) → × oran − önceki ödemeler; 4. dönem yok (doğrulanmalı); yurt içi asgari kurumlar vergisi karşılaştırması (doğrulanmalı).
  somut: TaxEstimator.ProvisionalAsync(quarter), CorporateAsync(year); ekranda "güven: düşük" rozeti.
  kabul: v1 kalemi takvimde görünür; v2 fikstürde matrah × oran − mahsup doğru; parametre değişince geçmiş dönem sonucu parametre sürümüne göre sabit.
  bagimlilik: [TAX-01, CF-01]
  efor: M
  adr: yeni ADR: ADR-011 Vergi ve mevzuat parametreleri
  kanit: docs/designs/MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md:69-84
  guven: orta
- id: TAX-04
  oncelik: P1
  tur: eksik
  iddia: Maaş, muhtasar ve SGK için kişi bazlı bordro tutmadan aylık "bordro özeti" yeterli; tahmin son onaylı özetten türetilmeli.
  gerekce: Kişi bazlı ücret hassas veri ve MM'de zaten var; nakit akışı yalnız toplamlar ve tarihler ister.
  oneri: payroll.monthly_summary (dönem, net ücret, GV stopajı, damga vergisi, SGK toplamı, teşvik indirimi, vade tarihleri, kaynak, onaylayan). Gelecek aylar: son özet × (1 + artış parametresi), asgari ücret değişim tarihlerinde basamak; kira/serbest meslek stopajı tekrar kuralından.
  somut: payroll şeması (RLS'li); POST /api/payroll/summaries (+ Excel şablonu); plan.cash_item source_type='payroll'; yalnız Yönetici görür.
  kabul: PayrollTests: Eylül özeti girilince Ekim muhtasar/SGK kalemleri planned, Kasım ve sonrası forecast; Muhasebe rolü özet ayrıntısını göremiyorsa 403 (rol matrisi kararına göre).
  bagimlilik: [TAX-01, CF-01]
  efor: M
  adr: yeni ADR: ADR-011 Vergi ve mevzuat parametreleri
  kanit: docs/designs/MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md:155-184
  guven: yuksek
- id: INT-01
  oncelik: P1
  tur: eksik
  iddia: Ön muhasebe olaylarından kanonik yevmiye fişi üreten katman ve kural bazlı hesap kodu eşlemesi yok; kart başına 1:1 account_code ölçeklenmez.
  gerekce: Yüzlerce veli için kart kart kod girmek sürdürülemez; Logo bağlantı kodu mantığı "kural + istisna"dır.
  oneri: integration.account_rule (öncelik sıralı; olay türü, kart türü, kategori, KDV oranı, belge türü, kanal, kod deseni → hesap kodu şablonu, ör. "120.01.{party_code}"); kart üzerindeki account_code en yüksek öncelikli istisna. JournalBuilder yalnız aktarım sahibi uusport olan resmi olaylardan dengeli fiş üretir; eşlenemeyen satır partiyi engeller.
  somut: integration.chart_of_accounts, account_rule, journal_batch, journal_line (bölüm 3); JournalBuilder; GET /api/exports/journal/preview?period=.
  kabul: JournalTests: her fiş borç = alacak; kuralsız olay "Eşlenemedi" listesine düşer, parti oluşmaz; kart istisnası kuralı ezer.
  bagimlilik: [ACC-02, ACC-03, CSH-01, INT-02]
  efor: L
  adr: yeni ADR: ADR-006 Hesap planı eşlemesi ve aktarım sahipliği
  kanit: Entities.cs:57-58, :89-90; docs/designs/SURUM2_HAZIRLIK_NOTU_2026-09-26.md:65-84
  guven: yuksek
- id: INT-02
  oncelik: P1
  tur: risk
  iddia: Hangi olayın muhasebeye hangi sistemden gideceği (aktarım sahipliği) tanımlanmadan fiş aktarımı çift kayıt üretir.
  gerekce: e-Arşiv faturalar MM'ye GİB/UBL yoluyla, banka hareketleri ekstre sihirbazıyla gidebilir (A5); UU Sport aynılarını fiş olarak da gönderirse GMS.NET'te iki kez kayıt olur.
  oneri: MM ile olay türü başına sahiplik matrisi (bölüm 8.1). GMS.NET yolu: önce Excel/XML fiş içe aktarımı (sözleşmesiz, MM kontrolünde, test firmasında); API yalnız lisans, ücret ve test ortamı netleşince. İlk değerli çıktı: banka girişleri için "cari eşleme dosyası" (hangi havale hangi veli).
  somut: integration.export_ownership(event_type, owner: uusport | mm_ubl | mm_bank | none, valid_from); JournalBuilder sahibi uusport olmayan olayı atlar; GMS.NET dönüştürücüsü (INT-03).
  kabul: MM test firmasına 1 aylık sentetik parti yüklenir; GMS.NET mizanı UU Sport resmi mizanıyla hesap bazında eşit; çift kayıt 0.
  bagimlilik: []
  efor: S
  adr: yeni ADR: ADR-006 Hesap planı eşlemesi ve aktarım sahipliği
  kanit: docs/decisions/ADR-004:191-193; belge bölüm 6 madde 5
  guven: orta
- id: INT-03
  oncelik: P2
  tur: iyilestirme
  iddia: Program bağımsız kanonik fiş formatı + program başına dönüştürücü kurulmalı; dönüştürücüler sırayla eklenmeli.
  gerekce: Satılabilir üründe müşterinin MM'si farklı programda olacak; tek kanonik model test yükünü düşürür.
  oneri: Kanonik alanlar bölüm 8.2. IJournalExporter { Format, Validate(batch), Write(batch, stream) }. Öncelik: GMS.NET → Logo → Luca → Mikro → Zirve. Her biri için MM'den gerçek örnek şablon + altın dosya testi.
  somut: Features/Export/Exporters/*.cs; GET /api/exports/journal/{batchId}?format=gmsnet-xlsx.
  kabul: GoldenFileTests: sabit partiden üretilen dosya altın dosyayla hücre düzeyinde eşit; doğrulayıcı tanımsız hesap kodunu reddeder.
  bagimlilik: [INT-01]
  efor: M
  adr: yeni ADR: ADR-006 Hesap planı eşlemesi ve aktarım sahipliği
  kanit: yok
  guven: orta
- id: INT-04
  oncelik: P2
  tur: eksik
  iddia: İçe aktarımlar elle girişin yerine geçmeli: UBL-TR (e-Arşiv/e-Fatura XML), GİB portalı belge listesi, banka ekstresi, POS üye işyeri ekstresi, MM mizanı.
  gerekce: ADR-005'in olumsuz yanı "fatura iki kez yazılır"; UBL'den ETTN, VKN ve KDV kırılımı hatasız gelir; mizan resmi açılışın kaynağıdır (ACC-08).
  oneri: Mevcut aktarım altyapısına (önizle → uygula → geri al) yeni türler: ubl_invoice (XML/zip), gib_portal_list (Excel), bank_statement (banka başına kolon şablonu), pos_statement, trial_balance. Dosyalar yalnız canlıda işlenir; saklama süresi parametreli.
  somut: ImportKinds genişletme; billing.document +ettn uuid null, +source ('manual' | 'ubl'); UBL ayrıştırıcı (cbc:ID, cbc:UUID, cac:TaxTotal/cac:TaxSubtotal).
  kabul: UblImportTests: sentetik e-Arşiv XML → belge no, ETTN, KDV satırları doğru; aynı ETTN ikinci kez reddedilir.
  bagimlilik: [ACC-08]
  efor: L
  adr: ADR-005 değişir
  kanit: docs/decisions/ADR-005 bölüm 5
  guven: yuksek
- id: INT-05
  oncelik: P2
  tur: eksik
  iddia: Banka ekstresi eşleştirme motoru tahsilat girişini ve cari atamayı büyük ölçüde otomatikleştirir.
  gerekce: Veli havalelerinin açıklaması öğrenci adı veya telefon içerir; elle eşleştirme en büyük zaman kaybıdır.
  oneri: finance.bank_statement_line (değişmez) → öneri: tutar + tarih ± 3 gün + referans; açıklamada sporcu/veli arama anahtarı (ADR-003 normalizasyonu); öğrenilen kurallar (bank_rule: desen → cari/kategori). Kabul edilen öneri Payment veya cash_voucher üretir ve satıra bağlanır.
  somut: finance.bank_statement_line, finance.bank_rule; GET /api/bank/reconciliation/queue; POST /api/bank/lines/{id}/accept.
  kabul: BankMatchTests: "AHMET YILMAZ AIDAT EYLUL" açıklamalı 3.800 TL satırda eşleşen veli ilk öneri; kabul sonrası satır reconciled, ikinci kez kullanılamaz.
  bagimlilik: [INT-04, CSH-01]
  efor: L
  adr: yok
  kanit: belge bölüm 5.3 (ADR-005 dışarıda: banka ekstresi eşleştirme)
  guven: yuksek
- id: SAAS-01
  oncelik: P1
  tur: risk
  iddia: Kiracı kullanıcının kimliğinden değil "veritabanındaki ilk kiracı"dan çözülüyor ve kimlik doğrulamadan önce ayarlanıyor.
  gerekce: İkinci kiracı eklendiği an tüm kullanıcılar ilk kiracının verisini görür; en pahalı SaaS kapısı, bugün küçük iş.
  oneri: Girişte tenant_id claim'i çereze yazılır; kiracı ara katmanı UseAuthentication'dan sonra claim'den okur; anonim uçlar kiracısız; TenantResolver yalnız OnPrem kurulum yardımcısı.
  somut: Program.cs ara katman sırası; AppClaimsPrincipalFactory (tenant_id); TenantContext claim okuması.
  kabul: MultiTenantTests: iki kiracı, iki kullanıcı; her biri yalnız kendi carilerini görür; claim'siz oturum 401.
  bagimlilik: []
  efor: S
  adr: yeni ADR: ADR-012 Kiracı çözümleme ve SaaS kapıları
  kanit: src/UUSport.Api/Data/TenantContext.cs:47-62; src/UUSport.Api/Program.cs:153-163
  guven: yuksek
- id: SAAS-02
  oncelik: P1
  tur: risk
  iddia: identity şemasında RLS yok ve kullanıcı uçları kiracı filtresi uygulamıyor; çok kiracıda bir yönetici diğer kiracının kullanıcılarını listeler, parolasını ve MFA'sını sıfırlar.
  gerekce: Tek kiracıda sömürülemez; SaaS'ta en ağır yetki yükseltme yolu; şimdi ucuz.
  oneri: AppUser için EF global query filter (TenantId == current) + kullanıcı uçlarında açık kiracı eşitliği; girişte global benzersiz kullanıcı adıyla arama korunur (filtre IgnoreQueryFilters ile yalnız sign-in deposunda).
  somut: AppDbContext AppUser HasQueryFilter; UserEndpoints tüm uçlar; negatif testler.
  kabul: CrossTenantUserTests: A yöneticisi B kullanıcısına GET/PUT/password/reset-mfa → 404; liste yalnız A kullanıcıları.
  bagimlilik: [SAAS-01]
  efor: S
  adr: yeni ADR: ADR-012 Kiracı çözümleme ve SaaS kapıları
  kanit: src/UUSport.Api/Features/Users/UserEndpoints.cs:23, :58, :86, :99; src/UUSport.Api/Data/MigrationSecurity.cs:21-27
  guven: yuksek
- id: SAAS-03
  oncelik: P2
  tur: risk
  iddia: app.tenant_id bağlantı açılışında oturum düzeyinde ayarlanıyor; standart "her transaction başında" diyor; güvenlik havuz davranışına örtük bağlı.
  gerekce: Npgsql havuza dönen bağlantıyı varsayılan olarak sıfırlar ve her açılışta interceptor çalışır, bugün sızıntı beklenmez (doğrulanmalı); EF dışı bağlantı, uzun açık bağlantı veya "No Reset On Close" bu güvenceyi bozar.
  oneri: Değer işlem-yerel kurulsun: tüm işler açık transaction içinde, transaction başında set_config('app.tenant_id', @t, true) (DbTransactionInterceptor); test modunda komut interceptor'ı değerin TenantContext ile aynı olduğunu doğrular; bağlantı dizesine "No Reset On Close=false" açıkça yazılır.
  somut: Data/TenantTransactionInterceptor.cs; UnitOfWork (ACC-06) ile birleşir; PoolLeakTests.
  kabul: PoolLeakTests: Max Pool Size=1, iki kiracı arasında 1.000 dönüşümlü istek → yabancı satır 0.
  bagimlilik: [ACC-06, SAAS-01]
  efor: S
  adr: yeni ADR: ADR-012 Kiracı çözümleme ve SaaS kapıları
  kanit: TenantContext.cs:38; docs/standards/UUSPORT_GUVENLIK_STANDARDI.md §4.2
  guven: orta
- id: SAAS-04
  oncelik: P1
  tur: iyilestirme
  iddia: "Her kiracı tablosunda RLS açık ve zorlanmış" kuralı katalogdan otomatik doğrulanmıyor; yeni migration unutursa fark edilmez.
  gerekce: 26.09 migration güvenlik yardımcısı hatası örtük kuralların kırılgan olduğunu gösterdi.
  oneri: Şema koruma testi: pg_catalog'dan tenant_id kolonu olan her tablo → relrowsecurity ∧ relforcerowsecurity ∧ tenant_isolation politikası; append-only listede app rolünde UPDATE/DELETE/TRUNCATE yok; public şemada uygulama nesnesi yok; zincir sıfır veritabanında.
  somut: tests/UUSport.Tests/SchemaGuardTests.cs (gerçek PostgreSQL); tools/ci.ps1 adımı.
  kabul: ApplyRls çağırmadan tablo ekleyen dal testte kırmızı.
  bagimlilik: []
  efor: S
  adr: yok
  kanit: src/UUSport.Api/Data/MigrationSecurity.cs:13-81; belge bölüm 4 (26.09)
  guven: yuksek
- id: SAAS-05
  oncelik: P2
  tur: eksik
  iddia: Kiracı başına ayar, terminoloji, marka, numara serisi, para birimi ve saat dilimi kapıları yok; bazı değerler koda gömülü.
  gerekce: Ekstre başlığı "UU Sport" sabit; öğrenci aktarımı "Sayfa1 (2)" sabit; kod numarası max+1 (eşzamanlı çakışma); "bugün" sunucu saati.
  oneri: core.tenant_setting; core.tenant_sequence (seri → sonraki no, FOR UPDATE); terminoloji sözlüğü (sporcu/öğrenci/üye, veli/müşteri); para tutan tablolara currency char(3) default 'TRY' şimdi; kiracı saat dilimi (Europe/Istanbul) ile IClock.Today.
  somut: PartyPages.tsx:344 → tenant.name; ImportService.cs:24 → ayar; PartyEndpoints.cs:18-23 → SequenceService; AccountingService.cs:39 → IClock.
  kabul: İki kiracı farklı adla ekstre basar; 50 paralel cari oluşturmada kod çakışması 0.
  bagimlilik: [SAAS-01]
  efor: M
  adr: yeni ADR: ADR-012 Kiracı çözümleme ve SaaS kapıları
  kanit: PartyPages.tsx:344; src/UUSport.Api/Features/Imports/ImportService.cs:24; PartyEndpoints.cs:18-23; AccountingService.cs:39
  guven: yuksek
- id: SAAS-06
  oncelik: P3
  tur: fikir
  iddia: Aynı kod tabanı OnPrem (tek kiracı) ve SaaS (çok kiracı) modlarında yaşamalı; fark yapılandırmada ve kiracı kataloğunda olmalı.
  gerekce: İlk müşteriler on-prem isteyebilir; iki kod dalı bakım maliyetini ikiye katlar.
  oneri: Deployment:Mode=OnPrem|Saas. OnPrem: tek kiracı, kurulum jetonu (SEC-04), yerel yedek. Saas: core.tenant_catalog (durum, plan, bölge, isteğe bağlı ayrılmış veritabanı bağlantısı), provisioning, core.tenant_feature (özellik bayrakları); lisans/abonelik dış sistemde. İzolasyon varsayılanı paylaşımlı şema + RLS; regüle/büyük müşteriye aynı kodla ayrılmış veritabanı.
  somut: appsettings Deployment bölümü; core.tenant_catalog, core.tenant_feature; ProvisioningService.
  kabul: Aynı derleme iki modda kabul testlerini geçer.
  bagimlilik: [SAAS-01, SAAS-05]
  efor: L
  adr: yeni ADR: ADR-012 Kiracı çözümleme ve SaaS kapıları
  kanit: yok
  guven: orta
- id: SAAS-07
  oncelik: P3
  tur: eksik
  iddia: Kiracı bazlı dışa aktarım/silme (KVKK), kiracı bazlı geri yükleme ve destek personelinin denetimli erişimi tasarlanmalı; paylaşımlı şemada tek kiracıyı yedekten dönmek zordur.
  gerekce: SaaS'ta bir müşterinin hatası için tüm veritabanı geri yüklenemez.
  oneri: Kiracı mantıksal dışa aktarımı (tüm tenant_id satırları, sürümlü paket + manifest) ve yeni tenant_id'ye içe aktarımı aynı araçla; destek erişimi süreli, gerekçeli, kiracı onaylı; her okuma audit'e.
  somut: tools/tenant-export; core.support_access_grant(tenant_id, grantee, reason, expires_at, approved_by).
  kabul: Dışa aktar → boş veritabanına içe aktar → integrity (ACC-04) ve bakiye toplamları eşit.
  bagimlilik: [ACC-04, SAAS-06]
  efor: L
  adr: yeni ADR: ADR-012 Kiracı çözümleme ve SaaS kapıları
  kanit: yok
  guven: orta
- id: SEC-01
  oncelik: P0
  tur: risk
  iddia: Yedek yok; kurulduğunda en olası sızıntı yolu şifresiz yedek dosyası ve yedeklenmemiş anahtarlar olacak.
  gerekce: Çocuk ve finans verisi tek bilgisayarda; Data Protection anahtarları ve DPAPI ile korunan gizli dosyalar kullanıcı profiline bağlı; makine kaybında şifreli alanlar (SEC-07) açılamaz.
  oneri: Yedekler istemci tarafında şifreli (restic veya 7-Zip AES-256; parola iki kişide, ayrı yerde); harici disk + yurt içi uzak depo; Data Protection anahtar halkası sertifikayla korunur ve yedeklenir; gizli bilgiler için kurtarma prosedürü. Mekanik OPS-01.
  somut: deployment/backup/*.ps1; Program.cs:129-133 canlıda ProtectKeysWithCertificate; anahtar klasörü yedek kapsamında; docs/operations kurtarma bölümü.
  kabul: Yedek parolasız açılamaz; temiz makinede yalnız yedek + parola + sertifikayla uygulama kalkar ve MFA'lı giriş çalışır.
  bagimlilik: [OPS-01]
  efor: M
  adr: yeni ADR: ADR-016 Yedekleme ve anahtar yönetimi
  kanit: src/UUSport.Api/Program.cs:129-133; belge bölüm 5.6 madde 4
  guven: yuksek
- id: SEC-02
  oncelik: P0
  tur: eksik
  iddia: Faz 1 için HTTPS, LAN bağlama, güvenlik duvarı kuralı ve disk şifrelemesi yok; çerezler Secure değil.
  gerekce: Parola ve oturum çerezi Wi-Fi'da açık gider; standart §6 HTTPS'i zorunlu sayar.
  oneri: İç CA (kök sertifika ofis cihazlarına) + sunucu sertifikası (Windows sertifika deposu); Kestrel yalnız LAN IP:443; AllowedHosts sunucu adı; UseHsts; çerez SecurePolicy=Always ve __Host- önekli adlar; Windows Güvenlik Duvarı: 443 yalnız ofis alt ağı, 5432 yalnız 127.0.0.1; BitLocker sistem ve veri diskinde, kurtarma anahtarı kasada.
  somut: appsettings.Production.json (Kestrel Endpoints Https); Program.cs:86, :101; deployment/firewall.ps1; deployment/tls/README.md.
  kabul: Ofisteki ikinci bilgisayardan https://<sunucu-adı> uyarısız açılır; http reddedilir; port taramasında yalnız 443; manage-bde -status "Protection On".
  bagimlilik: []
  efor: M
  adr: yok
  kanit: src/UUSport.Api/appsettings.json:2, :19; Program.cs:86, :101
  guven: yuksek
- id: SEC-03
  oncelik: P0
  tur: risk
  iddia: Gerçek öğrenci/veli verisi içeren kaynak Excel hâlâ depo klasöründe; ajanların erişimi yalnız talimatla engelli.
  gerekce: Tek bir hatalı glob/grep çocuk verisini modele taşır; talimat teknik kontrol değildir (K2 açık).
  oneri: Dosya depo dışına, ajanların çalıştığı Windows hesabının okuyamadığı klasöre (NTFS deny); ajan hariç tutma ayarları ek katman; ajanlar ayrı düşük yetkili Windows hesabında; canlı veritabanı geliştirme makinesinde hiç bulunmaz.
  somut: C:\UUSport\uu sport ltd → depo dışı klasör (ACL: yalnız sahip); tools/check-no-real-data.ps1 (depo içinde xlsx/pdf taraması, ci.ps1'e bağlı).
  kabul: Ajan hesabıyla C:\UUSport altında *.xlsx araması gerçek dosya döndürmez; ACL erişim reddi kanıtı ana hafızada.
  bagimlilik: []
  efor: S
  adr: yok
  kanit: belge bölüm 5.6 madde 15; docs/project-memory/UUSPORT_ANA_HAFIZA.md:92-97
  guven: yuksek
- id: SEC-04
  oncelik: P1
  tur: risk
  iddia: Kullanıcı yokken /api/setup herkese açık; kurulum bitmeden ağa açılan veya veritabanı boşaltılan sunucuda ilk gelen yönetici olur.
  gerekce: Yalnız "hiç kullanıcı yok mu" kontrolü var; jeton yok.
  oneri: Kurulum betiği tek kullanımlık rastgele jeton üretir, yalnız sunucuda okunabilir dosyaya yazar (DPAPI makine kapsamı); POST /api/setup jetonsuz reddedilir; kurulum bitince dosya silinir.
  somut: AuthEndpoints setup ucu; deployment/install.ps1 jeton üretimi.
  kabul: SetupTests: jetonsuz 403; doğru jetonla bir kez 200; ikinci kez 400.
  bagimlilik: []
  efor: S
  adr: yok
  kanit: src/UUSport.Api/Features/Auth/AuthEndpoints.cs:47-51
  guven: yuksek
- id: SEC-05
  oncelik: P1
  tur: eksik
  iddia: Giriş, başarısız giriş, kilitlenme, MFA hatası, çıkış ve dışa aktarma denetim kaydına yazılmıyor; standart §2.2 ve §9 zorunlu kılıyor.
  gerekce: İçeriden tehdit ve parola denemesi tespit edilemez.
  oneri: auth.login_succeeded/failed/locked, auth.mfa_failed, auth.logout, export.* olayları (kullanıcı adı, IP, user-agent özeti; parola/kod asla); eşik aşımında e-posta uyarısı (OPS-04).
  somut: AuthEndpoints.cs login/mfa/logout; audit.audit_event +client_ip inet; AuditWriter IP doldurur.
  kabul: AuthAuditTests: 3 hatalı + 1 doğru giriş → 3 failed + 1 succeeded; özet alanında parola yok.
  bagimlilik: []
  efor: S
  adr: yok
  kanit: AuthEndpoints.cs:91-125; docs/standards/UUSPORT_GUVENLIK_STANDARDI.md §2.2, §9
  guven: yuksek
- id: SEC-06
  oncelik: P1
  tur: eksik
  iddia: API ucu düzeyinde yetki testi yok (72 eşleme); standart §3 "testi olmayan uç birleştirilmez" diyor.
  gerekce: Mevcut testler RLS ve iş kuralını doğruluyor, uçların politika bağını değil; unutulan tek RequireAuthorization tüm veriyi açar.
  oneri: WebApplicationFactory ile uç kataloğunu gezen test: anonim → 401 (izinli liste: setup/status, setup, login, login/mfa, health, me), MFA'sız oturum → 403, Muhasebe → Admin uçlarında 403. İzinli anonim liste kodda tek yerde.
  somut: tests/UUSport.Tests/EndpointAuthorizationTests.cs; EndpointDataSource üzerinden tüm RouteEndpoint'ler.
  kabul: Politikasız yeni uç eklenirse test kırmızı.
  bagimlilik: []
  efor: S
  adr: yok
  kanit: docs/standards/UUSPORT_GUVENLIK_STANDARDI.md §3; tests klasöründe uç yetki testi yok
  guven: yuksek
- id: SEC-07
  oncelik: P2
  tur: risk
  iddia: TOTP gizli anahtarı ASP.NET Identity varsayılanıyla identity.user_token'da düz metin; veritabanı yedeği sızarsa MFA da düşer.
  gerekce: Identity authenticator anahtarını ek yapılandırma olmadan şifrelemez (doğrulanmalı).
  oneri: UserStore'da Get/SetAuthenticatorKeyAsync'i IDataProtector ("UUSport.Totp.v1") ile saran alt sınıf; mevcut anahtarlar tek seferlik komutla şifrelenir.
  somut: Infrastructure/ProtectedUserStore.cs; Program.cs AddUserStore; UUSport.Api.exe protect-totp.
  kabul: user_token'daki AuthenticatorKey değeri base32 desenine uymaz; mevcut kullanıcı kodla giriş yapar.
  bagimlilik: [SEC-01]
  efor: S
  adr: yok
  kanit: AuthEndpoints.cs:148-152
  guven: orta
- id: SEC-08
  oncelik: P2
  tur: risk
  iddia: MFA kurtarma yolu yok; tek yöneticinin telefonu kaybolursa kimse giremez.
  gerekce: "Yönetici sıfırlar" yalnız başka yönetici varsa çalışır; kurtarma kodu yok.
  oneri: En az iki Yönetici kuralı (uyarı); sunucuda yerel yönetici + veritabanı sahibi kimliğiyle çalışan `UUSport.Api.exe reset-mfa --user` (audit'e yazar); isteğe bağlı 10 tek kullanımlık kurtarma kodu (hash'li).
  somut: Program.cs komut satırı modu; Kullanıcılar ekranında "tek yönetici" uyarısı; Identity recovery codes.
  kabul: Komut yalnız owner bağlantısıyla çalışır ve user.mfa_reset_cli yazar; kurtarma kodu bir kez kullanılabilir.
  bagimlilik: []
  efor: S
  adr: yok
  kanit: belge bölüm 5.5 ("Kurtarma kodu yok")
  guven: yuksek
- id: SEC-09
  oncelik: P2
  tur: risk
  iddia: Ekstre ve rapor çıktıları tarayıcıda üretiliyor; kimin neyi dışa aktardığı bilinmiyor, çıktılar kişisel veri taşıyor ve filtre kontrolleriyle birlikte basılıyor.
  gerekce: PDF/Excel dışa aktarımları en olası sızıntı yollarından; standart §9 dışa aktarımı denetim kapsamına alır.
  oneri: Cari ekstre ve rapor PDF'leri sunucuda (QuestPDF; lisans koşulu doğrulanmalı) üretilir ve export.* audit olayı yazılır; altbilgide hazırlayan, tarih/saat, sayfa X/Y; telefon/e-posta varsayılan çıktı dışında; tarayıcı yazdırma yalnız önizleme.
  somut: Features/Documents/StatementPdf.cs; GET /api/parties/{id}/statement.pdf?scope&from&to; audit export.statement.
  kabul: PDF indirilince audit kaydı oluşur; PDF'de form kontrolü, "Gayri" ve iç jargon yok.
  bagimlilik: [ACC-01, SEC-05]
  efor: M
  adr: yok
  kanit: PartyPages.tsx:400; ek 14-ekstre-pdf-ciktisi.pdf
  guven: yuksek
- id: SEC-10
  oncelik: P2
  tur: iyilestirme
  iddia: Hareketsizlik zaman aşımı yok (8 saat kayan), sağlık ucu anonim olarak sürüm ve migration adını veriyor, hız sınırı ters vekil arkasında tek IP'ye düşecek.
  gerekce: Standart §2.2 hareketsizlikte zaman aşımı ister; Faz 2'de vekil arkasında RemoteIpAddress herkes için aynıdır.
  oneri: Hareketsizlik 60 dk (parametre) + mutlak 12 saat; anonim health yalnız {status}; ForwardedHeaders yalnız bilinen vekil; hız sınırı bölümlemesi kullanıcı adı + IP; /api için genel üst sınır.
  somut: Program.cs:81-91, :113-119, :165-169; ForwardedHeadersOptions.KnownProxies.
  kabul: 61 dk işlemsiz oturum 401; anonim health yanıtında version alanı yok.
  bagimlilik: []
  efor: S
  adr: yok
  kanit: Program.cs:87-88, :117, :165-169
  guven: yuksek
- id: SEC-11
  oncelik: P2
  tur: risk
  iddia: Açılış bakiyesini oluşturan kullanıcı kendi kaydını onaylayabiliyor; "muhasebe onayı" görev ayrılığı sağlamıyor.
  gerekce: Staff politikası iki rolü de kapsıyor; karar veren ≠ oluşturan kontrolü yok.
  oneri: DecideOpeningBalanceAsync: DecidedBy ≠ CreatedBy ve onaylayıcı rolü kiracı ayarından (varsayılan Muhasebe); aynı kural dekont ve eşik üstü düzeltmelere (ACC-11).
  somut: AccountingService.cs:503-521; core.tenant_setting 'approval.opening_balance.roles'.
  kabul: SoDTests: oluşturan onaylarsa 403; başka yetkili kullanıcı onaylar.
  bagimlilik: []
  efor: S
  adr: yok
  kanit: AccountingService.cs:503-521; AccountingEndpoints.cs:129-133
  guven: yuksek
- id: SEC-12
  oncelik: P2
  tur: eksik
  iddia: KVKK için veri modeli kapıları yok: aydınlatma/açık rıza kaydı, saklama-imha, veri sahibi başvurusu.
  gerekce: Çocuk verisi; Faz 3 ve SaaS öncesi geriye dönük eklemek pahalı.
  oneri: crm.consent (cari/sporcu, amaç, metin sürümü, kanal, verildi/geri alındı, kaydeden); core.retention_policy (veri sınıfı, süre, anonimleştir|sil) + zamanlanmış imha işi (finans kaydı silinmez, kişi alanları anonimleşir; yasal saklama süresi AV/MM); veri sahibi başvuru kaydı ve dışa aktarma; minimizasyon: doğum yılı yeterliyse tam tarih tutulmaz, TCKN tutulursa alan şifreli.
  somut: crm.consent, core.retention_policy, core.data_subject_request (bölüm 3); GET /api/parties/{id}/personal-data-export.
  kabul: İmha işi test verisinde süresi dolan sporcuyu anonimleştirir; ledger toplamları değişmez.
  bagimlilik: [SEC-05]
  efor: M
  adr: yeni ADR: ADR-017 KVKK veri yaşam döngüsü
  kanit: docs/standards/UUSPORT_GUVENLIK_STANDARDI.md §8
  guven: orta
- id: SEC-13
  oncelik: P2
  tur: eksik
  iddia: Faz 2'nin (internete açık sunucu + mobil) kontrolleri somut değil.
  gerekce: RDP'nin internete açılması ve kaybolan mobil cihaz en olası iki büyük olay.
  oneri: 3389 asla açık değil (WireGuard veya RD Gateway + MFA); uygulama 443'te ters vekil veya Kestrel + otomatik yenilenen sertifika; barındırma Türkiye'de; mobil: kısa ömürlü erişim + cihaza bağlı yenileme belirteci, cihaz iptali, işletim sistemi güvenli deposu, çevrimdışı önbellekte kişisel veri yok, fiş fotoğrafı galeriye kaydedilmeden doğrudan sunucuya.
  somut: identity.device(user_id, tenant_id, name, refresh_hash, last_seen_at, revoked_at); POST /api/auth/devices/{id}/revoke; deployment/faz2/*.
  kabul: Dış taramada yalnız 443; iptal edilen cihazın yenileme isteği 401.
  bagimlilik: [SEC-02, SEC-10]
  efor: L
  adr: yok
  kanit: docs/standards/UUSPORT_GUVENLIK_STANDARDI.md §6
  guven: yuksek
- id: UX-01
  oncelik: P1
  tur: iyilestirme
  iddia: Arayüzde geliştirici dili var: "ADR-004", "tek kaynak defter", "Fatura cariye ayrı tutar yazmaz; satırın belgesi olarak görünür", "Çek kaydı cariye tutar yazmaz".
  gerekce: İçerik merceği: kullanıcı muhasebe dilini bekler; bu metinler PDF'e de basılıyor (ek 14).
  oneri: Sayfa açıklamaları tek cümle, kullanıcı diliyle ("Tüm carilerin hareketleri"); kural açıklaması gerekiyorsa "?" yardım bağlantısında. Metinler tek sözlük dosyasında (SAAS-05 terminolojisiyle).
  somut: WorkPages.tsx:140, :198; PartyPages.tsx:249; CashDetailPages.tsx:254; src/UUSport.Web/src/texts.ts.
  kabul: Web kaynaklarında görünür metinde "ADR-" ve "tutar yazmaz" araması 0.
  bagimlilik: []
  efor: S
  adr: yok
  kanit: src/UUSport.Web/src/pages/WorkPages.tsx:198; PartyPages.tsx:249; CashDetailPages.tsx:254; ekran 02, 06
  guven: yuksek
- id: UX-02
  oncelik: P1
  tur: eksik
  iddia: Yazdırma sayfa bazlı yamalarla ilerliyor; ek 14'te filtre kontrolleri basılı, başlık bloğu, filtre özeti ve sayfa numarası yok, kâğıt Letter.
  gerekce: Muhasebe çıktısı arşivlenir ve üçüncü kişiye verilir; tutarlı başlık/altbilgi güven verir.
  oneri: Ortak PrintLayout: kiracı adı, rapor adı, kapsam (Toplam/Belgelenmiş), tarih aralığı, oluşturma zamanı, kullanıcı; @page { size: A4; margin: 12mm }; tüm form kontrollerinde .no-print; thead her sayfada tekrar. Uzun vadede SEC-09 sunucu PDF'i.
  somut: components.tsx PrintLayout; styles.css @media print; Hesap Hareketleri ve Raporlar sayfaları.
  kabul: Headless Edge PDF'i A4, başlık bloğu ve "Sayfa 1/2" içerir; input/select yok (görsel test).
  bagimlilik: []
  efor: S
  adr: yok
  kanit: ek 14-ekstre-pdf-ciktisi.pdf (612×792 pt, filtreler basılı); src/UUSport.Web/src/styles.css:319-341
  guven: yuksek
- id: UX-03
  oncelik: P2
  tur: eksik
  iddia: Logo kullanıcısının hızı için klavye-öncelikli giriş yok: komut paleti, kısayollar, hızlı tarih girişi, çok satırlı satış ızgarası.
  gerekce: Günlük iş (tahsilat, satış, fatura girişi) tekrarlı; fareyle diyalog açmak Logo'ya göre yavaş hissettirir.
  oneri: Ctrl+K komut paleti (fiil + nesne: "tahsilat al yılmaz", "fatura gir"); "/" arama; diyalogda Ctrl+Enter kaydet, Esc vazgeç; tarih alanı "26", "26.9", "b" (bugün), "+3" kabul eder; satış diyaloğunda çok satırlı ızgara (Enter sonraki hücre, kodla ürün); kısayollar düğme ipuçlarında ve aria-keyshortcuts ile görünür. Kısayollar keşfedilebilir ama zorunlu değil.
  somut: components.tsx CommandPalette, DateInput, useHotkeys; Dialogs.tsx satış ızgarası.
  kabul: Fare kullanmadan cari bul → 3 satırlı satış → tahsilat → kaydet 30 sn altında (uçtan uca test); tüm kontrollere Tab ile erişim.
  bagimlilik: []
  efor: M
  adr: yok
  kanit: src/UUSport.Web/src/components.tsx:117 (tek keydown işleyicisi); belge bölüm 5.9
  guven: yuksek
- id: UX-04
  oncelik: P2
  tur: iyilestirme
  iddia: Satır işlemleri kalabalık ve yıkıcı işlem sıradan görünüyor (Çek Portföyü'nde her satırda 4 işlem, "Karşılıksız" mavi bağlantı).
  gerekce: Agency/Responsibility: yıkıcı işlem ayırt edilir, sonucu söylenir, vazgeçilebilir.
  oneri: Duruma göre tek birincil işlem (Portföyde → "Tahsile ver"; Tahsilde → "Tahsil edildi") + "⋯" taşma menüsü; "Karşılıksız" kırmızı metin, onayda sonuç cümlesi ("Veli carisine 2.500 TL borç yazılacak") ve gerekçe zorunlu.
  somut: CashDetailPages.tsx çek satırı; components.tsx RowMenu, ConfirmDialog.
  kabul: Her satırda en fazla 1 görünür düğme + menü; karşılıksız onayı sonuç tutarını gösterir.
  bagimlilik: [ACC-09]
  efor: S
  adr: yok
  kanit: ekran 11-cek-portfoyu.png
  guven: yuksek
- id: UX-05
  oncelik: P2
  tur: iyilestirme
  iddia: Kasa ve Banka ekranı eksi bakiyeyi açıklamasız, yükümlülük hesaplarını ₺0 gösteriyor; açılış bakiyesi yokken bile sayı veriyor.
  gerekce: Yanlış görünen tek sayı uygulamaya güveni düşürür.
  oneri: Varlık/Yükümlülük grupları; yükümlülükte "Borç" etiketi; açılışsız hesapta "Açılış bakiyesi girilmedi — Gir" boş durum düğmesi; eksi kasa/banka kırmızı rozet + açıklama.
  somut: CashDetailPages.tsx Kasa ve Banka listesi; bakiye yanıtına hasOpening, isLiability.
  kabul: Görsel test: açılışsız hesapta tutar yerine düğme; kredi satırında kalan anapara "Borç".
  bagimlilik: [CSH-01, CSH-03]
  efor: S
  adr: yok
  kanit: ekran 09-kasa-banka-hiyerarsi.png
  guven: yuksek
- id: UX-06
  oncelik: P2
  tur: iyilestirme
  iddia: Cari detayda işlem çubuğu hiyerarşisi tutarsız ("Ekstre Al" bağlantı, diğerleri düğme) ve kartlar eşit ağırlıkta; kart etiketleri yeni resmi diliyle uyumsuz.
  gerekce: Görsel hiyerarşi: tek birincil işlem, ikincil işlemler gruplu.
  oneri: Birincil "Tahsilat Al"; ikincil "Satış", "Fatura Gir"; "⋯" menüde Ekstre, Alış, Ödeme, Dekont. Kartlar: Bakiye (imza kartı) + "Belge bekliyor" (amber) + "Belgelendi" + "Dağıtılmamış"; etiketler ACC-14 dili; sekmeler ACC-02 tanımı.
  somut: PartyPages.tsx üst çubuk ve özet kartları.
  kabul: Üst çubukta 1 birincil + en fazla 2 ikincil + menü (görsel test).
  bagimlilik: [ACC-02, ACC-14]
  efor: S
  adr: yok
  kanit: ekran 02-cari-detay.png
  guven: orta
- id: UX-07
  oncelik: P2
  tur: iyilestirme
  iddia: Muhasebe Bekleyen iş kuyruğu gibi davranmıyor: yaş/süre ve sıralama yok, işlem için cariye gitmek gerekiyor.
  gerekce: Ürünün ana farkı bu kuyruk; 7 gün eşiği (ACC-14) burada yaşar.
  oneri: Kolonlar: yaş (gün), süre rozeti, tutar; varsayılan sıralama en eski; aynı cariden çoklu seçim → tek "Fatura Gir" (toplu fatura, ADR-004 S2); satır içi "Dağıt" ve "Onayla".
  somut: WorkPages.tsx Muhasebe Bekleyen; LineRow.AgeDays; fatura diyaloğunda çoklu satır ön seçimi.
  kabul: Aynı veliye ait 3 bekleyen satır seçilip tek faturayla belgelenir; liste yaşa göre sıralı.
  bagimlilik: [ACC-14]
  efor: S
  adr: yok
  kanit: WorkPages.tsx:121; ekran 08-muhasebe-bekleyen.png
  guven: yuksek
- id: UX-08
  oncelik: P2
  tur: eksik
  iddia: Sunucu güncellenince açık sekme eski istemciyi çalıştırıyor; kullanıcı hatayı uygulamaya bağlıyor.
  gerekce: 27.09 "menüler çalışmıyor" olayı.
  oneri: Her API yanıtına X-App-Version; istemci derleme sürümüyle farklıysa kalıcı bant "Yeni sürüm yüklendi — Yenile"; sürüm uyuşmazlığında yazma isteği 409 "Sayfayı yenileyin".
  somut: SecurityMiddleware OnStarting başlığı; api.ts yanıt kontrolü; Layout.tsx bant; vite define __APP_VERSION__.
  kabul: Sürüm değiştirilip sunucu yeniden başlatılınca açık sekmede bant ilk istekte görünür.
  bagimlilik: []
  efor: S
  adr: yok
  kanit: belge bölüm 4 (27.09) ve 5.6 madde 13
  guven: yuksek
- id: UX-09
  oncelik: P2
  tur: eksik
  iddia: Nakit akışı ekranları için bilgi mimarisi ve görselleştirme kararı gerekli.
  gerekce: Ufuk, kesinlik ve belge kapsamı birlikte okunmalı; yanlış grafik sessiz toplama yapar (CF-05).
  oneri: Üç ekran (bölüm 7.3): Nakit Akışı panosu, Ödeme Takvimi, Senaryo Karşılaştırma. Her sayı tıklanınca açıklama paneli (CF-03 explain).
  somut: pages/CashflowPages.tsx; hafif grafik kütüphanesi (paket boyutu ölçülerek seçilir).
  kabul: Ufuk klavyeyle değişir; her grafiğin tablo eşdeğeri var; kova tıklanınca kalem listesi açılır.
  bagimlilik: [CF-03, CF-05]
  efor: M
  adr: yok
  kanit: yok
  guven: orta
- id: UX-10
  oncelik: P3
  tur: fikir
  iddia: Faz 2 personel mobil uygulaması "hızlı işlem" merkezli olmalı, masaüstü ekranların küçültülmüşü değil.
  gerekce: Hoca ve yönetici sahada (tahsilat, fiş, sporcu); muhasebe masada.
  oneri: Rol bazlı sekme çubuğu: Bugün · Tahsilat · Fiş · Sporcular · Diğer. Fiş: kamera → GİB karekodu çözülür → alanlar dolu gelir → onay → sunucuya (cihazda kopya yok); karekod yoksa sunucu OCR kuyruğu. Hocada borç bilgisi yok (standart §3).
  somut: src/UUSport.Mobile (Expo); POST /api/receipts (multipart, içerik türü kontrolü, web kökü dışı depolama, kiracı yolu).
  kabul: Karekodlu fiş 3 dokunuşta kaydedilir; hoca rolü API'den borç alanı alamaz.
  bagimlilik: [SEC-13]
  efor: XL
  adr: yok
  kanit: belge bölüm 3.2
  guven: orta
- id: DATA-01
  oncelik: P0
  tur: eksik
  iddia: Canlıya geçiş planı yok: geçiş tarihi, resmi/toplam açılışlar, kasa-banka açılışları, stok sayımı, portföydeki çekler ve kredi bakiyeleri hangi sırayla, kimin onayıyla girilecek belirsiz.
  gerekce: İlk gün yanlış açılış tüm raporları kalıcı bozar; Excel bakiyeleri doğrulanmış değil.
  oneri: T tarihi (ay sonu); MM'den T tarihli cari bakiye listesi ve mizan; Excel toplam bakiyeleri (scope=unofficial) ve MM listesi (scope=official) aktarımı; kasa sayımı ve banka ekstre bakiyeleriyle açılış; stok sayımı; çek ve krediler ADR-007 sonrası; maskeli kopyada kuru prova; 2 hafta paralel çalışma; fark 0 olunca Excel kapanır.
  somut: docs/operations/UUSPORT_CANLIYA_GECIS_PLANI.md; aktarım türleri opening_official, opening_total, cash_opening, stock_count; integrity (ACC-04) + "resmi bakiye = MM listesi" kontrolü.
  kabul: Kuru provada tüm kontroller 0 fark; paralel çalışma sonunda toplam alacak = Excel, resmi alacak = MM listesi.
  bagimlilik: [ACC-08, CSH-01, OPS-01]
  efor: M
  adr: yok
  kanit: belge bölüm 4 (23.09) ve 5.6 madde 7
  guven: yuksek
- id: DATA-02
  oncelik: P2
  tur: iyilestirme
  iddia: Aktarım bu işletmenin Excel'ine sabitlenmiş ("Sayfa1 (2)"); ürünleştirme için sayfa seçimi ve kayıtlı eşleme profili gerekli.
  gerekce: Başka akademinin dosya yapısı farklı olacak.
  oneri: Sayfa seçici (varsayılan kiracı ayarında), kayıtlı sütun eşleme profilleri, şablon sayfa adı "Veri"; UU Sport'un mevcut kuralı ayar olarak korunur.
  somut: ImportService.cs:24, :464-499; integration.import_profile(tenant_id, kind, name, sheet_name, mapping_json).
  kabul: Farklı sayfa adlı dosya profil seçilerek hatasız aktarılır; mevcut testler yeşil.
  bagimlilik: [SAAS-05]
  efor: S
  adr: yok
  kanit: src/UUSport.Api/Features/Imports/ImportService.cs:24; ekran 13-excel-aktarim.png
  guven: yuksek
- id: DATA-03
  oncelik: P2
  tur: risk
  iddia: Listeler sessizce kesiliyor (cari 500, ürün 500) veya sınırsız dönüyor (hesap hareketleri, satırlar); resmi hesabı cari başına tüm satırları yeniden yüklüyor.
  gerekce: 600 velili akademide cari listesi eksik görünür; Hesap Hareketleri N+1 ile yavaşlar (5.6-12).
  oneri: Anahtar tabanlı sayfalama (occurred_on, id) + toplam sayı; resmi tutar küme tabanlı SQL görünümüyle tek sorguda; sınıra ulaşıldığında arayüzde uyarı.
  somut: AccountingQueries.AllLedgerAsync, LinesAsync; PartyEndpoints.cs:60; CatalogEndpoints.cs:53; finance.v_ledger_official (ACC-02).
  kabul: 5.000 cari / 100.000 hareketli sentetik veride /api/ledger ilk sayfa < 300 ms; cari listesi toplam sayı döndürür.
  bagimlilik: [ACC-02]
  efor: M
  adr: yok
  kanit: PartyEndpoints.cs:60; CatalogEndpoints.cs:53; AccountingQueries.cs:89, :97-102
  guven: yuksek
- id: OPS-01
  oncelik: P0
  tur: eksik
  iddia: Yedekleme ve geri yükleme provası yok; RPO 15 dk / RTO 4 saat yalnız belgede.
  gerekce: Kullanıcı yedeği öne aldı; gerçek veri girilmeden kanıtlanmalı.
  oneri: WAL arşivi (archive_mode=on, archive_timeout=300, yerel arşiv klasörü) + gecelik pg_basebackup + gecelik pg_dump -Fc; Görev Zamanlayıcı; şifreli kopya harici disk + yurt içi uzak depo (SEC-01); 30 gün saklama; haftalık otomatik geri yükleme testi geçici veritabanına + integrity (ACC-04) + satır sayısı karşılaştırması; başarısızlıkta e-posta.
  somut: deployment/backup/backup.ps1, restore-test.ps1, postgresql.conf parçası; docs/operations bölüm 10 güncellemesi.
  kabul: Kuru provada PITR ile son işlem ≤ 15 dk kayıpla döner; restore-test.ps1 çıkış kodu 0 ve rapor dosyası.
  bagimlilik: []
  efor: M
  adr: yeni ADR: ADR-016 Yedekleme ve anahtar yönetimi
  kanit: docs/operations/UUSPORT_GELISTIRME_VERI_VE_CANLIYA_ALMA_PROSEDURU.md:253-262
  guven: yuksek
- id: OPS-02
  oncelik: P1
  tur: eksik
  iddia: Sürüm paketi, Windows servisi ve kurulum/geri alma betikleri yok; uygulama masaüstü kısayoluyla çalışıyor.
  gerekce: Canlıda oturum kapanınca uygulama durmamalı; tanımlı prosedür (precheck → apply → verify → recovery) araçsız uygulanamaz.
  oneri: dotnet publish (win-x64, self-contained) + web derlemesi → değişmez sürüm klasörü + manifest.json (SHA256, sürüm, migration seviyesi); UseWindowsService + sanal servis hesabı; install/upgrade/rollback betikleri; migration paketi ayrı; upgrade öncesi otomatik yedek + verify.
  somut: deployment/release/build-release.ps1; Program.cs UseWindowsService; deployment/install/*.ps1.
  kabul: Temiz Windows 11 VM'de install → upgrade → rollback provası; servis yeniden başlatmada kalkar; manifest bozuk dosyayı yakalar.
  bagimlilik: [OPS-01]
  efor: M
  adr: yok
  kanit: belge bölüm 5.6 madde 5 ve 5.7
  guven: yuksek
- id: OPS-03
  oncelik: P1
  tur: eksik
  iddia: CI, bağımlılık açığı ve gizli bilgi taraması yok; standart §11 bunları sürüm kapısı sayıyor.
  gerekce: Ajanlar hızlı kod üretiyor; kapı otomatik değilse atlanır.
  oneri: tools/ci.ps1: derleme (uyarı = hata), testler, SchemaGuard, `dotnet list package --vulnerable --include-transitive`, `npm audit --omit=dev`, gitleaks, oxlint, BannedSymbols, check-no-real-data; commit öncesi kanca; uzak depo olursa aynı betik özel depoda CI olarak.
  somut: tools/ci.ps1; .githooks/pre-commit; isteğe bağlı .github/workflows/ci.yml.
  kabul: Bilinçli eklenen sahte anahtar commit'i engeller; bilinen açıklı paket CI'ı kırar.
  bagimlilik: [SAAS-04, SEC-06]
  efor: S
  adr: yok
  kanit: belge bölüm 5.6 madde 5
  guven: yuksek
- id: OPS-04
  oncelik: P2
  tur: eksik
  iddia: Loglama konsolda; dosya logu, kişisel veri maskeleme, alarm ve izleme yok.
  gerekce: Windows servisinde konsol logu kaybolur; yedek/integrity/kilitlenme olayları kimseye ulaşmaz.
  oneri: Serilog → döner dosya (30 gün) + Windows Olay Günlüğü (hata); telefon/e-posta/ad maskeleme; correlation id; zamanlanmış izleme: health + disk doluluğu + son yedek yaşı → SMTP e-posta. Yurt dışı SaaS hata takip servisi kullanılmaz.
  somut: Program.cs Serilog; Infrastructure/PiiMaskingEnricher.cs; deployment/monitor.ps1.
  kabul: Log dosyasında test telefonu maskeli; 26 saattir yedek yoksa e-posta gider.
  bagimlilik: [OPS-02]
  efor: S
  adr: yok
  kanit: belge bölüm 5.6 madde 14
  guven: yuksek
- id: OPS-05
  oncelik: P2
  tur: itiraz
  iddia: 8 GB RAM'li i3 ofis bilgisayarında sanal makine yerine uygulama ve PostgreSQL doğrudan Windows servisi olarak çalışmalı; bellek 16 GB'a çıkarılmalı.
  gerekce: Ana hafıza "donanım uygunsa sanal makine" diyor; 8 GB'ta Windows + VM + PostgreSQL + tarayıcı takasa düşer. APP01/DB01 ayrımı ayrı servis hesabı ve klasör ACL'iyle korunur.
  oneri: VM yok; iki servis, ayrı hesap ve klasör ACL'leri; shared_buffers ≈ RAM'in %15-25'i, küçük work_mem; RAM yükseltmesi canlı öncesi.
  somut: deployment/postgresql/postgresql.conf.production; docs/operations bölüm 4.1 güncellemesi.
  kabul: 8 saatlik yük provasında takas < %5, p95 API < 300 ms.
  bagimlilik: [OPS-02]
  efor: S
  adr: yok
  kanit: docs/project-memory/UUSPORT_ANA_HAFIZA.md:44-47, :53-55
  guven: orta
- id: BIZ-01
  oncelik: P1
  tur: risk
  iddia: Tek sahip + ajan hızında en büyük risk kapsam genişlemesi: nakit akışı, SaaS ve mobil aynı anda açılırsa muhasebe çekirdeği yarım kalır.
  gerekce: 5 günde 17 ekran; doğruluk borcu (ACC-01..10) birikiyor.
  oneri: Aynı anda tek aktif kilometre taşı; M0-M2 bitmeden CF kodu yazılmaz; her taş çıkış kriteri test ve ekran görüntüsüyle kapanır; yeni istek "sonraki taş" listesine yazılır.
  somut: Ana hafızaya "Aktif kilometre taşı" alanı; bölüm 9.
  kabul: Ana hafızada her an tek aktif taş ve kapanış kanıt bağlantıları.
  bagimlilik: []
  efor: S
  adr: yok
  kanit: belge bölüm 4
  guven: orta
- id: BIZ-02
  oncelik: P3
  tur: fikir
  iddia: Satılabilir ürünün konumlandırması "gayri resmi takip" değil "belgelenme + nakit planı + muhasebeci mutabakatı" olmalı.
  gerekce: İlk mesaj hukuki risk taşır ve kurumsal müşteriyi uzaklaştırır; asıl değer MM ile uyum ve nakit görünürlüğü.
  oneri: Segment: 50-1.000 üyeli spor okulları, kurslar, dans stüdyoları. Paketler: Temel (cari, tahsilat, belge kuyruğu, MM aktarımı) / Nakit (plan, senaryo) / Veli (Faz 3). Fiyat kararı M7'de.
  somut: docs/product/KONUMLANDIRMA.md.
  kabul: Kullanıcı onaylı konumlandırma belgesi.
  bagimlilik: [ACC-14]
  efor: S
  adr: yok
  kanit: belge bölüm 2
  guven: dusuk

## 3 VERI_MODELI
```
-- KASA/BANKA (ADR-008) --------------------------------------------------------
finance.cash_account(+linked_account_id uuid null fk finance.cash_account, +link_role text null check in ('settles_to','paid_from','disbursed_to'), +is_liability bool generated (kind in ('credit_card','loan')), +currency char(3) not null default 'TRY')  -- tür+bağlantı; kind'e 'check_portfolio' eklenir (CSH-03, ACC-09)
finance.cash_category(id uuid pk, tenant_id uuid not null, code text collate "C" not null, name text not null, kind text check in ('income','expense','tax','payroll','financing','other'), parent_id uuid null, cf_group text not null, account_code text collate "C" null, is_active bool; unique(tenant_id, code))  -- carisiz fiş ve nakit akışı kategorisi
finance.cash_movement(id uuid pk, tenant_id, account_id uuid not null fk, occurred_on date not null, value_on date not null, amount numeric(18,2) not null check (amount <> 0), source_type text collate "C" not null check in ('payment','payment_reversal','transfer','cash_voucher','opening','loan_drawdown','loan_installment','card_statement_payment','pos_settlement','check_collection','check_endorsement'), source_id uuid not null, leg smallint not null default 1, category_id uuid null, description text, created_at, created_by; unique(tenant_id, source_type, source_id, leg))  -- APPEND-ONLY; hesap bakiyesinin tek kaynağı
finance.cash_movement_split(id, tenant_id, movement_id uuid fk, category_id uuid fk, amount numeric(18,2) not null)  -- APPEND-ONLY; taksitte anapara/faiz/BSMV/KKDF/masraf ayrımı
finance.cash_voucher(id, tenant_id, account_id fk, direction text check in ('in','out'), occurred_on date, amount numeric(18,2) check > 0, category_id fk not null, document_ref text null, description, reverses_voucher_id uuid null unique)  -- carisiz gelir/gider fişi
finance.transfer(id, tenant_id, from_account_id fk, to_account_id fk check <> from, occurred_on date, amount numeric(18,2) check > 0, fee_amount numeric(18,2) default 0, description, reverses_transfer_id uuid null unique)  -- virman
finance.opening_cash(id, tenant_id, account_id fk, as_of_date date, amount numeric(18,2), approved_by, approved_at)  -- kasa/banka açılışı; onayda cash_movement(opening)
finance.period_lock(id, tenant_id, locked_through date not null, locked_by uuid, reason text, created_at)  -- APPEND-ONLY; geçerli kilit = son satır (ACC-07)
-- CARİ VE BELGE ------------------------------------------------------------------
finance.payment(+receipt_no text null, +installment_count smallint not null default 1, +currency char(3) default 'TRY', +version xmin)  -- ACC-03, CSH-04, ACC-05
finance.opening_balance(+as_of_date date not null, +due_date date null, +scope text not null check in ('official','unofficial'))  -- ACC-08
finance.adjustment_voucher(id, tenant_id, type text check in ('debit_note','credit_note','party_transfer'), party_id fk, counter_party_id uuid null, occurred_on date, amount numeric(18,2) check > 0, reason text not null, status text check in ('pending','approved','rejected'), approved_by uuid null, approved_at timestamptz null)  -- cari fişi (ACC-11)
billing.trade_line(+due_on date null, +version xmin)  -- ACC-12, ACC-05
billing.document(+system_gross numeric(18,2), +difference numeric(18,2), +review_status text check in ('ok','needs_review'), +ettn uuid null, +source text check in ('manual','ubl'); unique(tenant_id, ettn) where ettn is not null)  -- ACC-13, INT-04
crm.party(+payment_terms_days int not null default 0)  -- ACC-12
-- ÖDEME ARAÇLARI (ADR-007) -----------------------------------------------------
finance.pos_profile(id, tenant_id, pos_account_id fk, settle_account_id fk, installment_count smallint, commission_rate numeric(9,6), block_days smallint, valid_from date, valid_to date null)  -- POS komisyon/valör kuralı
finance.pos_settlement_expected(id, tenant_id, payment_id fk, seq smallint, expected_on date, gross numeric(18,2), commission numeric(18,2), net numeric(18,2), status text check in ('pending','settled','cancelled'), matched_movement_id uuid null)  -- POS'tan bankaya beklenen geçiş
finance.card_profile(account_id uuid pk fk, tenant_id, statement_day smallint check between 1 and 31, due_offset_days smallint, credit_limit numeric(18,2))  -- şirket kartı döngüsü
finance.card_installment(id, tenant_id, card_account_id fk, source_type text, source_id uuid, seq smallint, statement_period date, amount numeric(18,2))  -- harcamanın ekstrelere dağılımı
finance.card_statement(id, tenant_id, card_account_id fk, period date, closing_on date, due_on date, total numeric(18,2), minimum_due numeric(18,2), paid_amount numeric(18,2) default 0, status text check in ('open','closed','paid','overdue'))  -- ekstre
finance.loan(+disbursed_on date, +disbursement_account_id uuid fk, party_id -> nullable)  -- ACC-10
finance.loan_installment(+bsmv_amount numeric(18,2) default 0, +kkdf_amount numeric(18,2) default 0, +paid_movement_id uuid null)  -- ACC-10
finance.check_note(+receipt_payment_id uuid null, +endorse_payment_id uuid null, +endorsed_party_id uuid null, +clearing_days smallint default 1)  -- ACC-09
-- STOK ---------------------------------------------------------------------------
inventory.stock_movement(+unit_cost numeric(18,6) null, +total_cost numeric(18,2) null, +location_id uuid null)  -- STK-01, STK-03
inventory.item(+parent_item_id uuid null)  -- varyant kapısı (STK-03)
inventory.item_cost_state(item_id uuid pk, tenant_id, qty_on_hand numeric(18,4), avg_cost numeric(18,6), version xmin)  -- hareketli ortalama durumu
inventory.stock_adjustment(id, tenant_id, item_id fk, occurred_on date, quantity numeric(18,4) check <> 0, reason text check in ('count','waste','gift','kit_included','internal_use'), approved_by uuid, note text)  -- satışsız stok hareketi (STK-02)
-- PLAN VE SENARYO (ADR-010, ADR-015) --------------------------------------------
plan.recurring_rule(id, tenant_id, name, direction text check in ('in','out'), category_id fk, party_id null, account_id null, amount_kind text check in ('fixed','parameter','formula'), amount numeric(18,2) null, formula text null, frequency text check in ('weekly','monthly','quarterly','yearly'), day_of_period smallint, start_on date, end_on date null, business_day_rule text check in ('none','next','previous'), escalation_rate numeric(9,6) null, escalation_on date null, doc_scope text check in ('official','pending','none'), is_active bool)  -- tekrarlayan kural
plan.cash_item(id, tenant_id, direction, category_id fk, party_id null, account_id null, due_on date not null, amount numeric(18,2) check > 0, certainty text check in ('planned','forecast'), doc_scope text check in ('official','pending','none'), status text check in ('open','partially_settled','settled','cancelled'), source_type text check in ('rule','manual','loan_installment','card_statement','check','pos_settlement','tax_estimate','payroll','billing_plan','mm_estimate'), source_id uuid null, period_key text null, note text; unique(tenant_id, source_type, source_id, period_key))  -- planlı/tahmini tek kalem
plan.settlement_match(id, tenant_id, cash_item_id fk, movement_source_type text, movement_source_id uuid, amount numeric(18,2), matched_by, matched_at)  -- plan ↔ gerçekleşen
plan.scenario(id, tenant_id, name, kind text check in ('base','optimistic','conservative','custom'), created_by, is_archived bool)
plan.scenario_adjustment(id, tenant_id, scenario_id fk, seq int, selector jsonb not null, operation text check in ('scale','shift_days','add','remove'), value numeric(18,6) null, add_item jsonb null, valid_from date null, valid_to date null, note text)  -- senaryo düzeltme katmanı
plan.forecast_snapshot(id, tenant_id, snapshot_on date, scenario_id, granularity text, period_start date, category_id, certainty, doc_scope, amount numeric(18,2))  -- tahmin doğruluğu (CF-06)
academy.billing_plan(id, tenant_id, athlete_id fk, party_id fk, item_id fk, amount numeric(18,2), frequency text, day_of_month smallint, start_on date, end_on date null, status text check in ('active','paused','ended'))  -- cariye yazmayan tahsilat planı (CF-02)
payroll.monthly_summary(id, tenant_id, period date, net_pay, income_tax_withholding, stamp_tax, sgk_total, incentive_deduction, net_pay_due_on date, tax_due_on date, sgk_due_on date, source text check in ('mm','estimate'), approved_by; unique(tenant_id, period))  -- bordro özeti; kişi verisi yok (TAX-04)
-- PARAMETRE VE AYAR ------------------------------------------------------------
core.tax_parameter(id, key text collate "C", value_num numeric(18,6) null, value_text text null, valid_from date not null, valid_to date null, source text not null, verified_by text null, verified_at timestamptz null; exclusion no-overlap per key)  -- KÜRESEL, RLS yok, app yalnız SELECT (TAX-01)
core.tenant_parameter(tenant_id, key, value_num, value_text, valid_from, valid_to)  -- kiracı istisnası
core.holiday(on_date date pk, name text, kind text check in ('national','religious','half_day'))  -- iş günü kaydırma
core.tenant_setting(tenant_id, key text collate "C", value jsonb, updated_by, updated_at; pk(tenant_id, key))
core.tenant_sequence(tenant_id, series text, next_value bigint; pk(tenant_id, series))  -- FOR UPDATE ile numara
-- ENTEGRASYON (ADR-006) ---------------------------------------------------------
integration.chart_of_accounts(id, tenant_id, account_code text collate "C", name, type text, is_active bool, source text check in ('mm_trial_balance','manual'); unique(tenant_id, account_code))
integration.account_rule(id, tenant_id, priority int, event_type text, match jsonb, account_code_template text, side text check in ('debit','credit'), valid_from date, valid_to date null)  -- kural bazlı bağlantı kodu
integration.export_ownership(tenant_id, event_type text, owner text check in ('uusport','mm_ubl','mm_bank','none'), valid_from date; pk(tenant_id, event_type, valid_from))  -- aktarım sahipliği
integration.journal_batch(id, tenant_id, period_from date, period_to date, target_format text, status text check in ('draft','exported','acknowledged','superseded'), file_sha256 text, exported_by, exported_at)
integration.journal_line(id, tenant_id, batch_id fk, voucher_no int, line_no int, entry_date date, document_type text, document_no text, ettn uuid null, description text, account_code text collate "C", party_code text null, party_tax_no text null, debit numeric(18,2), credit numeric(18,2), vat_rate numeric(9,4) null, vat_amount numeric(18,2) null, cost_center text null, source_type text, source_id uuid, currency char(3), fx_rate numeric(18,6) default 1)  -- APPEND-ONLY; kanonik fiş
integration.export_link(tenant_id, source_type text, source_id uuid, batch_id fk; pk(tenant_id, source_type, source_id, batch_id))  -- aktarılmış kaydı dondurur (ACC-16)
integration.import_profile(id, tenant_id, kind, name, sheet_name, mapping_json jsonb)  -- DATA-02
finance.bank_statement_line(id, tenant_id, account_id fk, import_batch_id fk, booked_on date, value_on date, amount numeric(18,2), description text, reference text, balance_after numeric(18,2) null, status text check in ('unmatched','suggested','reconciled','ignored'), matched_source_type text null, matched_source_id uuid null)  -- APPEND-ONLY dışında status
finance.bank_rule(id, tenant_id, pattern text, match_kind text, party_id null, category_id null, created_by)  -- öğrenilen eşleme kuralı
-- KVKK VE GÜVENLİK ---------------------------------------------------------------
crm.consent(id, tenant_id, party_id fk, athlete_id null, purpose text, text_version text, channel text, given_at timestamptz, revoked_at timestamptz null, recorded_by)  -- rıza/aydınlatma kaydı
core.retention_policy(tenant_id, data_class text, retain_days int, action text check in ('anonymize','delete'))
core.data_subject_request(id, tenant_id, party_id, type text, received_at, due_at, status, closed_at)
identity.device(id, tenant_id, user_id fk, name, refresh_token_hash text, created_at, last_seen_at, revoked_at null)  -- Faz 2 mobil
audit.audit_event(+client_ip inet null)
core.tenant_catalog / core.tenant_feature / core.support_access_grant  -- SONRA (SAAS-06, SAAS-07)
-- Tüm yeni kiracı tabloları: tenant_id + RLS ENABLE/FORCE + tenant_isolation; APPEND-ONLY işaretliler için app rolünden UPDATE/DELETE/TRUNCATE alınır; her migration kendi dondurulmuş listesiyle (MigrationSecurity kuralı).
```

## 4 NAKIT_AKISI_MOTORU
### 4.1 Kaynaklar → ara model (cf_item)
```
cf_item { id, date, direction(in|out), amount>0, category, party?, account?,
          certainty(realized|planned|forecast), doc_scope(official|pending|none),
          lineage{ source_type, source_id, rule_id?, formula?, param_versions[], adjustments[] } }
```
- realized: finance.cash_movement (yalnız likit hesaplar: cash, bank_current) as_of'a kadar; valör varsa value_on.
- planned (kesin vadeli): açık loan_installment (vade); açık card_statement (due_on) + gelecek card_installment dönemleri; portföydeki alınan çek (due + clearing_days, in); ödenmemiş verilen çek (due, out); pending pos_settlement_expected (net, expected_on) + komisyon zaten net içinde; plan.cash_item certainty=planned (sabit tutarlı kural, elle kalem, MM tahmini); beyan edilmiş vergi (tutar kesin).
- forecast: plan.cash_item certainty=forecast (formüllü kural); billing_plan beklenen tahsilat × olasılık; plansız açık alacak tahmini; KDV/geçici/kurumlar tahmini; bordro tahmini.
- doc_scope: tahsilat/alacak kalemleri ACC-02 resmi tutarıyla bölünür; carisiz kalemler none.
- Çift sayım korumaları: settlement_match ile kapanan planlı kalemin kalanı düşer; billing_plan ayının beklenen tutarından o ay o cariye yapılan tahsilat düşer; plansız alacak tahmini billing_plan kapsamındaki carileri dışlar; alınan çek cariyi girişte kapattığı için (ACC-09) alacak tahmininde tekrar yer almaz; kart harcaması nakit değildir, yalnız ekstre ödemesi çıkar; POS brüt tahsilat nakit değildir, beklenen net geçiş çıkar.

### 4.2 Hesaplama adımları
1. as_of (varsayılan bugün), ufuk sonu, granularity ve scenario_id alınır.
2. opening = Σ cash_movement(likit hesaplar, occurred_on ≤ as_of). POS bekleyen ve portföydeki çek "yakın nakit" olarak ayrı gösterilir, açılışa katılmaz.
3. Kaynaklardan cf_item listesi toplanır (4.1), parametreler tarihe göre çözülür, sürüm id'leri lineage'a yazılır.
4. Senaryo düzeltmeleri seq sırasıyla uygulanır: remove → shift_days → scale → add; her etkilenen kaleme adjustment id eklenir.
5. İş günü kaydırması: vergi/SGK/kural kalemleri core.holiday ve hafta sonuna göre kaydırılır (kural: next; parametre).
6. Günlük ızgara: net_d = Σin_d − Σout_d; balance_d = opening + Σ_{≤d} net. min_point = argmin balance_d; shortfall = balance_d < cf.min_cash_buffer olan günler (ilk gün ve süre).
7. Kovalar: ISO hafta (Pzt-Paz), ay, çeyrek, yıl. Her kova: in/out × certainty × doc_scope matrisi, net, açılış, kapanış. Etiketli genel toplam ayrı alan.
8. Çıktı: {opening, nearCash{pos, checks}, buckets[], minPoint, shortfalls[], warnings[] (teyitsiz parametre, eksik açılış, eşlenmemiş banka satırı)}; her hücrede item id listesi (explain için).

### 4.3 Formüller (tüm oranlar ve günler core.tax_parameter'dan)
- KDV_m: H_m = Σ vat_amount (düzenlenmiş satış belgeleri, belge tarihi m) − Σ satış iade belgesi KDV'si; İ_m = Σ vat_amount (alış belgeleri, m); Ödenecek_m = max(0, H_m − İ_m − D_{m−1}); D_m = max(0, İ_m + D_{m−1} − H_m). Vade: m+1 ayının `vat.return.due_day` günü. Ayrı satır: BekleyenKDV_m = Σ (satır vat_amount × bekleyen miktar/miktar), toplamlara girmez.
- Geçici vergi (v2): Matrah_YTD = Σ resmi satış neti − Σ resmi alış/gider neti + KKEG_param − Amortisman_elle; Vergi_q = max(0, Matrah_YTD × `corporate_tax.rate`) − Σ önceki çeyrek ödemeleri (YTD). Vade `provisional_tax.due_rule`; 4. dönem `provisional_tax.q4_enabled`.
- Kurumlar (v2): max(0, Matrah_yıl × oran − Σ geçici vergiler); asgari kurumlar vergisi karşılaştırması `corporate_tax.domestic_minimum_rate` (formülü MM'den; teyitsiz ise yalnız uyarı).
- Muhtasar/SGK: özet varsa planned (özetin tutarları, vade günleri parametre); yoksa forecast = son özet × (1 + `payroll.growth_rate`) ve asgari ücret değişim ayında `payroll.min_wage_step` çarpanı. Kira stopajı = brüt kira × `withholding.rent_individual` (kural ev sahibi gerçek kişiyse).
- Kredi: taksit toplamı = anapara + faiz + BSMV + KKDF + masraf (planned, vade); erken kapama senaryosu = kalan anapara + parametreli ceza (add/remove).
- Kart: ekstre due_on'da total (planned); açık dönem harcamaları (forecast) bir sonraki due_on'a.
- Öğrenci tahsilatı: billing_plan tutarı planned; beklenen = tutar × p_cari, tarih = plan günü + medyan gecikme_cari. p_cari = son 6 ayda vadesinde + 30 gün içinde tahsil edilen / planlanan; veri < 3 ay ise `cf.collection.default_rate`, `cf.collection.default_delay_days`.
- Plansız açık alacak: Σ açık bakiye × p(yaş kovası) (0-30: 0,8; 31-60: 0,6; 61-90: 0,4; 90+: 0,1 — kiracı parametresi, ilk değerler varsayım) `cf.receivable.spread_weeks` haftaya eşit yayılır.

### 4.4 Parametre tablosu (örnek değerler; HEPSİ doğrulanmalı, MM teyidi esastır)
| key | örnek | not |
|---|---|---|
| vat.rate.general | 20 | %; 10.07.2023'ten beri (doğrulanmalı) |
| vat.rate.reduced_1 / reduced_2 | 10 / 1 | % (doğrulanmalı) |
| vat.return.due_day | 28 | izleyen ay (doğrulanmalı) |
| muhsgk.return.due_day | 26 | izleyen ay, beyan ve vergi ödemesi (doğrulanmalı) |
| sgk.premium.due_rule | izleyen ayın son günü | (doğrulanmalı) |
| provisional_tax.due_rule | çeyreği izleyen 2. ayın 17'si | (doğrulanmalı) |
| provisional_tax.q4_enabled | false | 4. dönem beyanı yok (doğrulanmalı) |
| corporate_tax.rate | 25 | % (doğrulanmalı) |
| corporate_tax.due_rule | izleyen yıl Nisan son günü | (doğrulanmalı) |
| corporate_tax.domestic_minimum_rate | 10 | %; 2025 hesap döneminden (doğrulanmalı) |
| withholding.rent_individual | 20 | %; gerçek kişiden işyeri kirası (doğrulanmalı) |
| stamp_tax.wage | 0,00759 | binde 7,59 (doğrulanmalı) |
| invoice.max_days_after_delivery | 7 | VUK 231/5 (doğrulanmalı) |
| ba_bs.threshold | 5000 | TL, KDV hariç (doğrulanmalı) |
| loan.bsmv_rate_commercial | 5 | % faiz üzerinden (doğrulanmalı) |
| loan.kkdf_rate_commercial_try | 0 | % (doğrulanmalı) |
| payroll.min_wage_step | dönemsel | değer MM'den girilir |
| cf.min_cash_buffer | kiracı | kullanıcı belirler |
| cf.collection.default_rate / default_delay_days | 0,90 / 7 | varsayım; ilk 3 ay sonra veriden |
| cf.check.clearing_days | 1 | varsayım |
| cf.receivable.spread_weeks | 4 | varsayım |

### 4.5 Senaryo
- base = düzeltmesiz + tarihsel oranlar. optimistic = tahsilat oranı 1,0, gecikme 0. conservative = tahsilat × 0,85, +15 gün, tüm "out" forecast × 1,05. Hazır ayarlar plan.scenario_adjustment satırı olarak tohumlanır, kullanıcı kopyalayıp değiştirebilir.
- Kullanıcı tanımlı örnekler: "500 bin kredi" → add(in, 500.000, tarih) + add(out, taksit serisi; faiz oranı ve vade kullanıcıdan); "20 öğrenci ayrılır" → scale(billing_plan, 1 − 20/aktif_plan_sayısı, tarihten); "kira %40" → scale(rule=kira, 1,40, tarihten).
- Karşılaştırma: aynı ızgarada base vs senaryo kapanış serisi, kova farkları, min nakit farkı ve ilk eşik altı gün farkı.
- Deterministik; Monte Carlo yok. İleride (P3) yalnız tahsilat oranı/gecikme için bant (P10-P90) eklenebilir.

### 4.6 Açıklanabilirlik
- GET /api/cashflow/items/{id}/explain → {kaynak kayıt bağlantısı, formül metni ("3.800 × 0,92 (son 6 ay tahsilat oranı)"), parametre anahtar/değer/sürüm/teyit durumu, uygulanan senaryo düzeltmeleri, eşleşmiş gerçekleşen hareketler}.
- Her kova hücresi item listesine iner; her item kaynak ekrana (cari, kredi, kural) bağlanır.
- Teyitsiz parametre kullanan hücrede "teyit bekliyor" rozeti; eksik veri (açılışsız hesap, plansız sporcu sayısı) üst uyarıda.

### 4.7 İlk sürümde basit tut (CF v1)
- Yalnız aylık ve haftalık kova; kategori seti sabit (cf_group: tahsilat, tedarikçi, maaş, SGK, vergi, kira, kredi, kart, diğer).
- Vergi: KDV otomatik; muhtasar/SGK bordro özetinden; geçici/kurumlar yalnız MM tahmini kalemi.
- Senaryo: yalnız üç hazır ayar + en fazla 5 düzeltmeli kullanıcı senaryosu.
- POS ve kart döngüsü v1'de varsa kullanılır; yoksa POS tahsilatı T+1 net=brüt varsayımı uyarıyla.
- Anlık görüntü ve doğruluk raporu v2.

## 5 SAAS_KAPILARI
ŞİMDİ (ucuz, sonra pahalı):
- Kiracı claim'den; kiracı ara katmanı kimlik doğrulamadan sonra (SAAS-01).
- identity uçlarında kiracı filtresi + çapraz kiracı negatif testleri (SAAS-02).
- Şema koruma testi: RLS/FORCE/politika/append-only yetkileri katalogdan (SAAS-04).
- İşlem-yerel app.tenant_id + havuz sızıntı testi (SAAS-03, ACC-06 ile aynı iş).
- Küresel vs kiracı parametre ayrımı: core.tax_parameter küresel, core.tenant_parameter istisna (TAX-01).
- Kiracı ayarı, numara serisi tablosu, terminoloji sözlüğü, kiracı adıyla çıktı başlığı, currency char(3) default 'TRY', kiracı saat dilimi (SAAS-05).
- Dosya depolama yolu baştan {tenant_id}/{yyyy}/{mm}/{uuid}; yüklenen dosya adı yol olarak kullanılmaz (UX-10, standart §7.1).
- Arka plan işleri kiracıyı açık parametreyle taşır; kiracısız iş RLS nedeniyle boş görür ve hata loglar (CF-01 motoru, OPS-01 verify).
- Audit ve loglarda tenant_id (mevcut) + client_ip (SEC-05).
- Aktarım altyapısında sayfa/eşleme profili (DATA-02) ve Excel aktarım türlerinin kiracı bağımsız şablonları.
- Stok/depo/varyant nullable kolonları (STK-03).
SONRA (pahalı; tasarım kararı şimdi, kod sonra):
- Kiracı kataloğu, provisioning, özellik bayrakları, plan/lisans; OnPrem/Saas modları (SAAS-06).
- Kiracı mantıksal dışa/içe aktarımı, kiracı bazlı geri yükleme, KVKK silme, destek erişim yetkisi (SAAS-07, SEC-12).
- İzolasyon: varsayılan paylaşımlı şema + RLS korunur; ayrılmış veritabanı seçeneği aynı kodla (SAAS-06). Şema-başına-kiracı önerilmez (migration ve bağlantı yönetimi maliyeti).
- Sektör şablonları: hesap planı eşleme kuralları (INT-01), kategori seti (CSH-01), terminoloji (SAAS-05), aktarım şablonları; akademi / kurs / dans okulu paketleri.
- Faturalama/abonelik dış sistemde; UU Sport yalnız kiracı durumunu okur.

## 6 GUVENLIK
### 6.1 Tehdit modeli (STRIDE, kısa)
- S (kimlik sahteciliği): parola tahmini, çalınan oturum çerezi, kurulum ucunun ele geçirilmesi → MFA (var), HTTPS+Secure çerez (SEC-02), kurulum jetonu (SEC-04), giriş denetimi (SEC-05).
- T (kurcalama): muhasebe kaydını değiştirme/silme → append-only + RLS (var), dönem kilidi (ACC-07), eşzamanlılık (ACC-05), integrity (ACC-04).
- R (inkâr): "ben yapmadım" → audit (var), giriş/dışa aktarma olayları (SEC-05, SEC-09), IP.
- I (bilgi ifşası): yedek, dışa aktarım, log, kiracılar arası, ajan erişimi → SEC-01, SEC-09, OPS-04, SAAS-01..04, SEC-03.
- D (hizmet engelleme): hız sınırı vekil arkasında tek IP, kilitlenme ile hesap kapatma → SEC-10; yedekten dönüş RTO (OPS-01).
- E (yetki yükseltme): politika unutulmuş uç, kiracılar arası kullanıcı yönetimi → SEC-06, SAAS-02; tek yönetici bağımlılığı → SEC-08; görev ayrılığı → SEC-11.

### 6.2 En olası 10 sızıntı yolu → kontrol → test
1. Yedek dosyası (harici disk/uzak depo) → istemci tarafı şifreleme, parola ayrı, yurt içi depo (SEC-01) → parolasız açma denemesi başarısız.
2. Excel/PDF dışa aktarımları → sunucu PDF'i + audit + veri minimizasyonu (SEC-09) → her indirmede audit satırı; PDF'te telefon yok.
3. Tarayıcı önbelleği/yazdırma → API Cache-Control: no-store (var), hareketsizlik zaman aşımı (SEC-10), paylaşılan bilgisayarda "tarayıcıyı hatırla" kapatılabilir (kullanıcı seçimi) → yanıt başlığı testi; 61 dk sonra 401.
4. Loglar → PII maskeleme, 30 gün saklama, log klasörü ACL (OPS-04) → log dosyasında test telefonu maskeli.
5. RDP'nin internete açılması (Faz 2) → 3389 kapalı, VPN/RD Gateway + MFA (SEC-13) → dış port taraması.
6. Mobil cihaz kaybı → cihaz bağlı belirteç + uzaktan iptal, çevrimdışı PII yok (SEC-13) → iptal sonrası 401.
7. Yapay zekâ geliştirme akışı → gerçek veri depo dışında ve ACL'li, ajan ayrı hesapta, canlı DB geliştirmede yok (SEC-03) → ajan hesabıyla erişim reddi kanıtı.
8. İçeriden tehdit (personel) → rol matrisi, görev ayrılığı (SEC-11), dışa aktarma denetimi, personel çıkışında tek işlemle erişim kapama (var: IsActive + güvenlik damgası) → pasif kullanıcı ≤ 1 dk'da 401 (var) + dışa aktarma raporu.
9. Kiracılar arası sızıntı (SaaS) → claim tabanlı kiracı, işlem-yerel ayar, şema koruma, çapraz kiracı testleri (SAAS-01..04) → PoolLeakTests, CrossTenantUserTests.
10. Tedarik zinciri (npm/NuGet) → kilit dosyaları, açık taraması, gizli bilgi taraması (OPS-03) → CI kırmızı.

### 6.3 Faz bazlı kontrol listesi (kontrol → nasıl test edilir)
Faz 1 (ofis LAN, canlı öncesi zorunlu):
- HTTPS iç CA, Secure/__Host- çerez, HSTS → ikinci bilgisayardan uyarısız açılış, http reddi (SEC-02).
- Güvenlik duvarı: 443 yalnız ofis alt ağı, 5432 yalnız loopback → port taraması (SEC-02).
- BitLocker → manage-bde -status (SEC-02).
- Şifreli yedek + haftalık geri yükleme testi → restore-test.ps1 çıkış kodu (OPS-01, SEC-01).
- Kurulum jetonu → SetupTests (SEC-04).
- Gerçek veri depo dışında → ACL kanıtı (SEC-03).
- Giriş/başarısız giriş denetimi → AuthAuditTests (SEC-05).
- Uç yetki testleri → EndpointAuthorizationTests (SEC-06).
- En az iki Yönetici + CLI MFA sıfırlama → prosedür provası (SEC-08).
Faz 2 (internete açık sunucu + mobil):
- 3389 kapalı, VPN/RD Gateway + MFA → dış tarama (SEC-13).
- Genel geçer sertifika + otomatik yenileme → sertifika bitişine 20 gün kala uyarı testi.
- ForwardedHeaders yalnız bilinen vekil, hız sınırı kullanıcı+IP → vekil arkasında 21. denemede 429 (SEC-10).
- Hareketsizlik zaman aşımı → 61 dk testi (SEC-10).
- Mobil cihaz kaydı/iptali → iptal sonrası 401 (SEC-13).
- TOTP anahtarı şifreli → DB'de ciphertext (SEC-07).
- Sunucu PDF + dışa aktarma denetimi → audit satırı (SEC-09).
- Merkezi log + alarm → yedek gecikmesi e-postası (OPS-04).
- Barındırma Türkiye'de → sözleşme ve veri merkezi konumu belgesi.
SaaS:
- Kiracı claim + işlem-yerel ayar + şema koruma + çapraz kiracı testleri (SAAS-01..04).
- Kiracı dışa aktarım/silme, destek erişimi süreli ve onaylı (SAAS-07).
- KVKK: rıza, saklama-imha işi, başvuru kaydı (SEC-12).
- Bağımsız sızma testi (yıllık) ve olay müdahale tatbikatı.

### 6.4 Mevcut kontrollerde zayıf noktalar
- MFA kurtarma yok (SEC-08); TOTP anahtarı düz metin (SEC-07).
- Oturum 8 saat kayan, hareketsizlik yok (SEC-10).
- Çerez SameAsRequest; HTTPS yok (SEC-02).
- CSP iyi (script-src 'self'); style-src 'unsafe-inline' kabul edilebilir; img data: gerekli değilse kaldırılabilir.
- Hız sınırı yalnız auth uçlarında ve IP bazlı (SEC-10).
- Denetim kaydı giriş/dışa aktarma ve IP içermiyor (SEC-05); once/sonra özeti kısmi (ör. item.update yalnız eski fiyat).
- Şifreleme: disk yok (BitLocker), yedek yok, alan düzeyi yok; Data Protection anahtarları kullanıcı DPAPI'sine bağlı (SEC-01).
- /api/health anonim sürüm ifşası (SEC-10); /api/setup jetonsuz (SEC-04).
- Kiracı çözümleme ve identity uçları tek kiracı varsayımında (SAAS-01, SAAS-02).

### 6.5 KVKK
- Çocuk verisi: velinin aydınlatılması ve açık rıza gereken işlemler (fotoğraf, sağlık, pazarlama) ayrı amaçlarla crm.consent'te; sağlık modülü kapalı kalır (standart §8.1).
- Saklama/imha: veri sınıfı başına süre AV/MM ile; finans kayıtları yasal süre boyunca saklanır, kişi alanları süre sonunda anonimleşir (SEC-12).
- Veri sahibi başvurusu: kayıt, yanıt süresi takibi (30 gün, doğrulanmalı), kişisel veri dışa aktarımı.
- İhlal bildirimi: tespit → sınırlama → Kurul'a bildirim (72 saat, doğrulanmalı) → ilgili kişilere bildirim; olay kaydı şablonu docs/operations'a.
- Yurt dışı aktarım: barındırma, yedek, log ve hata takibi Türkiye'de; yurt dışı servis kullanılacaksa hukuki dayanak AV ile (doğrulanmalı).
- VERBIS kaydı yükümlülüğü AV/MM ile teyit.

## 7 TASARIM
### 7.1 Mercek sırasıyla notlar
- Erişilebilirlik: odak halkası tanımlı (styles.css:108), tablo rakamları tabular-nums, rozetler renk + nokta + metin; --ink-3 (#5b6878) beyaz zeminde ≈ 5,7:1 (geçer). Eksikler: tablolarda caption/scope, grafiklerin tablo eşdeğeri (UX-09), "Resmi Cari" onay kutusunun tıklama alanı etiketle ≥ 28 px olmalı, tarih alanlarında klavye girişi (UX-03), yıkıcı işlem onayı (UX-04).
- Platform geleneği: Windows/web'de Segoe UI Variable doğru; Logo kullanıcısı için klavye kısayolları görünür ipucu olarak; <dialog> kullanımı doğru, yazdırma portal deseni ortak bileşene alınmalı (UX-02).
- Görsel hiyerarşi ve zanaat: sayfa başına tek birincil işlem (UX-06); kartlarda tek imza kart (koyu lacivert "Toplam cari bakiye" iyi bir imza); eksi ve yükümlülük bakiyeleri açıklamalı (UX-05); boş durumlar eylem önerir.
- Etkileşim: satır işlemleri duruma göre (UX-04); kuyruklar yaş sıralı ve satır içi işlemli (UX-07); uzun işlemlerde arayüz içi geri bildirim (bildirim balonu var); sürüm yenile bandı (UX-08).
- İçerik dili: geliştirici jargonu kalkar (UX-01); "Gayri resmi" yerine "Belge bekliyor/Belgelendi/Süresi geçti" (ACC-14); etiketler sonucu söyler ("Tahsilatı Kaydet" iyi örnek).

### 7.2 Ekran bazlı öneriler
- Genel Bakış: kartlar → "Kasa+banka", "Bu ay tahsilat", "Açık alacak", "Belge bekliyor (süresi geçen)"; altında "Önümüzdeki 14 gün ödemeler" listesi (CF v1 sonrası); belgelenme oranı şeridi (imza öğe, 7.6).
- Cari detay: UX-06; ekstre penceresi tarih aralığı + kapsam seçici (Toplam/Belgelenmiş) + sunucu PDF (ACC-01, SEC-09).
- Hesap Hareketleri: sayfalama, toplam satırı (sunucu), yazdırma başlığı (UX-02, DATA-03).
- Muhasebe Bekleyen: yaş kolonlu kuyruk, toplu fatura (UX-07); "Tutar farkı" sekmesi (ACC-13).
- Kasa ve Banka: Varlık/Yükümlülük, açılış düğmesi, hesap defteri görünümü (UX-05, CSH-01).
- Krediler: kullandırım, BSMV/KKDF kolonları, gelecek taksit "Öde" onayı vade uzaksa uyarı (ACC-10).
- Çek Portföyü: durum bazlı birincil işlem, ciro için cari seçimi, vade haftası özeti (UX-04, ACC-09).
- Raporlar: ortak filtre çubuğu (dönem, kapsam, senaryo) tasarım taslağı §1'e uygun; Excel dışa aktarımı sunucuda ve denetimli.
- Excel Aktarımı: sayfa seçici ve profil (DATA-02); 5 adımlı akış iyi, korunmalı.

### 7.3 Nakit akışı bilgi mimarisi ve görselleştirme
- Nakit Akışı panosu: üstte ufuk segment kontrolü (Hafta | Ay | 3 Ay | Yıl) + senaryo seçici; ana grafik: kapanış bakiyesi çizgisi (günlük), eşik bandı (cf.min_cash_buffer), en düşük nokta işaretçisi ve etiketi; altında kova başına giriş/çıkış çubukları, kesinlik düz (gerçekleşen) / taralı (planlı) / noktalı (tahmini) + metin etiketi; sağda "neden" paneli (seçili kovanın kalemleri). Belge kapsamı ayrı bir anahtarla (Tümü | Belgelenmiş | Belge bekliyor) süzülür, grafikte karıştırılmaz.
- Ödeme Takvimi: tarih gruplu liste (Geciken, Bu hafta, Gelecek hafta, Bu ay), satırda kategori, karşı taraf, tutar, kesinlik rozeti, "Öde/Eşleştir"; takvim görünümü ikincil.
- Senaryo Karşılaştırma: iki kapanış çizgisi + fark tablosu (kova, base, senaryo, fark), en düşük nokta ve ilk eşik altı gün karşılaştırması; düzeltme listesi yan panelde düzenlenebilir.
- Grafik türleri: çizgi (bakiye), gruplu/istif çubuk (giriş/çıkış), şelale (dönem açılış → kapanış köprüsü, kategori bazlı). Pasta grafik yok.

### 7.4 Faz 2 mobil (personel) bilgi mimarisi
- Sekme çubuğu (rol bazlı, en fazla 5): Bugün · Tahsilat · Fiş · Sporcular · Diğer.
- Bugün: günün tahsilatları, bekleyen işler (Muhasebe rolünde), yaklaşan ödemeler (Yönetici).
- Hızlı işlemler: "Tahsilat al" (cari ara → tutar → araç → kaydet), "Fiş ekle" (kamera → karekod → onay).
- Fiş akışı: kamera → GİB karekodu çözümlenir → VKN/tutar/tarih dolu → kategori seçimi → sunucuya yükleme; cihazda kalıcı kopya yok; karekodsuz fiş sunucu OCR kuyruğu.
- Hoca rolünde borç/tahsilat verisi yok; sporcu listesi grup üyeliğinden (standart §3).

### 7.5 Bileşen/desen listesi
PrintLayout; StatementView (sunucu verisi); CommandPalette; DateInput (akıllı tarih); MoneyInput (biçimli, sunucu önizlemeli); EntryGrid (çok satırlı giriş); RowMenu; ConfirmDialog (sonuç cümlesi + gerekçe); StatusBadge (Belgelendi/Belge bekliyor/Süresi geçti/İptal); AgeBadge; QueueTable (yaş sıralı, çoklu seçim); KpiCard (imza varyantı); LiabilityAmount ("Borç" etiketi); EmptyState (eylemli); VersionBanner; ExplainPanel (lineage); HorizonSwitch; ScenarioPicker; CashflowChart + tablo eşdeğeri.

### 7.6 İmza öğe
- "Belgelenme şeridi": her cari ve Genel Bakış'ta ince yatay şerit; dolu kısım belgelenmiş tutar, amber kısım belge bekleyen, kırmızı uç süresi geçen; yanında "%92 belgelendi". Ürünün farkını (Toplam → Resmi yakınsaması) tek bakışta anlatır, çevresi sakin kalır. Koyu lacivert kenar çubuğu ve amber "bekliyor" rengi marka paletinin çekirdeği.

## 8 ENTEGRASYON
### 8.1 Olay → fiş eşleme ve aktarım sahipliği (hesap kodları THP örnektir; MM teyidi esastır)
| olay | kapsam | sahip (öneri) | borç | alacak |
|---|---|---|---|---|
| Satış faturası (e-Arşiv) | resmi | mm_ubl | 120 Alıcılar | 600/601 Satışlar + 391 Hesaplanan KDV |
| Satış iade faturası | resmi | mm_ubl | 610 Satıştan iadeler + 391 | 120 |
| Alış faturası/gider | resmi | mm_ubl veya mm_manual | 153/7xx + 191 İndirilecek KDV | 320 Satıcılar |
| Nakit tahsilat (makbuzlu/belgeli satıra) | resmi | uusport | 100 Kasa | 120 |
| Havale/EFT tahsilat | kanal=resmi | mm_bank + uusport cari eşleme dosyası | 102 Bankalar | 120 (veya 340 Alınan avanslar) |
| POS tahsilat | kanal=resmi | uusport | 108 Diğer hazır değerler (POS) | 120 |
| POS valör geçişi | resmi | uusport | 102 + 653/780 komisyon (MM) | 108 |
| Alınan çek girişi | resmi | uusport | 101 Alınan çekler | 120 |
| Çek tahsili | resmi | uusport | 102 | 101 |
| Çek cirosu | resmi | uusport | 320 | 101 |
| Karşılıksız çek | resmi | uusport | 120 veya 128 (MM) | 101 |
| Verilen çek | resmi | uusport | 320 | 103 Verilen çekler |
| Verilen çek vadesi | resmi | mm_bank | 103 | 102 |
| Kredi kullandırımı | resmi | mm_bank | 102 | 300 Banka kredileri (veya 400) |
| Kredi taksiti | resmi | uusport (ayrım bilgisi) | 300 anapara + 780 faiz + 770/780 BSMV-masraf (MM) | 102 |
| Şirket kartı harcaması | belgeye bağlı | uusport | 7xx/153 + 191 | 309 Diğer mali borçlar |
| Kart ekstresi ödemesi | resmi | mm_bank | 309 | 102 |
| Kasa→banka virmanı | resmi | uusport | 102 | 100 |
| Banka masrafı/faizi | resmi | mm_bank | 780/770 | 102 |
| Maaş ödemesi | resmi | mm_bank | 335 Personele borçlar | 102 |
| Muhtasar/SGK/KDV ödemesi | resmi | mm_bank | 360/361 | 102 |
| Carisiz kasa gideri (belgeli) | resmi | uusport | 7xx (+191) | 100 |
| Belgesiz satır / belgesiz nakit | pending | none (aktarılmaz; kuyrukta kalır) | — | — |
- Kural: sahibi uusport olmayan olay fişe girmez; mm_bank olaylarında UU Sport yalnız "cari/kategori eşleme dosyası" üretir (banka satırı → cari kodu/kategori), MM kendi ekstre sihirbazında kullanır.

### 8.2 Kanonik format alanları (integration.journal_line)
batch_id, voucher_no, line_no, entry_date, document_type (e_arsiv|e_fatura|kagit|fis|makbuz|dekont|bordro|virman), document_no, ettn, description, account_code, party_code, party_tax_no, debit, credit, vat_rate, vat_amount, cost_center, currency, fx_rate, source_type, source_id, tenant_id, period (yyyy-mm), created_at; parti başlığı: period_from, period_to, target_format, file_sha256, exported_by, exported_at, status. Dengeli fiş kuralı: voucher_no başına Σdebit = Σcredit (kuruş).

### 8.3 Dönüştürücü öncelik sırası
1. GMS.NET Excel/XML fiş içe aktarımı (MM'nin gerçek şablonuyla; A4).
2. GMS.NET "banka eşleme dosyası" (mm_bank olayları için cari/kategori eşlemesi).
3. Logo Tiger/Go fiş aktarımı (Excel/XML).
4. Luca (Excel fiş).
5. Mikro, Zirve.
6. GMS.NET API (lisans, ücret ve test firması netleşirse; doğrulanmalı).

### 8.4 İçe aktarım
- UBL-TR XML (e-Arşiv/e-Fatura) ve GİB portalı düzenlenen belge listesi → billing.document otomatik doldurma, ETTN ile tekillik (INT-04).
- Banka ekstresi (banka başına Excel/CSV şablonu) → bank_statement_line → eşleştirme kuyruğu (INT-05).
- POS üye işyeri ekstresi → pos_settlement_expected eşleşmesi (CSH-04).
- MM mizanı ve cari bakiye listesi → chart_of_accounts ve resmi açılış (ACC-08, DATA-01).
- Bordro özeti Excel şablonu (TAX-04).

### 8.5 e-Defter ve UBL-TR ilişkisi
- UU Sport e-Defter üretmez (MM üretir); kanonik satır belge türü, belge no ve ETTN taşır ki MM'nin e-Defter belge alanları dolabilsin (alan eşlemesi MM ile doğrulanmalı).
- UU Sport e-Belge düzenlemez (ADR-005); UBL yalnız içe aktarılır. Entegratöre geçişte belge kaynağı değişir, ADR-004 modeli değişmez.

## 9 YOL_HARITASI
- M0: Güvenli temel (canlı öncesi) | içerik: [SEC-03, OPS-01, SEC-01, SEC-02, SEC-04, SEC-05, SEC-06, OPS-03, SAAS-04] | çıkış kriteri: gerçek Excel depo dışında ve ACL'li; şifreli yedek + haftalık geri yükleme testi yeşil; ofis ağından HTTPS; CI betiği (yetki, şema koruma, açık ve gizli bilgi taraması) yeşil | tahmini süre: 1-1,5 hafta
- M1: Doğru muhasebe çekirdeği | içerik: [ACC-06, ACC-05, ACC-04, ACC-02, ACC-03, ACC-01, ACC-08, ACC-14, ACC-07, UX-01, UX-02] | çıkış kriteri: üç resmi tanımı tek serviste; resmi ekstre sunucuda ve kendi bakiyesiyle; eşzamanlılık/atomiklik testleri; ADR-004 §4 kontrollerinin tamamı + ek kontroller verify'da 0; "Gayri resmi" metni 0 | tahmini süre: 1,5-2 hafta
- M2: Kasa/banka ve ödeme araçları + canlıya geçiş | içerik: [CSH-01, CSH-02, CSH-03, ACC-09, ACC-10, UX-04, UX-05, DATA-01, OPS-02, OPS-05, UX-08, SEC-11] | çıkış kriteri: ADR-007/008 onaylı; kasa sayımı ve banka bakiyesi sistemle eşit; kuru prova 0 fark; servis olarak kurulu sürüm; 2 hafta paralel çalışma sonunda Excel kapanır | tahmini süre: 2-3 hafta + 2 hafta paralel
- M3: Nakit akışı v1 | içerik: [TAX-01, CF-01, CF-02, CF-03, CF-05, TAX-02, TAX-04, CSH-05, CSH-04, UX-09, UX-07] | çıkış kriteri: haftalık/aylık pano, ödeme takvimi, en düşük nakit noktası; her rakam explain ile kaynağa iner; parametrelerin MM teyit durumu görünür | tahmini süre: 3 hafta
- M4: Muhasebeci aktarımı | içerik: [INT-02, INT-01, ACC-16, INT-03, ACC-13, ACC-11, SEC-09] | çıkış kriteri: sahiplik matrisi MM onaylı; MM test firmasında 1 aylık sentetik parti yüklenip mizan eşit; aktarılan dönem kilitli | tahmini süre: 2-3 hafta (MM şablonu gelmesine bağlı)
- M5: Senaryo, vergi tahmini, yaşlandırma ve tahsilat planı kararı | içerik: [CF-04, TAX-03, ACC-12, CF-06, UX-03, UX-06] | çıkış kriteri: üç hazır + kullanıcı senaryosu karşılaştırması; yaşlandırma raporu; aidat tahakkuku için ADR-015 kararı verilmiş | tahmini süre: 2 hafta
- M6: Faz 2 hazırlık (RDP + mobil) | içerik: [SAAS-01, SAAS-02, SAAS-03, SEC-07, SEC-08, SEC-10, SEC-13, OPS-04, DATA-03, INT-04, INT-05, UX-10] | çıkış kriteri: Türkiye'de kiralık sunucuda yalnız 443; mobil cihaz iptali çalışır; banka ekstresi eşleştirme kuyruğu; performans hedefleri | tahmini süre: 4-6 hafta
- M7: Ürünleştirme | içerik: [SAAS-05, SAAS-06, SAAS-07, SEC-12, DATA-02, STK-01, STK-02, STK-03, ACC-15, ACC-17, BIZ-02] | çıkış kriteri: ikinci (deneme) kiracı yalnız yapılandırmayla açılır; KVKK dışa aktarım/imha çalışır; konumlandırma ve paket kararı | tahmini süre: 6+ hafta
Ertelenecekler: Monte Carlo; döviz; çoklu depo UI; e-belge entegratörü (ADR-005 tetikleyicileri oluşana kadar); GMS.NET API; veli uygulaması (Faz 3); saha OCR kendi sunucumuzda (karekod önce).
En büyük 5 risk:
1. Resmi kavramının MM defteriyle uyuşmaması → kalıcı mutabakat farkı ve güven kaybı (ACC-02, ACC-03, INT-02).
2. Belgesiz satış kaydının hukuki/vergisel niteliği → AV/MM görüşü alınmadan satılabilir ürün riski (ACC-14, BIZ-02).
3. Yedeksiz veya şifresiz yedekle gerçek veriye geçiş → kayıp veya çocuk verisi sızıntısı (OPS-01, SEC-01).
4. Kapsam genişlemesi ve tek sahip bağımlılığı → çekirdek yarım kalır (BIZ-01).
5. Yanlış açılış bakiyeleri ve paralel çalışma yapılmadan Excel'in bırakılması (ACC-08, DATA-01).

## 10 ADR_ADAYLARI
- ADR-006: Hesap planı eşlemesi ve aktarım sahipliği | seçenekler: kart başına 1:1 kod; kural bazlı + kart istisnası; yalnız MM tarafında eşleme | önerilen: kural bazlı + kart istisnası + olay türü başına sahiplik matrisi | gerekçe: yüzlerce kartta sürdürülebilir, çift kaydı önler (INT-01, INT-02).
- ADR-007: Ödeme araçları (çek, kredi, kredi kartı, POS) revize | seçenekler: mevcut (çek/kredi cari dışı, taksit cariye); Logo pratiği (çek girişte cariyi kapatır, kredi yükümlülük hesabı, kart ekstre döngüsü, POS geri ödeme planı) | önerilen: Logo pratiği, ADR-004 tek yazma noktası korunarak (Payment + cash_movement) | gerekçe: MM defteriyle uyum, doğru cari ve nakit (ACC-09, ACC-10, CSH-02..05).
- ADR-008: Kasa/banka hareket defteri | seçenekler: bakiye = Σ payment (mevcut); ayrı append-only cash_movement | önerilen: cash_movement tek yazma noktası, payment otomatik bacak | gerekçe: carisiz hareket, virman, açılış, nakit akışı açılışı (CSH-01).
- ADR-009: Resmi kapsam tanımı | seçenekler: hep-ya-hiç bayrak (mevcut); tutar bazlı + kanal kuralı; aktarım anında dondurma | önerilen: tutar bazlı + tahsilatta kanal kuralı + aktarım sonrası dondurma + "belgelenme kuyruğu" dili | gerekçe: tek tanım, MM uyumu, hukuki risk azaltımı (ACC-01..03, ACC-14, ACC-16).
- ADR-010: Nakit akışı plan ve senaryo modeli | seçenekler: rapor içi hesap; plan kalemi + kural + düzeltme katmanı; olasılıksal model | önerilen: plan kalemi + kural + düzeltme katmanı, deterministik, lineage'lı | gerekçe: açıklanabilirlik ve küçük veri (CF-01, CF-03..05).
- ADR-011: Vergi ve mevzuat parametreleri | seçenekler: koda sabit; appsettings; tarih aralıklı küresel + kiracı tablo | önerilen: tarih aralıklı küresel + kiracı istisnası + teyit alanları | gerekçe: geçmiş dönem tekrar üretilebilirliği (TAX-01..04).
- ADR-012: Kiracı çözümleme ve SaaS kapıları | seçenekler: ilk kiracı (mevcut); claim tabanlı + işlem-yerel ayar; şema/veritabanı başına kiracı | önerilen: claim + işlem-yerel + paylaşımlı şema RLS, ayrılmış DB seçeneği | gerekçe: ucuz kapı, tek kod tabanı (SAAS-01..07).
- ADR-013: Dönem kilidi | seçenekler: yok; tarih kilidi; aktarım partisine bağlı otomatik kilit | önerilen: tarih kilidi + aktarımda otomatik ilerleme + Yönetici gerekçeli açma | gerekçe: resmi tutarlılık (ACC-07).
- ADR-014: Stok maliyeti | seçenekler: yok; FIFO; hareketli ağırlıklı ortalama; dönemsel ortalama | önerilen: hareketli ağırlıklı ortalama | gerekçe: az çeşit, basit, kârlılık raporu (STK-01).
- ADR-015: Tahsilat planı ve aidat | seçenekler: hiçbiri (mevcut); cariye yazmayan tahsilat planı; tam aidat tahakkuku | önerilen: şimdi tahsilat planı, M5'te tahakkuk kararı (kullanıcı onaylı toplu satış satırı) | gerekçe: nakit akışının gelir tarafı, ADR-005'i bozmadan (CF-02).
- ADR-016: Yedekleme ve anahtar yönetimi | seçenekler: pg_dump; WAL+PITR+dump; bulut yedek | önerilen: WAL+PITR + gecelik dump + istemci tarafı şifreleme + yurt içi uzak kopya + anahtar halkası yedeği | gerekçe: RPO/RTO hedefleri, KVKK (OPS-01, SEC-01).
- ADR-017: KVKK veri yaşam döngüsü | seçenekler: elle; rıza + saklama politikası + imha işi | önerilen: rıza kaydı + politika tablosu + anonimleştiren imha işi | gerekçe: çocuk verisi, SaaS öncesi (SEC-12).
- ADR-004 değişikliği: açılış bakiyesi kapsamı (ACC-08), cari fişi kaynak türleri (ACC-11), stok düzeltme kaynağı (STK-02).
- ADR-005 değişikliği: Logo kapsam eki (ACC-15), UBL/GİB içe aktarımı (INT-04).

## 11 SORULAR
- MM: Şirket e-Fatura mükellefi mi, yalnız e-Arşiv mi; e-Defter'e geçiş tarihi nedir? — belge türleri, aktarım ve entegratör tetikleyicisi buna bağlı (A1).
- MM: GMS.NET'e fiş aktarımı için örnek Excel/XML şablonu ve bir test firması verebilir misiniz? — INT-03 altın dosya testi olmadan dönüştürücü yazılamaz.
- MM: Banka hareketlerini ekstreden mi işliyorsunuz; UU Sport'tan "havale → cari kodu" eşleme dosyası işinizi kolaylaştırır mı? — aktarım sahipliği matrisi (INT-02).
- MM: Havale/POS ile gelen ama faturası henüz kesilmemiş veli tahsilatını hangi hesapta tutuyorsunuz (120 alacak / 340 avans)? — resmi tahsilat kanal kuralı ve resmi ekstrenin anlamı (ACC-03).
- MM: Geçiş tarihi itibarıyla cari bakiye listesi ve mizan (hesap planı dahil) verebilir misiniz? — resmi açılış ve hesap planı eşlemesi (ACC-08, INT-01).
- MM: Teslimden sonra fatura düzenleme süresi uygulamada kaç gün kabul edilmeli ve Muhasebe Bekleyen uyarı eşiği ne olmalı? — ACC-14 parametresi (ADR-004 açık konu 1).
- MM: KDV, muhtasar/SGK, geçici ve kurumlar vergisi vade günleri ile bölüm 4.4'teki oranları teyit eder misiniz? — TAX-01 teyit alanları.
- MM: Aylık bordro özetini (net ücret, stopaj, damga, SGK toplamı, teşvik) hangi biçimde paylaşabilirsiniz? — TAX-04.
- MM: Kredi taksitinde anapara/faiz/BSMV/KKDF ayrımını banka planından mı alıyorsunuz; POS komisyonu hangi hesaba? — ACC-10, CSH-04 fiş eşlemesi.
- MM: Karşılıksız çek ve protesto masrafını nasıl kaydediyorsunuz? — ACC-09.
- MM: İşyeri kirası gerçek kişiye mi ödeniyor (stopaj)? — A7, TAX-04.
- MM: Asgari kurumlar vergisi hesabı şirketimiz için nasıl yapılıyor? — TAX-03 v2.
- KU: Canlıya geçiş tarihi olarak hangi ay sonunu hedefliyorsunuz? — DATA-01.
- KU: Excel'i kapatmadan önce 2 hafta paralel çalışmayı kabul ediyor musunuz? — DATA-01 çıkış kriteri.
- KU: Veliye verilen ekstre hangisi olmalı: tüm hareketler mi, yalnız belgelenmiş olanlar mı? — ACC-01 varsayılanı ve çıktı dili.
- KU: Şirket kredi kartı, POS (hangi banka, taksitli satış var mı) ve portföyde çek şu an var mı? — ADR-007 önceliği.
- KU: Aylık ödeme planı olan sporcu sayısı ve paket türleri nedir; aidat tahakkukunu M5'te yeniden konuşmaya açık mısınız? — CF-02, ADR-015.
- KU: En az iki Yönetici hesabı (siz + güvendiğiniz bir kişi) tanımlanabilir mi? — SEC-08.
- KU: Ofis bilgisayarının RAM'i 16 GB'a çıkarılabilir mi? — OPS-05.
- KU: Uzak şifreli yedek için Türkiye'de bir depolama hizmeti kiralanabilir mi (aylık küçük ücret)? — SEC-01.
- AV: Belgesiz satışların ayrı bir katmanda sistematik izlenmesi vergi incelemesinde nasıl nitelendirilir; ürün dili ve saklama politikası nasıl olmalı? — ACC-14, BIZ-02.
- AV: Çocuk verisi için aydınlatma metni, açık rıza gerektiren işlemler, saklama/imha süreleri ve VERBIS yükümlülüğü nedir? — SEC-12.
- AV: İhlal bildirim süresi ve veri sahibi başvuru süresi güncel olarak nedir? — bölüm 6.5 (doğrulanmalı değerler).
- AV: SaaS'ta veri işleyen sıfatıyla müşteri akademilerle hangi sözleşme ve teknik tedbirler gerekir? — SAAS-06/07.
- AV: Yedek ve log gibi yardımcı veriler için yurt dışı hizmet kullanımı mümkün mü? — SEC-01, OPS-04.

## 12 FARKLAR_VE_FIKIRLER
- Belgelenme kuyruğu + yakınsama şeridi: rakiplerde "faturasız satış" ya yok ya gizli; burada açık bir iş kuyruğu, süre eşiği ve %belgelenme göstergesi (ACC-14, UX-07, 7.6).
- MM mutabakat paketi: aylık tek tıkla "resmi mizan + sahipliğe göre fiş + banka eşleme dosyası + uyarılar"; MM'nin işini azaltan ürün MM tarafından önerilir (satış kanalı).
- Veli/sporcu merkezli cari: tek veli çok sporcu, kardeş indirimi, sporcu adıyla veli arama (var) + tahsilat planı (CF-02).
- Açıklanabilir nakit akışı: Float/Pulse benzeri ufuk ve senaryo, Fathom benzeri KPI; farkı her rakamın kaynağa inmesi ve Türk vergi takviminin parametreli gelmesi.
- Banka kuralları (Xero'dan): açıklamadaki öğrenci adıyla otomatik öneri ve öğrenen kural (INT-05).
- Kural bazlı bağlantı kodları (Logo'dan) ve sektör şablonu: akademi için hazır hesap eşleme ve kategori seti.
- Tahsilat hatırlatma ve ödeme bağlantısı (Faz 3): veliye aylık özet ve güvenli ödeme bağlantısı; kart verisi saklanmaz (standart §8.1).
- Karekodlu fiş akışı (Paraşüt/Bizimhesap benzeri, ama görüntü şirket dışına çıkmadan).
- Muhasebeci salt okunur rolü (spesifikasyon O11): MM kendi girer, dışa aktarımı kendisi alır.
- Paket bazlı ürün: Temel / Nakit / Veli; on-prem ve SaaS aynı kod (SAAS-06).

## 13 ITIRAZLAR
- Çekin cariye etki etmemesi ve cironun etkisiz olması (Sürüm 2 notu O37 yorumu) yanlış; çek girişte cariyi kapatmalı, nakit vadede sayılmalı (ACC-09).
- Kredinin Party'ye bağlanması ve taksitin cariye ödeme yazması yanlış; kredi yükümlülük hesabı olmalı (ACC-10).
- "Resmi sorgu anında hesaplanır, saklanmaz" aktarım sonrası için yanlış; gönderilen kapsam dondurulmalı (ACC-16).
- Tahsilatın resmiliğini yalnız dağıtıma bağlamak yanlış; kanal kuralı gerekli (ACC-03).
- Açılış bakiyesinin onay tarihine yazılması ve her zaman resmi sayılması yanlış (ACC-08).
- Aidat tahakkukunun süresiz dışarıda kalması nakit akışı hedefiyle çelişiyor; şimdi tahsilat planı, M5'te karar (CF-02).
- "Gayri resmi" katman dili ve konumlandırması; hedef Toplam = Resmi olan belgelenme kuyruğu olmalı (ACC-14, BIZ-02).
- Ofis bilgisayarında 8 GB RAM ile sanal makine planı; doğrudan servis + RAM yükseltmesi (OPS-05).
- Ödeme aracı–hesap eşleşmesinde ana hafıza ile kod çelişiyor; kod kart ödemesini CreditCard'a açıyor ama tahsilatı da açık bırakıyor (CSH-02).
