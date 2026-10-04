# UUSPORT_DANISMA_CODEX_2026-09-27

## 0 META
arac: GPT-6 / Codex; çalışma eforu yüksek; sağlayıcıya ait alt sürüm bilgisi görünür değil
tarih: 2026-09-27
durum: Danışma raporu; bütün tasarımlar [ONERI], uygulama veya ADR onayı değildir.
yontem: Kaynak kod ve belge incelemesi; sağlanan sentetik PNG'lerin görsel incelemesi; birincil dış kaynak araştırması.
dogrulama_siniri: Derleme, test, migration, uygulama başlatma, Git komutu ve veritabanı bağlantısı yapılmadı. Canlı davranış, mevcut ACL ve geçmiş test başarıları doğrulanmadı.
yazma_kapsami: Yalnız bu rapor. Kaynak veri klasörleri ve gizli bilgi dosyaları açılmadı/listelenmedi.
okunan_ekler:
  - docs/reviews/DIS_YZ_STRATEJI_DANISMA_PROMPTU_2026-09-27.md
  - docs/project-memory/UUSPORT_ANA_HAFIZA.md
  - docs/decisions/ADR-001_TEKNOLOJI_YIGINI_VE_ISTEMCI_MIMARISI.md
  - docs/decisions/ADR-002_PARA_TIPI_VE_YUVARLAMA.md
  - docs/decisions/ADR-004_CARI_HAREKET_MODELI_SATIS_BELGE_TAHSILAT.md
  - docs/decisions/ADR-005_ILK_SURUM_KAPSAMI_VE_ELLE_FATURA.md
  - docs/standards/UUSPORT_GUVENLIK_STANDARDI.md
  - docs/standards/UUSPORT_POSTGRESQL_ADLANDIRMA_VE_MIGRATION_STANDARDI.md
  - docs/operations/UUSPORT_GELISTIRME_VERI_VE_CANLIYA_ALMA_PROSEDURU.md
  - docs/designs/MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md
  - src/UUSport.Api/{Program.cs,Domain/Entities.cs,Data/*,Features/*,Infrastructure/WebPipeline.cs}
  - src/UUSport.Web/src/{pages/*,components.tsx,styles.css}; ilgili testler; tools/build-uusport.ps1
  - docs/reviews/dis-yz-ekleri-2026-09-27/01-genel-bakis-ve-menu.png
  - docs/reviews/dis-yz-ekleri-2026-09-27/02-cari-detay.png
  - docs/reviews/dis-yz-ekleri-2026-09-27/03-cari-detay-koyu-gorunum.png
  - docs/reviews/dis-yz-ekleri-2026-09-27/04-ekstre-toplam.png
  - docs/reviews/dis-yz-ekleri-2026-09-27/05-ekstre-resmi-cari.png
  - docs/reviews/dis-yz-ekleri-2026-09-27/06-hesap-hareketleri.png
  - docs/reviews/dis-yz-ekleri-2026-09-27/07-tahsilat-penceresi.png
  - docs/reviews/dis-yz-ekleri-2026-09-27/08-muhasebe-bekleyen.png
  - docs/reviews/dis-yz-ekleri-2026-09-27/09-kasa-banka-hiyerarsi.png
  - docs/reviews/dis-yz-ekleri-2026-09-27/10-kredi-taksitler.png
  - docs/reviews/dis-yz-ekleri-2026-09-27/11-cek-portfoyu.png
  - docs/reviews/dis-yz-ekleri-2026-09-27/12-raporlar.png
  - docs/reviews/dis-yz-ekleri-2026-09-27/13-excel-aktarim.png
  - Apple Design Skill ve erişilebilirlik, düzen, tipografi, renk, web/platform uyarlaması, tablo, klavye, veri girişi, grafik referansları
eksik_ek: Promptta sözü edilen ekstre PDF'si ekler klasöründe bulunamadı; PDF çıktısı doğrulanmış değildir.
varsayimlar:
  - A1: İlk üretim kapsamı tek şirket, TRY, tek depo ve Yönetici/Muhasebe rolleridir; ikinci kiracı henüz açılmaz.
  - A2: Ürün stratejisi bakımından canlı kurulum yapılmadığı prompt bilgisi esas alındı; bağımsız kurulum doğrulaması yapılmadı.
  - A3: Mali müşavirin hesap planı, GMS.NET sürümü/lisansı ve kabul ettiği aktarım sözleşmesi bilinmiyor.
  - A4: Mevcut aidat tahakkuku yasağı korunur; geleceğe dönük tahsilat planı cari borç oluşturmaz.
  - A5: PNG'ler promptun belirttiği gibi sentetiktir; 03 ve 07–13 eski menüleri güncel tasarım kanıtı değildir.
  - A6: Eforlar ajan geliştirme günüdür; mali müşavir, hukuk, kullanıcı kabulü ve tedarikçi beklemesi dahil değildir.
  - A7: Güncel vergi oranı, istisna ve beyan takvimi onaylanmış kabul edilmez; eksik parametre sonuçta "hesaplanamadı" üretir.
  - A8: Yurt içi barındırma şirket kararıdır; tek başına bütün KVKK yükümlülüklerini yerine getirmez.
oncelik: P0 gerçek veri/canlı öncesi; P1 güvenilir finansal planlama veya ilgili faz açılmadan önce; P2 sonraki ürünleşme; P3 deneysel.
efor: S <1 gün; M 1–3 gün; L 4–10 gün; XL >2 hafta. Ortak altyapı nedeniyle süreler doğrudan toplanmaz.
kanit_dili: Kod kanıtı gözlenen uygulamayı, belge kanıtı hedefi, ekran kanıtı yalnız o PNG'yi gösterir. "Yok" iddiaları incelenen kapsamla sınırlıdır.

## 1 OZET (en fazla 12 satır: en önemli yargılar)
1. ADR-004'ün tek cari defteri ve belgenin yeniden borç yazmaması korunmalı; sorun çekirdek ilkenin kendisinden çok görünüm, eşleştirme ve işlem bütünlüğündedir.
2. İkinci migration şema genelinde UPDATE yetkisini yeniden veriyor; önceki append-only korumasını geri almıyor. Statik P0; fiili ACL ayrıca sınanmalıdır.
3. "Resmi Cari" boolean hesabı kısmi belgelenme, dağıtım geri alma ve filtreli bakiye için güvenilir değildir; belgelenme ile MM kabulü farklı eksenler olmalıdır.
4. Kredi/çek ödeme yollarında iki ayrı kayıt aşaması vardır; tek işlem ve idempotency olmadan çift veya bağlantısız ödeme mümkündür.
5. Kasa/banka için ödeme kaynaklı bakiye zaten var; "bakiye yok" iddiası yanlış. Eksik olan açılış, virman, carisiz hareket ve mutabakatlı hesap defteridir.
6. Önce bakiye doğruluğu, sonra kesin vadeli planlar, ardından açıklanabilir deterministik senaryolar; Monte Carlo ve kapsamlı ERP sonraya bırakılmalı.
7. Gerçekleşme, kesinlik, belgelenme ve aktarım durumu ayrı boyutlardır; aynı parayı beş kategori altında tekrar toplamamak gerekir.
8. GMS.NET için önce mali müşavirce kabul edilmiş dosya pilotu; program bağımsız fiş modeli ve aktarım alındısı, API'den daha önceliklidir.
9. Tek kiracı çözümlemesi, Identity kapsamı ve yalnız ID kullanan ilişkiler SaaS için yeterli değildir; ikinci kiracı açılması ayrı kapı olmalıdır.
10. HTTPS, geri yükleme provası, erişim denetimi ve gerçek verinin ajan çalışma alanından ayrılması ilk canlı koşullarıdır.
11. Teknoloji değişikliği önerilmiyor; modüler monolit ve mevcut .NET/React/Expo/PostgreSQL korunuyor.
12. Aşağıdaki kabul ölçütleri yapılacak doğrulamalardır; bu incelemede testler çalıştırılmış değildir.

## 2 BULGULAR

Bulgular şablondaki alanları koruyan YAML akış biçimindedir; her kayıt sekiz satırdır.

```yaml
- {id: "SEC-01", oncelik: "P0", tur: "risk",
   iddia: "İkinci migration önceki append-only tablolara UPDATE yetkisini yeniden veriyor.", gerekce: "ApplySchemaGrants tüm tablolara UPDATE verir; ikinci Up tekrar revoke çağırmaz.",
   oneri: "Yeni düzeltici migration ile yetkileri onar; gelecek migration grant'lerini açık tablo listesiyle sınırla.",
   somut: "MigrationSecurity.ApplySchemaGrants; AddLoansChecksAndAccountCodes.Up; ACL regresyon testi.",
   kabul: "Sıfırdan ve eski sürümden yükseltmede app rolü ledger/stock/audit/allocation UPDATE, DELETE, TRUNCATE yapamaz; mevcut ACL ayrıca ölçülür.",
   bagimlilik: [], efor: "M", adr: "yok",
   kanit: "src/UUSport.Api/Data/MigrationSecurity.cs:42; src/UUSport.Api/Data/Migrations/20260926062253_AddLoansChecksAndAccountCodes.cs:248; tests/UUSport.Tests/GuvenlikVeKuralTests.cs:49",
   guven: "yuksek"}

- {id: "ACC-01", oncelik: "P0", tur: "risk",
   iddia: "Tam belgelenme boolean'ı kısmi belgeli tutarı doğru temsil etmiyor.", gerekce: "Sorgu yalnız documented durumunu kabul ediyor; ADR-004 oransal kapsam istiyor.",
   oneri: "Belge kapsamını miktar ve tutar olarak hesapla; kısmi kısmı ayrı göster.",
   somut: "AccountingQueries.BuildOfficialFlags; document_link.covered_amount; cari/ekstre/PDF ortak projection.",
   kabul: "1.000 TL satışın %40'ı belgelenince 400 belgeli, 600 bekleyen görünür; toplam 1.000 kalır.",
   bagimlilik: [], efor: "L", adr: "ADR-004 açıklığa kavuşur",
   kanit: "src/UUSport.Api/Features/Accounting/AccountingQueries.cs:119; docs/decisions/ADR-004_CARI_HAREKET_MODELI_SATIS_BELGE_TAHSILAT.md:89",
   guven: "yuksek"}

- {id: "ACC-02", oncelik: "P0", tur: "risk",
   iddia: "Pozitif dağıtım varlığı tahsilatın tamamını belgeli sayıyor ve dağıtım iptalini dikkate almıyor.", gerekce: "Amount > 0 filtresi negatif geri almayı dışlıyor; Any tüm ödeme için boolean üretiyor.",
   oneri: "Net dağıtım ve belgeli satır kapsamı üzerinden tahsilat tutarını parçala.",
   somut: "AccountingQueries.BuildOfficialFlags; payment_allocation; ortak coverage sorgusu.",
   kabul: "1.000 tahsilatın 100'ü belgeli satıra bağlıysa 100 kapsanır; dağıtım geri alınınca 0 olur.",
   bagimlilik: ["ACC-01"], efor: "M", adr: "ADR-004 açıklığa kavuşur",
   kanit: "src/UUSport.Api/Features/Accounting/AccountingQueries.cs:138; src/UUSport.Api/Features/Accounting/AccountingQueries.cs:141",
   guven: "yuksek"}

- {id: "ACC-03", oncelik: "P0", tur: "risk",
   iddia: "Filtreli ekstre toplam defterin yürüyen bakiyesini göstererek görünür satır toplamıyla çelişiyor.", gerekce: "Bakiye filtre öncesi hesaplanıyor; istemci görünür toplamları Number ile hesaplıyor.",
   oneri: "Sunucu decimal ile filtre bakiyesi ve tüm cari bakiyesini ayrı döndürsün; PDF aynı sonucu kullansın.",
   somut: "AccountingQueries.Ledger; PartyPages.Statement; dönem başı/devri ve iki açık bakiye etiketi.",
   kabul: "Ekran ve çıktı aynı filtre/asOf için eşit; 05 PNG'deki 2.500 borç/2.500 alacak için görünür satırların neti 0 olur; devir ayrı gösterilir.",
   bagimlilik: ["ACC-01","ACC-02"], efor: "M", adr: "yok",
   kanit: "src/UUSport.Api/Features/Accounting/AccountingQueries.cs:71; src/UUSport.Web/src/pages/PartyPages.tsx:336; src/UUSport.Web/src/pages/PartyPages.tsx:372; docs/reviews/dis-yz-ekleri-2026-09-27/05-ekstre-resmi-cari.png",
   guven: "yuksek"}

- {id: "ACC-04", oncelik: "P0", tur: "itiraz",
   iddia: "Belgelenmiş olmak muhasebeye gönderilmiş veya kabul edilmiş olmak değildir.", gerekce: "Kodda MM kabul kaydı yok; ekstre açıklaması gönderilmiş anlamı taşıyor.",
   oneri: "Toplam, Belgelenen ve Muhasebe Bekleyen adlarını kullan; aktarım durumunu bağımsız göster.",
   somut: "Cari filtre sözlüğü; export_batch/item/receipt; PDF başlığı ve kapsam açıklaması.",
   kabul: "Belgesi olan fakat aktarılmayan kayıt gönderilmiş görünmez; reddedilen aktarım kabul edildi sayılmaz.",
   bagimlilik: ["ACC-01"], efor: "M", adr: "ADR-004 terminoloji ve görünüm sözleşmesi değişir",
   kanit: "src/UUSport.Api/Features/Accounting/AccountingQueries.cs:115; src/UUSport.Api/Domain/Entities.cs:145; belge bölüm 3.1 ve 5.3",
   guven: "yuksek"}

- {id: "ACC-05", oncelik: "P0", tur: "risk",
   iddia: "Elle girilen fatura farkı yalnız yanıt/denetim kaydı olarak kalıyor; kalıcı mutabakat iş akışı yok.", gerekce: "Belge tutarını korumak doğru; ekonomik satırla farkın açıklanması ve kapatılması da gerekir.",
   oneri: "Farkı kalıcı reconciliation_issue yap; sebep/onay iste; fiyat farkını izinli ekonomik satır veya onaylı düzeltmeyle çöz.",
   somut: "EnterDocument; belge KDV kontrolü; reconciliation_issue; aktarım engeli.",
   kabul: "Belge 1.001, satır 1.000 ise fark 1 açık kalır; belge tutarı değişmez ve cariye otomatik ikinci borç yazılmaz.",
   bagimlilik: ["ACC-04"], efor: "M", adr: "ADR-004 düzeltme sözleşmesi netleşir",
   kanit: "src/UUSport.Api/Features/Accounting/AccountingService.cs:444; src/UUSport.Api/Features/Accounting/AccountingService.cs:450",
   guven: "yuksek"}

- {id: "ACC-06", oncelik: "P0", tur: "risk",
   iddia: "Okuyup sınama ve sonradan yazma işlemleri eşzamanlı kapama, iade ve stok çıkışında yarışa açık.", gerekce: "Tek SaveChanges atomik olabilir; öncesindeki bakiye okumalarını seri hale getirmez.",
   oneri: "Komut transaction'ı, deterministik kilit sırası, uygun satır kilidi/version ve idempotency ekle.",
   somut: "AllocatePayment; ReverseLine; AddStock; document quantity allocation; paralel istek testleri.",
   kabul: "Aynı 100 açık tutara iki 80 dağıtımda yalnız biri geçer; son stok/iade sınırı hiçbir sırada aşılmaz.",
   bagimlilik: ["SEC-01"], efor: "L", adr: "yeni ADR: Finansal komut bütünlüğü",
   kanit: "src/UUSport.Api/Features/Accounting/AccountingService.cs:255; src/UUSport.Api/Features/Accounting/AccountingService.cs:273; src/UUSport.Api/Features/Accounting/AccountingService.cs:536",
   guven: "yuksek"}

- {id: "ACC-07", oncelik: "P0", tur: "risk",
   iddia: "İade sonrası açık satır tutarı orijinal brütten hesaplandığı için fazla açık bakiye üretilebilir.", gerekce: "OpenAmount iade brütünü düşmüyor; tam iadenin belge durumu da ayrı boolean sorunları taşıyor.",
   oneri: "Net ekonomik tutarı satış ve ters kayıtlarından hesapla; fazla kapamayı avans/iade iş akışına taşı.",
   somut: "AccountingQueries.Lines; ReverseLine; allocation açık-tutar doğrulaması.",
   kabul: "1.000 satış, 400 iade, 300 kapama => 300 açık; tam iadede eski belgenin tarihsel kapsamı kaybolmaz.",
   bagimlilik: ["ACC-01","ACC-06"], efor: "M", adr: "yok",
   kanit: "src/UUSport.Api/Features/Accounting/AccountingQueries.cs:186; src/UUSport.Api/Features/Accounting/AccountingQueries.cs:210; src/UUSport.Api/Features/Accounting/AccountingService.cs:273",
   guven: "yuksek"}

- {id: "ACC-08", oncelik: "P1", tur: "eksik",
   iddia: "Vade ve dönem kesiti olmadan yaşlandırma ve mutabakat güvenilir üretilemez.", gerekce: "TradeLine'da vade bulunmuyor; sorgular güncel belge/dağıtım durumunu geçmişe taşıyabilir.",
   oneri: "Vade planı ve effective_date/recorded_at kesiti ekle; onaylı dönem snapshot'ı üret.",
   somut: "obligation; opening_balance.effective_date; statement asOf; aging API.",
   kabul: "31–60 gün dilimi vade esaslıdır; sonraki ay yapılan dağıtım eski ayın onaylı çıktısını sessizce değiştirmez.",
   bagimlilik: ["ACC-01","ACC-07"], efor: "L", adr: "yeni ADR: Dönem ve vade sözleşmesi",
   kanit: "src/UUSport.Api/Domain/Entities.cs:114; src/UUSport.Api/Domain/Entities.cs:229; src/UUSport.Api/Features/Accounting/AccountingQueries.cs:58",
   guven: "yuksek"}

- {id: "ACC-09", oncelik: "P2", tur: "iyilestirme",
   iddia: "Kontrollü cari düzeltme ve cari virman gerekli olabilir; genel muhasebe modülü gerekli değil.", gerekce: "approved_adjustment kaynak tipi var; sınırsız serbest fiş ana defterin izlenebilirliğini zayıflatır.",
   oneri: "Sebep ve çift onaylı düzeltme/iki taraflı virman tasarla; döviz ve kur farkını ihtiyaç doğrulanana kadar ertele.",
   somut: "approved_adjustment belgesi; bağlı ledger_entry çiftleri; işlem/ters kayıt ekranı.",
   kabul: "Virman iki caride aynı anda oluşur ve toplamı sıfırdır; aktaran kişi kendi düzeltmesini onaylayamaz.",
   bagimlilik: ["ACC-06"], efor: "L", adr: "ADR-004 izinli düzeltmeler netleşir",
   kanit: "src/UUSport.Api/Data/AppDbContext.cs:291; docs/decisions/ADR-004_CARI_HAREKET_MODELI_SATIS_BELGE_TAHSILAT.md:57",
   guven: "orta"}

- {id: "CSH-01", oncelik: "P1", tur: "iyilestirme",
   iddia: "Prompttaki hesap bakiyesi yok iddiası kodla çelişiyor; bakiye ödeme kayıtlarından zaten türetiliyor.", gerekce: "Bu bakiye açılış, virman ve carisiz hareketleri kapsamıyor.",
   oneri: "Mevcut alanı ödeme hareketleri bakiyesi olarak tanımla; mutabakatlı hesap defterine kontrollü geçir.",
   somut: "CatalogEndpoints hesap listeleme; cash_entry; hesap mutabakat ekranı.",
   kabul: "Eski ödeme toplamıyla geçiş köprüsü eşittir; açılış ve transferler eklenince farkın kaynağı gösterilir.",
   bagimlilik: ["ACC-06"], efor: "L", adr: "yeni ADR: Kasa banka ve borç alt defteri",
   kanit: "src/UUSport.Api/Features/Catalog/CatalogEndpoints.cs:90; src/UUSport.Api/Features/Catalog/CatalogEndpoints.cs:108; belge bölüm 5.2",
   guven: "yuksek"}

- {id: "CSH-02", oncelik: "P1", tur: "iyilestirme",
   iddia: "Tek parent_account_id muhasebesel tür ile operasyonel bağlantıyı karıştırıyor.", gerekce: "POS'un ödeme bankası, kartın borç kapatma hesabı ve kredinin kullandırım hesabı farklı ilişkilerdir.",
   oneri: "Türü koru; banka kuruluşu ve hesaplar arası rol/tarih geçerli bağlantılar ekle; hiyerarşiyi yalnız sunumda kullan.",
   somut: "financial_institution; account_relation; cash_account.currency/purpose; hesap kartı.",
   kabul: "Bir POS başka bankaya yatabilir; kartın ödeme hesabı değişince geçmiş kayıtlar etkilenmez.",
   bagimlilik: ["CSH-01"], efor: "M", adr: "yeni ADR: Kasa banka ve borç alt defteri",
   kanit: "src/UUSport.Api/Domain/Entities.cs:187; src/UUSport.Api/Features/Catalog/CatalogEndpoints.cs:143",
   guven: "yuksek"}

- {id: "CSH-03", oncelik: "P0", tur: "risk",
   iddia: "Kredi taksiti ve çek işlemleri ödeme ile durum değişikliğini iki ayrı SaveChanges aşamasında yapıyor.", gerekce: "İlk kayıt başarılı, ikinci başarısız olabilir; eşzamanlı istekler iki ödeme üretebilir.",
   oneri: "Ödeme oluşturma ve kaynak güncellemesini tek transaction/komut kapsamına al; işleme özgü unique anahtar koy.",
   somut: "LoanService.Pay; CheckService.Collect/Bounce; AccountingService iç SaveChanges ayrımı.",
   kabul: "İki paralel veya tekrarlanan komut yalnız bir ödeme üretir; ikinci aşamada hata olunca hiçbir finansal etki kalmaz.",
   bagimlilik: ["ACC-06"], efor: "M", adr: "yeni ADR: Finansal komut bütünlüğü",
   kanit: "src/UUSport.Api/Features/Cash/LoanService.cs:105; src/UUSport.Api/Features/Cash/LoanService.cs:112; src/UUSport.Api/Features/Cash/CheckService.cs:116; src/UUSport.Api/Features/Cash/CheckService.cs:123",
   guven: "yuksek"}

- {id: "CSH-04", oncelik: "P0", tur: "risk",
   iddia: "Ödeme ters çevrilince taksit üzerindeki PaymentId tek başına ödenmişlik kanıtı olmaktan çıkar.", gerekce: "Kalan anapara PaymentId null koşuluyla hesaplanıyor.",
   oneri: "Ödenen tutarı net settlement olaylarından türet; iptal edilen ödemeyi plana geri aç.",
   somut: "LoanService.List/Installments; loan_installment settlement; çek durum geçişleri.",
   kabul: "Taksit öde–geri al sonrasında kalan anapara eski değerine döner; yeniden ödeme mümkün ve tekildir.",
   bagimlilik: ["CSH-03"], efor: "M", adr: "yok",
   kanit: "src/UUSport.Api/Features/Cash/LoanService.cs:130; src/UUSport.Api/Features/Cash/LoanService.cs:140; src/UUSport.Api/Features/Accounting/AccountingService.cs:314",
   guven: "yuksek"}

- {id: "CSH-05", oncelik: "P0", tur: "risk",
   iddia: "Kredi taksitinin sıradan cari ödeme olması borç bakiyesini ve raporlanan alacağı bozabilir.", gerekce: "Kredi kullandırımına karşılık gelen yükümlülük olayı yok; taksit cari ödemesi alacak bakiyesi yaratabilir.",
   oneri: "Kullandırım, anapara, faiz, vergi ve masrafı kredi alt defterinde ayır; cari etkisini ADR ile açık seç.",
   somut: "LoanService.Create/Pay; loan balance; Reports open balances; CSH-01 cash journal.",
   kabul: "500.000 kullandırım ve 20.000 anapara ödemesinde kredi borcu 480.000 olur; bankadan ticari alacak görünmez.",
   bagimlilik: ["CSH-01","CSH-03"], efor: "L", adr: "yeni ADR: ADR-007 Kredi ve finansman muhasebesi",
   kanit: "src/UUSport.Api/Features/Cash/LoanService.cs:35; src/UUSport.Api/Features/Cash/LoanService.cs:105; src/UUSport.Api/Features/Reports/ReportEndpoints.cs:35",
   guven: "yuksek"}

- {id: "CSH-06", oncelik: "P0", tur: "risk",
   iddia: "Kredi/çek gerçekleşme tarihi verilmezse vade tarihi kullanılıyor.", gerekce: "Gecikmiş ödemeyi geçmişte olmuş gibi kaydeder; günlük nakit ve dönem kesiti bozulur.",
   oneri: "Gerçek banka/kasa işlem tarihini zorunlu al; vade tarihini ayrı koru.",
   somut: "LoanService.Pay; CheckService.Collect; tarih alanlı onay penceresi.",
   kabul: "10 Eylül vadeli, 27 Eylül ödenen kayıt gerçekleşende 27 Eylül, gecikmede 17 gün gösterilir.",
   bagimlilik: ["CSH-03"], efor: "S", adr: "yok",
   kanit: "src/UUSport.Api/Features/Cash/LoanService.cs:107; src/UUSport.Api/Features/Cash/CheckService.cs:118",
   guven: "yuksek"}

- {id: "CSH-07", oncelik: "P1", tur: "eksik",
   iddia: "POS tahsilatı kullanılabilir banka bakiyesi değildir.", gerekce: "Brüt tahsilat, komisyon, bloke ve net valör ayrı izlenmiyor.",
   oneri: "POS alacağı ve settlement planı kur; bankaya yatışta POS'u kapat, komisyonu ayrıştır.",
   somut: "pos_batch/settlement; account_relation; POS raporu içe alma/eşleştirme.",
   kabul: "10.000 brüt, 200 komisyon için yalnız valörde 9.800 kullanılabilir nakit eklenir; brüt ikinci kez sayılmaz.",
   bagimlilik: ["CSH-01","CSH-02"], efor: "L", adr: "yeni ADR: Kasa banka ve borç alt defteri",
   kanit: "src/UUSport.Api/Domain/Entities.cs:189; src/UUSport.Api/Features/Catalog/CatalogEndpoints.cs:103",
   guven: "yuksek"}

- {id: "CSH-08", oncelik: "P0", tur: "risk",
   iddia: "Kart yöntemi yön kontrolü olmadan hem POS hem şirket kartını kabul ediyor.", gerekce: "Şirket kartına müşteri tahsilatı veya POS'tan tedarikçi ödemesi API'de seçilebilir.",
   oneri: "Yön–hesap amaç matrisi uygula; şirket kartı harcama/ekstre/ödeme modelini ayır.",
   somut: "AccountingService.EnsureMethod; card_statement/installment; kart ekranı.",
   kabul: "Müşteri kart tahsilatı yalnız POS; şirket kartı harcaması banka nakdini düşürmez; ekstre ödemesi bir kez düşürür.",
   bagimlilik: ["CSH-01"], efor: "L", adr: "yeni ADR: Kasa banka ve borç alt defteri",
   kanit: "src/UUSport.Api/Features/Accounting/AccountingService.cs:580; src/UUSport.Api/Features/Accounting/AccountingService.cs:586; src/UUSport.Api/Domain/Entities.cs:183",
   guven: "yuksek"}

- {id: "CSH-09", oncelik: "P1", tur: "eksik",
   iddia: "Çek ciro işlemi alıcıyı ve borç kapama ilişkisini kaydetmiyor; durum geçişleri yönle sınırlı değil.", gerekce: "Not alanı ciro edilen tarafı ve geri dönüş yükümlülüğünü kanıtlayamaz.",
   oneri: "Yön bazlı durum makinesi ve append-only çek olayları ekle; eksik ciro/teslim eylemlerini ilgili sürümde kapalı tut.",
   somut: "CheckService.Submit/Endorse; check_event; recipient_party_id; iş anahtarı tekilliği.",
   kabul: "Verilen çek tahsile verilemez; alınan çek ciro edilince hedef cari ve rücu ilişkisi görünür; çift kayıt engellenir.",
   bagimlilik: ["CSH-03","CSH-05"], efor: "L", adr: "yeni ADR: Çek senet yaşam döngüsü",
   kanit: "src/UUSport.Api/Features/Cash/CheckService.cs:97; src/UUSport.Api/Features/Cash/CheckService.cs:143; src/UUSport.Api/Data/AppDbContext.cs:230",
   guven: "yuksek"}

- {id: "CSH-10", oncelik: "P1", tur: "eksik",
   iddia: "Cari zorunluluğu banka masrafı, vergi ve hesaplar arası virmanı yapay carilere iter.", gerekce: "Nakit olayı ile ticari karşı taraf borcu aynı şey değildir.",
   oneri: "Ayrı treasury komutu, onaylı hesap açılışı ve carisiz gider/ödeme kaynağı ekle; cari gerekiyorsa aynı operation'a bağla.",
   somut: "cash_journal/entry; opening approval; transfer API; dönem mutabakatı.",
   kabul: "A–B banka virmanı tenant toplamında sıfır; banka masrafı tek gider/tek nakit çıkışı; belgeler cariye ikinci kayıt üretmez.",
   bagimlilik: ["CSH-01","CSH-02"], efor: "L", adr: "ADR-004 ile uyumlu yeni kasa banka ADR'si",
   kanit: "src/UUSport.Api/Domain/Entities.cs:204; src/UUSport.Api/Domain/Entities.cs:189",
   guven: "yuksek"}

- {id: "STK-01", oncelik: "P1", tur: "eksik",
   iddia: "Miktar stok raporu ürün kârlılığı ve vergi tahmini için maliyet sağlamıyor.", gerekce: "Item ve StockMovement maliyet taşımaz; alış ödemesi satılan mal maliyeti değildir.",
   oneri: "Tek depoda hareketli ağırlıklı ortalama öner; iade orijinal maliyete bağlansın; eksi stok geçici maliyet olarak işaretlensin.",
   somut: "inventory.cost_layer/valuation_entry; stock_movement.cost_amount; kârlılık raporu.",
   kabul: "10 adet 100 ve 10 adet 120 alım sonrası 5 çıkışın maliyeti 550; iade ve geriye tarihli düzeltme denetlenir.",
   bagimlilik: ["ACC-06"], efor: "L", adr: "yeni ADR: Stok maliyetlendirme",
   kanit: "src/UUSport.Api/Domain/Entities.cs:80; src/UUSport.Api/Domain/Entities.cs:97",
   guven: "yuksek"}

- {id: "STK-02", oncelik: "P2", tur: "iyilestirme",
   iddia: "Tek depo yeterli; sayım ve fireyi satış gibi göstermek gereksiz cari etki üretir.", gerekce: "Mevcut stok hareketleri ticari satırlara bağlıdır.",
   oneri: "Sayım/fire/açılış için onaylı stok belgesi ekle; çok depo yalnız gerçek operasyon ihtiyacında aç.",
   somut: "stock_adjustment; neden kodu; fiziksel sayım ekranı.",
   kabul: "Sayım farkı stok ve değerlemeyi düzeltir, cariyi değiştirmez; hizmet kartında stok oluşmaz.",
   bagimlilik: ["STK-01"], efor: "M", adr: "yeni ADR: Stok maliyetlendirme",
   kanit: "src/UUSport.Api/Domain/Entities.cs:97; src/UUSport.Api/Features/Accounting/AccountingService.cs:534",
   guven: "yuksek"}

- {id: "CF-01", oncelik: "P1", tur: "itiraz",
   iddia: "Gerçekleşen, Planlanan, Tahmini, Muhasebe Bekleyen ve Resmi birbirini dışlayan beş durum değildir.", gerekce: "Bir tahsilat gerçekleşmiş, belgesiz ve henüz aktarılmamış olabilir.",
   oneri: "Gerçekleşme, kesinlik, belge kapsamı ve aktarımı ayrı boyutlar yap; tutarı tek akış kimliğiyle say.",
   somut: "cashflow projection; lifecycle/certainty/coverage/export_status filtreleri.",
   kabul: "Aynı 1.000 TL gerçekleşmiş-belgesiz tahsilat toplamda yalnız 1.000 sayılır; her boyutta izlenebilir.",
   bagimlilik: ["ACC-04","CSH-01"], efor: "M", adr: "yeni ADR: Nakit akışı ve senaryo sözleşmesi",
   kanit: "belge bölüm 3.1-3 ve 8-D; src/UUSport.Api/Domain/Entities.cs:202",
   guven: "yuksek"}

- {id: "CF-02", oncelik: "P1", tur: "eksik",
   iddia: "Tahmin motoru kaynak başına plan üretirse çift sayımı önleyen ortak ekonomik kimlik gerekir.", gerekce: "Aynı borç satış, kart ekstresi, çek ve manuel planda görünebilir.",
   oneri: "Obligation–source–projection zinciri ve unique kaynak/plan sürümü kur; yeni temsil eskisini tüketir veya değiştirir.",
   somut: "finance.obligation; obligation_source; projection_key; forecast service.",
   kabul: "Satıcı borcu çekle kapatılınca açık satıcı planı ve verilen çek birlikte iki nakit çıkışı üretmez.",
   bagimlilik: ["CF-01","CSH-10"], efor: "L", adr: "yeni ADR: Nakit akışı ve senaryo sözleşmesi",
   kanit: "src/UUSport.Api/Domain/Entities.cs:114; src/UUSport.Api/Domain/Entities.cs:315; belge bölüm 8-D",
   guven: "yuksek"}

- {id: "CF-03", oncelik: "P1", tur: "fikir",
   iddia: "Aidat tahakkuku açmadan öğrenci tahsilat beklentisi üretilebilir.", gerekce: "Plan cari borç ve gelir değildir; mevcut kapsam kararı korunabilir.",
   oneri: "Yetkili tarafından onaylı paket/tahsilat planı ve ayrı yeni kayıt beklentisi oluştur; geçmiş eğilimi yalnız yeterli veriyle ekle.",
   somut: "collection_plan; schedule; öğrenci/paket varsayımı; planı satışa bağlama.",
   kabul: "Plan oluşturunca ledger değişmez; satış/ödeme oluşunca ilişkili plan kalan tutara iner ve iki kez tahmin edilmez.",
   bagimlilik: ["CF-02"], efor: "M", adr: "ADR-005 korunur; yeni nakit akışı ADR'si",
   kanit: "docs/decisions/ADR-005_ILK_SURUM_KAPSAMI_VE_ELLE_FATURA.md; belge bölüm 8-D",
   guven: "yuksek"}

- {id: "CF-04", oncelik: "P1", tur: "eksik",
   iddia: "Tekrarlayan gider kuralları sürümlü ve tarih sınırlı olmalıdır.", gerekce: "Kira artışı geçmiş ayları veya onaylı senaryoyu değiştirmemeli.",
   oneri: "effective_from/to, sıklık, iş günü politikası ve sonlandırma içeren recurrence üret; idempotent occurrence anahtarı kullan.",
   somut: "recurrence_rule/version; obligation occurrence; takvim API.",
   kabul: "Kira Ekim'de %40 artınca Eylül sabit kalır; kuralı tekrar işletmek aynı ayı çoğaltmaz.",
   bagimlilik: ["CF-02"], efor: "M", adr: "yeni ADR: Nakit akışı ve senaryo sözleşmesi",
   kanit: "docs/designs/MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md:186; belge bölüm 8-D",
   guven: "yuksek"}

- {id: "CF-05", oncelik: "P1", tur: "eksik",
   iddia: "Plan–gerçek eşleşmesi tek PaymentId ile çözülemez.", gerekce: "Kısmi ödeme, toplu ödeme, komisyon, gecikme ve ters kayıt çoktan çoğa ilişki gerektirir.",
   oneri: "Amount taşıyan append-only settlement kur; tahmini yalnız net gerçekleşen tutar kadar azalt.",
   somut: "obligation_settlement; cash_entry bağı; manual-match/reverse API.",
   kabul: "1.000 planın 400'ü gerçekleşince gelecekte 600 kalır; 400 geri alınırsa kalan tekrar 1.000 olur.",
   bagimlilik: ["CSH-04","CF-02"], efor: "L", adr: "yeni ADR: Nakit akışı ve senaryo sözleşmesi",
   kanit: "src/UUSport.Api/Domain/Entities.cs:335; src/UUSport.Api/Domain/Entities.cs:216; belge bölüm 8-D",
   guven: "yuksek"}

- {id: "CF-06", oncelik: "P1", tur: "fikir",
   iddia: "İlk senaryo motoru deterministik olmalıdır.", gerekce: "Veri hacmi ve dağılımlar doğrulanmadan Monte Carlo sayısal kesinlik izlenimi verir.",
   oneri: "Baz/iyimser/temkinli için tutar, gecikme, katılım ve gider çarpanı kullan; olasılığı beklenen değerle karıştırma.",
   somut: "scenario/assumption; run; 7 gün/ay/3 ay/yıl API; karşılaştırma ekranı.",
   kabul: "Aynı snapshot ve parametre aynı hash/toplamı üretir; senaryo asıl defteri değiştirmez.",
   bagimlilik: ["CF-02","CF-05"], efor: "L", adr: "yeni ADR: Nakit akışı ve senaryo sözleşmesi",
   kanit: "belge bölüm 3.1-3 ve 8-D",
   guven: "yuksek"}

- {id: "CF-07", oncelik: "P1", tur: "eksik",
   iddia: "Nakit rakamının kaynağı ve hesap sürümü saklanmadan açıklanabilirlik eksik kalır.", gerekce: "Kaynak sonradan değişirse eski senaryo yeniden üretilemez.",
   oneri: "Kaynak referansı, asOf, parametre sürümü, formül sürümü ve override gerekçesini run snapshot'ına bağla.",
   somut: "forecast_run/line/source; drill-down; veri güncellik göstergesi.",
   kabul: "Dönem sonundaki her tutar kaynak olaya iner; eski run silinmiş/yenilenmiş varsayımla sessizce yeniden yazılmaz.",
   bagimlilik: ["CF-06"], efor: "M", adr: "yeni ADR: Nakit akışı ve senaryo sözleşmesi",
   kanit: "belge bölüm 8-D; src/UUSport.Api/Features/Reports/ReportEndpoints.cs:16",
   guven: "yuksek"}

- {id: "CF-08", oncelik: "P1", tur: "eksik",
   iddia: "Vadeyi, bankadaki gerçekleşmeyi ve rapor tarihini tek tarihe indirgemek nakit açığını gizler.", gerekce: "Hafta sonu valörü ve gecikme günleri aylık toplamda görünmez.",
   oneri: "Europe/Istanbul iş günü takvimi, due_date/expected_cash_date/actual_date ayrımı ve günlük hesap kullan.",
   somut: "business_calendar; forecast günlük noktalar; min_cash/first_shortfall.",
   kabul: "Ay sonu pozitif olsa bile ara gündeki negatif bakiye bulunur; tatil kaydırması kaynağıyla açıklanır.",
   bagimlilik: ["CSH-06","CF-06"], efor: "M", adr: "yeni ADR: Nakit akışı ve senaryo sözleşmesi",
   kanit: "src/UUSport.Api/Features/Cash/LoanService.cs:107; belge bölüm 3.1-3",
   guven: "yuksek"}

- {id: "TAX-01", oncelik: "P1", tur: "eksik",
   iddia: "Vergi simülasyonu tarih aralıklı parametre kadar onaylı şirket vergi profiline de ihtiyaç duyar.", gerekce: "Oran tek başına mükellefiyet, istisna, dönem ve ödeme gününü belirlemez.",
   oneri: "MM onaylı profil ve kural sürümleri kur; eksik parametrede sıfır yerine hesaplanamadı ver.",
   somut: "tax_profile; tax_parameter; tax_calendar; approval; simulation API.",
   kabul: "Sınır tarihlerinde doğru sürüm seçilir; aynı kapsamda çakışan kural reddedilir; onaysız çıktı simülasyon etiketlidir.",
   bagimlilik: ["CF-07"], efor: "M", adr: "yeni ADR: Vergi simülasyonu ve MM onayı",
   kanit: "docs/designs/MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md:82; belge bölüm 1-3",
   guven: "yuksek"}

- {id: "TAX-02", oncelik: "P1", tur: "risk",
   iddia: "KDV tahmini yalnız tahsilattan veya yalnız gönderilmiş faturadan türetilmemelidir.", gerekce: "Vergiyi doğuran olay ile nakit ve aktarım tarihi farklıdır; bekleyen satışlar gizlenemez.",
   oneri: "MM'nin vergilendirme tarihine göre kapsam belirle; belge mutabakatı eksikse kapsama açığını ayrıca göster.",
   somut: "VAT simulation; carry-forward; deductible eligibility; coverage warning.",
   kabul: "İki eş satışın farklı tahsilat günleri hesaplanan KDV'yi kendiliğinden değiştirmez; devreden bir kez mahsup edilir.",
   bagimlilik: ["ACC-05","TAX-01"], efor: "L", adr: "yeni ADR: Vergi simülasyonu ve MM onayı",
   kanit: "docs/designs/MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md:49; belge bölüm 8-D",
   guven: "yuksek"}

- {id: "TAX-03", oncelik: "P2", tur: "risk",
   iddia: "Nakit fazlası vergi matrahı değildir; kredi ve stok alımı düz nakit-gider hesabına sokulamaz.", gerekce: "Tahakkuk, satılan mal maliyeti, amortisman, KKEG ve mahsuplar gerekir.",
   oneri: "Gelir–maliyet–gider köprüsü ve MM düzeltmeleriyle kümülatif matrah simüle et; eksik kapsamı açık göster.",
   somut: "tax_adjustment; period_profit; provisional/corporate simulation; mahsup kimlikleri.",
   kabul: "Kredi anaparası gelire, taksit anaparası gidere girmez; önceki mahsup iki kez düşülmez; dönem sayısı sabit kodlanmaz.",
   bagimlilik: ["STK-01","TAX-01"], efor: "L", adr: "yeni ADR: Vergi simülasyonu ve MM onayı",
   kanit: "docs/designs/MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md:69; belge bölüm 8-D",
   guven: "yuksek"}

- {id: "TAX-04", oncelik: "P2", tur: "eksik",
   iddia: "Maaş ve kira planlarında net/brüt ayrımı olmadan vergi ve SGK mükerrer nakit çıkışı olur.", gerekce: "Brüt maaş ile net maaş+kurum ödemesi aynı anda toplanamaz.",
   oneri: "İlk sürümde MM onaylı dönem toplamlarını al; bordro motoru kurma; kira net/brüt ve kesinti profilini ayır.",
   somut: "payroll_summary; rent_profile; statutory obligation links.",
   kabul: "Net maaş ve ayrı SGK/vergi ödemeleri toplam işveren nakdine bağlanır; kira kesintisi ikinci kez giderleşmez.",
   bagimlilik: ["TAX-01","CF-04"], efor: "M", adr: "yeni ADR: Vergi simülasyonu ve MM onayı",
   kanit: "docs/designs/MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md:155; docs/designs/MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md:186",
   guven: "yuksek"}

- {id: "INT-01", oncelik: "P1", tur: "eksik",
   iddia: "Aktarım dosyası üretmek muhasebe kabulünü kanıtlamaz.", gerekce: "Gönderim, alındı, kabul, ret ve düzeltme durumları ayrı gerekir.",
   oneri: "Dengeli kanonik fiş, değişmez batch hash'i ve MM alındısı oluştur; tekrar gönderimi aynı kimlikle yönet.",
   somut: "integration.export_batch/item/receipt; validate/preview/export/ack endpoints.",
   kabul: "Borç=alacak; aynı batch tekrar gönderilince ikinci fiş oluşmaz veya açıkça tespit edilir; ret nedeni izlenir.",
   bagimlilik: ["ACC-04","ACC-05"], efor: "L", adr: "yeni ADR: Muhasebe aktarım sözleşmesi",
   kanit: "src/UUSport.Api/Domain/Entities.cs:145; belge bölüm 8-E",
   guven: "yuksek"}

- {id: "INT-02", oncelik: "P1", tur: "eksik",
   iddia: "Cari/üründeki tek hesap kodu tüm fiş satırlarını belirlemeye yetmez.", gerekce: "KDV, satış/iade, tahsilat, banka, POS ve giderin ayrı bağlantı kuralları vardır.",
   oneri: "Öncelikli ve tarih geçerli eşleme kuralları kur; belirsiz eşlemeyi durdur.",
   somut: "account_mapping; tenant chart; event/item/party/tax/account dimensions; dry-run.",
   kabul: "İki eş öncelikli kural hata verir; eski batch yeni hesap koduyla değişmez; örnek kodlar MM onaysız kullanılmaz.",
   bagimlilik: ["INT-01"], efor: "M", adr: "yeni ADR: Muhasebe aktarım sözleşmesi",
   kanit: "src/UUSport.Api/Domain/Entities.cs:58; src/UUSport.Api/Domain/Entities.cs:80; belge bölüm 8-E",
   guven: "yuksek"}

- {id: "INT-03", oncelik: "P1", tur: "iyilestirme",
   iddia: "GMS.NET için önce kabul edilmiş Excel/XML fiş pilotu, sonra gerektiğinde API uygundur.", gerekce: "Resmi Mikrokom sayfası dosya aktarımı ve API bildiriyor; kurulu sürüm/lisans/çağdaş API uyumluluğu bilinmiyor.",
   oneri: "Sentetik 10–20 fişlik dosyayı MM test ortamında doğrulat; API gereksinimini ölç.",
   somut: "GmsFileAdapter; encoding/decimal/date contract; acknowledgement reconciliation.",
   kabul: "MM programında borç/alacak, KDV ve cari kontrol toplamları birebir tutar; hata satırı geri döner.",
   bagimlilik: ["INT-01","INT-02"], efor: "M", adr: "yeni ADR: Muhasebe aktarım sözleşmesi",
   kanit: "belge bölüm 8-E; [Mikrokom yazılımcı desteği](https://www.mikrokom.com/destek-api.html)",
   guven: "orta"}

- {id: "INT-04", oncelik: "P0", tur: "risk",
   iddia: "GMS'ye ayrıca ulaşan e-fatura ile UU Sport satış fişi aynı geliri ikinci kez oluşturabilir.", gerekce: "Belgelendirme, ekonomik olay ve dış muhasebe kayıt sahipliği birlikte belirlenmeli.",
   oneri: "Belge UUID/numara, kaynak olay ve MM fiş numarasıyla dedup yap; olay başına tek muhasebeleştirme sahibi seç.",
   somut: "export_item source/document keys; inbound UBL registry; reconciliation report.",
   kabul: "GMS'ye önceden gelen faturaya bağlı olay ikinci satış/KDV fişi üretmez; yalnız gereken kapama aktarılır.",
   bagimlilik: ["ACC-04","INT-01"], efor: "M", adr: "yeni ADR: Muhasebe aktarım sözleşmesi",
   kanit: "docs/decisions/ADR-004_CARI_HAREKET_MODELI_SATIS_BELGE_TAHSILAT.md:39; belge bölüm 8-E",
   guven: "yuksek"}

- {id: "SAAS-01", oncelik: "P1", tur: "risk",
   iddia: "Kiracı ilk kayıttan seçiliyor; kimlikten türetilmiyor.", gerekce: "İkinci tenant eklendiğinde isteğin sahibi ile veri kapsamı ayrışır.",
   oneri: "Şimdi tek-kiracı modunda adet kontrolüyle fail-closed; SaaS öncesi doğrulanmış üyelikten tenant çöz.",
   somut: "TenantResolver; Program middleware sırası; tenant membership.",
   kabul: "İkinci tenant varken tek-kiracı modu başlamaz; SaaS testinde A kimliği B header'ı göndererek kapsam değiştiremez.",
   bagimlilik: [], efor: "M", adr: "yeni ADR: Kiracı bağlamı ve üyelik",
   kanit: "src/UUSport.Api/Data/TenantContext.cs:47; src/UUSport.Api/Data/TenantContext.cs:57; src/UUSport.Api/Program.cs:153",
   guven: "yuksek"}

- {id: "SAAS-02", oncelik: "P1", tur: "risk",
   iddia: "Identity tabloları ve kullanıcı yönetimi SaaS izolasyonuna hazır değil.", gerekce: "Kullanıcı listeleme/FindById tenant filtresiz; iş tablosu RLS listesi Identity'yi kapsamıyor.",
   oneri: "Küresel kimlik+tenant üyeliği veya tenant-sınırlı kullanıcı modelini seç; yönetim hedefini sunucuda sınırla.",
   somut: "UserEndpoints; identity membership/role; yetki matrisi.",
   kabul: "Tenant A yöneticisi B kullanıcısını göremez, pasifleştiremez veya MFA sıfırlayamaz; ortak login tasarımı açık olur.",
   bagimlilik: ["SAAS-01"], efor: "L", adr: "yeni ADR: Kiracı bağlamı ve üyelik",
   kanit: "src/UUSport.Api/Features/Users/UserEndpoints.cs:23; src/UUSport.Api/Features/Users/UserEndpoints.cs:101; src/UUSport.Api/Data/MigrationSecurity.cs:20",
   guven: "yuksek"}

- {id: "SAAS-03", oncelik: "P1", tur: "iyilestirme",
   iddia: "Session-level tenant GUC'si açık bağlantıda sıfırlanıyor; buna rağmen tüm işlem yolları için sözleşme/test eksik.", gerekce: "Bu kod tek başına kanıtlanmış havuz sızıntısı değildir; worker ve transaction dışı yollar ayrıca ele alınmalı.",
   oneri: "Tenant veri erişimini transaction-local bağlamla sarmala; bootstrap yolunu ayır; boş/yanlış bağlamı reddet.",
   somut: "TenantConnectionInterceptor; tenant unit-of-work; worker wrapper; pool tests.",
   kabul: "A→B→boş tenant sıralarında aynı havuzda çapraz satır yok; exception/cancel sonrası bağlam taşınmaz.",
   bagimlilik: ["SAAS-01"], efor: "L", adr: "yeni ADR: Kiracı bağlamı ve üyelik",
   kanit: "src/UUSport.Api/Data/TenantContext.cs:38; src/UUSport.Api/Data/TenantContext.cs:41",
   guven: "yuksek"}

- {id: "SAAS-04", oncelik: "P1", tur: "risk",
   iddia: "Tenant alanı ve RLS, yalnız ID kullanan yabancı anahtarların tenant tutarlılığını garanti etmez.", gerekce: "İlişkiler tenant_id ile birleşik değil; PostgreSQL referans bütünlüğü kontrolleri RLS'i aşar.",
   oneri: "Tenant tablolarında UNIQUE(tenant_id,id) ve aynı tenant'ı zorlayan composite FK ekle.",
   somut: "AppDbContext trade/payment/athlete/document relations; migration; cross-tenant FK test.",
   kabul: "A satırının B carisine/hesabına referansı app rolüyle ve RLS dışı doğrulamada reddedilir.",
   bagimlilik: ["SAAS-01","SEC-01"], efor: "L", adr: "yeni ADR: Kiracı bağlamı ve üyelik",
   kanit: "src/UUSport.Api/Data/AppDbContext.cs:99; src/UUSport.Api/Data/AppDbContext.cs:141; src/UUSport.Api/Data/AppDbContext.cs:337",
   guven: "yuksek"}

- {id: "SAAS-05", oncelik: "P2", tur: "iyilestirme",
   iddia: "Tek kurulum ve SaaS ayrı kod çatalları gerektirmez.", gerekce: "Şimdiden mikroservis veya müşteri başına şema küçük ekip yükünü artırır.",
   oneri: "Modüler monolit/paylaşımlı şema+RLS sürdür; tenant ayarları, sektör terimleri ve özellik politikalarını soyutla.",
   somut: "tenant_settings; feature_policy; company-owned storage namespace; provisioning command.",
   kabul: "Aynı paket tek-tenant ve iki sentetik tenant testini geçer; istemci bayrağı sunucu yetkisini aşamaz.",
   bagimlilik: ["SAAS-01","SAAS-02","SAAS-04"], efor: "L", adr: "ADR-001 korunur; yeni SaaS işletim ADR'si",
   kanit: "docs/decisions/ADR-001_TEKNOLOJI_YIGINI_VE_ISTEMCI_MIMARISI.md; belge bölüm 8-F",
   guven: "yuksek"}

- {id: "SAAS-06", oncelik: "P2", tur: "eksik",
   iddia: "Tek tenant geri yükleme, bütün PostgreSQL kümesini geri almaktan farklı bir işlemdir.", gerekce: "Ortak kümenin PITR'si diğer müşterileri de geçmişe döndürür.",
   oneri: "Tenant mantıksal dışa aktarımı ve izole geri yükleme/diff/import prosedürü tasarla; saklama ve silme istisnalarını yönet.",
   somut: "tenant_export manifest; restore staging; attachment hashes; retention job.",
   kabul: "A tenant geri dönüş provasında B değişmez; dosya/kayıt toplamları tutar; hukuki saklama engeli silmeyi durdurur.",
   bagimlilik: ["SAAS-04","OPS-01"], efor: "L", adr: "yeni ADR: SaaS veri yaşam döngüsü",
   kanit: "belge bölüm 8-F; docs/operations/UUSPORT_GELISTIRME_VERI_VE_CANLIYA_ALMA_PROSEDURU.md",
   guven: "yuksek"}

- {id: "SEC-02", oncelik: "P0", tur: "risk",
   iddia: "Loopback geliştirme ayarları LAN üretim güvenliği yerine geçmez; ilk kurulum anonim ve yarışa açıktır.", gerekce: "Cookie SameAsRequest HTTP'de Secure sağlamaz; boş Identity'de ilk kurulum uç noktası kullanıcı oluşturabilir.",
   oneri: "LAN öncesi HTTPS/Secure/host-proxy sınırını kur; bootstrap tek kullanımlık ve yerel yetkili süreç olsun.",
   somut: "Program cookie/HTTPS; AuthEndpoints.setup; ilk kurulum kilidi; deployment config.",
   kabul: "HTTP kimlik doğrulaması kapalı; iki eşzamanlı setup yalnız bir yönetici oluşturur; setup üretimde tekrar açılamaz.",
   bagimlilik: [], efor: "M", adr: "yok",
   kanit: "src/UUSport.Api/Program.cs:86; src/UUSport.Api/Program.cs:101; src/UUSport.Api/Features/Auth/AuthEndpoints.cs:47",
   guven: "yuksek"}

- {id: "SEC-03", oncelik: "P0", tur: "risk",
   iddia: "MFA cihaz hatırlama kodda kullanıcı seçimine bağlı değil; kurtarma süreci eksik.", gerekce: "rememberClient:true sabit; yönetici reset'i ek doğrulama gerektirmiyor.",
   oneri: "Açık hatırla seçeneği, son MFA zamanı kontrolü, tek kullanımlık hash'li kurtarma kodu ve cihaz iptali ekle.",
   somut: "AuthEndpoints login-mfa; UserEndpoints reset-mfa; device/session UI.",
   kabul: "Ortak cihazda seçim kapalıysa hatırlama çerezi yok; kurtarma kodu ikinci kez işlemez; reset/rol değişimi denetlenir.",
   bagimlilik: ["SEC-02"], efor: "M", adr: "yok",
   kanit: "src/UUSport.Api/Features/Auth/AuthEndpoints.cs:113; src/UUSport.Api/Features/Users/UserEndpoints.cs:99",
   guven: "yuksek"}

- {id: "SEC-04", oncelik: "P0", tur: "iyilestirme",
   iddia: "Oturum ve yetki kontrolleri olumlu; hassas eylem ve hata yollarının güvenlik kanıtı eksik.", gerekce: "1 dakika stamp doğrulaması ve roller var; giriş/başarısız giriş audit kapsamı görünmüyor.",
   oneri: "Giriş/MFA/rol/reset/export olaylarını kaydet; yüksek riskte tekrar MFA ve nesne düzeyi yetki testi ekle.",
   somut: "AuthEndpoints; UserEndpoints; audit; cookie/session invalidation; negatif API tests.",
   kabul: "Pasifleştirme/rol düşürme belgelenen sürede tüm oturumları etkiler; Muhasebe rolü yönetici API'sini çağıramaz.",
   bagimlilik: ["SEC-03","SEC-01"], efor: "M", adr: "yok",
   kanit: "src/UUSport.Api/Program.cs:80; src/UUSport.Api/Features/Auth/AuthEndpoints.cs:91; src/UUSport.Api/Features/Users/UserEndpoints.cs:74",
   guven: "yuksek"}

- {id: "SEC-05", oncelik: "P0", tur: "risk",
   iddia: "Tam exception ve serbest JSON audit özetleri kişisel veriyi loglara taşıyabilir.", gerekce: "Alan bazlı allowlist/redaksiyon sözleşmesi yok.",
   oneri: "Yapılandırılmış olay kodu ve kimlik referansı kullan; isim, telefon, belge içeriği ve sırları logdan çıkar.",
   somut: "WebPipeline exception logging; AuditWriter; retention/access; login audit.",
   kabul: "Sentetik hassas işaretler hata/log/export audit testlerinde içerik olarak görünmez; olay yine izlenir.",
   bagimlilik: ["SEC-01"], efor: "M", adr: "yok",
   kanit: "src/UUSport.Api/Infrastructure/WebPipeline.cs:42; src/UUSport.Api/Features/Audit/AuditWriter.cs:19",
   guven: "yuksek"}

- {id: "SEC-06", oncelik: "P0", tur: "risk",
   iddia: "Excel yüklemesinde sıkıştırılmış dosya sınırı tek başına kaynak tüketimini ve veri ifşasını engellemez.", gerekce: "15 MB sınırı var; workbook bellekte açılıyor ve satır DataJson önizlemede dönüyor.",
   oneri: "Satır/hücre/ZIP açılmış boyut/süre sınırı; tür doğrulama; tenant kullanıcı kotası; asgari veri önizlemesi ekle.",
   somut: "ImportEndpoints; ImportService.ReadWorkbook; export policy; concurrency quota.",
   kabul: "ZIP bombası/büyük hücreli dosya sınırlı kaynakla reddedilir; yetkisiz kullanıcı import içeriği veya export alamaz.",
   bagimlilik: ["SEC-05"], efor: "M", adr: "yok",
   kanit: "src/UUSport.Api/Features/Imports/ImportEndpoints.cs:11; src/UUSport.Api/Features/Imports/ImportEndpoints.cs:57; src/UUSport.Api/Features/Imports/ImportService.cs:512",
   guven: "yuksek"}

- {id: "SEC-07", oncelik: "P0", tur: "eksik",
   iddia: "Çocuk verisi için tek açık rıza kutusu ve yurt içi sunucu yeterli yönetişim değildir.", gerekce: "Amaç/hukuki sebep, veli yetkisi, özel nitelikli veri ve saklama farklı değerlendirilir.",
   oneri: "AV ile envanter, aydınlatma/rıza ayrımı, yetki doğrulama, saklama/imha ve ihlal planını ilk veri öncesi onayla.",
   somut: "privacy register; consent versions; request/incident workflow; erişim matrisi.",
   kabul: "Her alanın amacı/erişim/saklama süresi kayıtlı; rıza geri alma senaryosu zorunlu saklamayı körlemesine silmez.",
   bagimlilik: [], efor: "L", adr: "yeni ADR: Kişisel veri yaşam döngüsü",
   kanit: "docs/standards/UUSPORT_GUVENLIK_STANDARDI.md:174; docs/standards/UUSPORT_GUVENLIK_STANDARDI.md:186; belge bölüm 8-G",
   guven: "yuksek"}

- {id: "SEC-08", oncelik: "P0", tur: "risk",
   iddia: "Gerçek kaynağın ajan çalışma alanı içinde bulunması .gitignore ile giderilmez.", gerekce: "Prompt yasak klasörü açıkça belirtiyor; ana hafızadaki gerçek geliştirme verisi ifadesi sentetik veri kuralıyla çelişiyor.",
   oneri: "Sahip yetkili süreçle üretim/kaynak dosyalarını ayrı ACL'li alan ve hesaba taşımalı; ajan yalnız sentetik repo görmeli.",
   somut: "İşletim prosedürü; ajan filesystem yetkileri; ana hafıza kanıt düzeltmesi.",
   kabul: "Ajan hesabının gerçek kaynak/backup/secret yoluna OS erişimi reddedilir; yalnız Git dışı bırakmak kabul edilmez.",
   bagimlilik: ["SEC-07"], efor: "M", adr: "yok",
   kanit: "belge bölüm 1-8; docs/project-memory/UUSPORT_ANA_HAFIZA.md, 26–27 Eylül değişiklik kayıtları",
   guven: "yuksek"}

- {id: "OPS-01", oncelik: "P0", tur: "eksik",
   iddia: "Yedek varlığı geri dönebilme kanıtı değildir; hedef RPO/RTO ölçülmemiştir.", gerekce: "DPAPI'ye bağlı anahtarlar başka makinede otomatik açılmaz; dump ile WAL tek başına PITR zinciri değildir.",
   oneri: "Fiziksel base backup+kesintisiz WAL veya doğrulanmış mantıksal strateji; bağımsız değiştirilemez kopya ve anahtar kurtarma provası kur.",
   somut: "Windows scheduled backup; manifest; isolated restore; DPAPI/service account recovery runbook.",
   kabul: "Bağımsız makinede uygulama açılır; muhasebe toplamları ve hash tutar; RPO 15 dk/RTO 4 saat hedefi ölçümle kanıtlanır.",
   bagimlilik: ["SEC-01","SEC-05"], efor: "L", adr: "yok",
   kanit: "src/UUSport.Api/Program.cs:129; docs/operations/UUSPORT_GELISTIRME_VERI_VE_CANLIYA_ALMA_PROSEDURU.md, yedek ve geri yükleme; belge bölüm 5.6-4",
   guven: "yuksek"}

- {id: "OPS-02", oncelik: "P0", tur: "risk",
   iddia: "Geçmiş 53 test iddiası bu kodun temiz kurulum ve yetki doğruluğunu kanıtlamıyor.", gerekce: "Build sabit test DB'yi migrate eder; test desteği yeni tenant açar, her seferinde sıfır DB yaratmaz.",
   oneri: "CI'da ayrı temiz zincir/yükseltme yolu, runtime-role ACL, concurrency ve temel UI/PDF testlerini kapı yap.",
   somut: "tools/build-uusport.ps1; TestSupport; GuvenlikVeKuralTests; izole disposable DB fixture.",
   kabul: "Her PR temiz ve önceki sürüm DB'sinde geçer; SEC-01 için mevcut testin sonucu kanıtlanır; SkipTests üretim paketini onaylamaz.",
   bagimlilik: ["SEC-01","ACC-06"], efor: "M", adr: "yok",
   kanit: "tools/build-uusport.ps1:33; tests/UUSport.Tests/TestSupport.cs:29; tests/UUSport.Tests/TestSupport.cs:63; tests/UUSport.Tests/GuvenlikVeKuralTests.cs:40",
   guven: "yuksek"}

- {id: "OPS-03", oncelik: "P0", tur: "eksik",
   iddia: "Küçük bütçeli üretim için sürüm kanıtı, güvenlik taraması ve işletim uyarıları birlikte gerekir.", gerekce: "Paket klasörü kurulum kanıtı değildir; npm ci --no-audit ayrı tarama ihtiyacını bırakır.",
   oneri: "Commit/paket hash/migration manifest, bağımlılık ve secret taraması, disk/WAL/backup/TLS uyarısı ve geri dönüş planı kur.",
   somut: "CI workflow; release manifest; health/readiness; owner-run smoke/restore raporu.",
   kabul: "Kurulu sürüm ayrı raporlanır; kritik bulgu gate'i çalışır; disk/yedek hatası sahibine kişisel verisiz uyarı üretir.",
   bagimlilik: ["OPS-01","OPS-02"], efor: "L", adr: "yok",
   kanit: "tools/build-uusport.ps1:17; src/UUSport.Api/Program.cs:165; docs/operations/UUSPORT_GELISTIRME_VERI_VE_CANLIYA_ALMA_PROSEDURU.md",
   guven: "yuksek"}

- {id: "OPS-04", oncelik: "P1", tur: "eksik",
   iddia: "Açık tarayıcı sekmesi eski kodla yeni API'ye yazabilir.", gerekce: "Promptta yaşanan menü sorunu yalnız görünüm değil sürüm uyumluluğu sorunudur.",
   oneri: "API build/schema contract başlığı ve yeni sürüm uyarısı ekle; taslağı koru; uyumsuz yazmayı durdur.",
   somut: "api.ts; App.tsx; version endpoint; safe reload banner.",
   kabul: "Açık form varken dağıtımda veri kaybolmaz; uyumsuz komut açıklamalı reddedilir; kontrollü yenileme çalışır.",
   bagimlilik: [], efor: "M", adr: "yok",
   kanit: "belge bölüm 4, 27 Eylül eski JavaScript olayı; src/UUSport.Web/src/api.ts",
   guven: "yuksek"}

- {id: "UX-01", oncelik: "P0", tur: "risk",
   iddia: "Koyu tema ile yüksek kontrast tercihi birleşince ikincil metin kontrastı düşüyor.", gerekce: "prefers-contrast:more ink-3 değerini koyu temada da #2f3b49 yapıyor; #161d26 zeminde yaklaşık 1,49:1.",
   oneri: "Kontrast token'larını tema bazında ayır; bütün kombinasyonları çalışan UI'da doğrula.",
   somut: "styles.css dark ve prefers-contrast kuralları; light/dark × normal/more görsel test.",
   kabul: "Normal metin en az 4,5:1; büyük metin 3:1; hata/focus/disabled anlamı yalnız renge bağlı değil.",
   bagimlilik: [], efor: "S", adr: "yok",
   kanit: "src/UUSport.Web/src/styles.css:85; src/UUSport.Web/src/styles.css:86",
   guven: "yuksek"}

- {id: "UX-02", oncelik: "P1", tur: "iyilestirme",
   iddia: "Yoğun finans girişi için klavye akışı hız kazandırır; tabloyu elektronik tabloya dönüştürmek şart değil.", gerekce: "Tabs/dialog/focus temeli mevcut; seri girişte kontrollü yapıştırma ve özet hata eksik.",
   oneri: "Tab/Enter/Esc sözleşmesi, görünür komut araması, yapıştır–önizle–onayla ve taslak geri al ekle.",
   somut: "components.tsx; seri çek/satır ızgarası; error summary; shortcut help.",
   kabul: "Klavye ile 20 satır girişte odak kaybolmaz; posted işlem undo yerine gerekçeli ters kayıt açar.",
   bagimlilik: ["ACC-06"], efor: "M", adr: "yok",
   kanit: "src/UUSport.Web/src/components.tsx:103; src/UUSport.Web/src/components.tsx:140; docs/reviews/dis-yz-ekleri-2026-09-27/11-cek-portfoyu.png",
   guven: "yuksek"}

- {id: "UX-03", oncelik: "P1", tur: "iyilestirme",
   iddia: "Ekstre ve cari sayfasındaki etiketler hesap kapsamından daha iddialı; teknik ADR dili son kullanıcıya sızıyor.", gerekce: "Resmi sekmesi tam satır brütü gösterebilir; filtre anlamı ekranlar arasında değişiyor.",
   oneri: "Tek ortak rapor projection'ı, belirgin kapsam etiketi ve sakin özet/detay düzeni kullan; teknik metni yardım alanına taşı.",
   somut: "PartyPages official tab/Statement; WorkPages hesap hareketleri; PDF başlıkları.",
   kabul: "Cari, hesap hareketleri ve PDF aynı kapsam/tarih için aynı tutarı verir; kullanıcı beş görevde kapsamı doğru tarif eder.",
   bagimlilik: ["ACC-01","ACC-03","ACC-04"], efor: "M", adr: "yok",
   kanit: "src/UUSport.Web/src/pages/PartyPages.tsx:166; src/UUSport.Web/src/pages/PartyPages.tsx:231; docs/reviews/dis-yz-ekleri-2026-09-27/06-hesap-hareketleri.png",
   guven: "yuksek"}

- {id: "UX-04", oncelik: "P2", tur: "fikir",
   iddia: "Ürünün görsel imzası süs yerine açıklanabilir nakit dayanma çizgisi olabilir.", gerekce: "Kullanıcının temel sorusu ne zaman ve neden nakit açığı oluşacağıdır.",
   oneri: "Günlük bakiye çizgisi, dip nokta ve kaynak çekmecesi; mobilde role göre Bugün/İşler/Belgeler/Hesabım kur.",
   somut: "CashflowDashboard; Calendar; ScenarioCompare; Expo rol navigasyonu/fotoğraf kuyruğu.",
   kabul: "Grafiksiz tablo eşdeğeri var; renk dışında çizgi/stil/etiket ayrımı var; hocanın API cevabında mali bakiye yok.",
   bagimlilik: ["CF-07","CF-08","SEC-07"], efor: "L", adr: "ADR-001 korunur",
   kanit: "belge bölüm 3.1 ve 8-H; docs/reviews/dis-yz-ekleri-2026-09-27/01-genel-bakis-ve-menu.png",
   guven: "orta"}

- {id: "DATA-01", oncelik: "P0", tur: "risk",
   iddia: "Excel aktarımı aynı adlı carileri birleştirebilir ve atlanan sporcu satırının bakiyesini yeniden toplayabilir.", gerekce: "Telefon bulunamazsa ada düşüyor; sporcu eşlemesi doğum yılını içermiyor; bakiye her satırdan toplanıyor.",
   oneri: "Belirsiz eşleşmeyi kullanıcı karar kuyruğuna al; kaynak satır kimliği ve mükerrer tutar kontrolü uygula.",
   somut: "ImportService.ApplyStudents; import_row resolution; opening candidate review.",
   kabul: "Aynı adlı farklı veliler birleşmez; yinelenen satır açılış adayını artırmaz; onaysız aday ledger'a yazılmaz.",
   bagimlilik: ["SEC-06","ACC-06"], efor: "L", adr: "yok",
   kanit: "src/UUSport.Api/Features/Imports/ImportService.cs:348; src/UUSport.Api/Features/Imports/ImportService.cs:359; src/UUSport.Api/Features/Imports/ImportService.cs:373",
   guven: "yuksek"}

- {id: "BIZ-01", oncelik: "P1", tur: "itiraz",
   iddia: "Logo'nun modül zenginliğini aynen almak akademi için doğruluk ve işletim maliyetini artırır.", gerekce: "Öncelik küçük işletmenin mutabakatlı bakiyesi ve nakit açığını önceden görmesidir.",
   oneri: "M1 güvenlik/doğruluk, M2 hazine, M3 plan, M4 MM aktarımı sırasını uygula; kapsamı özellik kapılarıyla sınırla.",
   somut: "Yol haritası; module readiness; kullanıcı kabul senaryoları.",
   kabul: "Her modülün sahibi, çıkış ölçütü ve kapalı eksikleri belli; maliyetsiz stoktan kâr veya mutabakatsız hesaptan kesin nakit iddia edilmez.",
   bagimlilik: ["OPS-01","ACC-01","CSH-01"], efor: "S", adr: "ADR-005 genişlemesi ayrı onay",
   kanit: "belge bölüm 8-A ve 8-J; docs/decisions/ADR-005_ILK_SURUM_KAPSAMI_VE_ELLE_FATURA.md",
   guven: "yuksek"}

- {id: "BIZ-02", oncelik: "P1", tur: "iyilestirme",
   iddia: "Tek sahip ve ajanlarla geliştirilen üründe kontrol sorumluluğu açık atanmalıdır.", gerekce: "Ajan testi MM mutabakatının, şirket kabulünün veya hukuk değerlendirmesinin yerine geçmez.",
   oneri: "Sahip–MM–AV–teknik sorumlu kabul matrisi ve aylık kapanış görevleri belirle.",
   somut: "release checklist; reconciliation sign-off; runbook owners; readiness dashboard.",
   kabul: "Her kapının isimli onaylayanı ve kanıtı var; kişi yoksa ilgili özellik üretime açılmaz.",
   bagimlilik: ["OPS-03","INT-01","SEC-07"], efor: "S", adr: "yok",
   kanit: "belge bölüm 2 ve 8-J; docs/operations/UUSPORT_GELISTIRME_VERI_VE_CANLIYA_ALMA_PROSEDURU.md",
   guven: "yuksek"}
```

## 3 VERI_MODELI
Bütün aşağıdakiler sözde DDL'dir; migration tasarımı/onayı değildir. Mevcut tabloyu sırf ad tercihinden dolayı yeniden adlandırma.

Ortak `T`: `id uuid PK, tenant_id uuid NOT NULL, created_at timestamptz, created_by uuid, UNIQUE(tenant_id,id)`; tenant tablolarında ENABLE+FORCE RLS; ilişkiler `(tenant_id,ref_id)` ile composite FK. Kodlarda ADR-003, tutarda `numeric(18,2)`, ara hesapta `numeric(18,6)`, miktarda `numeric(18,4)`, oranda `numeric(9,4)`; API para metni. Taslak güncellemesi `version bigint` ile optimistic concurrency; kesinleşmiş finansal hareketler append-only ve ters kayıt. Global vergi şablonları ayrı salt okunur katalogdur; tenant istisnası ayrı tablo, NULL-tenant ile belirsiz erişim yok.

| Sözde DDL | Amaç / kısıt |
|---|---|
| `core.tenant_settings(T, timezone text, currency char(3), sector_template text, min_cash numeric(18,2), version bigint)` | Varsayılan Europe/Istanbul ve TRY; diğer para birimleri ilk sürümde kapalı. |
| `identity.tenant_membership(T, user_id uuid, role_code text, status text, UNIQUE(tenant_id,user_id))` | Küresel kimlik tercih edilirse tenant üyeliği; login ve tenant yönetim yetkileri ayrılır. |
| `finance.operation(T, command_type text, idempotency_key text, request_hash text, status text, UNIQUE(tenant_id,command_type,idempotency_key))` | Aynı isteği tek kez uygular; aynı anahtar farklı payload ile reddedilir. Finansal kayıtlarla tek transaction. |
| `billing.document_link(..., covered_quantity numeric(18,4), covered_economic_amount numeric(18,2), document_amount numeric(18,2), reverses_link_id uuid)` | Ekonomik kapsam ve belgenin gerçek tutarı ayrı; son parça yuvarlama artığını alır. İptal geçmiş kapsamı silmez. |
| `billing.reconciliation_issue(T, source_id uuid, issue_kind text, difference numeric(18,2), resolution_operation_id uuid NULL, status text, approved_by uuid NULL)` | Fatura/ekonomik tutar ve KDV toplamı farklarının onaylı çözümü; yalnız notla kapanmaz. |
| `finance.financial_institution(T, code text, name text)` | Banka kuruluşu, gerçek para hesabından ayrılır. |
| `finance.cash_account(..., institution_id uuid NULL, purpose text, currency char(3), include_available boolean, version bigint)` | cash/bank/pos_receivable/card_payable/loan_payable/control amaçları; kullanılabilir bayrağı izinli türe göre zorlanır. |
| `finance.account_relation(T, from_account_id uuid, to_account_id uuid, relation_type text, effective_from date, effective_to date NULL)` | POS yatış, kredi kullandırım, kredi/kart ödeme hesabı bağlantıları; çakışan aktif ilişki engellenir. |
| `finance.cash_journal(T, operation_id uuid, event_type text, occurred_on date, recorded_at timestamptz, reverses_id uuid NULL, UNIQUE(tenant_id,operation_id,event_type))` | Hazine alt defteri üst kaydı; ticari cari defteriyle ortak operasyon kimliği. |
| `finance.cash_entry(T, journal_id uuid, account_id uuid, debit numeric(18,2), credit numeric(18,2), source_line_id uuid NULL, CHECK(one_positive_side))` | Kasa/banka, POS alacağı ve finansman yükümlülükleri. Journal dengesi komut düzeyi ve deferred DB kontrolüyle korunur; tarafsız kontrol hesabı resmi gelir hesabı değildir. |
| `finance.account_opening(T, account_id uuid, effective_on date, amount numeric(18,2), evidence_ref text, approved_by uuid NULL, operation_id uuid NULL)` | Mutabakatlı hesap açılışı; cari açılışından ayrıdır; bir onay bir journal üretir. |
| `finance.account_reconciliation(T, account_id uuid, statement_asof date, external_balance numeric(18,2), internal_balance numeric(18,2), status text, approved_by uuid)` | Banka ekstresi/kasa sayımı farkı ve kapanış kanıtı. |
| `finance.loan_event(T, loan_id uuid, event_type text, principal numeric(18,2), interest numeric(18,2), tax_amount numeric(18,2), fee numeric(18,2), operation_id uuid)` | Kullandırım, erken kapama, yapılandırma ve ters kayıt; eski plan sürümü korunur. |
| `finance.loan_installment(..., plan_version int, bsmv numeric(18,2), kkdf numeric(18,2), obligation_id uuid, UNIQUE(tenant_id,loan_id,plan_version,installment_no))` | Sözleşmeden alınan veya onaylı parametreyle üretilen taksit; PaymentId ödenmişlik kaynağı olmaktan çıkar. |
| `finance.card_statement(T, card_account_id uuid, period_start date, cut_on date, due_on date, statement_total numeric(18,2), minimum_due numeric(18,2), version int)` | Harcama, taksit ve ekstre borcu; asgari ödeme bakiye borcu ortadan kaldırmaz. |
| `finance.pos_settlement(T, pos_account_id uuid, source_operation_id uuid, gross numeric(18,2), fee numeric(18,2), net numeric(18,2), blocked_until date NULL, expected_on date, actual_journal_id uuid NULL)` | Brüt = komisyon + net + açıklanmış diğer kesintiler; kısmi yatış için alt satırlar. |
| `finance.check_event(T, check_note_id uuid, event_type text, counterparty_id uuid NULL, occurred_on date, operation_id uuid, reverses_event_id uuid NULL)` | Alınan/verilen yönüne uygun yaşam döngüsü; ciro alıcısı, tahsil, karşılıksız ve rücu ilişkileri. |
| `finance.obligation(T, direction text, amount numeric(18,2), currency char(3), due_on date, expected_cash_on date, certainty text, lifecycle text, source_version int, supersedes_id uuid NULL)` | Kesin veya tahmini tahsilat/ödeme beklentisi; kendi başına cari borç yazmaz. |
| `finance.obligation_source(T, obligation_id uuid, source_kind text, source_id uuid, source_part text, source_version int, UNIQUE(tenant_id,source_kind,source_id,source_part,source_version))` | Kaynak ve aktif temsil; satış→çek/kart dönüşümünde eski planın yerine geçen ilişki. Polimorfik kaynaklar registry+komut doğrulamasıyla korunur. |
| `finance.obligation_settlement(T, obligation_id uuid, cash_entry_id uuid, amount numeric(18,2), reverses_id uuid NULL)` | Çoktan çoğa net gerçekleşme; toplam kalan sınırı kilit altında kontrol edilir. |
| `finance.collection_plan(T, party_id uuid, athlete_id uuid NULL, package_ref text NULL, plan_version int, approved_by uuid, valid_from date, valid_to date)` | Aidat tahakkuku yapmayan tahsilat niyeti; kişisel veriyi senaryo metnine kopyalamaz. |
| `finance.recurrence_rule(T, category text, amount numeric(18,2), frequency text, effective_from date, effective_to date NULL, business_day_policy text, version int)` | Aylık kira/abonelik vb.; occurrence tekilliği kural+versiyon+dönem. |
| `finance.scenario(T, name text, base_snapshot_id uuid, status text, version int)`; `finance.scenario_assumption(T, scenario_id uuid, target_kind text, target_id uuid NULL, parameter text, value_json jsonb, effective_from date, effective_to date, reason text)` | Sonlu allowlist parametreleri; serbest SQL/formül çalıştırma yok; baz veriyi değiştirmez. |
| `finance.forecast_run(T, scenario_id uuid, asof timestamptz, start_on date, end_on date, engine_version text, input_hash text, status text)`; `finance.forecast_line(T, run_id uuid, flow_key text, cash_on date, amount numeric(18,2), source_refs jsonb, explanation jsonb, UNIQUE(tenant_id,run_id,flow_key))` | Yeniden üretilebilir hesap ve kaynak zinciri; kişisel adları snapshot'a çoğaltma. |
| `finance.tax_profile(T, tax_kind text, period_rule text, effective_from date, effective_to date NULL, approved_by uuid)`; `finance.tax_parameter(T, code text, valid_range daterange, value numeric(18,6), official_source text, approved_by uuid)` | Vergi/SGK koşulları ve MM onayı; aynı kural kapsamı ve tarihte çakışma engeli. |
| `finance.tax_adjustment(T, period text, tax_kind text, amount numeric(18,2), adjustment_type text, evidence_ref text, approved_by uuid)` | KKEG, istisna, zarar ve mahsuplar; aynı mahsup referansı tek kez kullanılır. |
| `integration.account_mapping(T, rule_version int, priority int, conditions jsonb, debit_or_credit text, account_code text, valid_range daterange)` | Kart varsayılanı + vergi/olay/hesap türü kuralları; sonuca birden fazla kural düşerse hata. |
| `integration.export_batch(T, target text, period text, mapping_version int, content_hash text, status text, created_by uuid)`; `integration.export_item(T, batch_id uuid, source_event_id uuid, document_key text NULL, external_posting_key text, payload jsonb)`; `integration.export_receipt(T, batch_id uuid, external_reference text, accepted_at timestamptz NULL, rejection_json jsonb)` | Gönderim, kabul ve ret kanıtları. Düzeltme yeni batch; kabul edilen payload değişmez. |
| `inventory.valuation_entry(T, stock_movement_id uuid, unit_cost numeric(18,6), cost_amount numeric(18,2), status text, reverses_id uuid NULL)` | Miktar defterinden ayrı maliyet izi; maliyet kesinleşmeden kesin kâr iddiası yok. |

Cash journal hazine alt defteridir, GMS genel muhasebe defteri değildir. Banka varlığı debit-credit, kart/kredi yükümlülüğü credit-debit sunulur; tek işaretli “bakiye” ile hepsi toplanmaz. Cari etkisi gereken işlem aynı operation altında mevcut ledger_entry üretir. Belge ve dağıtım gene ledger kaynağı olamaz.

Yeni tasarım için endpoint aileleri [ONERI]: `POST /api/treasury/transfers`, `POST /api/treasury/openings/{id}/approve`, `POST /api/obligations/{id}/settlements`, `POST /api/forecasts/runs`, `GET /api/forecasts/runs/{id}/sources`, `POST /api/accounting-exports/{id}/acknowledgements`. Bunlar mevcut uçlar olduğu iddiası değildir. Her yazmada tenant/rol, idempotency, concurrency ve audit aynı iş komutunun parçasıdır.

## 4 NAKIT_AKISI_MOTORU
### 4.1 Kaynak → ara model
| Kaynak | Ara model / gerçekleşme | Tek sayım kuralı |
|---|---|---|
| Mutabakatlı kasa+banka | Başlangıç kullanılabilir nakdi | POS alacağı, kredi limiti, kart bakiyesi ve bloke dahil değil. |
| Açık cari satış/alım | Vade satırlarından obligation | İadeler ve net kapamalar düşülür; belge tekrar plan üretmez. |
| Onaylı tahsilat planı | Plan/tahmin; ledger etkisi yok | Satış veya tahsilat oluşunca planın ilgili kısmı tüketilir. |
| POS slip/raporu | Net valör beklentisi | Müşteri tahsilatı POS alacağıdır; bankaya yatış tek kullanılabilir nakit artışıdır. |
| Şirket kartı | Ekstre/taksit ödeme yükümlülüğü | Harcamada banka nakdi yok; ekstre ödemesi bir nakit çıkışı. |
| Kredi sözleşmesi | Kullandırım girişi + taksit parçaları | Anapara gelir/gider değildir; her taksit bir nakit çıkışı. |
| Alınan/verilen çek | Durum ve yönüne bağlı beklenti | Çeke dönüşen borcun eski planı artık aktif değildir; ciro nakit yaratmaz. |
| Kira/maaş/SGK/vergi | Sürümlü occurrence | Onaylı gerçek tahakkuk/ödeme aynı tahmini değiştirir veya tüketir. |
| İç virman | Hesap bazında iki bacak | Şirket toplamında net sıfır; aynı gün değilse transferdeki para ayrı izlenir. |

Dört bağımsız boyut: `lifecycle=draft/active/partially_settled/settled/cancelled`; `certainty=contractual/estimated`; `document_coverage_amount`; `export_status=not_ready/ready/sent/accepted/rejected`. “Gerçekleşen” cash_entry olaylarıdır. Planın tamamını gerçekleşen diye yeniden ekleme.

### 4.2 Hesap sırası ve formüller
1. Tenant, TRY, dönem, saat dilimi, asOf ve en son mutabakat tarihi sabitlenir. Güvenilmez açılışta sonuç “mutabakatsız tahmin” olarak işaretlenir.
2. AsOf'a kadar cash journal'dan hesap bakiyeleri hesaplanır; POS/bloke/borç/limit kullanılabilir toplamdan ayrılır.
3. Aktif kaynaklar tekil flow_key ile çıkarılır. İade, ters kayıt, iptal, çeke/karta dönüşüm uygulanır. Eksik vade “tarihsiz” sepetinde kalır; görünmez olmaz.
4. `kalan_plan = plan_tutari − net_gerceklesme − onayli_iptal`. Net gerçekleşme pozitif settlement eksi ters settlement'dır. Negatif kalan kontrol hatasıdır; max(0) ile sessiz gizlenmez.
5. Kesin vadeli planlar ve tahmini planlar ayrı katmanlarda tutulur. Senaryo override'ı yalnız hedef, tarih ve parametre kapsamını değiştirir.
6. Günlük hesap: `C_s(d)=C0 + Σ giriş_s(t) − Σ çıkış_s(t)`, `asOf < t ≤ d`. C0 içindeki gerçekleşenler tekrar eklenmez.
7. `donem_sonu=C_s(bitis)`; `dip=min(C0,C_s(d))`; `ilk_acik=min{d:C_s(d)<esik}`; `fonlama_ihtiyaci=max(0,esik−dip)`. Eşik sıfır veya onaylı güvenlik tamponudur.
8. Günlük noktalar haftalık, aylık, üç aylık ve yıllık görünüme gruplanır; dip nokta daima günlük seriden gelir. Bir yıllık görüntü yıllık toplamla ara açığı örtmez.
9. Engine_version/input_hash/parametre sürümüyle run saklanır; kaynak metrikleri, bilinmeyen tutarlar ve varsayım kapsamı birlikte döner. Sunucu decimal kullanır; istemci toplamı yeniden hesaplamaz.

İlk sürüm: tek para birimi, tam tutarlı deterministik senaryolar, elle onaylı planlar, günlük hesap, basit iş günü takvimi. Haftalık 13 hafta görünümü üç aylık takvim görünümünün yerine geçmez; ayrıca sunulabilir. Tarih aralığı bitişinin dahil/haricî sözleşmesi API'de açık olmalı.

### 4.3 Senaryo kuralları ve örnekler
| Senaryo | Dönüşüm | Değişmeyen / kontrol |
|---|---|---|
| Baz | Onaylı plan ve beklenen ödeme günleri | Defter bakiyesi sabit; baz varsayım seti sürümlü. |
| İyimser | Açıkça seçilmiş tahsilatlar daha erken; yeni kayıt beklentisi artar | Keyfi olarak bütün alacaklara %100 tahsil garantisi verilmez. |
| Temkinli | Seçili tahsilatlar gecikir/azalır; giderler artar | Kesin borcun anaparası tahsilat varsayımıyla azaltılmaz. |
| 500.000 TL kredi | Kullandırım gününde +500.000, masraf ve vergiler ayrı; taksitler tarihlerinde eksi | Mevcut kredi tekrar eklenmez; anapara kâr değildir; net kullandırım açıklanır. |
| 20 öğrenci ayrılır | Yalnız gelecek yenileme/yeni satış tahmini hedeflenir | Doğmuş cari borç veya kesin alacak otomatik silinmez; iptal/iade ayrıca onay ister. |
| Kira %40 artar | Etkin tarihten sonraki kira kuralı ×1,40 | Geçmiş kira ve imzalı geçmiş dönem snapshot'ı değişmez. |

Kabul seti: C0=100.000, giriş=20.000, çıkış=30.000 ⇒ 90.000. POS 10.000−200 ⇒ valörde 9.800; öncesinde kullanılabilir artış 0. Kartla 6.000 alış ⇒ banka 0; ekstre ödemesi ⇒ −6.000 bir kez. Plan 1.000, gerçekleşen 400 ⇒ sonraki plan 600. Hesaplar arası 5.000 virman ⇒ şirket toplamı 0. Ay sonunda artı olup ara gün −10.000 olan senaryo açık uyarısı vermelidir. Bütün örnekler sentetiktir.

Olasılık, tahsilat gerçekleşme oranı ve istatistiksel güven aralığı aynı kavram değildir. İlk sürümde “%95 güven” etiketi yok; en erken/en geç tarih ve baz/temkinli farkı gösterilir. Geçmişten tahmin sonradan eklenirse zaman bölmeli backtest, hata metriği ve örnek sayısı zorunludur.

### 4.4 Vergi ve personel ödeme alt modeli
Aşağıdaki formüller **sade simülasyon iskeletidir**, beyanname motoru veya şirket için onaylı vergi hesabı değildir. Oran/takvim/istisna sabitleri verilmemiştir.

- KDV: uygun dönemin `H=hesaplanan KDV`, `I=indirime uygun KDV`, `D=onaylı önceki devreden`; sade durumda `ödenecek=max(0,H−I−D)`, `yeni_devreden=max(0,I+D−H)`. Tevkifat, iade, istisna ve düzeltme ayrı kurallardır; bu iskelete rastgele netleştirilmez. Bütün alış KDV'si otomatik indirilebilir değildir. Dayanak kontrolü: [GİB KDV Genel Uygulama Tebliği](https://gib.gov.tr/mevzuat/kanun/436/teblig/9047) ve [GİB Kanun metni](https://ordu.gib.gov.tr/mevzuat/kanun/436).
- Belgesi bekleyen ekonomik olay ayrı “vergi kapsamı eksik” tutarıyla görünür. Yalnız belgeli filtreyi seçmek ekonomik olayın olası yasal yükümlülüğünü ortadan kaldırmaz. Vergiyi doğuran olay/tarih ve indirim şartını MM belirler.
- Geçici vergi: `kümülatif ticari sonuç + KKEG − uygun istisna/indirim/zarar` üzerinden profil kuralı; önceki dönem tahakkuk/ödeme ve mahsup uygunluğu MM sözleşmesine göre düşülür. İş akışı, hesaplanan vergi ile ödenecek nakdi ayırır.
- Kurumlar vergisi: yıllık onaylı matrah × geçerli oran; uygun geçici vergi/tevkifat ve diğer mahsuplar referanslarıyla bir kez düşülür. Asgari vergi ve özel hükümler kapsam dışıysa sonuç “tam hesap değil” der. “Yılda üç geçici vergi dönemi” kodlanmamalı: 2026 GİB rehberi dördüncü dönemi de ele alıyor; şirketin geçerli takvimi MM tarafından teyit edilmelidir. [GİB 2026 Kurumlar Vergisi Beyan Rehberi](https://cdn.gib.gov.tr/api/gibportal-file/file/getFile?objectKey=DUYURU%2FUNIVERSAL%2F2026%2F2026_Kurumlar_Vergisi_Beyan_Rehberi.pdf).
- Maaş: ilk aşamada MM onaylı net ödeme ve işveren toplam maliyeti; SGK, gelir/damga kesintileri ve teşvikler ayrı tarihli yükümlülükler. Net maaş + kurumlara net ödeme nakit planına girer; brüt maaş bunlara tekrar eklenmez. Bordro kişisel ayrıntısı yetkisiz kullanıcıya açılmaz.
- Kira: sözleşme net/brüt, kiralayan türü, varsa stopaj/KDV profili; yalnız uygun basit durumda netten brüte dönüşüm parametreli formülle yapılır. Kira ödemesi ve vergi kesintisi ayrı alıcılara/son günlere bağlanır.
- Kredi: anapara/faiz/BSMV/KKDF/masraf ayrı; sözleşmenin onaylı ödeme planı tercih edilir. Vergi türünün o krediye uygulanabilirliği varsayılmaz. Erken kapama/yapılandırma eski planı silmez, kalan beklentiyi yeni sürümle değiştirir.

Hesaplanan tahmin → MM teyitli tahakkuk → gerçek ödeme zincirinde aynı obligation korunur veya açık supersedes bağı kurulur. Vergi ödeme gününün değişmesi önceki tahmini ikinci kez nakde eklemez.

### 4.5 Açıklanabilir ekran yanıtı
Örnek: “31 Ekim temkinli bakiye: 90.000 TL; başlangıç mutabakatı 27 Eylül; 12 kesin giriş, 8 kesin çıkış, 3 tahmin; 2 kaynağın vadesi eksik.” Detay: kaynak kimliği → kalan tutar → beklenen gün → uygulanan varsayım → parametre sürümü → hesap. Run paylaşımı gerçek veri içerebileceğinden export yetkisi ve erişim kaydı gerekir.

## 5 SAAS_KAPILARI
**ŞİMDİ — tek şirket sürümünde ucuz önlemler**

- SAAS-01: Tek-tenant modunda birden fazla aktif tenant'ı reddet; auth ve tenant sınırını açık servis arayüzüne al. Tenant ID'yi serbest istemci başlığından güvenilir kabul etme.
- SAAS-04: Yeni bütün ilişkileri composite tenant FK ile kur; mevcut ilişkiler için veri taraması ve sürümlü düzeltme planı hazırla. Sonradan yaygın veri karışmasını ayıklamak daha pahalıdır.
- SAAS-03: Bağlantı/transaction/worker bağlamı için tek desen ve havuz regresyon testi oluştur. PostgreSQL session GUC güvenilir uygulama bağlamıdır; SQL injection'a karşı bağımsız güvenlik sınırı değildir.
- SAAS-05: Tenant saat dilimi, para birimi ve kurum bilgisi; kod tekilliği tenant içinde. Küresel vergi şablonu ile tenant onayı ayrılır. “Veli/Sporcu” terimleri sektör sözlüğünden gelebilir; cari ve ödeme çekirdeği değişmez.
- SEC-07/SAAS-06: Dosya anahtarı tenant/nesne kimliğini taşır; her indirme sunucuda nesne yetkisi kontrol eder. Yol tahmin edilemezliği tek başına yetki değildir. Şirket hesapları, veri sözlüğü ve standart JSON/CSV+belge manifesti çıkış hakkını korur.
- OPS-02: Test verileri tenant A/B içerir. İşlem sayısı artınca raporlar sayfalı ve toplu sorgulu olur; AllLedger'ın cari başına sorgu desenini kaldırma kabulü sorgu sayısı ve sentetik yükle ölçülür (`AccountingQueries.cs:83–106`).
- OPS-03: Bir sürüm paketi, config ile tek şirket/SaaS; müşteri için fork veya ayrı elle migration yok.

**SONRA — ikinci tenant/internet/mobil öncesinde tamamlanması gerekenler**

- SAAS-01/02: Üyelik ve tenant seçimi, tenant-sınırlı yönetici, destek görevlisi ayrı rol; tenant kullanıcı adı/e-posta sözleşmesi. Global Identity modelinde tenantlar arasında hesap varlığını ifşa etmeyen login akışı.
- SAAS-03/04: Paylaşımlı şema+RLS önerilir. Tenant başına DB yalnız bağımsız restore, sözleşmesel izolasyon veya ölçülmüş ölçek gerektirirse ayrı ADR; tenant başına şema varsayılan değil.
- SAAS-05: İdempotent provisioning; plan/özellik/limit sunucuda, tenant kotası ve komşu tenant kaynak tüketimi testleri. Lisanslama ve abonelik tahsilatı ayrı modül; bugünün ilk canlısına eklenmez.
- SAAS-06: Mantıksal tenant export, izole restore-diff, hukuki saklama altında silme/anonimleştirme; support erişimi süreli, gerekçeli, onaylı ve audit'li. Tüm müşteri verisini geliştirmeye indiren destek yolu olmaz.
- OPS-01: Ortak küme yedeği tenant self-service restore değildir. Tenant bazlı RPO/RTO ve dosya depolama tutarlılığı ayrıca sözleşmeye yazılır.

Teknik dayanak: PostgreSQL RLS, superuser/BYPASSRLS ve referans bütünlüğü istisnalarına sahiptir; FORCE tek başına bütün erişim yollarını kapatmaz. Transaction-local ayar yalnız transaction ömründe geçerlidir; bu yüzden SELECT'ler dahil bütün tenant veri erişimi aynı kapsamdan geçmelidir. [PostgreSQL 18 RLS](https://www.postgresql.org/docs/18/ddl-rowsecurity.html), [PostgreSQL ayar kapsamı](https://www.postgresql.org/docs/18/config-setting.html).

## 6 GUVENLIK
### 6.1 Tehdit modeli ve en olası 10 sızıntı yolu
Varlıklar: çocuk/veli kimliği, ilişki bilgisi, finans kayıtları, dışa aktarımlar, yedekler, kimlik doğrulama anahtarları. Sınırlar: tarayıcı/mobil→API, API→DB, runtime→owner, tenant→tenant, üretim→geliştirme, şirket→tedarikçi. STRIDE: hesap ele geçirme (S), tutar/durum tahrifi (T), işlemi inkâr (R), veri ifşası (I), ZIP/rapor kaynak tüketimi (D), admin/tenant yetki yükseltme (E).

| Yol / STRIDE | Kontrol ve neden | Kabul deneyi |
|---|---|---|
| 1. Yedek/WAL/anahtar kopyası — I/T | Şifreli, bağımsız hesapta değiştirilemez kopya; restore hesabı ayrı; anahtar envanteri | Uygulama hesabı yedeği silemez; yeni makinede yetkili kurtarma ölçülür. |
| 2. Log/telemetri — I/R | SEC-05 allowlist, sınırlı saklama ve erişim; auth/finans olayları korelasyon ID | Sentetik telefon/TCKN işaretinin logda olmaması; başarısız MFA audit'i bulunması. |
| 3. Excel/PDF/indirme — I | Asgari alan, rol ve nesne yetkisi, audit, süreli indirme, formül enjeksiyonuna karşı metin hücresi | Yetkisiz export reddi; =,+,-,@ başlayan serbest metin çalışmaz, gerçek sayılar sayı kalır. |
| 4. Tarayıcı/cache/yazdırma — I | API no-store mevcut; ortak cihazda cihaz hatırlama kapalı; logout sonrası erişim engeli; print dosyası kullanım politikası | Geri tuşu ve eski download URL ile veri alınamaz; indirilen dosyanın merkezi iptal edilemeyeceği kabul edilir. |
| 5. RDP/VDS yönetimi — S/E | İnternete doğrudan RDP yok; VPN/gateway+MFA; DB portu kapalı; şirket sahipli hesaplar | Dış ağdan 3389/5432 erişilemez; yalnız HTTPS ve izinli yönetim yolu çalışır. |
| 6. Mobil kaybı/fotoğraf — I/S | Kısa erişim token'ı, döndürülen refresh token, secure storage, cihaz iptali; fotoğraflar şirkette; offline minimum | Kayıp cihazın token'ı reddedilir; iptal edilmiş kullanıcı bekleyen fotoğrafı yükleyemez. |
| 7. AI geliştirme/source klasörü — I | Ayrı OS hesabı/ACL, sentetik repo, gerçek log ve DB kopyası yok | Ajan hesabı gerçek kaynak/backup/secret okuyamaz; hatalı ek paylaşımı prosedürü denenir. |
| 8. İçeriden personel — I/T/E | İşi kadar yetki, export kısıtı, onay ayrılığı, audit append-only | Muhasebe rolü yönetici reset'i yapamaz; iki onaylı açılışta kendi kendine onay yok. |
| 9. Tenant/nesne atlama — I/E | Üyelik, RLS, composite FK, tenant storage yetkisi | Tenant A kimliği B UUID'leriyle GET/POST/export/support uçlarında başarısız olur. |
| 10. npm/NuGet/CI — E/I/D | Lockfile, bağımlılık/secret taraması, ayrılmış build hesabı, SBOM ve imzalı/hash'li paket | Kritik zafiyet politika kapısını tetikler; build üretim sırrını göremez. |

### 6.2 Faz kapıları
| Faz | Kontrol → test |
|---|---|
| Faz 1: LAN | SEC-01 ACL ve finansal bütünlük → temiz/yükseltilmiş DB üzerinde app-role testleri; SEC-02 HTTPS ve bootstrap → HTTP/login/setup yarış testi; SEC-03/04 MFA ve yetki → kayıp cihaz/rol düşürme senaryosu; SEC-05/06 log ve dosya sınırı → sentetik ifşa/ZIP deneyi; OPS-01 → bağımsız restore; SEC-07/08 → veri envanteri ve OS erişim kontrolü. |
| Faz 2: internet+personel mobil | Reverse proxy trust/host/IP sınırı, Secure cookies/HSTS uygunluğu, kullanıcı+IP bazlı rate limit ve arka plan OCR kotası → dış ağ taraması ve yük testi; token rotasyonu/cihaz iptali → replay testi; dosya karantinası/yerel OCR → MIME/zararlı dosya/tenant indirme testi; API sürüm uyumu/Expo OTA → imzalı güncelleme, runtime uyumsuzluğu ve rollback testi. |
| SaaS | SAAS-01..06 tam tenant ayrımı, destek erişimi ve provisioning → A/B saldırgan testleri; tenant restore → B etkilenmez; kaynak kotası → bir tenant ağır raporu diğerini hizmet dışı bırakmaz; sözleşmeli saklama/export → hesap kapatma provası. |

Korunacak mevcut kontroller: CSP'de script-src self; unsafe-inline yalnız style tarafında. SameSite=Strict + özel yazma başlığı, permissive CORS olmadığında anlamlı CSRF korumasıdır; “CSRF yok” diye raporlanmamalı. Üretimde Origin doğrulaması ve proxy sınırı ayrıca test edilir. 20/dakika IP limiti ortak ofis NAT'ında meşru kullanıcıları sınırlayabilir; IP+kullanıcı anahtarlı ve uç bazlı politika gerekir. MFA policy'sinde etkinlik kontrolü ile hassas işlemin yakın zamanda doğrulanmış MFA kanıtı ayrılmalıdır; burada kanıtlanmış bir MFA bypass iddiası yoktur.

Disk şifreleme çalınan diske, alan şifreleme seçili hassas alanlara, TLS aktarıma, yedek şifreleme kopyaya karşıdır; hiçbiri yetkili ama ele geçirilmiş uygulamanın bütün okuma riskini ortadan kaldırmaz. Anahtar üretim DB'siyle aynı yedek parolasına teslim edilmez. Alan şifrelemesi yalnız gerekçeli TCKN/sağlık gibi alanlarda, ayrı anahtar yaşam döngüsüyle; sağlık verisi mevcut kapsamda gerekmiyorsa hiç toplanmaz. [PostgreSQL şifreleme seçenekleri](https://www.postgresql.org/docs/18/encryption-options.html).

### 6.3 KVKK ve çocuk verisi
- Aydınlatma ile açık rıza ayrı süreçlerdir; gerekli olmayan pazarlama/fotoğraf izni hizmetin şartı haline getirilmez. Hangi işleme için hangi hukuki sebebin kullanılacağını AV doğrular. Şubat 2026 Kurul kararı bu ayrımı ayrıca vurgular. [KVKK 2026/347 duyurusu](https://www.kvkk.gov.tr/Icerik/8710/veri-sorumlulari-tarafindan-acik-riza-ve-aydinlatma-metinlerinin-ayri-ayri-duzenlenmesi-gerektigi-hakkinda-kisisel-verileri-koruma-kurulunun-18-02-2026-tarihli-ve-2026-347-sayili-ilke-kararina-iliskin-kamuoyu-duyurusu).
- Veli ilişkisi ve temsil yetkisi doğrulanır; personel yalnız atandığı sporcuları görür. Çocuk verisi yüksek koruma ister; tüm çocuk verisini otomatik “özel nitelikli veri” saymak yerine sağlık gibi alanları ayrıca sınıflandır.
- Aydınlatma sürümü/teslimi, gerektiğinde rıza kanıtı, başvuru kimlik doğrulaması, yetki devri/velayet değişimi, erişim düzeltme ve silme talepleri için kayıtlı iş akışı kur. Saklama süreleri muhasebe/hukuk amacı bazında AV+MM tarafından onaylanır.
- Türkiye'de DB olması uzaktan destek, yabancı telemetri, push payload, hata izleme veya yurt dışı yedek aktarımını kendiliğinden engellemez. Her tedarikçi ve aktarım yolu için geçerli hukuki mekanizma incelenir; “Türkiye dışı her aktarım yasaktır” genellemesi yapılmaz. [KVKK yurt dışına aktarım açıklaması](https://www.kvkk.gov.tr/Icerik/2053/Yurtdisina-Aktarim).
- İhlal planı: öğrenme zamanı, kapsam, containment, kanıt koruma, ilgili kişiler ve Kurul bildirimi sorumlusu. Kurula bildirim için öğrenmeden itibaren gecikmeksizin ve en geç 72 saat kuralını operasyon alarmına bağla; ilgili kişi bildirimini ayrı izle. [KVKK veri ihlali bildirimi](https://www.kvkk.gov.tr/Icerik/5362/Veri-Ihlali-Bildirimi).

### 6.4 Yedek ve işletim
Windows'ta servis hesaplarını ayır; uygulama hesabı owner parolasını ve yedek silme yetkisini taşımasın. Günlük mantıksal dump taşınabilir ek kopya olabilir; PITR için fiziksel base backup ve eksiksiz WAL zinciri gerekir. WAL arşiv gecikmesi/disk büyümesi izlenmeden 15 dakika RPO vaat edilmez. PostgreSQL belgeleri fiziksel PITR ile pg_dump kullanımını ayrı ele alır. [PITR](https://www.postgresql.org/docs/18/continuous-archiving.html), [pg_dump](https://www.postgresql.org/docs/18/app-pgdump.html).

İlk restore provası sentetik verili ayrı makine/ortamda; ileride üretim restore provası yalnız yetkili üretim güvenlik sınırında, ajanların erişmediği yerde yapılır. DB+belgeler+konfigürasyon+anahtar kurtarma+uygulama sürümü birlikte sınanır. Aylık prova raporu süre, son kurtarılabilen işlem, hash, migration seviyesi ve defter mutabakatını içerir. 8 GB hedefte OCR/CI aynı anda üretimle yarışmamalı; RAM/disk/restore süresi ölçülüp gerekirse yükseltilmeli. Prometheus/ELK gibi ek platformlar ilk şart değil; Windows görevleri, PostgreSQL ölçümleri ve PII içermeyen basit alarm yeterli başlangıçtır.

## 7 TASARIM
Bağlam: Windows tarayıcısında React muhasebe arayüzü; gelecekte Expo iOS/Android. Tasarım tezi: **Önce ne kadar borç/nakit kaldığını ve güvenilirliğini göster; kullanıcıyı kaynak işleme bir adımda götür.** İnceleme 13 statik PNG ve CSS/TSX'e dayanır; canlı klavye, ekran okuyucu, zoom ve yazdırma doğrulaması yapılmadı.

### 7.1 Beş mercek, sırasıyla
1. **Erişilebilirlik — UX-01 kritik.** Koyu+yüksek kontrastta hesaplanan 1,49:1 düzeltilmeli. Açık ikincil metin #5b6878/beyaz yaklaşık 5,68:1; normal koyu #9aa8b8/#161d26 yaklaşık 7,00:1; genel “koyu tema okunmuyor” iddiası doğru değil. Rozetlerde metin+renk, görünür focus, ARIA tabs ve native dialog olumlu. %200 zoom, %400 reflow, ekran okuyucu tablo başlıkları, hata özeti, modal odak dönüşü ve print çıktısı sonraki canlı kabul testleridir. Web normal metin hedefi 4,5:1; büyük metin eşiği yalnız kalın olmasına göre düşürülmez. Kaynak: `accessibility.md › Vision`: “Convey information with more than color alone.”; `typography.md`, `color.md`.
2. **Platform geleneği — UX-02.** Windows Ctrl ve tarayıcı alışkanlıklarını koru; F5/F11/Ctrl+L gibi kısayolları finans komutuna çevirme. Görünür komut araması ve isteğe bağlı, değiştirilebilir çakışmasız kısayollar sun. Mac pencere düğmesi/menu bar taklidi yok. Enter satır onayı yalnız bağlam belirginken; çok satırlı metinde yeni satır. Kaynak: `keyboards.md › Best practices`: “Respect standard keyboard shortcuts.”; `references/cross-platform.md`.
3. **Görsel hiyerarşi ve zanaat — UX-03/04.** 01'in iki seviyeli güncel menüsü korunur; geçmiş PNG menüleri sorun sayılmaz. Cari başlığında net borç/alacak, kapsam ve tarih; sonra birincil eylem, sonra tablolar. Tutar sağa hizalı/tabular numerals; borç/alacak ayrı, tarih sabit format; durum küçük fakat okunur. Yardım metnini 12 px altına küçültmek yerine yoğunluğu düzenle. Kaynak: `layout.md › Visual hierarchy`: “Use progressive disclosure to make layouts cleaner and easier to interact with.”
4. **Etkileşim — UX-02.** Yapıştırılan Excel verisinde satır bazlı önizleme ve hata; yanlış satırın yerinde düzeltilmesi; yükleme boyunca tekrarlanan tıklama idempotent. Taslakta geri al, kesinleşmiş finans kaydında gerekçeli ters işlem. Kapanan dialog odak başladığı düğmeye döner. Kaynak: `entering-data.md › Best practices`: “Dynamically validate field values.”
5. **İçerik dili — ACC-04/UX-03.** “Resmi” yerine neyin kanıtlandığını söyle: Belgelenen / MM'ye Gönderilen / MM Kabul Etti. “Dağıtım” açıklaması “Tahsilatı borca eşleştir”; eksik sonuç “Hesaplanamadı: vade eksik”; teknik ADR ve tenant dilini son kullanıcı ekranından kaldır. Bu dil önerileri ürün yargısıdır, Apple'a atfedilen hukuki tanım değildir.

Sekiz ilkenin karşılığı: Purpose→nakit yeterliliği; Agency→senaryo kopyala/geri dön; Responsibility→veri kapsamı ve belirsizlik; Familiarity→Windows/web; Flexibility→klavye/zoom/mobil; Simplicity→önce özet; Craft→doğru tutar/etiket/hiza; Delight→kaynağı anlaşılır tek hareketle bulma. Dekoratif cam/gradient önerilmiyor.

### 7.2 Ekran ve desenler
| Ekran | Öneri ve kabul |
|---|---|
| Cari / ekstre (02–05) | Üç görünüm aynı sunucu projection'ı; belgeli tutar ve toplam ayrı; tarih, kapsam, üretim zamanı çıktıda kalır. PDF'de kutu gizlenebilir, filtre anlamı gizlenemez. Çok sayfa/başlık tekrarı/toplam/boş sayfa yeniden test edilmeli; ek PDF bulunmadığından mevcut çözüm doğrulanmadı. |
| Hesap Hareketleri (06) | Sunucuda sayfalama ve tüm filtre sonucunun toplamı; sayfa toplamından ayrı etiket. Toplu cari sorgusu; “gösterilen 50 satır” ile “tüm sonuç” ayrımı. |
| Kasa/Kredi/Çek (09–11) | Hesap türü ve kullanılabilirlik görünür; kredi anapara/gelecek maliyet ayrı; çek satırında yön+durum+son eylem. Yanlış yön eylemi yalnız gizlenmez, API'de reddedilir. |
| Excel (13) | Önizleme, belirsiz eşleşme, bakiye adayı onayı, uygulama özeti; sayı/isim yerine kaynak satır kimliğiyle iz. Geri alma bağımlı finans olayını sessiz silmez. |
| Nakit panosu | Üstte kullanılabilir bugün, dönem sonu, dip/tarih, veri güncelliği. Ortada günlük çizgi ve sıfır/tampon; altta kesin/tahmin ayrımlı olay listesi. |
| Ödeme takvimi | Haftalık liste varsayılan; ay ızgarası alternatif. Geciken, tarihsiz ve yaklaşan ayrı; sürükleme gerçek vade değiştirmez, yalnız senaryo beklenen tarihini onayla değiştirir. |
| Senaryo karşılaştırma | Aynı asOf/ufukta üç ince çizgi; fark köprüsü kategori bazında; sağda değişen varsayımlar. Veri yokken sahte sıfır grafik yok. |

İmza öğe: **nakit dayanma çizgisi** — bugün, dip nokta ve ilk açık günü tek çizgide; tıklamada tutarı oluşturan birkaç olay. Tahmin çizgisi kesikli; kesin/gerçekleşmiş düz ve etiketli. Grafikle eşdeğer erişilebilir tablo ve metin özeti şarttır. Kaynak: `charting-data.md › Best practices`: “Make every chart in your app accessible.”

Mobil: role uygun sabit sekmeler; finans personelinde Bugün/İşlemler/Belgeler/Hesabım, hocada Bugün/Gruplarım/Belgeler/Hesabım. Hoca cariyi API'den alamaz. Fotoğraf: çek→kalite kontrolü→şirkete şifreli yükleme→önce QR→yerel OCR→alanları kullanıcı doğrulasın→belge eşleştir→kaydet. QR adresini sunucudan körlemesine açma (SSRF); izinli kaynak/sıkı URL politikası. OCR sonucu otomatik kesin kayıt değildir. Bağlantısız durumda kısa ömürlü cihaz kuyruğu, yükleme sonrası yerel temizleme ve cihaz kaybı politikası; fotoğrafın tüketici bulutuna otomatik yedeklenmediği cihaz akışında doğrulanır.

## 8 ENTEGRASYON
### 8.1 Logo Tiger 3 ile kapsam eşlemesi
Bu tablo **UU Sport için kavramsal tasarım eşlemesidir**; Tiger'ın iç tablolarının veya belirli lisans modülünün doğrulandığı anlamına gelmez.

| Alan | UU Sport'ta minimum | Şimdilik alınmayacak |
|---|---|---|
| Cari | Tek kaynak defter, net kapama, vade/yaşlandırma, mutabakat, onaylı açılış | Serbest sınırsız cari fiş ve ikinci “resmi cari” kartı |
| Malzeme/hizmet | Kart ayrımı, tek depo, sayım/fire, ağırlıklı ortalama maliyet | Üretim reçetesi, MRP, seri/lot ve çok ambar |
| Satış/satınalma | Ekonomik satır, iade, iskonto yetkisi, belge bağlantısı | Karmaşık teklif/sipariş/sevkiyat zinciri |
| Fatura | GİB'de kesilen belgenin numara/tutar/KDV kaydı, iptal ve fark mutabakatı | İlk sürümde entegratör/e-belge düzenleme motoru |
| Kasa/banka | Açılış, hesap defteri, virman, masraf, banka mutabakatı | Çok döviz ve otomatik kur değerleme |
| Kart/POS/kredi | Tür+rol bağlantısı; POS alacağı, kart/kredi borcu, vadeli ödeme | Tek bakiye alanında hepsini nakit saymak |
| Çek/senet | Yön/durum, ciro tarafı, tahsil/ödeme ve geri dönüş izi | İleri hukuki takip otomasyonu |
| Finans planları | Açık tutara dayalı kesin plan, hafif tahsilat planı, senaryo | Aidat tahakkukunu sessizce geri getirmek |
| Muhasebe bağlantısı | Tarihli eşleme, dengeli kanonik fiş, MM kabul alındısı | Resmi yevmiye/e-Defter uygulaması |
| Raporlar | Cari/hesap mutabakatı, kaynak izli nakit, yaşlandırma | Güvenilmez veriden çok sayıda hazır BI raporu |

ADR-004'ün ekonomik olayı bir kez kaydetmesi bu kapsam için sağlamdır. “Logo gibi olsun” gerekçesiyle faturaya tekrar cari borç yazdırmak yanlış olur. Eksikler: vade, parasal belge kapsamı, dönem kesiti, noncash/finansman işlemlerinin açık sınıflandırması ve dış muhasebe mutabakatıdır.

**Prompt bölüm 6 doğrulama sonucu:**

1. “Kredi Kartı Hesabı” ile müşteri POS alacağı ve şirket kredi kartı borcu aynı şey kabul edilemez. Tiger sürümüne ait resmi kullanım kılavuzundan bu özel tür/geri ödeme adlarını doğrulayamadım; kullanıcının ekran/kılavuz örneği gelmeden UU Sport modelini bu adlara kilitleme.
2. Hesap bağlantısı fikri uygundur; “02 → 150.02.001” örneği resmi sürüm belgesiyle doğrulanmadı ve akademinin MM hesap planı değildir.
3. “Altı maliyet yöntemi” listesi bu incelemede resmi Tiger kaynağından doğrulanmadı. LIFO dahil bu listeyi Türkiye'de mali uygunluk onayı gibi kullanma; yöntem seçimi STK-01 ve MM kararıdır.
4. Rakipte eşini bulamamak benzersizlik kanıtı değildir. “Resmi/gayri resmi” etiketinin rakiplerde e-fatura mükellefiyetine eşit olduğu iddiası da doğrulanmadı.
5. GMS API ve Excel/XML fiş aktarımı resmi Mikrokom sayfasında doğrulandı; kurulu sürüm/lisans/şema uyumu doğrulanmadı. [Mikrokom yazılımcı desteği](https://www.mikrokom.com/destek-api.html).
6. Logo'nun resmi ekosistem sayfası banka ve POS hareket aktarımını desteklenen entegrasyon alanı olarak gösteriyor; bu, Tiger'ın her lisansında aynı modülün bulunduğu kanıtı değildir. [Logo banka ekstresi aktarımı](https://www.logo.com.tr/logo-ekosistem-cozumleri/online-banka-ekstre-aktarimi).

### 8.2 Olay → kanonik fiş
Hesap numaraları verilmez; aşağıdaki hesap rolleri MM'nin onaylı planına eşlenir. UU Sport cari etkisi ile GMS fişi ayrı izdüşümlerdir.

| Olay | Kanonik fiş örüntüsü | Çift kayıt önleme |
|---|---|---|
| Satışın muhasebeleştirilmesi | Borç müşteri; alacak satış geliri + hesaplanan KDV | Fatura GMS'ye başka kanaldan geliyorsa ikinci satış fişi üretme; mevcut fişe referans. |
| Mal/hizmet alışı | Borç stok/gider/varlık + uygun indirilecek KDV; alacak satıcı | Nakit ödeme ikinci gider değildir; belge farkı çözülmeden otomatik aktarım yok. |
| Satış/alım iadesi | Orijinal olayın uygun ters/iadeye özgü fişi | Orijinal kaynak/fiş bağı zorunlu; kabul edilmiş fiş yerinde değiştirilmez. |
| Nakit/havale tahsilatı | Borç kasa/banka; alacak müşteri veya MM'nin avans hesabı | Dağıtım ayrıca gelir veya tahsilat fişi üretmez. |
| POS tahsilatı | Borç POS alacağı; alacak müşteri | Bankaya henüz nakit girmemiştir. |
| POS yatışı | Borç banka net + komisyon/gider; alacak POS alacağı brüt | Vergi/komisyon belgesine göre ek hesaplar MM kuralıyla; müşteri tekrar kapatılmaz. |
| Şirket kartıyla tedarikçi ödemesi | Borç satıcı; alacak kart borcu | Satın alma olayındaki gider tekrarlanmaz. |
| Kart ekstresi ödeme | Borç kart borcu; alacak banka | Faiz/masraf varsa ayrı kaynak olay, aynı gider tekrar yok. |
| Kredi kullandırım | Borç banka; alacak kredi borcu; masraf ayrı | Kredi para girişi satış geliri değildir. |
| Kredi taksiti | Borç kredi anapara borcu + faiz/vergi/masraf; alacak banka | Anapara gider değildir; taksit kaydı tek operation. |
| Banka virmanı | Borç hedef banka; alacak kaynak banka | Şirket nakdi ve cari toplamı değişmez; transferdeki tutar gerekirse ara hesap. |
| Alınan/verilen çek | MM'nin çek hesaplarına transfer; tahsilde/ödemede banka karşılığı | İlk modelde belge/çek teslimi ile nakit gerçekleşmesi ayrılır; cari kapamanın hangi aşamada olduğu ADR'de kararlaştırılır. |
| Ciro | Çek varlığı ve ilgili satıcı borcunun noncash kapaması | Banka nakdi üretmez; alıcı/rücu/iptal izi olmadan açılmaz. |
| Vergi/SGK/maaş ödeme | Borç onaylı ilgili yükümlülük; alacak banka | Tahakkuk fişi ve ödeme fişi farklıdır; bordro gideri yeniden yazılmaz. |
| Açılış/düzeltme | Yalnız MM onaylı açılış/karşı hesap kuralı | Önceki yıl kapanışını ve zaten aktarılmış açılışı mükerrer gönderme. |

**Belgesiz olayların ele alınması:** Otomatik resmi fiş batch'i yalnız üzerinde uzlaşılan muhasebeleştirme koşullarını sağlayan olayları içerir. Ancak bütün bekleyen ekonomik olaylar ayrı MM kontrol listesine dahil olur; vergi matrahından saklamak veya beyan dışı bırakmak üzere filtre kurulmaz. “Kabul edildi” MM'nin referansıyla gösterilir; UU Sport bu etiketi hukuki doğruluk garantisine dönüştürmez.

### 8.3 Kanonik sözleşme ve dönüştürücüler
Kanonik kayıt: `schema_version, tenant_company_ref, batch_id, journal_id, source_event_id, operation_id, document_uuid/no/date, economic_date, posting_date, currency, lines[{account_code,party_ref,debit,credit,tax_code,source_line_ref}], mapping_version, reversal_of, totals, content_hash`. Para string, ISO tarih, UTF-8, satır sırası kararlı; açıklamada gereksiz çocuk/veli bilgisi yok. Journal borç/alacak eşitliği, kaynak tekilliği, dönem ve eşleme eksiksizliği export önkoşuludur.

Sıra:
1. MM onaylı örnek + GMS Excel/XML adaptörü; 10–20 sentetik fişte satış/iade/avans/POS/kredi/fark/iptal/tekrar aktarım.
2. GMS API ancak dosya sürecinin ölçülen darboğazı varsa. Eski API dokümanı COM/ActiveX ve farklı dillerle kullanım izleri taşıyor; REST veya .NET 10 uyumu varsayılmaz. Gerekirse şirket kontrolünde ayrı Windows adaptör servisi, HTTPS kuyruk ve idempotency; istemciler DB'ye bağlanmaz. [Mikrokom API dokümanı](https://www.mikrokom.com/docs/aktarim/MikroKomAPI.pdf).
3. Logo/Mikro/Luca/Zirve adaptörü yalnız gerçek müşteri ve kabul edilmiş format olduğunda; aynı kanonik çekirdek, program başına fixture ve alındı sözleşmesi.

GMS'nin e-Defter ürünü yevmiye/berat ve imza akışını üstlenir; UU Sport kanonik JSON/XML'i hukuki e-Defter sayılmaz. UBL-TR fatura verisidir; doğrudan UU Sport cari defteri değildir. [Mikrokom e-Defter](https://www.mikrokom.com/urunler/urunler-edefter.html).

İçe aktarım sırası: banka ekstresi CSV/XLSX ve POS raporu → UBL belge eşleştirme → MM mizanı kontrol amaçlı. Dosya hash'i tek başına yeterli değil; banka işlem referansı/hesap/tarih/tutar gibi iş anahtarları ve UBL UUID tekilliği gerekir. Eşleştirme önce kesin kurallar, sonra öneri; belirsiz eşleşme insan onayı. Mizan ithali kendiliğinden yeni cari açılışı üretmez.

## 9 YOL_HARITASI
Süreler bir uygulayıcı ajan için planlama aralığıdır; kabul/tedarikçi beklemesi dahil değildir. M0 dışındaki geliştirme bu raporla yetkilendirilmiş olmaz.

- M0: Karar ve güvenli çalışma alanı | içerik: [BIZ-01, BIZ-02, SEC-08] | çıkış kriteri: rapor triage'ı, sorumlular, P0 kapsamı ve gerçek veri erişim sınırı onaylı | tahmini süre: 1–2 gün.
- M1: Gerçek veri öncesi doğruluk ve güvenlik | içerik: [SEC-01..08, ACC-01..07, CSH-03..06, CSH-08, DATA-01, UX-01, OPS-01..03] | çıkış kriteri: açık P0 yok; atomiklik/ters kayıt/ACL/Excel/ekstre testleri, HTTPS ve bağımsız restore kanıtı; henüz tamamlanmayan kredi/kart/çek eylemleri API seviyesinde kapalı | tahmini süre: 12–20 gün; kredi alt defteri M2'ye bırakılırsa kredi modülü kapalı.
- M2: Mutabakatlı hazine | içerik: [CSH-01, CSH-02, CSH-05, CSH-07..10, ACC-08, UX-02, UX-03, OPS-04] | çıkış kriteri: kasa/banka açılış ve hareketleri dış ekstre/sayımla tutarlı; POS/kart/kredi/çek senaryoları tek sayılıyor; geçiş kontrol toplamı sıfır fark | tahmini süre: 10–20 gün.
- M3: Nakit planı ve açıklanabilir senaryo | içerik: [CF-01..08, TAX-01, UX-04 pano kısmı] | çıkış kriteri: bölüm 4 kabul seti, günlük dip/7 gün/ay/3 ay/yıl, kaynak ayrıntısı ve aynı input aynı output kanıtı; aidat tahakkuku oluşmuyor | tahmini süre: 8–15 gün.
- M4: Mali müşavir mutabakatı ve GMS pilotu | içerik: [INT-01..04, ACC-04, ACC-05] | çıkış kriteri: sentetik dosya GMS testinde kabul; borç/alacak/KDV/cari toplamları birebir; alındı ve tekrar aktarım kontrolü | tahmini süre: 5–10 gün + MM beklemesi. Eşleme sözleşmesi M1'de hazırlanabilir; otomatik export bu kapıya kadar kapalı.
- M5: Maliyet ve vergi simülasyonu | içerik: [STK-01, TAX-02..04, STK-02 ihtiyaç varsa] | çıkış kriteri: maliyet ve onaylı vergi parametreleri; MM örnek hesaplarıyla fark toleransı açık; kapsam dışı haller hesaplanamadı olarak görünür | tahmini süre: 8–15 gün + MM kabulü.
- M6: Personel mobil ve VDS | içerik: [SEC Faz 2, UX-04 mobil, OPS-03/04] | çıkış kriteri: dış erişim güvenlik testi; şirket içi QR/OCR; cihaz iptali; store/OTA sürüm ve rollback kanıtı; sahada rol bazlı pilot | tahmini süre: 3–6 hafta.
- M7: Veli / sonra SaaS | içerik: [SAAS-01..06, SEC-07, BIZ-02] | çıkış kriteri: veli–çocuk yetki bağı ayrı kabul; SaaS için iki sentetik tenant izolasyonu, provisioning, tenant restore/export, sözleşme ve destek erişimi | tahmini süre: ayrı keşif sonrası; bugün güvenilir toplam verilemez.

P0, eksik bir gelecek modülünün hemen yazılması demek değildir: INT-04 için export'u, CSH-05/08 için tamamlanmamış finans işlemini sunucuda kapalı tutmak geçici kapıdır; hatalı çalışan özelliğe uyarı yazısı eklemek yeterli değildir. SAAS P1'leri ikinci tenant öncesinde, mobil kontroller Faz 2 öncesinde zorunlu kapıya dönüşür.

En büyük beş risk:
1. SEC-01 + ACC-06: Defter değişmezliği ve yarış koşulları; etki yanlış finans kaydı, sahibi teknik sorumlu.
2. ACC-01..05 + INT-04: Yanlış “resmi” bakiye ve mükerrer muhasebe; sahibi ürün sahibi+MM.
3. CSH-01/05/07/08 + CF-02: Mutabakatsız veya iki kez sayılmış nakde göre karar; sahibi ürün sahibi.
4. SEC-07/08 + OPS-01: Çocuk verisinin sızması veya geri yüklenememesi; sahibi veri sorumlusu+teknik sorumlu.
5. BIZ-01 + OPS-02: Çok modülü test/operasyon kapasitesinden hızlı açmak; sahibi ürün sahibi.

Ertelenecekler: mikroservis, Kubernetes, müşteri başına şema, Monte Carlo, tahsilat için LLM kararları, tam bordro/yevmiye/e-Defter, üretim/çok depo, döviz ve kur farkı, talep olmadan beş muhasebe adaptörü, lisans/abonelik pazaryeri. E-belge entegratörü ve aidat tahakkuku ADR-005 kapsamında kapalı kalır.

## 10 ADR_ADAYLARI
Numaralar öneridir; kayıt anında güncel decisions dizini kontrol edilmeli. Bu rapor hiçbir ADR'yi onaylamaz.

- ADR-006: Belge kapsamı, dönem kesiti ve muhasebe aktarım durumu | seçenekler: boolean resmi / miktar-tutar kapsamı+ayrı aktarım ekseni | önerilen: ikincisi | gerekçe: ACC-01..05/08; ADR-004'ün oransal görünümünü açıklaştırır; maliyet L.
- ADR-007: Kredi ve finansman muhasebesi | seçenekler: banka carisine sıradan ödeme / ayrı borç alt defteri ve kontrollü cari ilişkisi | önerilen: ayrı alt defter | gerekçe: CSH-05; anapara/gelir/gider ayrımı; ADR-004'e finansman/çek noncash etkileri açık eklenmeli; maliyet L.
- ADR-008: Kasa/banka/POS/kart ve çek modeli | seçenekler: yalnız parent hiyerarşisi / tür+bağlantı+append-only hesap defteri | önerilen: ikincisi | gerekçe: CSH-01/02/07..10; kullanılan nakit ve yükümlülük ayrımı; maliyet XL.
- ADR-009: Finansal komut bütünlüğü | seçenekler: dağınık SaveChanges / tek transaction+idempotency+concurrency | önerilen: ikinci | gerekçe: ACC-06, CSH-03/04; maliyet L.
- ADR-010: Nakit planları ve senaryo motoru | seçenekler: cari tahakkuk / ledger'dan bağımsız obligation+settlement / yalnız Excel | önerilen: obligation+deterministik snapshot | gerekçe: CF-01..08; ADR-005 aidat kararı korunur; maliyet XL.
- ADR-011: Vergi simülasyonu ve MM onayı | seçenekler: kodda oran / tarihli kurallar ve tenant profili / MM dönem toplamı | önerilen: tarihli profil, ilk payroll için MM toplamı | gerekçe: TAX-01..04; maliyet L.
- ADR-012: Muhasebe aktarım sözleşmesi | seçenekler: GMS'ye doğrudan kod / kanonik fiş+adaptör+alındı | önerilen: ikinci, ilk adaptör dosya | gerekçe: INT-01..04, sağlayıcıdan çıkış; maliyet L.
- ADR-013: Kiracı bağlamı ve Identity | seçenekler: mevcut ilk tenant / doğrulanmış üyelik+transaction bağlamı; shared schema / DB-per-tenant | önerilen: üyelik ve shared schema+RLS+composite FK | gerekçe: SAAS-01..06; alternatif DB-per-tenant bağımsız restore ihtiyacıyla yeniden değerlendirilebilir; maliyet L.
- ADR-014: Stok maliyetlendirme | seçenekler: hareketli ortalama / FIFO / maliyetsiz | önerilen: tek depoda hareketli ortalama; maliyet kesinlik işareti | gerekçe: STK-01/02; MM uygunluk onayı; maliyet L.
- ADR-015: Kişisel veri ve tenant yaşam döngüsü | seçenekler: belirsiz süresiz saklama / amaç-süre ve onaylı silme-export-restore | önerilen: ikincisi | gerekçe: SEC-07/08, SAAS-06; maliyet L, AV onayı.

## 11 SORULAR
- MM-01: UU Sport'un geçerli vergi mükellefiyetleri, özel hesap dönemi, istisna/tevkifat ve asgari vergi kapsamı nedir? TAX profilini belirler.
- MM-02: İlk üretim açılış tarihi ve onaylı cari/kasa/banka/POS/kart/kredi bakiyeleri hangi imzalı kontrol listesiyle verilecek? C0 ve geçiş farkını belirler.
- MM-03: GMS.NET sürüm/lisansı, kabul edilen Excel/XML şeması ve sentetik test ortamı var mı? Adaptör ve API gereksinimini belirler.
- MM-04: Faturalar GMS'ye GİB/UBL üzerinden ayrıca geliyor mu; hangi olayın muhasebeleştirme sahibi kim? INT-04 mükerrerliği önler.
- MM-05: Satış belgelenmeden ekonomik olay oluştuğunda kontrol listesi ve vergiye esas tarih nasıl izlenecek? Belgesiz kaydı görünmez bırakmayı önler.
- MM-06: Cari/ürün/vergi/POS/kart/kredi bağlantı hesapları ve avans politikası nedir? Kural eşlemesini belirler.
- MM-07: Gelen belgede ekonomik tutar/KDV farkı hangi toleransta ve hangi düzeltme türüyle kapatılır? ACC-05 kabulünü belirler.
- MM-08: Çek alınması, tahsile verilmesi, ciro ve karşılıksız aşamalarında cari kapama ve rücu nasıl izlenecek? ADR-007/008 noncash sözleşmesini belirler.
- MM-09: Kredi faiz/BSMV/KKDF/masraf, erken kapama ve yapılandırma planlarının güvenilir kaynağı nedir? Planı sözleşmeyle tutarlı yapar.
- MM-10: Maliyet yöntemi, geriye tarihli alış/iade ve eksi stokta geçici maliyet politikası uygun mu? Stok kârı ve vergi köprüsünü belirler.
- MM-11: Bordro, kira ve kamu ödemeleri için kişisel ayrıntı taşımayan hangi onaylı dönem toplamları sağlanabilir? İlk simülasyon kapsamını küçültür.
- KU-01: İlk canlıda kredi/kart/çek modüllerinden hangileri gerçekten zorunlu? Eksik özelliği kapalı tutarak M1'i daraltmayı sağlar.
- KU-02: En düşük güvenli nakit tamponu ve haftalık finans kontrolünün sahibi kim? Uyarının anlamını belirler.
- KU-03: Tahsilat planı paket sözleşmesinden mi, ödeme sözü/manuel plandan mı gelecek? Aidat tahakkuku dışındaki CF-03 modelini belirler.
- KU-04: Banka/POS kuruluşları, valör-komisyon sözleşmeleri ve şirket kartı sayısı nedir? Hesap bağlantılarını ve ilk adaptörü belirler.
- KU-05: On-prem/VDS/yedek hesaplarının şirket sahipliği, yetkili teknik destek ve aylık bütçe sınırı nedir? İşletim ve sağlayıcı çıkışını belirler.
- KU-06: Logo'da kullanılan kredi kartı/POS ekranının sürüm bilgili sentetik örneği sağlanabilir mi? Prompt bölüm 6 terminolojisini doğrular.
- AV-01: Sporcu, veli ilişkisi, fotoğraf ve olası sağlık alanlarında hukuki sebepler ve temsil yetkisi nasıl doğrulanacak? Veri minimizasyonu/aydınlatmayı belirler.
- AV-02: Hangi veri kaç yıl hangi amaçla saklanacak; muhasebe saklama yükümlülüğüyle silme talebi nasıl uzlaşacak? Retention ve tenant kapanışını belirler.
- AV-03: Mobil push, crash log, mağaza/OTA, uzaktan destek ve yedek sağlayıcılarında yurt dışı aktarım değerlendirmesi nedir? Türkiye hosting dışındaki veri yollarını kapsar.
- AV-04: İhlal bildirimi, veri sahibi başvurusu ve çocuk/veli yetki uyuşmazlığında sorumlu kişi ve süreç nedir? Operasyonel süre ve erişim kararını belirler.

## 12 FARKLAR_VE_FIKIRLER
| Fikir | Kaynak / doğrulama sınırı | UU Sport kabul ölçütü |
|---|---|---|
| Beklenen ödeme tarihiyle kısa nakit planı | Xero resmi dokümanı geçmiş nakit, fatura/borç ve planlanan tarihlerden öngörü anlatıyor. [Xero kısa vadeli nakit](https://central.xero.com/0/article/Short-term-cash-flow-AU). | CF-03/08: vade ile beklenen ödeme ayrı; kullanıcı gecikmenin etkisini görür. |
| Baz plan üzerine senaryo katmanı | Float resmi ürününde 13 haftalık ve aylık tahmin; senaryo katmanları yardım belgesinde bulunuyor. [Float](https://www.floatapp.com/), [Float senaryolar](https://intercom.help/floatapp/en/articles/7854551-create-scenarios-to-answer-what-if-questions). | CF-06: ana kayıt değişmeden kredi/kira/öğrenci ayrılması karşılaştırılır. |
| İşlemden muhasebe kabulüne iz | Logo bağlantı fikri + GMS dosya kabiliyeti; özel Tiger kurallarının teyidi eksik. | INT-01/04: olay→belge→batch→MM fişi; sıfır açıklanmamış fark. |
| Nakit rakamının güvenilirlik kartı | Ürün önerisi; rakipte olmadığı iddia edilmiyor. | Mutabakat tarihi, eksik vade/tutar, tahmin payı ve veri gecikmesi görünür; tek sahte güven skoru yok. |
| Akademiye uygun hafif plan | ADR-005 aidat dışarıda kuralına uyarlanmış öneri. | Paket/tahsilat beklentisi borç oluşturmaz; plan gerçekleşince çift sayım yok. |
| Taşınabilir çıkış paketi | Şirket veri sahipliği ve SAAS-06 önerisi. | Hesaplar, defter, ilişkiler, belgeler, sözlük, hash ve sürüm manifesti standart biçimde bağımsız okunabilir. |

Satılabilir fark “kayıt dışını yönetmek” olarak konumlanmamalı; **eksik belgeyi görünür tutan, mali müşavirle izlenebilir mutabakat ve anlaşılır nakit planı** olarak ölçülmeli. Paraşüt/Mikro/Luca/Bizimhesap/QuickBooks/Fathom/Pulse hakkında bu incelemede doğrulanmamış özellik karşılaştırması yapılmadı. Pazar benzersizliği iddiasından önce hedef işletmelerle görev bazlı doğrulama gerekir.

Başarı ölçümleri [ONERI]: haftalık nakit kontrolüne harcanan süre, açıklanamayan mutabakat farkı, MM ret oranı, kaynağına inilemeyen tahmin tutarı, 4/13 haftalık tahmin hatası, vadesiz açık plan oranı. Hedef sayılar ilk sentetik pilot ve gerçek işletimde yetkili ölçümden sonra belirlenir; bugünden kazanım yüzdesi vaat edilmez.

## 13 ITIRAZLAR
- ACC-04: “Belgeli = resmi = muhasebeye gönderilmiş” eşitliğine itiraz. Üçü ayrı kanıt; ADR-004 sözlüğü ve projection'ı düzeltilmeli. Maliyet M/L; ekonomik tek defter korunur.
- CF-01: Beş etiketi tek durum enum'u yapmaya itiraz. Ayrı boyutlar gerekir; aksi halde ya çifte sayım ya bilgi kaybı. Maliyet M.
- CSH-05/08: Krediyi normal cari ödeme, POS ve şirket kartını eşdeğer hesap saymaya itiraz. Finansman borcu ve nakit zamanı ayrı. Maliyet L/XL; tamamlanana kadar ilgili eylem kapalı.
- SAAS-01/02: Mevcut RLS'nin SaaS'a hazır olduğu yorumuna itiraz. İlk tenant çözümü ve Identity hedef erişimi tamamlanmadan ikinci tenant açılmaz. Maliyet L.
- OPS-02: “53 test geçti” ifadesini güncel kurulum/ACL doğruluğu kanıtı saymaya itiraz. Bu incelemede test çalışmadı; SEC-01 ile mevcut append-only testinin geçmiş başarı iddiası yeniden uzlaştırılmalı. Maliyet M.
- SEC-08: Gerçek dosyayı yalnız Git dışı bırakmanın ajanlardan koruduğu varsayımına itiraz. OS/hesap sınırı gerekir; kaynak taşıma bu raporda yapılmadı. Maliyet M.
- BIZ-01/STK-01: Logo'nun bütün modül ve maliyet yöntemlerini almaya itiraz. Akademi kapsamındaki sade alt küme daha denetlenebilir; kesin Tiger özellik iddiaları ayrıca teyit edilmeli.
- SEC-02/SEC-07: “İnternete açık RDP” doğrudan yönetim portu açılması anlamında kullanılıyorsa itiraz; VPN/gateway+MFA gerekir. Türkiye hosting kararı korunur, bütün KVKK uyumu yerine geçmez.
- ADR-001/002/003/005'in teknoloji, para/metin, manuel belge ve aidat tahakkuku dışarıda kararlarına itiraz yok. Bu rapor sessiz kapsam veya teknoloji değişikliği önermiyor.

