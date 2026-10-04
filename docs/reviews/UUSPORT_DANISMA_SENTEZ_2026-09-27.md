# UU Sport — Dış Danışma Raporlarının Sentezi ve Birleşik İş Listesi

**Tarih:** 27.09.2026 · **Hazırlayan:** Claude Code (Opus 5.5, yüksek efor) · **Durum:** `[ONERI]` — kullanıcı onayı bekler

Kaynaklar:
- `C:` = `docs/reviews/UUSPORT_DANISMA_CLAUDE_2026-09-27.md` (Claude, 1474 satır, 70 bulgu)
- `X:` = `docs/reviews/UUSPORT_DANISMA_CODEX_2026-09-27.md` (Codex/GPT, 964 satır, 55 bulgu)
- Kimlikler rapordaki bulgu kimlikleridir (ör. `X:SEC-01`). Bu belgedeki iş kimlikleri `S-xx`.

## 0. Doğrulama (Claude Code, 27.09.2026)

| İddia | Sonuç | Kanıt |
|---|---|---|
| `X:SEC-01` İkinci migration append-only tablolara UPDATE yetkisini geri veriyor | **DOĞRU, P0** | Sıfır veritabanında (`uusport_aclcheck`, silindi) migrate sonrası `uusport_app`: ledger_entry, stock_movement, audit_event, payment_allocation için UPDATE=**true** (DELETE/TRUNCATE false). `uusport` ve `uusport_test` etkilenmemiş: migration'ın düzeltme öncesi hali uygulanmıştı. Testler `uusport_test`'te koştuğu için hata gizlendi (`X:OPS-02`). Kök neden: `MigrationSecurity.ApplySchemaGrants` şemadaki TÜM tablolara UPDATE verir; 2. migration `RevokeUpdateDelete`'i tekrar çağırmaz. |
| `X:ACC-07` İade sonrası açık tutar orijinal brütten hesaplanıyor | **DOĞRU** | `AccountingQueries.cs:210` `OpenAmount = GrossAmount - alloc`; iade satırlarının brütü düşülmüyor. |
| `X:CSH-04` Taksit ödemesi iptal edilince taksit "ödenmiş" kalıyor | **DOĞRU** | `LoanService.cs:102,130,140` ödenmişlik `PaymentId != null`; ödeme ters kaydı bunu sıfırlamaz, yeniden ödeme "already_paid" ile engellenir. |
| `X:UX-01` Koyu tema + yüksek kontrastta ikincil metin ≈1,49:1 | **DOĞRU** | `styles.css:85-87` `prefers-contrast: more` kuralı temadan bağımsız `--ink-3:#2f3b49` yazıyor; koyu zemin `#161d26`. |
| `C:ACC-01` / `X:ACC-03` Resmi ekstre bakiyesi toplam defterden geliyor, görünür satırlarla çelişiyor | **DOĞRU** | `PartyPages.tsx` StatementDialog: bakiye sütunu `r.balance` (tüm defter), toplamlar istemcide `Number()` ile (ADR-002 ihlali). |
| Ek 14 PDF | **Hazırlayan hatası** | `14-ekstre-pdf-ciktisi.pdf` aslında Hesap Hareketleri sayfasının yazdırma çıktısı (ekran görüntüsü betiği dosyayı ezdi). `C:UX-02`, `C:SEC-09` kanıtlarının bir kısmı bu dosyaya dayanıyor; sonuçları değişmiyor (Hesap Hareketleri sayfasında gerçekten yazdırma düzeni yok). |
| `X:SEC-08` notu: ana hafızadaki "gerçek veri korundu" ifadesi | **İfade hatası** | Kastedilen, kullanıcının geliştirme veritabanına girdiği deneme kayıtlarıydı. Ana hafızada düzeltilecek; içeriğin sentetik olduğu kullanıcıya sorulacak. |
| Logo iddiaları (prompt bölüm 6) | **Doğrulanamadı** | Codex resmi Tiger kılavuzundan teyit edemedi (`X:8.1`). Tasarım Logo adlarına kilitlenmez; mantık (tür + bağlantı, kural bazlı eşleme) korunur. |

## 1. İki raporun uzlaştığı yargılar (yüksek güven)

1. ADR-004 çekirdeği (tek cari defteri, belge cariye yazmaz, append-only, ters kayıt) **korunur**. Teknoloji değişmez (ADR-001/002/003).
2. "Resmi" hesabı yanlış: hep-ya-hiç bayrak yerine **tutar bazlı kapsam**; dağıtımda **net** tutar (ters dağıtım dahil); resmi ekstre **sunucuda**, kendi devri ve yürüyen bakiyesiyle (`C:ACC-01..03`, `X:ACC-01..03`).
3. Finansal komut bütünlüğü: tek transaction, satır kilidi/sürüm, idempotency (`C:ACC-05/06`, `X:ACC-06`, `X:CSH-03`).
4. Kasa/banka **hareket defteri** gerekli: açılış, virman, carisiz gelir/gider, banka masrafı, vergi/SGK/maaş ödemesi (`C:CSH-01`, `X:CSH-01/10`).
5. Hesap ilişkisi: tek `parent_account_id` yerine **tür + rol bağlantısı** (POS→banka, kart→ödeme hesabı, kredi→kullandırım hesabı) (`C:CSH-03`, `X:CSH-02`).
6. Kredi = **yükümlülük alt defteri** (kullandırım, anapara/faiz/BSMV/KKDF/masraf ayrı), banka carisine sıradan ödeme değil (`C:ACC-10`, `X:CSH-05`).
7. POS = alacak + valör/komisyon geçişi; şirket kartı = ekstre döngüsü; yön–araç–hesap matrisi (`C:CSH-02/04/05`, `X:CSH-07/08`).
8. Stok: tek depo, **hareketli ağırlıklı ortalama**, sayım/fire belgesi (`C:STK-01/02`, `X:STK-01/02`).
9. Nakit akışı: **deterministik**, günlük ızgara (dip nokta günlükten), kaynağa inen açıklama, senaryo = temel veriyi değiştirmeyen düzeltme katmanı, Monte Carlo yok; kesinlik / belge kapsamı / aktarım durumu **ayrı eksenler** (`C:CF-01..05`, `X:CF-01..08`).
10. Öğrenci tahsilatı: **cariye yazmayan tahsilat planı**; aidat tahakkuku kararı şimdilik korunur (`C:CF-02`, `X:CF-03`).
11. Vergi: tarih aralıklı parametre + MM teyit alanı; bordro = MM onaylı aylık özet; KDV belgelerden; geçici/kurumlar ilk sürümde düşük güven (`C:TAX-*`, `X:TAX-*`).
12. Aktarım: kanonik dengeli fiş + kural bazlı hesap eşleme + **aktarım sahipliği** (UBL/banka ekstresiyle çift kayıt önleme) + değişmez parti; GMS.NET için önce Excel/XML dosya pilotu, API sonra (`C:INT-*`, `X:INT-*`).
13. SaaS kapıları: kiracı kimlikten (claim/üyelik), Identity uçlarında kiracı filtresi, işlem-yerel kiracı ayarı + havuz testi, paylaşımlı şema + RLS korunur (`C:SAAS-01..04`, `X:SAAS-01..03`).
14. Canlı öncesi güvenlik: yedek + gerçek geri yükleme provası, HTTPS/LAN, kurulum ucunun kilitlenmesi, giriş olaylarının denetimi, gerçek verinin ajan çalışma alanından **işletim sistemi yetkisiyle** ayrılması, MFA kurtarma, CI taramaları (`C:SEC-01..08`, `X:SEC-02..08`, `OPS-*`).
15. Konumlandırma: "kayıt dışını yönetmek" değil, **belgelenme kuyruğu + MM mutabakatı + nakit planı** (`C:ACC-14`, `C:BIZ-02`, `X:12`).
16. Kapsam disiplini: aynı anda tek kilometre taşı; Logo'nun tamamı alınmaz (`C:BIZ-01`, `C:ACC-15`, `X:BIZ-01`).

## 2. Yalnız bir raporun yakaladığı, kabul edilenler

Codex: `SEC-01` (doğrulandı), `ACC-07` (doğrulandı), `CSH-04` (doğrulandı), `CSH-06` gerçekleşme tarihi ≠ vade, `UX-01` (doğrulandı), `SEC-03` "hatırla" kullanıcı seçimi olmalı, `SEC-05` log/audit JSON'da kişisel veri, `SEC-06` Excel yükleme kaynak sınırları, Excel dışa aktarımda formül enjeksiyonu (`X:6.1-3`), `SAAS-04` kiracı tutarlı bileşik FK, `OPS-02` testler sıfır veritabanında da koşmalı, `ACC-04` belgelendi ≠ MM'ye gönderildi ≠ MM kabul etti, `DATA-01` Excel aktarımında aynı adlı velilerin birleşmesi riski, `INT-01` MM alındısı.

Claude: `ACC-08` açılış bakiyesine `as_of_date` + kapsam (resmi/toplam), `ACC-07` dönem kilidi, `ACC-11` cari fişleri (dekont/virman), `ACC-12` vade + yaşlandırma + sunucu tarafı kapama, `ACC-13` fatura farkının saklanması, `ACC-16` aktarım sonrası dondurma, `SEC-04` kurulum jetonu, `SEC-07` TOTP anahtarı düz metin, `SEC-10` hareketsizlik zaman aşımı + health sürüm ifşası, `SEC-11` görev ayrılığı, `UX-05..08` ekran bazlı düzeltmeler, `DATA-01` **canlıya geçiş planı + 2 hafta paralel çalışma**, `DATA-03` sayfalama/N+1, `OPS-05` 8 GB'ta sanal makine yerine doğrudan servis + RAM, somut parametre tablosu (`C:4.4`).

## 3. Çelişkiler ve Claude Code'un önerisi

| Konu | Claude raporu | Codex raporu | Öneri |
|---|---|---|---|
| Tahsilatın "resmi" sayılması | Kanal kuralı: banka/POS/çek tamamen resmi (MM ekstreden görür); nakit yalnız belgeli satıra net dağıtım veya makbuz kadar | Belgelenmiş satıra net dağıtım kadar | **Claude**: "Resmi Cari = MM defterindeki cari" hedefiyle uyumlu. Codex'in ayrı `aktarım durumu` ekseni de eklenir. **MM teyidi şart** (havale ile gelen faturasız tahsilat hangi hesapta). |
| Kasa/banka defteri | Tek taraflı append-only `cash_movement` (+ bölme satırı) | Çift taraflı `cash_journal`/`cash_entry` + kontrol hesapları | **Claude**: UU Sport genel muhasebe değil; çift taraflı fiş aktarım anında üretilir (INT). Yükümlülük hesapları `is_liability` ile ters işaret gösterilir. |
| Alınan çek | Girişte cariyi kapatır (portföy hesabına), ciro tedarikçi borcunu kapatır | Kapama aşaması ADR'de MM'ye göre | **Claude varsayılanı** (Türk ön muhasebe pratiği: 101 Alınan Çekler), MM teyidiyle ADR-007'ye yazılır. |
| Nakit planı modeli | `plan.cash_item` + `recurring_rule` + `settlement_match`; istek anında hesap, aylık anlık görüntü | `obligation` + `obligation_source` + `obligation_settlement` + saklanan `forecast_run` (girdi hash) | **Claude modeli + Codex'in kaynak tekilliği/`supersedes` kuralı** (çift sayımı önler). Çalıştırma kaydı yalnız kaydedilen senaryolar için. |
| Geçici vergi 4. dönem | Parametre `q4_enabled=false` | GİB 2026 rehberi 4. dönemi ele alıyor | Koda değil parametreye bağlı; **MM teyidi**. |
| Ürün imzası | "Belgelenme şeridi" | "Nakit dayanma çizgisi" | İkisi farklı modüllerde; ürün imzası nakit akışı modülü gelince kararlaştırılır (düşük öncelik). |
| Terim | Belgelendi / Belge bekliyor / Süresi geçti; "Gayri resmi" kalkar | Belgelenen / MM'ye gönderilen / MM kabul etti | Uyumlu: belge ekseni Claude, aktarım ekseni Codex. **"Gayri resmi" metni kaldırılır; "Resmi Cari" düğme adı kullanıcı kararı.** |

## 4. Birleşik iş listesi (kilometre taşları)

Kural: aynı anda tek aktif taş (`C:BIZ-01`). Her iş: test + gerekiyorsa ekran görüntüsü + sıfır veritabanı kanıtı.

### M0 — Doğrulanmış hatalar ve bütünlük (gerçek veri öncesi, ~3-5 ajan günü)
- **S-01** Yetki düzeltme migration'ı: 4 append-only tabloda UPDATE/DELETE/TRUNCATE geri alınır; `ApplySchemaGrants` UPDATE'i tablo listesiyle verir veya her çağrıdan sonra append-only listesi yeniden revoke edilir. Şema koruma testi (RLS/FORCE/politika + append-only yetkisi, katalogdan) **sıfır veritabanında** koşar; test altyapısı her koşuda temiz veritabanı da dener. (`X:SEC-01`, `X:OPS-02`, `C:SAAS-04`)
- **S-02** Finansal komut bütünlüğü: `UnitOfWork` tek transaction; kredi taksiti ve çek tahsil/karşılıksız tek commit; `trade_line`/`payment` sürüm (xmin) + dağıtım/fatura/stok işlemlerinde `FOR UPDATE`; 409 eşlemesi. (`C:ACC-05/06`, `X:ACC-06`, `X:CSH-03`)
- **S-03** Taksit ödenmişliği net ödemeden türetilir; ödeme iptali taksiti yeniden açar. Kredi/çek işlemlerinde gerçekleşme tarihi zorunlu, vade ayrı. (`X:CSH-04/06`)
- **S-04** İade sonrası açık tutar = net ekonomik tutar − net dağıtım. (`X:ACC-07`)
- **S-05** Yön–araç–hesap matrisi (müşteri kart tahsilatı yalnız POS). (`C:CSH-02`, `X:CSH-08`)
- **S-06** Kontrast token'ları tema bazlı. (`X:UX-01`)
- **S-07** Arayüzden geliştirici dili ("ADR-004", "tek kaynak defter", "tutar yazmaz") kaldırılır. (`C:UX-01`)
- **S-08** Sürüm uyumsuzluğu bandı ("Yeni sürüm yüklendi — Yenile"). (`C:UX-08`, `X:OPS-04`)

### M1 — Güvenli işletim temeli (canlı öncesi zorunlu)
- **S-10** Yedek: WAL + gecelik base backup + gecelik dump, istemci tarafı şifreleme, yurt içi uzak kopya, Data Protection anahtar yedeği, haftalık otomatik geri yükleme testi. (`C:OPS-01/SEC-01`, `X:OPS-01`)
- **S-11** HTTPS (iç CA), LAN bağlama, Secure/__Host- çerez, HSTS, güvenlik duvarı, BitLocker. (`C:SEC-02`, `X:SEC-02`)
- **S-12** Kurulum ucu tek kullanımlık jetonla. (`C:SEC-04`)
- **S-13** Giriş/başarısız giriş/kilit/MFA/çıkış/dışa aktarma denetimi + IP; log ve audit'te kişisel veri maskeleme. (`C:SEC-05`, `X:SEC-04/05`, `C:OPS-04`)
- **S-14** Uç yetki testi (tüm uçlar katalogdan). (`C:SEC-06`)
- **S-15** MFA: "bu tarayıcıyı hatırla" kullanıcı seçimi; kurtarma kodları; sunucuda CLI ile MFA sıfırlama; en az iki yönetici uyarısı; TOTP anahtarı Data Protection ile şifreli; hareketsizlik zaman aşımı; anonim health'te sürüm yok. (`X:SEC-03`, `C:SEC-07/08/10`)
- **S-16** Gerçek kaynak Excel proje dışına, ACL'li klasöre (kullanıcı eylemi, Claude Code rehberlik eder). (`C:SEC-03`, `X:SEC-08`)
- **S-17** CI betiği: derleme (uyarı=hata), testler, şema koruma, NuGet/npm açık taraması, gizli bilgi taraması, gerçek veri taraması; commit öncesi kanca. (`C:OPS-03`, `X:OPS-03`)
- **S-18** Sürüm paketi + Windows servisi + kurulum/yükseltme/geri alma betikleri + manifest. (`C:OPS-02`)
- **S-19** Excel içe aktarım sertleştirme: satır/hücre/açılmış boyut sınırları; belirsiz eşleşme karar kuyruğu (aynı adlı veliler birleşmez); dışa aktarımda formül enjeksiyonu önlemi. (`X:SEC-06`, `X:DATA-01`)

### M2 — Resmi kapsam, ekstre ve dönem (ADR-009, ADR-013)
- **S-20** Tek `OfficialScopeService`: tutar bazlı kapsam, net dağıtım, tahsilatta kanal kuralı (MM teyidi), ters kayıt mirası. (`C:ACC-02/03`, `X:ACC-01/02`)
- **S-21** Sunucu tarafı ekstre (`scope`, tarih aralığı, devir, yürüyen bakiye, toplamlar) + sunucu PDF + dışa aktarma denetimi; istemci para toplamaz. (`C:ACC-01`, `X:ACC-03`, `C:SEC-09`)
- **S-22** Terminoloji: "Gayri resmi" kalkar; Belgelendi / Belge bekliyor / Süresi geçti; teslimden sonra fatura süresi parametresi. (`C:ACC-14`, `X:ACC-04`)
- **S-23** Açılış bakiyesi: `as_of_date`, `due_date`, `scope`; görev ayrılığı (oluşturan onaylayamaz). (`C:ACC-08`, `C:SEC-11`)
- **S-24** Fatura–satır farkı kalıcı; "Tutar farkı" kuyruğu. (`C:ACC-13`, `X:ACC-05`)
- **S-25** Dönem kilidi. (`C:ACC-07`)
- **S-26** Mutabakat görünümleri (ADR-004 §4 + ek kontroller) + `verify` komutu. (`C:ACC-04`)
- **S-27** Muhasebe Bekleyen iş kuyruğu (yaş, sıralama, toplu fatura). (`C:UX-07`)

### M3 — Hazine ve ödeme araçları + canlıya geçiş (ADR-007, ADR-008)
- **S-30** `cash_movement` (append-only) + kategori + carisiz fiş + virman + hesap açılışı; mevcut ödemeler için geçiş bacakları. (`C:CSH-01`, `X:CSH-01/10`)
- **S-31** Hesap türü + rol bağlantısı; Varlık/Yükümlülük gösterimi. (`C:CSH-03`, `X:CSH-02`, `C:UX-05`)
- **S-32** Kredi yükümlülük alt defteri. (`C:ACC-10`, `X:CSH-05`)
- **S-33** Çek yaşam döngüsü (portföy hesabı, girişte cari kapama, ciro alıcısı, karşılıksız). (`C:ACC-09`, `X:CSH-09`)
- **S-34** POS geçiş planı; şirket kartı ekstresi. (`C:CSH-04/05`, `X:CSH-07`)
- **S-35** Cari fişleri (dekont, virman). (`C:ACC-11`, `X:ACC-09`)
- **S-36** Canlıya geçiş planı: T tarihi, MM cari bakiye listesi (resmi) + Excel (toplam) açılışları, kasa sayımı, banka bakiyeleri, stok sayımı, kuru prova, 2 hafta paralel çalışma. (`C:DATA-01`)

### M4 — Nakit akışı v1 (ADR-010, ADR-011, ADR-015)
`C:CF-01..05`, `X:CF-01..08`, `C:TAX-01/02/04`, `X:TAX-01/02/04`, `C:CF-02` tahsilat planı, `C:UX-09` ekranlar. İlk sürüm: haftalık/aylık, KDV otomatik, muhtasar/SGK bordro özetinden, geçici/kurumlar MM tahmini kalemi, üç hazır senaryo.

### M5 — Muhasebeci aktarımı (ADR-006, ADR-012)
`C:INT-01..03`, `X:INT-01..04`, `C:ACC-16`. MM'nin gerçek şablonu ve test firması gelmeden başlamaz.

### M6 — Senaryo genişletme, vergi v2, yaşlandırma, stok maliyeti (ADR-014)
`C:CF-04/06`, `C:TAX-03`, `X:TAX-03`, `C:ACC-12`, `C:STK-01/02`, `X:STK-01/02`, `C:UX-03` klavye-öncelikli giriş.

### M7 — Faz 2 (kiralık sunucu + personel mobil)
`C:SEC-13`, `C:UX-10`, `C:INT-04/05` (UBL, banka ekstresi eşleştirme), `C:DATA-03`, SaaS kapılarından ucuz olanlar (`C:SAAS-01..03`, `X:SAAS-01..04`).

### M8 — Ürünleştirme / SaaS
`C:SAAS-05..07`, `X:SAAS-05/06`, `C:SEC-12`, `X:SEC-07`, `C:DATA-02`, `C:BIZ-02`.

## 5. ADR adayları (birleşik numaralandırma)
- ADR-006 Hesap planı eşlemesi, aktarım sahipliği ve aktarım sözleşmesi (C:ADR-006 + X:ADR-012)
- ADR-007 Ödeme araçları: çek, kredi, şirket kartı, POS (C:ADR-007 + X:ADR-007)
- ADR-008 Kasa/banka hareket defteri ve hesap bağlantıları (C:ADR-008 + X:ADR-008)
- ADR-009 Resmi kapsam, belge kapsamı ve aktarım durumu (C:ADR-009 + X:ADR-006)
- ADR-010 Nakit akışı plan ve senaryo modeli (C:ADR-010 + X:ADR-010)
- ADR-011 Vergi ve mevzuat parametreleri (C:ADR-011 + X:ADR-011)
- ADR-012 Kiracı çözümleme ve SaaS kapıları (C:ADR-012 + X:ADR-013)
- ADR-013 Dönem kilidi ve finansal komut bütünlüğü (C:ADR-013 + X:ADR-009)
- ADR-014 Stok maliyeti
- ADR-015 Tahsilat planı ve aidat
- ADR-016 Yedekleme ve anahtar yönetimi
- ADR-017 KVKK veri yaşam döngüsü (C:ADR-017 + X:ADR-015)

## 6. Kullanıcıya (KU) — birleşik
- KU-1 Kilometre taşı sırası onayı (önce doğruluk ve güvenlik, sonra hazine, sonra nakit akışı).
- KU-2 "Gayri resmi" ifadesinin kaldırılması ve "Resmi Cari" düğme/PDF başlığı adı.
- KU-3 Geliştirme veritabanına girilen deneme kayıtlarında gerçek kişi adı/telefonu var mı?
- KU-4 Canlıya geçiş hedef ayı; Excel kapanmadan önce 2 hafta paralel çalışma kabulü.
- KU-5 Şirket kartı, POS (banka, taksitli satış), portföyde çek ve kredi şu an var mı; ilk canlıda hangileri zorunlu?
- KU-6 İkinci yönetici hesabı; ofis bilgisayarı RAM 16 GB; Türkiye'de şifreli uzak yedek alanı; aylık bütçe.
- KU-7 Veliye verilecek ekstre: tüm hareketler mi, yalnız belgelenmiş mi?

## 7. Mali müşavire (MM) — birleşik, sade dil
1. Şirket e-Fatura mükellefi mi, yalnız e-Arşiv mi; e-Defter kullanılıyor mu?
2. GMS.NET'e fiş aktarımı için örnek Excel/XML şablonu ve deneme firması verebilir misiniz?
3. Banka hareketlerini ekstreden mi işliyorsunuz? Faturası henüz kesilmemiş veli havalesini hangi hesaba alıyorsunuz (120 / 340)?
4. Satış faturaları size GİB/UBL üzerinden ayrıca geliyor mu? (Çift kayıt olmaması için hangi olayı kim işler?)
5. Geçiş tarihi için cari bakiye listesi ve mizan (hesap planı dahil) verebilir misiniz?
6. Teslimden sonra fatura düzenleme süresi pratikte kaç gün olmalı?
7. Alınan çek, ciro ve karşılıksız çeki nasıl kaydediyorsunuz?
8. Kredi taksitinde anapara/faiz/BSMV/KKDF ayrımını nereden alıyorsunuz? POS komisyonu hangi hesaba?
9. Aylık bordro özetini (net ücret, stopaj, damga, SGK, teşvik) hangi biçimde paylaşabilirsiniz?
10. KDV, muhtasar/SGK, geçici vergi (4. dönem dahil) ve kurumlar vergisi vade günleri ile oranları teyit eder misiniz? Asgari kurumlar vergisi bizde nasıl hesaplanıyor?
11. İşyeri kirası gerçek kişiye mi ödeniyor (stopaj)?
12. Stok maliyeti için hareketli ağırlıklı ortalama uygun mu?

## 8. Avukata (AV)
1. Belgesiz satışların ayrı izlenmesinin vergi incelemesindeki niteliği; ürün dili ve saklama.
2. Çocuk verisi: aydınlatma / açık rıza ayrımı, saklama-imha süreleri, VERBIS.
3. İhlal bildirimi ve veri sahibi başvuru süreci; yurt dışı hizmetler (push, hata izleme, mağaza, yedek).
4. SaaS'ta müşteri akademilerle veri işleme sözleşmesi.
