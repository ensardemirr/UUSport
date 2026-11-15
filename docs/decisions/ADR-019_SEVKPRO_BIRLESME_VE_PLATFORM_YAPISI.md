# ADR-019 sevk.pro Birleşmesi ve Platform Yapısı

**Durum:** `[KARAR]` Kullanıcı tarafından 15 Kasım 2026 tarihinde onaylandı (tasarım oturumu:
yedi karar, yığın, proje adı ve adım listesi).  
**Tarih:** 15 Kasım 2026  
**Hazırlayan:** Claude Code (Fable 5.2)  
**Karar veren:** Kullanıcı  
**İlişkili:** ADR-001 (yığın), ADR-004 (cari modeli), ADR-005 (kapsam), ADR-012 (çoklu şirket);
`C:\Sevk.Pro\docs\karar-haritasi.md` (Sürüm 18), `C:\Sevk.Pro\docs\teknik-sartname.md`  
**Geçersiz kıldığı:** sevk.pro Karar Haritası Başlık 7'deki "TypeScript tek dil, Node.js backend"
kararının arka uç kısmı; sevk.pro'nun "claude.ai asıl kopya" çalışma kuralı; pilot kiracı için
"ERP patron" ilkesi (bölüm 2.6).

Numara notu: ADR-006..018 danışma sentezinde aday olarak ayrıldığı için bu karar 019 aldı.

---

## 1. Bağlam

İki proje aynı kurucu ve aynı yöntemle (Claude Code + Codex) yürüyordu:

- **UU Sport** (`C:\UUSport`): üç şirket için ön muhasebe ve yönetim. Çalışan kod var
  (ASP.NET Core + React + PostgreSQL RLS v2, 83 test), canlı yok.
- **sevk.pro** (`C:\Sevk.Pro`): dağıtıcılar için çok kiracılı saha satış SaaS'ı. Kod yok;
  15/15 mimari karar kapalı, saha uygulamasının 14 ekranından 7'sinin spec'i yazılı.

`[KULLANICI KARARI]` Kullanıcının hedefi tek üründür: **ön muhasebe çekirdeği** + ona veri giren
**saha uygulamaları** (plasiyer, hoca) + müşterinin kendi carisini gördüğü **müşteri uygulaması**;
çok şirketli müşterilere tek kurulumdan abonelikle satılır.

---

## 2. Karar

### 2.1 Tek ürün adı: sevk.pro

Projenin adı **sevk.pro**'dur. "UU Sport" artık ürün adı değil, ilk müşteri grubunun (kullanıcının
kendi şirketleri) adıdır. Belgelerde ve yeni kararlarda ad hemen değişir. **Koddaki adlar**
(proje adları, veritabanı ve rol adları, çerez ve başlık adları, depo adı) **ilk canlıdan sonra**
ayrı bir adımda değişir (bölüm 5, adım 14).

### 2.2 Yığın

Arka uç **C# / ASP.NET Core** (ADR-001 geçerli). Web **React + TypeScript**, mobil
**React Native (Expo) + OTA**, veritabanı **PostgreSQL + RLS**. Rust ve arka ucun TypeScript'e
çevrilmesi değerlendirildi ve reddedildi.

Gerekçe: sevk.pro'da kod olmadığı için değişimin maliyeti bir belgedir; çalışan kodu yeniden
yazmak tahminen 20-30 ajan günü ve en riskli alanlarda (kiracı izolasyonu, finans kuralları)
sessiz hata riski demektir.

Kabul edilen bedel: çevrimdışı çalışan cihaz fiyat, iskonto ve KDV'yi TypeScript ile, sunucu C# ile
hesaplar. Önlem: cihazdaki hesap küçük tutulur, sunucu esas alınır, **aynı örnek hesap seti iki
dilde de test olarak çalışır** (ADR-002 kuralları dahil).

Gözden geçirme tetikleyicisi: cihazın çevrimdışıyken ağır iş kuralı (koşullu kampanya gibi)
çalıştırması gerekirse.

### 2.3 Tek depo, ortak çekirdek

Tek depo, tek karar sistemi, tek standart seti. Ortak çekirdek (kimlik, kiracı/RLS, denetim, para,
Türkçe metin, cari ve belge temeli) kod olarak paylaşılır; **çekirdek, saha modülü kodu başlarken
çıkarılır**, daha önce değil.

### 2.4 Kiracı modeli

**Kiracı = tüzel kişi (VKN).** Üstünde **müşteri (grup/hesap)** katmanı bulunur: abonelik gruba,
veri izolasyonu şirkete bağlanır. ADR-012'nin `core.tenant_group` öngörüsü bu kararla yol haritasına
girer. Bir kullanıcı kendi grubunun birden çok şirketinde rol alabilir; gruplar arası okuma yoktur.

### 2.5 Kurulum ve müşteri yönetimi

- **Tek kurulum, tek sürüm, bir iç yönetim paneli.** Müşteriye özel kod yazılmaz; farklar yalnız
  parametreyle açılır (sevk.pro parametre filtresi geçerli).
- Kullanıcının kendi şirketleri ilk canlıda **ayrı kurulumda** çalışır. Bu geçici bir aşamadır:
  SaaS kurulumu güvenlik oturumu ve bağımsız sızma testinden geçince kendi şirketleri oraya ilk
  müşteri olarak taşınır.
- **Destek görüntüleme:** iç yönetim panelinden "müşteri gözüyle görüntüle" (o kullanıcının gördüğü
  ekranın salt okunur hali) ve "son hareketler" izi (hangi ekranda kaldı, son hata, kuyrukta
  bekleyen). sevk.pro'nun destek modu kuralları aynen geçerli: salt okunur varsayılan, müşteri
  yöneticisi onayı, süreli, her erişim kayıtlı. Canlı ekran kaydı yapılmaz. Ayrıntı tasarımı adım 17.

### 2.6 Dağıtım firması ve ERP

`[KULLANICI KARARI]` Kullanıcının dağıtım firmasında ön muhasebe **bu sisteme geçer**; firma grubun
dördüncü şirketi olur. Sonuçlar:

- Pilot, ERP'siz (SELF) modda çalışır; **Logo aktarımı ilk sürümün kapsamından çıkar**.
  `LogoAdapter` sonraki müşteriler için aday olarak kalır.
- "Her araç ayrı ambar" modeli (sevk.pro Başlık 14) artık Logo'da değil bu sistemde kurulur;
  mevcut tek depolu stok **çok ambarlı** hale gelir (adım 18 ve 25).
- ERP'siz kiracının defteri ADR-004 modelidir.

### 2.7 e-Belge

ADR-005 ilk canlı için geçerlidir (elle fatura). e-belge entegratörü (Nilvera adaptörü) pilotta
kanıtlandıktan sonra ayrı ADR ile tüm şirketlere açılır.

### 2.8 Tek doğruluk kaynağı

**Repo.** Karar haritası, ekran spec'leri ve parametre envanteri repoda yaşar; claude.ai karar
konuşma yeri olabilir ama çıktısı repoya yazılmadıkça karar sayılmaz. `C:\Sevk.Pro` git'e alındı;
depo yeniden yapılandırılana kadar sevk.pro belgeleri orada, kararlar bu depoda durur.

### 2.9 Sıra

İlk canlı bitene kadar yapısal taşıma ve koddaki ad değişikliği **dondurulur**. "Kasım 2026" tarihi
bırakıldı (KU-4'ün tarih kısmı geçersiz); yeni tarih, canlı makine kararının verildiği günden sayılır.

---

## 3. Sonuçlar

- Güvenlik standardı sevk.pro'nun sıkı maddeleriyle genişler: denetim kaydında hash zinciri, her
  view `security_invoker`, tenant_id ile başlayan bileşik indeks, uygulama rolüne EXPLAIN ANALYZE
  yasağı, bağımsız sızma testi (adım 15).
- Çevrimdışı senkron, Nilvera ve iyzico adaptörleri C# ile yazılır.
- sevk.pro'nun diğer kararları (abonelik merdiveni, destek modu, silme merdiveni, konum modeli,
  "GİB'e giden belge dondu", "fiş resmi belge değildir", parametre envanteri) geçerliliğini korur.
- sevk.pro `CLAUDE.md` bu ADR'ye göre güncellendi.

---

## 4. Açık konular

| # | Konu | Kimden | Ne zaman |
|---|---|---|---|
| 1 | Belge iptali: ADR-004 S5 "belge iptal edilir" ile sevk.pro "GİB'e giden belge iptal edilmez" tek kurala bağlanmalı | Mali müşavir | Adım 12 |
| 2 | Giriş: "parolasız giriş yasak" (güvenlik standardı 2.1) ile saha PIN'i; ürün bazlı istisna | Tasarım | Adım 15 |
| 3 | Kodun ve ürünün hangi tüzel kişide duracağı; tek depo sakıncası | Avukat | Adım 16 |
| 4 | Barındırma: Windows RDP mi Linux mu (SaaS kurulumu için) | Tasarım | Adım 17 |
| 5 | Depo ve çözüm adlarının yeni hali | Kullanıcı | Adım 13-14 |
| 6 | sevk.pro Karar Haritası Sürüm 19 claude.ai'den kurtarılacak | Kullanıcı | Adım 3 |
| 7 | İlk canlı geliştirme makinesine kurulacak (`[KULLANICI KARARI]` 15.11.2026). Windows Evaluation lisansı yaklaşık 5 Şubat 2027'de bitiyor: lisans alınacak veya canlı taşınacak | Kullanıcı | Süre bitmeden |

---

## 5. Adım listesi (onaylı)

Süreler tahmindir (ajan günü). "Kullanıcı" yazan adımlar ajan tarafından yapılamaz.

**Aşama 0 - Kayıt**

1. Bu ADR, ana hafıza kaydı, sevk.pro `CLAUDE.md` güncellemesi. **Yapıldı (15.11.2026).**
2. `C:\Sevk.Pro` belgelerinin git'e alınması. **Yapıldı (15.11.2026).**
3. Sürüm 19'un claude.ai'den kurtarılması. Kullanıcı.

**Aşama 1 - İlk canlı (kullanıcının kendi şirketleri)**

4. Kararlar: canlı makine `[KARAR]` geliştirme makinesi `BAKON-DEV01` (ayrı PostgreSQL kümesi, ayrı
   servis hesabı ve klasörler; ajanlar canlı veriye dokunmaz; ayrıntı ana hafıza bölüm 0); alan adı `[KARAR]` sevk.pro alt alan adı; yedek `[KARAR]`
   şimdilik aynı makinede, kullanıcı elle dışarı alır, sonra bulut alanına taşınır.
5. Çalışma dalının `master`'a birleştirilmesi.
6. Sürüm paketi, Windows servisi, kurulum ve güncelleme betikleri (S-18; 1,5-2).
7. Yedek görevi ve gerçek geri yükleme provası (S-10; 0,5).
8. HTTPS ve güvenlik duvarı (S-11; 0,5-1).
9. Temiz makinede kurulum provası (0,5).
10. M2: resmi kapsam, ekstre, dönem kilidi (S-20..S-27).
11. Canlı kurulum, açılış bakiyeleri, ikinci yönetici. Kullanıcı + ajan.
12. Mali müşavir soruları: belge iptali, stopajlı belgeler, açılış bakiyesi kaynağı. Kullanıcı.

**Aşama 2 - Birleştirme (canlı oturduktan sonra)**

13. Deponun yeni klasör yapısına taşınması, sevk.pro belgelerinin yerleştirilmesi (1).
14. Koddaki adların sevk.pro yapılması (1).
15. Güvenlik standardının birleştirilmesi (1).
16. Avukat sorusu: sahiplik. Kullanıcı.

**Aşama 3 - Tasarım (koda dokunmaz; Aşama 1 ile paralel yürüyebilir)**

17. Müşteri (grup) katmanı, platform yöneticisi, destek görüntüleme.
18. Veri modeli: çok ambarlı stok, araç ambarı, saha kayıtları.
19. Çevrimdışı senkron ve cihaz-sunucu ortak hesap test seti.
20. Kalan saha ekranları: 1-2-3, tahsilat 9-10-11, ayarlar 14.
21. Müşteri paneli ve iç yönetim paneli ekranları.
22. Akademi modülü (hoca uygulaması) ve müşteri/veli uygulamasının kapsamı.

**Aşama 4 - Yapım**

23. Ortak çekirdeğin çıkarılması (4-6).
24. Grup katmanı ve platform yöneticisi (3-5).
25. Çok ambarlı stok (3-5).
26. Saha uygulaması: 14 ekran, çevrimdışı, termal yazıcı (15-25).
27. Nilvera e-belge adaptörü (4-6).
28. İç yönetim paneli ve destek modu (5-8).
29. Abonelik ve iyzico (4-6).
30. Kendi kendine açılış sihirbazı ve Excel içe alma (4-6).

**Aşama 5 - Pilot ve satış**

31. Dağıtım firması dördüncü şirket olarak girer; plasiyerler sahada dener.
32. Ayrı güvenlik oturumu: izolasyon ve view taraması.
33. Bağımsız sızma testi. Dış uzman.
34. Avukat onaylı sözleşme ve KVKK metinleri. Avukat.
35. Fiyat rakamları. Kullanıcı.
36. İlk dış müşteri.
37. Hoca uygulaması, sonra müşteri/veli uygulaması.

### Hedef klasör yapısı (adım 13'te uygulanır)

```text
<depo>/
|-- CLAUDE.md, AGENTS.md        # tek giriş
|-- docs/
|   |-- hafiza/                 # tek ana hafıza
|   |-- kararlar/               # ADR'ler
|   |-- standartlar/            # güvenlik, PostgreSQL, canlıya alma
|   `-- urun/                   # karar haritası, ekranlar, parametreler, hukuk
|-- src/
|   |-- Cekirdek/               # adım 23'te çıkarılır
|   |-- Api, Web                # bugünkü UUSport.Api, UUSport.Web
|   `-- saha, panel, ops        # React Native ve React uygulamaları
|-- tests/
`-- deployment/
```

---

## 6. Onay

`[KULLANICI KARARI]` 15 Kasım 2026: yedi karar onaylandı; arka uç C#, istemciler TypeScript; proje
adı sevk.pro; tek kurulum, tek sürüm ve iç yönetim paneli; müşteri ekranına destek erişimi;
dağıtım firmasında ön muhasebe bu sisteme geçer; adım listesinin tamamı onaylandı.
