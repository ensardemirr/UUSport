# UU Sport — Dış Yapay Zekâ Strateji ve Mimari Danışma Promptu

> **Kullanıcı için (3 satır):** Bu satırın altındaki çizgiden sonrasını olduğu gibi kopyalayıp
> Claude (Fable 5.1) ve GPT sohbetine yapıştırın. İsterseniz bölüm 10'daki dosyaları da ekleyin.
> Size bir `.md` dosyası verecekler; o dosyayı Claude Code'a geri verin.

---

## 0. Rolün ve görevin

Sen; Türkiye'de ön muhasebe/ERP ürünleri (özellikle **Logo Tiger 3**), küçük işletme nakit akışı
planlaması, çok kiracılı (multi-tenant) SaaS mimarisi, uygulama güvenliği ve kurumsal arayüz
tasarımı konularında kıdemli bir **ürün mimarı ve danışmansın**.

Aşağıda, 5 günde sıfırdan yazılmış ve geliştirme ortamında çalışan **UU Sport** adlı bir ön
muhasebe ve yönetim platformunun tüm hikâyesini, mevcut durumunu ve hedefini bulacaksın.

Görevin: projeyi incelemek, **nereye gitmesi gerektiğini** ve **oraya hangi sırayla, hangi
kararlarla gideceğini** önermek. Önerilerini bu projenin kodunu yazan yapay zekâ kod ajanı
(Claude Code) uygulayacak. Bu yüzden çıktın **insan okuru için değil, o ajan için** yazılır:
yoğun, yapılandırılmış, kimlikli (ID), uygulanabilir.

## 1. Çalışma kuralları

1. **Analizde yaratıcı ve geniş ol, çıktıda kısa ve yoğun ol.** Düşünürken alternatifleri tart,
   varsayımlarımıza itiraz et, başka ürünlerden fikir al (Logo Tiger 3, Logo İşbaşı, Mikro,
   Paraşüt, Luca, Bizimhesap; QuickBooks, Xero, Float, Fathom, Pulse gibi nakit akışı araçları).
   Çıktıya yalnız sonuçları, gerekçeyi ve uygulanabilir adımı yaz. Temel kavram anlatma,
   giriş/sonuç paragrafı yazma, tekrar etme.
2. **Soru sorup bekleme.** Bilgi eksikse makul varsayım yap, varsayımı `A1, A2...` diye kaydet ve
   ilerle. Kullanıcıya veya mali müşavire sorulması gerekenleri ayrı bölüme yaz.
3. **Türkiye mevzuatını parametre olarak düşün.** KDV, geçici vergi, kurumlar/gelir vergisi,
   Muhtasar ve Prim Hizmet Beyannamesi, SGK, stopaj, damga vergisi oranları ve tarihleri koda
   sabit yazılmaz; tarih aralıklı parametre tablosuyla çalışır. Bildiğin güncel değerleri örnek
   olarak verebilirsin ama her birini `doğrulanmalı` diye işaretle. Mali müşavir teyidi esastır.
4. **Sabit kararlara saygı göster, ama körü körüne değil.** Bölüm 7'deki kararlar onaylıdır.
   Birine itirazın varsa bunu `tur: itiraz` olarak, gerekçesiyle ve değiştirmenin maliyetiyle yaz;
   sessizce çiğneme.
5. **Uygulanabilirlik.** Her öneri; hangi tabloyu/alanı/API ucunu/ekranı/testi değiştireceğini ve
   "bitti" sayılma ölçütünü (kabul kriteri) içermeli. "Daha iyi bir mimari düşünün" gibi soyut
   öneri yazma.
6. **Gerçek kişisel veri yok.** Bu belgede ve eklerde gerçek öğrenci/veli/çocuk verisi yoktur;
   ekran görüntüleri sentetik veridir. Önerilerinde gerçek verinin yapay zekâ araçlarına veya
   geliştirme ortamına gitmesini gerektiren hiçbir adım olmasın.
7. **Çıktı biçimi bölüm 9'daki şablondur.** Şablona uymayan çıktı birleştirilemez.
8. **Depoya erişimin varsa (ör. VS Code içindeki Codex):**
   - **Salt okunur çalış.** Yazacağın tek dosya:
     `docs/reviews/UUSPORT_DANISMA_<ARAC>_2026-09-27.md`. Başka hiçbir dosyayı oluşturma,
     değiştirme veya silme. Derleme, test, migration, uygulama başlatma, paket kurma, git
     komutu (commit/checkout vb.) çalıştırma.
   - **Şunları açma, okuma, listeleme:** `uu sport ltd/` klasörü (depo içinde, Git dışı) ve
     `C:\Users\Ensaradmin\Desktop\uu sport ltd\` — **gerçek öğrenci/veli verisi içerir**;
     `%LOCALAPPDATA%\UUSport\secrets\` (parolalar); PostgreSQL veritabanlarına bağlanma.
   - Bu belgedeki iddiaları koddan doğrula; her bulguya `kanit: <dosya>:<satır>` ekle. Belge ile
     kod çelişiyorsa kodu esas al ve çelişkiyi bulgu olarak yaz.

## 2. Proje kimliği ve iş bağlamı

- **Şirket:** UU Sport Ltd. — Samsun'da faaliyet gösteren futbol akademisi (PSG Academy Samsun).
  Gelirler: öğrenci paket/aidat ödemeleri, ürün satışı (forma seti, çorap vb.), özel ders gibi
  hizmetler. Giderler: antrenör, kira, maaş, SGK, vergi, tedarikçi, kredi ve kredi kartı.
- **Kullanıcı / ürün sahibi:** Şirket sahibi; yazılımcı değil. Gerçek hayatta **Logo Tiger 3**
  kullanıyor ve onun muhasebe/ön muhasebe mantığını "sağlam" buluyor. Türkçe çalışır. Teknik
  kararları büyük ölçüde yapay zekâya devretmiş durumda ("karar vermem gerekiyorsa sen benim
  yerime ver").
- **Resmi muhasebe:** Resmi defter ve beyanlar mali müşavirde, **GMS.NET** (Mikrokom) programında
  kalır. UU Sport resmi defter değildir; **yönetim ve ön muhasebe katmanıdır**. Mali müşavirin
  mizanı/hesap planı henüz alınmadı.
- **Kritik iş gerçeği — resmi ve gayri resmi:** İşletmede bazı satış ve tahsilatlar faturası
  kesilmeden önce (veya hiç kesilmeden) nakit/kartla gerçekleşir. Kullanıcı aynı veli için iki
  ayrı cari **istemiyor**: tek cari içinde hem tüm hareketleri (toplam) hem de yalnız muhasebeye
  gönderilmiş/belgelenmiş hareketleri (resmi) istediği an ayrı ayrı görebilmek, raporlayabilmek
  ve PDF alabilmek istiyor. Bu, projenin ana farkıdır.
- **Geliştirme şekli:** Tek sahip + yapay zekâ kod ajanları (VS Code içinde Claude Code ve OpenAI
  Codex). Ajanlar kodu yazar, test eder, ekran görüntüsüyle doğrular; kararlar ADR dosyalarına ve
  tek bir "ana hafıza" belgesine yazılır. Kullanıcı masaüstü kısayolundan uygulamayı açıp dener
  ve geri bildirim verir.
- **Ortam:** Windows (geliştirme makinesi Windows Server 2025). Canlı henüz yok. İlk canlı: ofisteki
  normal Windows bilgisayar (yerel ağ). Sonra kiralık RDP/VDS. Bütçe küçük işletme ölçeğinde.
- **Uzun vadeli niyet:** Ürünü **satılabilir** hale getirmek — başka akademilere / benzer hizmet
  işletmelerine (spor kulüpleri, kurslar, dans okulları vb.). Gerekirse SaaS'a evrilmek.

## 3. Hedef (nereye ulaşmak istiyoruz)

### 3.1 Ürün vizyonu
1. **Logo Tiger 3'ün ana hatlarını taşıyan ama çok daha sade bir ön muhasebe:** cari, stok/hizmet,
   satış/alış, fatura kaydı, kasa, banka (alt hesaplar: vadesiz/ticari hesap, kredi kartı, kredi,
   POS), çek/senet portföyü, tahsilat/ödeme, dağıtım/kapama, yaşlandırma, mutabakat, raporlar.
2. **Resmi / gayri resmi tek cari:** aynı carinin tüm hareketleri tek defterde; istenen görünüm
   (Toplam / Resmi / Muhasebe Bekleyen) filtreyle; ekstre, rapor ve dışa aktarımda aynı ayrım.
3. **Öngörülebilir ve simüle edilebilir nakit akışı:** kullanıcı **haftalık, aylık, 3 aylık ve
   yıllık** ufukta şunları tek yerde görebilmeli ve "ya şöyle olursa" diye senaryo kurabilmeli:
   - kasa + banka kullanılabilir bakiye ve tahmini dönem sonu bakiyesi, en düşük nakit noktası
   - beklenen tahsilatlar (alınacak borçlar, öğrenci ödemeleri, çekler, POS valörleri)
   - verilecek borçlar (tedarikçi, kira, antrenör hakedişi)
   - kredi taksitleri (ana para / faiz / masraf ayrı), kalan ana para
   - şirket kredi kartı ekstre ve taksit ödemeleri (hesap kesim / son ödeme günü)
   - maaş, SGK, Muhtasar ve Prim Hizmet Beyannamesi ödemeleri
   - KDV, geçici vergi, kurumlar (veya gelir) vergisi, stopaj ve diğer vergi ödemeleri
   - verilen/alınan çek vadeleri
   - her rakamın `Gerçekleşen / Planlanan (kesin vadeli) / Tahmini (varsayımlı) /
     Muhasebe Bekleyen / Resmi` olarak ayrıştırılması; gruplar sessizce toplanmaz
   - "bu rakam nereden geldi" ayrıntısına inilebilmesi (açıklanabilirlik)
   - senaryo: baz / iyimser / temkinli ve kullanıcı tanımlı (ör. "500 bin TL kredi çekersem",
     "20 öğrenci ayrılırsa", "kira %40 artarsa")
4. **Muhasebeciye ve başka programlara aktarım:** mali müşavirin GMS.NET'inin istediği biçimde
   (muhasebe fişi/mizan/hesap kodu eşlemesiyle) aktarım; aynı verinin Logo, Mikro, Luca, Zirve vb.
   programlara da dönüştürülebilir olması (genel bir dışa aktarım katmanı).
5. **SaaS'a evrilebilirlik:** Bugün tek şirket, tek kiracı. Yarın çok müşterili SaaS olabilir.
   Şimdi **ucuz ama doğru kapıları** bırakmak istiyoruz; bugünden büyük SaaS altyapısı kurmak
   istemiyoruz.
6. **Güvenlik:** Çocuk verisi (özel koruma), finans verisi. Veri sızıntısına karşı savunma
   derinliği; faz faz (yerel ağ → internete açık RDP + mobil → SaaS) artan kontroller.
7. **Tasarım:** Apple tasarım ilkeleri (bölüm 5.9) temel alınır ama Windows/web'e uyarlanır;
   macOS taklidi değil. Logo kullanıcısının hızı (klavye, yoğun veri) ile Apple sadeliği
   bağdaştırılmalı.

### 3.2 Faz planı (onaylı)
- **Faz 1:** Ofis bilgisayarında, yalnız yerel ağda, HTTPS ile web uygulaması. Roller: Yönetici,
  Muhasebe. Masaüstü istemci yok (tarayıcı).
- **Faz 2:** Canlı kiralık RDP/VDS'ye taşınır; personel mobil uygulaması (hoca, yönetici,
  muhasebe) App Store / Google Play'den, Expo OTA güncellemeli. Fiş/fatura fotoğrafı → önce GİB
  karekodu, sonra kendi sunucumuzda OCR; **görüntü şirket dışına çıkmaz**.
- **Faz 3:** Veli uygulaması (çocuğunun gelişimi ve carisi).
- **Sonrası (niyet, onaysız):** Ürünleştirme / SaaS.

## 4. Tarihçe: nereden başladık, ne denedik, ne olmadı, ne oldu

Tarihler 2026. Proje 23 Eylül'de belge aşamasında, 26 Eylül'de çalışan sürümle ilerledi.

| Tarih | Olay | Sonuç / ders |
|---|---|---|
| ≤17.09 | Ürün spesifikasyonu v0.11 yazıldı (Node.js + TypeScript arka uç, WPF masaüstü istemci öngörülüyordu). Karar raporu v0.10 PDF. | Tarihsel kaynak; teknoloji kısmı sonra geçersiz kılındı. |
| 23.09 | Codex ortak "ana hafıza"yı kurdu; PostgreSQL 18 kuruldu; kaynak Excel (17 sayfa) incelendi: yalnız `Sayfa1 (2)` güncel öğrenci listesi; `2025 FIYAT` sayfasında 58 formülün 29'u `#REF!`; öğrenci bakiyeleri doğrulanmış muhasebe kaydı değil. Statik HTML mockup'lar üretildi. | Excel'deki bakiye alanları açılış bakiyesi sayılmaz; onay gerekir. |
| 24.09 | Rapor kapsamı kararı: aylık KDV simülasyonu, 3/6/9/12 aylık vergi simülasyonu, ödeme takvimi, kredi kartı, kredi, maaş/SGK/muhtasar, kira, toplam ödeme ve nakit yeterlilik. | Kapsam onaylı; formüller mali müşavir teyidi bekliyor. Kod yok. |
| 25.09 | Kullanıcı geliştirmeyi durdurup bağımsız salt okunur denetim istedi (Claude Code). Bulgular: **K1** fatura cariye ikinci kez borç yazabilir (tasarım açığı); **K2** gerçek kaynak Excel proje klasöründe; **Y1** iki çakışan PostgreSQL kurulum yolu (servis vs kullanıcı modu); **Y2** kurulum betiği parolayı komut satırından alıyor; **Y3** `bigint` kuruş vs `numeric(18,2)` çelişkisi; **Y4** yurt içi barındırma şartı operasyon belgelerinde yok; **Y5** fatura önizlemesinde KDV kırılımı yok; **Y7** e-belge entegratörünün yeri belirsiz; ayrıca erişilebilirlik ve mockup hataları. | Denetim, ADR'lerin temelini oluşturdu. |
| 26.09 | Kullanıcı mimari vizyonu verdi (web tabanlı, faz planı, fiş görüntüsü şirket dışına çıkmaz). **ADR-001..005 ve güvenlik standardı onaylandı** (bölüm 7). Kullanıcı karar yetkisini devretti. | Node.js + WPF planı terk edildi. |
| 26.09 | **Sürüm 0.1 sıfırdan yazıldı** ve geliştirme ortamında çalıştı: ASP.NET Core (.NET 10) + React + PostgreSQL 18 (RLS). 31 test + 44 kontrollü uçtan uca duman testi. Masaüstü kısayolu. | Çalışan temel. |
| 26.09 | Kullanıcı denedi, 4 geri bildirim: Excel şablon indirme; cari/ürün için mali müşavir hesap kodu alanı; kredi kartı/kredi/çek; aynı faturada stok+hizmet. İlk üçü yapıldı (kredi taksit tablosu, çek portföyü seri girişli, kasa/banka hiyerarşisi: kredi kartı ve kredi hesabı bir bankanın altına bağlanır). Dördüncüsü zaten çalışıyordu. | — |
| 26.09 | **Hata (bulundu, düzeltildi):** Migration güvenlik yardımcısı paylaşılan tablo listesi kullanıyordu; ikinci migration yeni tablo ekleyince **sıfır veritabanında** ilk migration henüz olmayan tabloya RLS uygulamaya çalışıp patladı. Mevcut veritabanlarında görünmüyordu. | Her migration kendi dondurulmuş tablo listesiyle güvenlik kurar. Kural: her migration zinciri sıfır veritabanında denenmeden güvenilir sayılmaz. |
| 26.09 | Kullanıcı HTTPS/güvenlik duvarı sordu; **yedeklemeyi öne almaya** karar verdi (henüz yapılmadı). | Yedek + geri yükleme provası öncelikli. |
| 26–27.09 | Sol menü daraltılabilir gruplara ayrıldı. Cari sayfasına **"Ekstre Al"** penceresi: küçük **"Resmi Cari"** tik kutusu (tikliyken yalnız muhasebeye gönderilmiş kayıtlar), ekranda görüldüğü gibi yazdır/PDF. | — |
| 27.09 | **Olmayan (çözüldü):** Yerleşik `<dialog>` tarayıcının "üst katmanında" çalıştığı için Chromium yazdırmada/PDF'te **boş sayfa** çıktı. Çözüm: içerik ayrıca `document.body`'ye portal ile kopyalanır, yalnız `@media print`'te görünür; ayrıca `height:100%` kuralları yazdırmada sıfırlandı. | Yazdırılabilir her pencerede bu desen kullanılmalı. |
| 27.09 | **Olmayan (vazgeçildi):** Kullanıcı geliştirme ortamında girişteki 2 adımlı doğrulama kodunu kaldırmak istedi. MFA'yı tamamen kapatan değişiklik, kod ajanının kendi güvenlik sınıflandırıcısı tarafından "güvenliği zayıflatıyor" diye durduruldu. Kullanıcı **"bu tarayıcıyı 30 gün hatırla"** seçeneğini onayladı. | MFA kapatılmaz; sürtünme cihaz hatırlama ile azaltılır. |
| 27.09 | **Olmayan (kullanıcı tarafı):** Sunucu yeniden başlatılınca açık kalan tarayıcı sekmesi eski JavaScript'i çalıştırmaya devam etti; kullanıcı "menüler çalışmıyor" sandı. | Sürüm değişince istemciye "yenile" uyarısı veren mekanizma yok (teknik borç). |
| 27.09 | "Ekstre Al" üst işlem çubuğuna taşındı; PDF'te "Resmi Cari" kutusu gizlendi. Yeni **Hesap Hareketleri** sayfası: tüm carilerin hareketleri tek listede, cari/tarih/"Resmi Cari" filtreli. 53 test geçiyor. | — |
| 27.09 | Logo Tiger araştırması (bölüm 6). | Bu promptun sebebi. |

## 5. Mevcut durum (as-is)

### 5.1 Mimari
- Tek dağıtılabilir uygulama: ASP.NET Core API, React derleme çıktısını `wwwroot`'tan sunar;
  tek süreç, tek port. Şu an yalnız `http://127.0.0.1:5080` (yalnız bu bilgisayar), HTTPS yok.
- PostgreSQL 18, ICU `tr-TR`; iki rol: `uusport_owner` (migration, DDL, tablo sahibi) ve
  `uusport_app` (çalışma zamanı; `NOSUPERUSER NOBYPASSRLS`). Parolalar Windows DPAPI ile
  kullanıcıya bağlı dosyalarda.
- EF Core + snake_case adlandırma; migration'lar çalışma zamanından ayrı `migrate` komutuyla.
- Şemalar: `core, identity, crm, academy, inventory, billing, finance, integration, audit`.
- Kod büyüklüğü: ~4.400 satır C# (migration hariç), ~3.300 satır TypeScript/TSX, 54 API ucu,
  17 ekran, 53 xUnit testi (gerçek PostgreSQL + uygulama rolü + RLS açıkken).
- Mobil klasörü boş (Faz 2).

### 5.2 Veri modeli (tablolar)
- `core.tenant` — kiracı. **Her iş tablosunda `tenant_id` + RLS (ENABLE + FORCE)**; politika
  `tenant_id = current_setting('app.tenant_id')`.
- `identity.*` — ASP.NET Identity (kullanıcıda `tenant_id`, ad, azami iskonto yüzdesi).
- `crm.party` — cari (veli/müşteri/tedarikçi/ikisi). Alanlar: kod, ad, tür, telefon, e-posta,
  vergi no, not, `account_code` (mali müşavir hesap kodu, boş olabilir), arama anahtarı, aktiflik.
- `academy.athlete` — sporcu (cariye bağlı; ad, soyad, grup, doğum yılı).
- `inventory.item` — ürün/hizmet kartı: kod, ad, tür (stok/hizmet), birim, birim fiyat, fiyat KDV
  dahil mi, KDV oranı, `account_code`. **Maliyet fiyatı yok.**
- `inventory.stock_movement` — yalnız miktar hareketi (satış/alış satırından türer). **Tek depo,
  maliyetlendirme yok** (FIFO/ortalama yok). Append-only.
- `billing.trade_line` — satış/alış satırı = ekonomik olay. Miktar, birim fiyat, iskonto, KDV
  oranı, matrah (18,6), satır KDV'si (18,6, yuvarlanmaz), brüt (18,2), belgelenen miktar,
  `reverses_line_id` (iade = eksi miktarlı yeni satır).
- `billing.document` + `document_vat_line` + `document_link` — elle girilen fatura
  (e-Arşiv/e-Fatura/kâğıt/fiş), durum (düzenlendi/iptal), KDV kırılımı, satırlara miktar bazlı bağ.
  **Fatura cariye tutar yazmaz.**
- `finance.cash_account` — kasa/banka/POS/kredi kartı/kredi hesabı; `parent_account_id`
  (kredi kartı ve kredi bir banka hesabına bağlanır). **Açılış bakiyesi, bakiye, virman, döviz yok.**
- `finance.payment` — tahsilat/ödeme: cari, yön, yöntem (nakit/havale/kart/çek), kasa hesabı,
  tutar, tarih, referans, `reverses_payment_id`. Ödeme her zaman bir cariye bağlı.
- `finance.payment_allocation` — tahsilatın satırlara dağıtımı (kapama). Cariye tutar yazmaz.
  Geri alma eksi tutarlı satırla. Append-only.
- `finance.opening_balance` — açılış bakiyesi; onaylanınca cariye yazar.
- `finance.ledger_entry` — **carinin tek kaynak defteri.** Kaynak türleri: `sales_line`,
  `sales_line_reversal`, `purchase_line`, `purchase_line_reversal`, `payment`, `payment_reversal`,
  `opening_balance`, `approved_adjustment`. `document` kaynak olamaz (CHECK kısıtı). Kaynak başına
  tek satır (benzersizlik). Append-only.
- `finance.loan` + `finance.loan_installment` — kredi ve taksit planı (ana para/faiz/masraf);
  taksit ödenince normal bir `payment` (ödeme) üretir.
- `finance.check_note` — çek portföyü: alınan/verilen, çek no, banka, vade, tutar, durum
  (portföyde/tahsile verildi/tahsil edildi/ciro edildi/karşılıksız), seri giriş partisi; tahsil
  edilince otomatik `payment` üretir (vade tarihinde).
- `integration.import_batch` + `import_row` — Excel aktarımı (önizle → uygula → geri al).
- `audit.audit_event` — denetim kaydı, append-only.

### 5.3 İş kuralları (ADR özetleri, onaylı)
- **ADR-001 Teknoloji:** ASP.NET Core C# (.NET 10) API + PostgreSQL + React/TypeScript web +
  React Native (Expo) mobil. Masaüstü istemci yok. İstemciler veritabanına bağlanmaz.
- **ADR-002 Para:** `numeric(18,2)` / C# `decimal`; API'de para metin (string) olarak taşınır;
  yuvarlama "yarım yukarı" (away from zero), açıkça; satır KDV'si yuvarlanmaz, belge toplamında
  yuvarlanır; gelen belgede KDV yazdığı gibi alınır; istemci para hesabı yapmaz (önizleme sunucuda).
- **ADR-003 Metin:** insan metni Türkçe harmanlama, kod/anahtar alanları `COLLATE "C"`, tek bir
  Türkçe arama normalizasyonu.
- **ADR-004 Cari hareket modeli:** Cariye tutar **yalnız** satış/alış satırı, tahsilat/ödeme,
  onaylı açılış bakiyesi (ve bunların ters kayıtları) yazar. Fatura ve dağıtım asla yazmaz.
  Silme yok; düzeltme ters kayıtla. Üç görünüm aynı veriden hesaplanır: **Toplam** (tüm defter),
  **Belgeli/Resmi**, **Muhasebe Bekleyen** (faturası girilmemiş satırlar + dağıtılmamış
  tahsilatlar + onay bekleyen açılış bakiyeleri). S0–S10 senaryoları test olarak çalışır.
- **"Resmi" tanımı (uygulamada):** satış/alış satırı tam belgelenmişse resmi; tahsilat,
  belgelenmiş bir satıra dağıtılmışsa resmi; ters kayıt orijinalinin durumunu miras alır; onaylı
  açılış bakiyesi resmi. Sorgu anında hesaplanır, alan olarak saklanmaz.
- **ADR-005 İlk sürüm kapsamı:** İçeride: giriş, iki rol + MFA; cari ve sporcular; ürün/hizmet
  kataloğu ve tek depo stok; satış/çıkış ve iskonto yetkisi; tahsilat, kasa/banka, dağıtım, avans;
  **elle fatura kaydı** (fatura GİB portalında kesilir, numarası ve tutarlarıyla sisteme girilir,
  tutarlar yazdığı gibi alınır, sistem kendi hesabını yalnız kontrol için gösterir); alış faturası
  ve gider; üç görünüm; denetim kaydı; Excel aktarımı; temel raporlar. Dışarıda: **aidat
  tahakkuku (kullanıcı kararı)**, e-belge entegratörü, mobil, veli, KDV/vergi simülasyonu ve ödeme
  takvimi, banka ekstresi eşleştirme, hakediş, yoklama.
- İskonto sınırı kullanıcı/role bağlı, sunucuda zorlanır; eksi stok yalnız Yönetici.
- Ödeme yöntemi–hesap türü eşleşmesi zorunlu (nakit→kasa, kart→POS veya şirket kredi kartı,
  havale→banka).

### 5.4 Ekranlar
Genel Bakış (günlük/aylık tahsilat, açık alacak, muhasebe bekleyen sayısı); Cariler; Cari detay
(özet kartları, Toplam/Belgeli-Resmi/Muhasebe Bekleyen sekmeleri, tüm satırlar, tahsilat/ödeme,
faturalar, sporcular; üstte Ekstre Al, Stok/Hizmet Çıkışı, Tahsilat Al, Kesilen Faturayı Gir,
Alış Girişi, Ödeme Yap); Ekstre penceresi (Resmi Cari filtresi, yazdır/PDF); Muhasebe Bekleyen;
Faturalar; Hesap Hareketleri; Ürün ve Hizmetler; Kasa ve Banka (hiyerarşi); Krediler (taksit
planı, taksit öde); Çek Portföyü (seri giriş, tahsil, karşılıksız); Excel Aktarımı (şablon indir,
5 adım); Raporlar (günlük tahsilat, açık bakiyeler, faturası girilmemiş, dağıtılmamış, stok);
Kullanıcılar; Denetim Kaydı. Giriş, ilk kurulum, MFA kurulumu.

### 5.5 Çalışan güvenlik kontrolleri
- ASP.NET Identity (yavaş tuzlu hash); parola ≥12 karakter + rakam; 5 hatada 15 dk kilit;
  oturum çerezi HttpOnly + SameSite=Strict, 8 saat kayan süre; güvenlik damgası 1 dakikada
  doğrulanır (pasif yapılan kullanıcı en geç 1 dk'da düşer).
- **TOTP MFA zorunlu** (personel politikası MFA açık olmayı şart koşar); "tarayıcıyı 30 gün
  hatırla" çerezi. Yönetici kullanıcının MFA'sını sıfırlayabilir. Kurtarma kodu yok.
- CSRF: SameSite=Strict + her yazma isteğinde özel başlık (`X-UUSport-Request`).
- Hız sınırı: giriş/kod/kurulum uçlarında dakikada 20.
- Güvenlik başlıkları: CSP, X-Frame-Options DENY, nosniff, no-referrer. `AllowedHosts` yalnız
  localhost; Kestrel yalnız 127.0.0.1.
- Veritabanı: RLS + FORCE; uygulama rolü RLS'i aşamaz; append-only tablolarda UPDATE/DELETE/
  TRUNCATE yetkisi alınmış; `pg_hba.conf` yalnız yerel adres + scram-sha-256.
- Her API grubunda yetki politikası; is kuralı sınırları sunucuda.
- Geliştirme ve test yalnız sentetik veriyle; gerçek veri yapay zekâ araçlarına verilmez.
- Kiracı bağlamı: bağlantı açılışında `set_config('app.tenant_id', <id>, false)` (oturum düzeyi);
  değer yoksa RLS hiçbir satır göstermez.

### 5.6 Bilinen sınırlar ve teknik borç (dürüst liste)
1. **Tek kiracı çözümlemesi:** kiracı, veritabanındaki **ilk kiracı** olarak okunup önbelleğe
   alınıyor; kullanıcının kimliğinden/claim'inden türetilmiyor. SaaS için engelleyici.
2. Kiracı ayarı oturum düzeyinde `set_config(..., false)`; bağlantı havuzuyla etkileşimi ve
   işlem-yerel (`SET LOCAL`) alternatifi değerlendirilmedi.
3. HTTPS, yerel ağ erişimi, güvenlik duvarı kuralı yok (Faz 1 için planlı: iç sertifika otoritesi;
   şirketin alan adı yok).
4. **Yedek ve geri yükleme provası yok** (öncelikli olarak kararlaştırıldı).
5. Sürüm paketi (publish + manifest + SHA256 + kurulum/geri alma betikleri) yok; CI yok;
   bağımlılık açığı taraması ve commit öncesi gizli bilgi taraması yok.
6. Stok: maliyet yok, tek depo; satış kârlılığı hesaplanamıyor.
7. Kasa/banka: hesap bakiyesi/açılış bakiyesi yok; hesaplar arası virman yok; banka ekstresi
   aktarımı/eşleştirme yok; POS komisyon/valör takibi yok; şirket kredi kartı ekstre döngüsü
   (kesim/son ödeme) yok; döviz yok; şube/masraf merkezi yok.
8. Kredi ve çek, kredi verenin/çek carisinin cari bakiyesine normal ödeme olarak yazılıyor; kredi
   verenin cari bakiyesi **kalan kredi borcunu göstermez** (kalan ana para taksit tablosundan
   okunur). Bunun için ADR-007 bekliyor.
9. Hesap kodu kart başına 1:1 (Logo'daki kural bazlı bağlantı kodu yok); yevmiye/fiş üretimi ve
   GMS.NET aktarımı yok (ADR-006, mali müşavirin hesap planı bekleniyor).
10. **Nakit akışı/yükümlülük modeli hiç yok:** maaş, SGK, vergi, kira, abonelik gibi planlanan
    ödemeler için varlık yok; ödeme takvimi ve simülasyon yok. Aidat tahakkuku kapsam dışı olduğu
    için **beklenen öğrenci tahsilatlarının** kaynağı yok.
11. Tüm ödemeler bir cariye bağlı (vergi/SGK ödemesi için "SGK" gibi bir cari açmak gerekiyor).
12. "Resmi" bayrağı sorgu anında hesaplanıyor; tüm cariler listesinde cari başına ek sorgu
    (ölçekte performans riski).
13. Sunucu güncellenince açık sekme eski istemciyi çalıştırıyor; sürüm kontrolü yok.
14. Loglama: varsayılan konsol; merkezi log/izleme/alarm yok. KVKK saklama/imha otomasyonu yok;
    kiracı bazlı veri dışa aktarımı yok.
15. Gerçek kaynak Excel hâlâ proje klasörü içinde (Git dışı); taşınmalı.
16. Çok dilli/çok para birimli değil; yalnız Türkçe ve TL.

### 5.7 Geliştirme ortamı ve kanıt disiplini
- Her değişiklikte derleme + tüm testler; arayüz değişikliği gerçek tarayıcı ekran görüntüsüyle
  doğrulanır (headless Edge). Her doğrulama **ayrı ve geçici** bir sentetik veritabanında yapılır;
  gerçek geliştirme verisine dokunulmaz.
- Canlıya alma prosedürü tanımlı ama hiç uygulanmadı: `precheck → apply → verify → recovery`,
  değişmez sürüm klasörü, manifest + SHA256, migration ile runtime paketinin ayrılması, canlı
  öncesi yedek ve geri yükleme kanıtı, uygulama ve veritabanı geri dönüşünün ayrı sayılması.

### 5.8 Canlı hedef ve barındırma
- Faz 1 hedef makine: Windows 11 Pro, i3-10100, 8 GB RAM (sanallaştırma için yetersiz sayıldı).
- Mantıksal roller: APP01 (uygulama) ve DB01 (PostgreSQL); tek makinede de ayrı servis
  hesapları/klasörler. Hedef RPO 15 dk, RTO 4 saat (WAL arşivi + PITR, günlük tam yedek, ayrı
  hesapta değiştirilemez yedek, ayda bir gerçek geri yükleme testi) — henüz uygulanmadı.
- Spesifikasyonda çocuk verisi nedeniyle **yurt içi (Türkiye) barındırma** şartı var.

### 5.9 Tasarım dili (mevcut)
- Rehber: "Apple Design Skill" (Apple HIG tabanlı; 2026'da yeniden tanımlanan 8 ilke: Purpose,
  Agency, Responsibility, Familiarity, Flexibility, Simplicity, Craft, Delight; inceleme sırası:
  erişilebilirlik → platform geleneği → görsel hiyerarşi ve zanaat → etkileşim → içerik dili).
  Web ve Windows'a uyarlanır; macOS taklidi değildir. Kurallardan örnekler: hiçbir anlam yalnız
  renkle verilmez; kontrast ≥4.5:1 (küçük metin); masaüstü kontrol hedefi ≥28 px; kenar çubuğu en
  fazla iki seviye; uyarı pencereleri nadir, geri bildirim arayüz içinde; yıkıcı işleme uyarı +
  vazgeç; etiketler ne olacağını söyler; açık/koyu görünüm sistemi izler, uygulamaya özel tema
  düğmesi yok; tek bir "imza" öğe, çevresi sakin.
- Uygulama: özel CSS belirteçleri; Segoe UI Variable; koyu lacivert sol kenar çubuğu (Genel
  Bakış sabit + daraltılabilir gruplar: Günlük iş / Tanımlar / Rapor / Yönetim); özet kartları;
  sekmeler; tablolar; durum rozetleri (renk + nokta + metin: mavi = birincil işlem, amber =
  muhasebe bekliyor, yeşil = belgelendi, kırmızı = hata/iptal); yerleşik `<dialog>` pencereler;
  bildirim balonu; yazdırma CSS'i. Açık/koyu görünüm otomatik.
- Eksikler: klavye kısayolu/komut paleti yok (Logo kullanıcısı F-tuşlarına alışık); hızlı toplu
  veri girişi (ızgara) yok; grafik yok; boş durum ve hata metinleri kısmen; mobil tasarım yok.

## 6. Bizim Logo Tiger 3 araştırmamızdan çıkanlar (doğrulanması senden)

1. Logo'da **kredi kartı ayrı bir "banka hesabı türü"** (Kredi Kartı Hesabı / Dövizli Kredi Kartı
   Hesabı). Kredi kartı hesabında "Geri Ödeme Planları" ile slip tahsilatının bankaya hangi gün
   ve hangi komisyonla geçeceği tanımlanır; "Banka Geri Ödeme Hareketleri" raporuyla izlenir.
   Banka kredileri "Banka Kredi Takip" ile ayrı ayrı tanımlanıp geri ödemeleri izlenir.
   (Bizde kredi kartı ve kredi, `parent_account_id` ile bir bankanın altına asılı.)
2. Logo'da **muhasebe bağlantı kodları kural bazlı**: cari, malzeme, kasa ve banka kartları kendi
   sekmelerinden muhasebe hesabına bağlanır; kurallarla çok sayıda kart tek hesaba eşlenebilir
   (ör. `02` ile başlayan tüm malzemeler → `150.02.001`). Muhasebeleştirme bu kodlarla fiş üretir.
3. Logo stokta 6 maliyet yöntemi (FIFO, LIFO, eldekilerin ortalaması, dönemsel ortalama,
   girişlerin ortalaması, fiziksel) ve çoklu ambar destekler.
4. Logo'nun "resmi/gayri resmi" ayrımı ağırlıkla e-Fatura mükellefiyeti etrafında; bizim "aynı
   carinin nakit + belgeli hareketlerini tek defterde tutup filtreyle ayırma" modelimizin hazır
   karşılığını bulmadık — bu bizim farkımız olabilir.
5. **GMS.NET'in yazılımcılar için bir API'si var** (C#, VB.NET, Java, Delphi); başka yazılımdan
   GMS.NET'e fiş kesilebiliyor. Ayrıca GMS.NET Excel/XML muhasebe fişi içe aktarımı, UBL'den fatura,
   banka ekstresi sihirbazı destekliyor. API'nin lisans/ücret/teknik ayrıntısı bulunamadı.

## 7. Sabitler ve kısıtlar (değişikliği yeni ADR ister)

1. Teknoloji yığını ADR-001; masaüstü istemci yok; istemciler yalnız HTTPS API ile konuşur.
2. Para ADR-002; metin ADR-003; **cari defter modeli ADR-004** (bu projenin çekirdeği; yalnız
   satır/tahsilat/açılış bakiyesi yazar, belge yazmaz, silme yok, ters kayıt).
3. İlk sürümde fatura elle girilir, e-belge entegratörü yok (ADR-005); aidat tahakkuku dışarıda.
4. Gerçek kişisel veri geliştirme/test/yapay zekâ ortamına girmez; fiş/fatura görüntüsü şirket
   dışına çıkmaz; çocuk verisi yurt içinde barındırılır; KVKK.
5. UU Sport resmi defter değildir; resmi muhasebe mali müşavirde (GMS.NET).
6. Yetki ekranda gizlemeyle değil API + RLS ile sağlanır; muhasebe kaydı silinmez.
7. MFA kapatılmaz.
8. Uygulayıcı: yapay zekâ kod ajanları + teknik olmayan sahip. Öneriler küçük, test edilebilir
   adımlara bölünebilmeli; her adım kendi başına çalışır bırakılmalı.
9. Bütçe küçük işletme ölçeğinde; kurulum Windows üzerinde.

## 8. Senden istenen analiz (her alan için bulgu ve öneri üret)

**A. Muhasebe çekirdeği ve Logo Tiger 3 eşleşmesi (`ACC`)**
- Logo Tiger 3 modüllerini (cari, malzeme, satınalma, satış, fatura, kasa, banka, çek/senet,
  finans/ödeme planları, genel muhasebe bağlantısı, raporlar) bizim modelle karşılaştır:
  hangisi **minimum biçimde alınmalı**, hangisi hiç alınmamalı (akademi ölçeğinde gereksiz).
- ADR-004 defter modelimiz Logo mantığına göre eksik veya hatalı mı? Kapama/dağıtım, yaşlandırma,
  vade, mutabakat, cari hesap fişleri (virman, borç/alacak dekontu, kur farkı) nasıl eklenmeli?
- "Resmi / gayri resmi" modelimiz (bölüm 5.3) sağlam mı? Kısmi belgelenmiş satırın tamamını
  "resmi değil" saymak doğru mu? Resmi tarafın muhasebeciye giden veriyle **birebir tutarlı**
  olmasını garanti eden mutabakat kontrolleri öner. Vergi ve hukuki riskleri (ör. kayıt dışı
  izlenim) nasıl yönetmeli, ürünün dili/raporları bu riski nasıl azaltmalı?

**B. Kasa, banka, kredi kartı, kredi, POS, çek (`CSH`)**
- Banka ve alt hesaplarının (vadesiz/ticari, kredi kartı, kredi, POS, döviz) ideal modeli ve
  aralarındaki ilişki: hesap kartı türleri, hiyerarşi mi yoksa Logo gibi "tür + bağlantı" mı?
- Kasa/banka hesap defteri (hesap bakiyesi, açılış bakiyesi, virman, masraf/komisyon, faiz) ADR-004
  ile nasıl yaşar? Carisiz para hareketi (banka masrafı, vergi, SGK, maaş) nasıl modellenmeli?
- Şirket kredi kartı: harcama, taksit, ekstre döngüsü (kesim/son ödeme), asgari ödeme, faiz.
- POS: brüt/komisyon/net, valör, blokeli tutar, taksitli satış, banka geri ödeme planı.
- Kredi: ana para/faiz/BSMV-KKDF/masraf, erken kapama, yapılandırma; kredi verenin cari bakiyesi
  sorunu (5.6-8) için önerin (ADR-007 taslağı).
- Çek/senet: alınan/verilen, ciro, tahsile verme, karşılıksız, portföy raporları, nakit akışına etkisi.

**C. Stok (`STK`)**
- Akademi ölçeği için doğru stok modeli: maliyet yöntemi (öneri + gerekçe), maliyet fiyatı, çoklu
  depo gerekli mi, sayım/fire/iade, kârlılık raporu. Minimum ama ileride genişleyebilir tasarım.

**D. Nakit akışı öngörüsü ve simülasyon motoru (`CF`, `TAX`) — en önemli alan**
- Veri modeli: yükümlülük/beklenti (ödeme ve tahsilat planı) varlıkları, tekrarlayan kurallar
  (maaş, kira, SGK, vergi, abonelik), taksit/vade kaynakları (kredi, kart, çek), planlanan ile
  gerçekleşenin eşleştirilmesi, senaryo ve varsayım tabloları, tarih aralıklı vergi parametreleri.
- Öğrenci tahsilatı tahmini: aidat tahakkuku kapsam dışıyken beklenen tahsilat nasıl üretilir
  (hafif "tahsilat planı", paket bazlı tahmin, geçmiş veriden istatistik)? Aidat kararının
  yeniden açılması gerekiyorsa gerekçesiyle söyle.
- Hesaplama: haftalık/aylık/3 aylık/yıllık ufuk, dönem sonu bakiyesi, en düşük nakit noktası,
  yetersizlik uyarıları; KDV (hesaplanan − indirilecek − devreden), geçici vergi, kurumlar vergisi,
  muhtasar ve SGK tahmini — hangi kaynaktan, hangi formülle, hangi belirsizlikle.
- Senaryo motoru: baz/iyimser/temkinli + kullanıcı tanımlı "ya şöyle olursa"; deterministik mi,
  olasılıksal (ör. Monte Carlo) mı? Açıklanabilirlik ("bu rakam nereden geldi").
- Gerçekleşen / Planlanan / Tahmini / Muhasebe Bekleyen / Resmi ayrımının veri modelinde ve
  ekranda nasıl korunacağı.
- Ekranlar: nakit akışı panosu, ödeme takvimi, senaryo karşılaştırma; grafik türleri.

**E. Muhasebeciye ve diğer programlara aktarım (`INT`)**
- Ön muhasebe olaylarından muhasebe fişi (yevmiye) üretimi: hangi olay hangi fişi doğurur;
  kural bazlı hesap kodu eşleme (Logo bağlantı kodu benzeri) tasarımı; hesap planı şablonları.
- GMS.NET: API mi, Excel/XML fiş aktarımı mı? Hangisi önce, neden; resmi/gayri resmi ayrımı
  aktarıma nasıl yansır (yalnız resmi mi gider?).
- Program bağımsız "kanonik dışa aktarım" katmanı (ara format) + program başına dönüştürücü
  (GMS.NET, Logo, Mikro, Luca, Zirve); e-Defter ve UBL-TR ile ilişkisi.
- İçe aktarım: banka ekstresi, POS raporu, e-Arşiv/e-Fatura (UBL), mizan.

**F. SaaS'a evrilme kapıları (`SAAS`)**
- Şimdi yapılması **ucuz** ama sonra yapılması **pahalı** olan hazırlıklar listesi (ör. kiracının
  kimlikten türetilmesi, kiracı başına ayarlar/parametreler, küresel vs kiracıya özel parametre
  ayrımı, kimlik ve kod alanlarının kiracı içinde benzersizliği, dosya depolamada kiracı yolu).
- Kiracı izolasyon modeli: paylaşımlı şema + RLS (mevcut) mı, şema/veritabanı başına kiracı mı?
  Bağlantı havuzu ve `app.tenant_id` ayarının güvenli deseni.
- Aynı kod tabanıyla "tek şirkete kurulum (on-prem)" ve "SaaS" sürümlerini birlikte yaşatmak.
- Kiracı açma (provisioning), plan/özellik bayrakları, lisanslama/faturalama, kiracı bazlı yedek,
  geri yükleme, dışa aktarım ve silme (KVKK), destek personelinin denetimli erişimi.
- Ürünleştirme: başka akademi/kurs işletmelerine satmak için neyin genelleştirilmesi gerekir
  (terminoloji, hesap planı şablonları, sektör şablonları)?

**G. Güvenlik ve veri sızıntısı (`SEC`)**
- Tehdit modeli (kısa, STRIDE mantığıyla) ve en olası 10 sızıntı yolu: yedek dosyaları,
  loglar, Excel/PDF dışa aktarımları (kişisel veri içerir), tarayıcı önbelleği/yazdırma, RDP'nin
  internete açılması, mobil cihaz kaybı, yapay zekâ geliştirme akışı (ajanların veriye erişimi),
  içeriden tehdit (personel), kiracılar arası sızıntı, tedarik zinciri (npm/NuGet).
- Faz bazlı kontrol listesi: Faz 1 (yerel ağ) / Faz 2 (internete açık RDP + mobil) / SaaS.
  Her kontrol için: neden, nasıl, nasıl test edilir.
- Mevcut kontrollerimizde (bölüm 5.5) zayıf gördüğün noktalar (ör. MFA kurtarma, oturum
  süresi, CSP, rate limit, denetim kaydının kapsamı, şifreleme: diskte/yedekte/alan düzeyinde).
- KVKK: çocuk verisi, açık rıza, saklama/imha, veri sahibi başvurusu, ihlal bildirimi.

**H. Tasarım (`UX`)**
- Ekran görüntüleri (ek) ve bölüm 5.9'a göre, Apple ilkeleri + 5 mercek sırasıyla notlar:
  kritik erişilebilirlik sorunları, hiyerarşi, yoğun muhasebe tablolarının sadeleştirilmesi.
- Logo kullanıcısının hızını koruyan desenler: klavye-öncelikli veri girişi, komut paleti,
  kısayollar, hızlı ızgara girişi, geri al — bunları Apple sadeliğiyle nasıl bağdaştırırsın?
- Nakit akışı ve simülasyon ekranları için bilgi mimarisi ve görselleştirme önerisi.
- Faz 2 mobil (personel) uygulaması için bilgi mimarisi (sekme çubuğu, hızlı işlemler, fiş
  fotoğrafı akışı).
- Satılabilir ürün için görsel kimlik: "tek imza öğe" ne olabilir?

**I. Operasyon ve kalite (`OPS`)**
- Yedek/geri yükleme (Windows + PostgreSQL), sürüm paketleme, CI, bağımlılık taraması, izleme,
  log, hata takibi, veri göçü; küçük bütçeye uygun araçlar.

**J. Yol haritası (`BIZ`)**
- Sıralı kilometre taşları (her biri çıkış kriterli), neyin ertelenmesi gerektiği, en büyük 5 risk.

## 9. Çıktı şablonu (zorunlu)

**Teslim biçimi:**
- Tüm çıktını **tek bir indirilebilir `.md` dosyası** olarak üret. Dosya adı:
  `UUSPORT_DANISMA_<ARAC>_2026-09-27.md` (`<ARAC>` = `CLAUDE`, `GPT` veya `CODEX`).
- Sohbet penceresine dosya dışında **yalnız** şunu yaz: dosya adı + en kritik 3 bulgu (3 satır).
- Dosya üretemiyorsan tüm içeriği **tek bir** ```` ```markdown ```` kod bloğu içinde ver; blok
  dışına bir şey yazma.
- Dil: Türkçe; teknik terimler İngilizce kalabilir. Süsleme, emoji, giriş/kapanış paragrafı yok.
- Uzunluk serbest ama dolgu yok; her bulgu en fazla ~8 satır. Hedef: 40–90 bulgu.

**Dosya yapısı (başlıkları aynen kullan):**

```
# UUSPORT_DANISMA_<ARAC>_2026-09-27
## 0 META
arac: <model adı ve sürümü>
okunan_ekler: [dosya adları | yok]
varsayimlar:
  - A1: ...
## 1 OZET (en fazla 12 satır: en önemli yargılar)
## 2 BULGULAR
- id: CF-01            # önekler: ACC CSH STK CF TAX INT SAAS SEC UX OPS DATA BIZ
  oncelik: P0|P1|P2|P3 # P0 = gerçek veri/canlı öncesi zorunlu
  tur: risk|eksik|iyilestirme|fikir|itiraz
  iddia: <tek cümle>
  gerekce: <kısa>
  oneri: <ne yapılmalı>
  somut: <tablo/alan, API ucu, ekran, betik, test adları>
  kabul: <bitti sayılma ölçütü, test edilebilir>
  bagimlilik: [id, ...]
  efor: S|M|L|XL       # S < 1 gün ajan işi, XL > 2 hafta
  adr: yok|ADR-00X değişir|yeni ADR: <başlık>
  kanit: <dosya:satır | "belge bölüm X" | yok>
  guven: yuksek|orta|dusuk
## 3 VERI_MODELI
# yeni/değişen tablolar: şema.tablo(alan tip kısıt, ...) biçiminde sözde DDL; her tabloya 1 satır amaç.
## 4 NAKIT_AKISI_MOTORU
# kaynaklar -> ara model -> hesaplama adımları -> senaryo -> çıktı; formüller; parametre tabloları;
# açıklanabilirlik; ilk sürümde neyin basit tutulacağı.
## 5 SAAS_KAPILARI
# ŞİMDİ (ucuz) / SONRA (pahalı) ayrımıyla madde listesi; her madde bir bulgu id'sine bağlı.
## 6 GUVENLIK
# tehdit modeli özeti; faz bazlı kontrol listesi (Faz1 / Faz2 / SaaS); her kontrol -> test yöntemi.
## 7 TASARIM
# ilke/mercek bazlı notlar; ekran bazlı öneriler; bileşen/desen listesi.
## 8 ENTEGRASYON
# olay -> fiş eşleme tablosu; kanonik format alanları; program dönüştürücü öncelik sırası.
## 9 YOL_HARITASI
- M1: <ad> | içerik: [id...] | çıkış kriteri: ... | tahmini süre
## 10 ADR_ADAYLARI
- ADR-006: <başlık> | seçenekler: ... | önerilen: ... | gerekçe: ...
## 11 SORULAR
# MM: mali müşavire / KU: kullanıcıya / AV: avukata — her soru tek satır, neden önemli olduğuyla.
## 12 FARKLAR_VE_FIKIRLER
# ürünü satılabilir kılacak ayırt edici özellikler; rakiplerden ödünç alınacak fikirler.
## 13 ITIRAZLAR
# mevcut kararlarımızdan yanlış bulduklarını kısa yaz (id ile bulgulara bağla).
```

## 10. Ekler (varsa oku, yoksa bu belge esastır)

Ekran görüntüleri ve PDF sentetik veridir (gerçek kişi yok). `03` ve `07`–`13` numaralı görüntüler
daha önceki sürümlerden kalmadır; sol menü yapısı güncel değildir (güncel menü: `01`).

Proje kökü `C:\UUSport` altında:
- `docs/reviews/dis-yz-ekleri-2026-09-27/` — 13 ekran görüntüsü + 1 ekstre PDF çıktısı
- `docs/decisions/ADR-001_TEKNOLOJI_YIGINI_VE_ISTEMCI_MIMARISI.md`
- `docs/decisions/ADR-002_PARA_TIPI_VE_YUVARLAMA.md`
- `docs/decisions/ADR-004_CARI_HAREKET_MODELI_SATIS_BELGE_TAHSILAT.md`
- `docs/decisions/ADR-005_ILK_SURUM_KAPSAMI_VE_ELLE_FATURA.md`
- `docs/standards/UUSPORT_GUVENLIK_STANDARDI.md`
- `docs/designs/MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md`
- `src/UUSport.Api/Domain/Entities.cs` (tüm varlıklar)
- `src/UUSport.Api/Features/Accounting/AccountingQueries.cs` (üç görünüm ve "resmi" hesabı)
- `src/UUSport.Api/Data/TenantContext.cs` ve `src/UUSport.Api/Data/MigrationSecurity.cs` (kiracı ve RLS)

Başla.
