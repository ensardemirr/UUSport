# UU Sport Bagimsiz Denetim Raporu

**Surum:** 2.0 (capraz dogrulama sonrasi revizyon)
**Tarih:** 25 Eylul 2026
**Denetleyen:** Claude Code (Opus 5), bagimsiz ve salt okunur
**Capraz inceleme:** Codex karsi degerlendirmesi, kullanici araciligiyla iletildi
**Kapsam:** `C:\UUSport` tamami + surum 0.11 sistem spesifikasyonu
**Istek:** `docs/reviews/CLAUDE_CODE_UUSPORT_TAM_DENETIM_PROMPTU_2026-09-25.md`

**Denetim makinesi:** `BAKON-DEV01`, kullanici `Ensaradmin`.
Bu makine **gelistirme ortamidir**. Ilk canli kurulumun yapilacagi normal Windows
bilgisayar ayri bir makinedir ve bu denetimde incelenmedi.

**Durum:** Bu turda da hicbir uygulama dosyasi degistirilmedi, SQL calistirilmadi,
PostgreSQL yeniden kurulmadi, betik silinmedi, kaynak dosya tasinmadi veya silinmedi.
Kaynak Excel ve PDF **acilmadi**. Yalnizca bu rapor dosyasi guncellendi.

Etiketler: `[KULLANICI KARARI]` kullanicinin acik karari,
`[ONERI]` denetleyenin onerisi (onaylanmadi), `[CIKARIM]` dosyalardan cikarilan sonuc.

---

## 0. Revizyon ozeti

Surum 1.0'daki 25 bulgunun capraz dogrulama sonrasi durumu:

| No | Konu | Durum | Gerekce |
|---|---|---|---|
| K1 | Cift borclandirma riski | **Korundu**, yeniden siniflandirildi | Tasarim acigi; gerceklesmis hata degil |
| K2 | Kaynak dosyalarin yeri | **Kismen duzeltildi**, Kritik'ten Yuksek'e indirildi | "Tek nusha" iddiasi geri cekildi |
| K3 | "PostgreSQL kurulu degil" | **GERI CEKILDI** | Kurulu ve calisiyor; komutla dogrulandi |
| Y1 | Iki PostgreSQL kurulum yolu | **Korundu ve guclendi** | Catisma gecmiste fiilen yasanmis |
| Y2 | Parolanin komut satirinda verilmesi | **Korundu**, etkisi sinirlandirildi | Betigin calistigi dogrulandi; log sizintisi kanitlanmadi |
| Y3 | `bigint` kurus / `numeric(18,2)` | **Korundu** | Belge celiskisi aynen duruyor |
| Y4 | Yurt ici barindirma sarti | **Korundu** | Operasyon belgelerinde yok |
| Y5 | Fatura onizlemesinde KDV yok | **Korundu** | Kanit degismedi |
| Y6 | "Mimari fiilen secilmis" | **Kismen geri cekildi** | Bos klasor uygulama kaniti degil |
| Y7 | E-belge entegratorunun yeri | **Korundu** | Belgeler arasi celiski duruyor |
| Y8 | Excel incelemesi celiskisi | **Kismen geri cekildi** | Celiski degil, tarih/kapsam kaydi eksikligi |
| O1 | Bozuk `td` etiketleri | **Korundu** | Dort eslesme dogrulandi |
| O2 | Kume yerel ayari | **Kismen geri cekildi** | "Sonradan degistirilemez" yanlisti |
| O3 | "Yalniz 127.0.0.1" zorlanmiyor | **Kismen geri cekildi** | `pg_hba.conf` fiilen kisitliyor |
| O4 | Veri klasoru ACL'i | **GERI CEKILDI**, yerine O4-YENI | Yanlis hedef; gercek bulgu farkli yerde |
| O5 | Tahsilat ekraninda eksik alanlar | **Korundu** | Kanit degismedi |
| O6 | Filtre / bakiye uyumsuzlugu | **Korundu** | Kanit degismedi |
| O7 | Maketin gercek kayit mesaji | **Korundu** | Kanit degismedi |
| O8 | Erisilebilirlik | **Kismen duzeltildi** | `disabled` dugme maddesi cikarildi |
| O9 | TL isareti / font / koyu gorunum | **Buyuk olcude geri cekildi** | Kaynakta karakter hatasi yok |
| D1 | Klasor agaci guncel degil | **Korundu** | - |
| D2 | `.gitignore` uzanti kaliplari | **Korundu** | - |
| D3 | 412 MB kurucu kasa icinde | **Korundu** | - |
| D4 | Lisans esigi | **Kismen duzeltildi** | Ciro tek basina olcut degil |
| D5 | Acilis bakiyesi satiri | **GERI CEKILDI** | Bilincli tasarim, callout'ta anlatilmis |

**Yeni bulgular:** O4-YENI (sandbox grubu ACL'i), D6 (`UUSport-Gorsel-Ornekler/`),
D7 (denetim sirasinda depoya paralel mudahale).

---

## 1. Bulgu turlerine gore ayrim

Karisikligi onlemek icin dort tur ayri tutulur:

**A. Gerceklesmis hata (calisan ortamda olmus):**
- Y1: Servis modu kurulumu yapilip sonradan sokulmus; depoda her iki yol da duruyor.
- Y2: Parola komut satiri argumaniyla kurucuya verilmis (kurulum tamamlanmis).
- O1: Kaba onizlemede dort bozuk tablo satiri.

**B. Tasarim acigi (kod yazilmadan kapatilmali):**
- K1: Fatura kaydinin cariye ikinci borc yazma ihtimali.
- Y3: Para alani tipi celiskisi.
- Y5: Fatura onizlemesinde matrah/iskonto/KDV/genel toplam ayrimi yok.
- O5: Tahsilat ekraninda tarih, kasa/banka hesabi, tahsil eden alanlari yok.
- O6: Filtrelenen satir toplami ile cari toplaminin ayrilmamasi.
- O8: Sekme semantigi, klavye gezinmesi ve erisilebilir hata bildirimi.

**C. Dokumantasyon tutarsizligi:**
- K2, K3-duzeltmesi, Y4, Y6, Y7, Y8, O2, O3, D1, D4, D7.

**D. Potansiyel guvenlik riski (gerceklesmis ihlal degil):**
- O4-YENI: Calisan kumenin veri klasoru ve gizli bilgi klasoru sandbox grubuna okunabilir.
- K2: Kaynak dosyalarin Git calisma agaci ve Obsidian kasasi icinde bulunmasi.
- Y2'nin kalinti riski: kurulum logunun icerigi denetlenmedi.
- D2: `.gitignore` uzanti bazli savunma icermiyor.
- D6: Ignore edilmeyen yeni klasor.

---

## 2. GERI CEKILEN BULGULAR

### K3 - "PostgreSQL kurulu degil" — GERI CEKILDI

Surum 1.0'da ana hafizadaki `[DOGRU]` PostgreSQL satirlarinin gercegi yansitmadigi
iddia edilmisti. **Bu iddia yanlistir ve tumuyle geri cekilmistir.**

**Dogrulama (`BAKON-DEV01` / `Ensaradmin`, 25 Eylul 2026):**

| Kontrol | Sonuc |
|---|---|
| `ls "/c/Program Files/PostgreSQL"` | `18` mevcut |
| Kullanici modu veri dizini | `%LOCALAPPDATA%\UUSport\PostgreSQL\18\data` dolu, `PG_VERSION` = `18` |
| `netstat -ano \| grep :5432` | `127.0.0.1:5432` **ve** `[::1]:5432` LISTENING |
| `pg_controldata` | `Database cluster state: in production`, `Data page checksum version: 1` |
| `sc query ... \| grep postgres` | **Windows servisi yok** |
| `pgAdmin 4/web/version.py` | `APP_RELEASE = 9`, `APP_REVISION = 17` -> **9.17** |

Buna gore ana hafizanin su ifadeleri **dogrudur**:
- Satir 20-21: PostgreSQL 18.6 yerel gelistirme sunucusu calisiyor ve baglanti kabul ediyor.
- Satir 22: pgAdmin 4 9.17 kurulu.
- Satir 297: "Bu kurulum Windows servisi degildir" — dogru; servis bulunamadi.
- Satir 293: veri klasoru `%LOCALAPPDATA%\UUSport\PostgreSQL\18\data` — dogru.

**Hatanin nedeni:** Denetim sirasinda ortam dogrulama komutu reddedildiginde
kullanicinin kisa beyani ortam kaniti yerine kabul edildi. Bu beyanin buyuk
olasilikla **henuz kurulum yapilmamis canli hedef bilgisayara** iliskin oldugu,
gelistirme makinesini kapsamadigi anlasilmistir. `[DOGRU]` etiketini curutmek icin
beyan degil komut ciktisi gerekir; bu kural denetleyen icin de gecerlidir.

**Yerine gecen kucuk bulgu (Dusuk):** Ana hafiza satir 292 ve `README.md` satir 43
"yalniz `127.0.0.1:5432`" diyor. Sunucu ayrica **IPv6 loopback `[::1]:5432`**
adresinde de dinliyor. Ikisi de yerel geri dongu adresidir, disari acik degildir;
ancak ifade eksiktir. Duzeltme: "yalniz yerel geri dongu (`127.0.0.1` ve `::1`)".

**Ayrica:** Ana hafiza gelistirme ortami ile canli hedefi ayirmiyor. Bolum 0'daki
PostgreSQL satirlarina "gelistirme makinesi `BAKON-DEV01`" ibaresi eklenirse ayni
karisiklik tekrar etmez.

---

### O4 - Program Files veri klasoru ACL'i — GERI CEKILDI

Surum 1.0'da `repair-postgresql-dev.ps1` satir 67-76'daki ACL islemi nedeniyle
`C:\Program Files\PostgreSQL\18\data` icin yerel `Users` okuma hakki kalabilecegi
soylenmisti. **Dogrulama:** o klasor **bostur**; calisan kume orada degildir.
Gercek ACL kaniti olmadan "herkes erisebilir" sonucu cikarilmasi hataliydi; geri
cekilmistir. Betikteki kalitim kesme eksikligi teknik olarak halen dogrudur, ancak
bu betik kullanildiginda anlam kazanir (bkz. Y1).

---

### O9 - TL isareti bozuk — BUYUK OLCUDE GERI CEKILDI

**Dogrulama:**

| Dosya | U+20BA (TL isareti) | U+0141 (cizgili L) |
|---|---:|---:|
| `docs/designs/cari-hesap-mockup.html` | 29 | 0 |
| `docs/designs/UUSPORT_MUHASEBE_KABA_ONIZLEME.html` | 22 | 0 |

**Kaynak dosyalarda karakter hatasi yoktur.** Surum 1.0'daki "ekran goruntulerinde
TL isareti yerine cizgili L gorunuyor" gozlemi, PNG'lerin denetleyen tarafindan
gorsel okunmasina dayaniyordu; bagimsiz olarak dogrulanamaz ve buyuk olasilikla
okuma hatasidir. Geri cekilmistir.

**Kalan dusuk siddetli not:** `cari-hesap-mockup.html` satir 33'te `Inter` fontu
bildiriliyor, dosyada `@font-face` veya font baglantisi yok. Sistem fontuna dusmek
tek basina hata degildir; yalnizca "tasarim Inter ile yapildi" varsayimiyla
degerlendirme yapan kisi farkli bir tipografi gorur. Satir 9'daki
`color-scheme: light` sabittir ve koyu gorunum degiskeni yoktur; ana hafiza satir
206 koyu gorunumu tasarim ilkeleri arasinda sayiyor. Ikisi de `[ONERI]` duzeyindedir.

---

### D5 - Acilis bakiyesi satiri — GERI CEKILDI

Ust kartlarin filtreden bagimsiz genel toplami gosterdigi, ekrandaki callout metninde
acikca anlatilmistir. Bilincli tasarim kabul edilmistir.

---

## 3. KORUNAN BULGULAR (revize edilmis)

### K1 - Faturanin cariye ikinci kez borc yazma riski

**Onem:** Kritik · **Tur:** Tasarim acigi · **Durum:** Korundu, yeniden siniflandirildi

Bu **calisan uygulamada gerceklesmis bir hata degildir**; uygulama kodu henuz yoktur.
Kod yazilmadan once kapatilmasi gereken tasarim acigidir.

**Dosya:** `docs/designs/MUHASEBE_CARI_STOK_BELGE_AKISI_TASLAK.md` bolum 5 (satir
142-153) ve bolum 7 (satir 167-178); `docs/standards/UUSPORT_POSTGRESQL_ADLANDIRMA_VE_MIGRATION_STANDARDI.md`
bolum 5 (satir 105-107); `docs/designs/cari-hesap-mockup.html` satir 982-995.

**Kanit:** Akis 5.3 "Satis satiri `Fatura Bekliyor` durumunda cariye baglanir" —
cikis satiri cariye borc yaziyor. 5.4 faturayi bu satirlardan uretiyor. Standart
bolum 5 dort kaydin ayri oldugunu soyluyor. Mockup'ta fatura satiri cari hareket
tablosunda Borc 7.500,00 TL ile gorunuyor. "Belge kaydi cariye tutar yazmaz" kurali
hicbir belgede yok.

**Kabul senaryosu (kullanici tarafindan verildi, `[KULLANICI KARARI]`):**

1. 2.500 TL urun teslimi -> cariye 2.500 TL borc, stok 1 adet duser.
2. Ayni teslimin sonradan faturalanmasi -> borc **yine 2.500 TL**; stok ikinci kez dusmez.
3. 1.000 TL tahsilat -> kalan borc 1.500 TL.

**Tek cumle yetmez.** Asagidaki senaryolar da yazili kurala baglanmalidir:

- **Kismi faturalama:** 10 adetlik teslimin 4 adedi faturalandiginda faturalanan ve kalan miktar ayri izlenir; cari borcu degismez.
- **Toplu fatura:** Bir fatura birden cok cikisi toplayabilir; toplam borc, toplanan cikislarin borcunu asamaz.
- **Iskonto ve fiyat farki:** Fatura tutari cikis satirindan farkliysa fark ayri ve gerekceli bir hareket olarak yazilir; sessizce ustune yazilmaz.
- **Iade:** Ilk cikisa bagli ters stok ve ters cari hareketi; belge iadesi ayri kayittir.
- **Iptal:** Fatura iptali stok cikisini yok etmez; cikis satiri `Fatura Bekliyor` durumuna doner.
- **Tekrar gonderim:** Ayni belgenin yeniden gonderilmesi yeni cari hareketi uretmez; idempotency anahtari kullanilir.
- **Taslak fatura duzenlenmis resmi belge sayilmaz.** `draft_document` ile `documented` durumlari cari toplamlarinda ve raporlarda ayri gorunur.

**Aksiyon:** Bu yedi senaryo akis belgesi bolum 5'e ve standart bolum 5'e yazilsin;
`03_verify` adimina "bir satis = bir borc hareketi" mutabakat sorgusu eklensin.

---

### K2 - Kaynak dosyalarin yeri ve ana hafizadaki yol

**Onem:** Yuksek (Kritik'ten indirildi) · **Tur:** Dokumantasyon + potansiyel risk
**Durum:** Kismen duzeltildi

**Dogrulanan:** `C:\Users\Ensaradmin\Desktop\uu sport ltd\` klasoru **yoktur**.
Uc dosya `C:\UUSport\uu sport ltd\` altindadir. Ana hafiza satir 97 ve satir 110
("Kaynak dosyalar masaustundeki asil konumlarinda korunur") **guncel degildir**.

**Geri cekilen:** "Tek nusha" ifadesi. `C:\Users\Ensaradmin`, `C:\UUSport`, `D:` ve
`E:` koklerinde altinci seviyeye kadar yapilan **ad bazli** aramada baska kopya
bulunmadi; ancak bu arama tum surucileri, ag paylasimlarini, bulut eslemelerini ve
yedekleri kapsamaz. Dogru ifade: "bu aramada bulunan tek kopya". Dosyalari kimin,
ne zaman tasidigi **kanitlanmamistir** ve iddia edilmemektedir.

**Korunan uyari:** `.gitignore` yalnizca Git'i engeller. Obsidian kasa eslemesi,
klasor yedegi, dosya senkronizasyonu ve dogrudan dosya erisimi `.gitignore` ile
engellenmez. Kaynak dosyalarin calisma agaci icinde bulunmasi bu yollarla disari
cikma riskini tasir.

**Aksiyon:** Ana hafiza 2.1 ve 2.2 gercek yola gore duzeltilsin. Dosyalarin repo
disina (`%LOCALAPPDATA%\UUSport\incoming\<batch_id>`) tasinip tasinmayacagi
**kullanici karari**dir; bu denetimde tasinmadi ve silinmedi.

---

### Y1 - Iki PostgreSQL kurulum yolu — KORUNDU VE GUCLENDI

**Onem:** Yuksek · **Tur:** Gerceklesmis hata + dokumantasyon
**Durum:** Korundu; artik varsayim degil, kayitli gecmis

**Yeni kanit:** `C:\Program Files\PostgreSQL\18\installation_summary.log`

```
Data Directory: C:\Program Files\PostgreSQL\18\data
Database Port: 5432
Operating System Account: NT AUTHORITY\NetworkService
Database Service: postgresql-x64-18
===== Installation completed at: 2026-09-23 - 17:07:30 =====
```

Bu degerler `tools/install-postgresql-dev.ps1` satir 13-16'daki degerlerle birebir
ortusuyor (`[CIKARIM]`: betik calistirilmistir). **Mevcut durum ise sudur:**

- `C:\Program Files\PostgreSQL\18\data` klasoru **bostur** (`PG_VERSION` yok).
- `postgresql-x64-18` adinda Windows servisi **yoktur**.
- Calisan kume, ayni gun 16:17'de olusturulan kullanici modu kumesidir.

Yani servis modu kurulumu yapilmis, ardindan sokulup kullanici modu kumesine
gecilmistir. Catisma **varsayimsal degil, bir kez yasanmis** bir durumdur. Her iki
betik de depoda duruyor ve hangisinin gecerli oldugu hicbir belgede yazmiyor.

**Aksiyon:** Tek yol secilsin ve ana hafiza 5.3'e yazilsin. `[ONERI]` Kullanici modu
kume kalsin; `install-postgresql-dev.ps1` ve `repair-postgresql-dev.ps1` ya kaldirilsin
ya da baslarina "kullanim disi — kullanici modu kume gecerlidir" notu konsun.
**Bu denetimde hicbir betik silinmedi.**

---

### Y2 - Parolanin komut satiri argumaniyla verilmesi

**Onem:** Yuksek · **Tur:** Gerceklesmis uygulama hatasi + kalinti risk
**Durum:** Korundu; etkisi kanitla sinirlandirildi

**Dosya:** `tools/install-postgresql-dev.ps1` satir 51-66. `--superpassword` argumani
duz metin parolayla kurucuya komut satirindan geciriliyor. Calisma aninda islem komut
satiri ayni makinedeki diger sureclerce gorulebilir.

**Kurulumun gerceklestigi dogrulandi** (Y1'deki `installation_summary.log`,
2026-09-23 17:07:30). Dolayisiyla bu maruziyet **bir kez fiilen olusmustur**.

**Sinirlandirma — asiri yorumdan kacinildi:** `installation_summary.log` dosyasinda
"password" gecen satir yoktur. `%TEMP%\install-postgresql.log` (2,7 MB) dosyasinda
"superpassword" gecen satir **yoktur**, "password" gecen 43 satir vardir. **Bu
dosyanin icerigi, gizli bilgi aciga cikmasin diye okunmamistir.** Parolanin bir log
dosyasina yazildigi **kanitlanmamistir**.

**Aksiyon:** `[ONERI]` (a) Betik, secenek dosyasi yontemine veya kurulum sonrasi
`ALTER ROLE` yaklasimina cevrilsin — depodaki `initialize` ve `repair` betikleri zaten
dogru ornekleri iceriyor. (b) Ihtiyati tedbir olarak `postgres` superuser parolasi
yenilensin ve `%TEMP%\install-postgresql.log` kullanici tarafindan incelenip silinsin.

---

### Y3 · Y4 · Y5 · Y7 — Degismeden korundu

| No | Ozet | Kanit | Aksiyon |
|---|---|---|---|
| Y3 | Spesifikasyon satir 598 `bigint` kurus diyor; standart satir 82 `numeric(18,2)` diyor. Yuvarlama politikasi hicbir belgede yok | Iki belge karsilastirmasi | ADR; hem tip hem yuvarlama ve API tasima bicimi kapatilsin |
| Y4 | Spesifikasyon satir 61 "Yurt ici (Turkiye) — zorunlu, cocuk verisi nedeniyle (O5, O72)". Operasyon proseduru bolum 4 ve 10, ana hafiza bolum 8'de ulke sarti yok | Uc belge taramasi | Prosedur bolum 4 ve 10'a madde; bolum 12 envanterine "yerlesim ulkesi" alani |
| Y5 | Fatura taslagi onizlemesi yalniz kalem ve toplam gosteriyor; matrah, iskonto, KDV orani, KDV tutari ve genel toplam yok. Forma seti %20, egitim %10 kalemi | `cari-hesap-mockup.html` satir 1175-1181; `cari-hesap-fatura-dialog-mockup.png` | Onizlemeye dort alan ve MEB kapsam rozeti eklensin |
| Y7 | Spesifikasyon satir 498 e-belge entegratorunu "MVP ilk modulu" sayiyor; ana hafiza 4.2, akis bolum 8 ve rapor bolum 15'te hic gecmiyor | Dort belge taramasi | Ilk surumdeki yeri karara baglansin; disarida kalacaksa yazili kabul olsun |

---

### Y6 - ADR yoklugu — KISMEN GERI CEKILDI

**Onem:** Yuksek · **Tur:** Dokumantasyon tutarsizligi

**Korunan:** `docs/decisions/` bostur (yalniz `.gitkeep`). Ana hafiza satir 59 ve
173-175 teknoloji yigini karari `[ACIK]` diyor. ADR yoklugu gercektir ve kodlama
oncesi kapatilmasi gereken en oncelikli belge eksigidir.

**Geri cekilen:** "Mimari fiilen secilmis" yorumu. Bos `src/UUSport.*` klasorleri,
`.vscode` eklenti onerisi, `.editorconfig` girinti ayari ve indirilmis bir kurulum
dosyasi **mimarinin uygulandigini kanitlamaz**. Bunlar hazirlik izidir, karar degildir.
"Klasorleri geri al" onerisi de geri cekilmistir; **klasor silmek yerine karar yazilmalidir.**

**Ayrica duzeltme:** Surum 1.0 ASP.NET Core API secimi ile WPF masaustu secimini tek
bir "yon" gibi ele almisti. Bunlar **ayri kararlardir**: API'nin ASP.NET Core olmasi
masaustunun WPF olmasini gerektirmez; masaustu WPF, web React, mobil PWA olabilir.
ADR bu uc katmani ayri ayri karara baglamalidir.

---

### Y8 - Excel incelemesi — KISMEN GERI CEKILDI

**Onem:** Orta (Yuksek'ten indirildi) · **Tur:** Dokumantasyon tutarsizligi

**Geri cekilen:** "Belge hem inceledik hem incelemedik diyor, bu bir celiskidir"
degerlendirmesi. Ana hafiza bolum 7'deki inceleme sonucu ile satir 73-74'teki
"bu denetimde incelenmedi" ifadesi **farkli tarihlere ve farkli kapsamlara** ait
olabilir; tek baslarina celiski olusturmazlar. Kisisel veri ihlali oldugu yolundaki
ima da kanit bulunmadigi icin geri cekilmistir.

**Korunan (daraltilmis):** Ana hafiza bolum 7, incelemenin **hangi tarihte, hangi
aracla ve hangi sayfalar okunarak** yapildigini kaydetmiyor. Kaynak veri incelemesi
gibi hassas bir islemde bu kaydin tutulmamasi izlenebilirlik eksigidir.

**Aksiyon:** Bolum 7'ye tarih, arac ve kapsam satiri eklensin. Excel'in kisisel veri
icerip icermedigi `[ONERI]` insan tarafindan, cevrimdisi ve modele okutulmadan
degerlendirilsin. **Bu denetimde Excel ve PDF acilmadi.**

---

### O1 · O5 · O6 · O7 — Degismeden korundu

| No | Ozet | Kanit |
|---|---|---|
| O1 | `UUSPORT_MUHASEBE_KABA_ONIZLEME.html` satir 5'te dort hareket satirinda aciklama hucresinin **acilis etiketi eksik**; `thead` bes sutun, `tbody` dort hucre. Aciklamalar tablonun disina tasinir, sutunlar bir sola kayar | Dosyada dort eslesme dogrulandi |
| O5 | `cari-hesap-mockup.html` satir 1131-1151 ve `tahsilat-al-mockup.png`: tahsilat dialogunda yalniz odeme araci, tutar ve referans var. Akis belgesi satir 132 ayrica **tarih, kasa/banka/POS hesabi ve tahsil eden** istiyor. Kaba onizlemede tarih alani var; gelismis makette yok | Belge ile ekran karsilastirmasi |
| O6 | `cari-hesap-mockup.html` satir 1201-1220: bakiye ozeti yalniz aktif sekmeye gore yaziliyor, arama terimini hesaba katmiyor. Sekme rozetleri (5/2/3) sabit. Arama yapildiginda "1 hareket gosteriliyor" ile cari toplami yan yana cikar | Kod okumasi |
| O7 | `cari-hesap-mockup.html` satir 1361-1363 ve 1394: "stoktan dusuldu", "fatura taslagi kaydedildi" mesajlari veriliyor; tabloya satir eklenmiyor, stok degismiyor. Kaba onizleme satir 10 bunu dogru yapiyor ("Gercek kayit olusturulmadi") | Iki dosyanin karsilastirmasi |

---

### O2 - Kume yerel ayari — KISMEN GERI CEKILDI

**Onem:** Dusuk (Orta'dan indirildi) · **Tur:** Dokumantasyon eksigi

**Geri cekilen:** "initdb anindaki yerel ayar sonradan degistirilemez, yeniden initdb
gerekir" ifadesi **teknik olarak asiriydi**. Dogrusu:

- Veritabani **varsayilan** harmanlamasi ile **sutun/ifade bazli** `COLLATE` secenegi ayri seylerdir.
- Sutun ve sorgu duzeyinde `COLLATE` istenildigi zaman verilebilir.
- Yeni bir veritabani farkli varsayilan harmanlama ile olusturulabilir.
- **UUSport veritabani henuz olusturulmamistir**, dolayisiyla harmanlama halen serbestce secilebilir. Kume yeniden kurulmasi gerekmez.

**Dogrulanan:** `postgresql.conf` icinde `lc_messages`, `lc_monetary`, `lc_numeric`,
`lc_time` = `C`; `default_text_search_config = 'pg_catalog.english'`.

**Dogrulanamadi:** Betiklerdeki `--icu-locale=tr-TR` degerinin mevcut veritabanlarinda
etkin olup olmadigi. PostgreSQL 15 ve sonrasinda harmanlama bilgisi `pg_controldata`
ciktisinda yer almaz; sunucuya baglanmak gerekir, bu da DPAPI korumali parola kullanimi
demektir ve bu salt okunur turun disindadir.

**Korunan (daraltilmis):** Standart belgesi harmanlama politikasi hakkinda **hicbir sey
soylemiyor**, oysa betikler bir deger sabitliyor. Turkce harmanlamada noktasiz i/I
kurali, e-posta ve kod gibi ASCII alanlarda buyuk-kucuk harf davranisini degistirebilir.

**Aksiyon:** Standarda kisa bir bolum: veritabani varsayilan harmanlamasi; hangi
sutunlarin `COLLATE "C"` kullanacagi (kod, e-posta, dis sistem anahtari, hash);
hangilerinin Turkce harmanlama kullanacagi (ad, unvan, arama).

---

### O3 - "Yalniz 127.0.0.1" — KISMEN GERI CEKILDI

**Onem:** Dusuk (Orta'dan indirildi) · **Tur:** Dokumantasyon eksigi

**Geri cekilen:** Sunucunun belgelenenden genis bir yuzeyde dinliyor olabilecegi ima
edilmisti. **Dogrulama bunu curutuyor:**

`pg_hba.conf` etkin satirlari yalnizca sunlardir:

```
local   all          all                      scram-sha-256
host    all          all    127.0.0.1/32      scram-sha-256
host    all          all    ::1/128           scram-sha-256
local   replication  all                      scram-sha-256
host    replication  all    127.0.0.1/32      scram-sha-256
host    replication  all    ::1/128           scram-sha-256
```

`netstat` ciktisi da yalniz `127.0.0.1:5432` ve `[::1]:5432` gosteriyor. Sunucu
**disari acik degildir**; kimlik dogrulama her yolda `scram-sha-256`'dir.

**Korunan (cok daraltilmis):** Depodaki hicbir betik bu ayarlari **yazmiyor veya
dogrulamiyor**; mevcut dogru durum initdb varsayilanlarindan geliyor. Bir sonraki
kurulumda ayni sonucu garanti eden bir kontrol yok.

**Aksiyon:** `initialize-postgresql-user-dev.ps1` sonuna `listen_addresses` ve
`pg_hba.conf` teyidi eklensin; her calistirmada dogrulansin.

---

### O4-YENI - Sandbox grubunun veri ve gizli bilgi klasorlerine okuma hakki

**Onem:** Orta · **Tur:** Potansiyel guvenlik riski · **Durum:** Yeni bulgu

**Dogrulama (`icacls`):**

```
%LOCALAPPDATA%\UUSport                        CodexSandboxUsers:(I)(OI)(CI)(RX)
%LOCALAPPDATA%\UUSport\PostgreSQL\18\data     CodexSandboxUsers:(I)(OI)(CI)(RX)
%LOCALAPPDATA%\UUSport\secrets                CodexSandboxUsers:(I)(OI)(CI)(RX)
```

`net localgroup CodexSandboxUsers`: aciklama "Codex sandbox internal group (managed)",
uyeler `CodexSandboxOffline` ve `CodexSandboxOnline`.

**Etki — olcusuyle birlikte:**

- Calisan kumenin **ham veri dosyalari** bu gruba okunabilir durumdadir. Dosya duzeyinde okuma, RLS ve API yetki katmanini tumuyle atlar. **Su an veritabaninda proje verisi yoktur**, dolayisiyla etki ileriye donuktur.
- `secrets` klasoru de okunabilir. **Ancak** icindeki dosya `ConvertFrom-SecureString` ile uretilmistir ve DPAPI ile **kullaniciya baglidir**; dosyayi okumak tek basina parolayi vermez. Parolanin aciga ciktigi **iddia edilmemektedir**.
- Haklar `(I)` ile **kalitimlidir**; proje betikleri tarafindan verilmemistir, ust klasorden gelmektedir.

**Aksiyon:** `[ONERI]` (a) Bu kalitimin bilincli olup olmadigi karara baglansin.
(b) Gelistirme kumesinde yalnizca maskeli/sentetik veri tutulacagi zaten karardir;
bu karar operasyon prosedurune "gelistirme veri klasoru sandbox hesaplarina okunabilir
olabilir" notuyla birlikte yazilsin. (c) **Canli ortamda** veri ve gizli bilgi
klasorleri icin kalitim kesme ve en az yetki zorunlu kilinsin; bu, prosedur bolum 12
envanterinin kontrol maddesi olsun. Bu denetimde **hicbir ACL degistirilmedi**.

---

### O8 - Erisilebilirlik — KISMEN DUZELTILDI

**Onem:** Orta · **Tur:** Tasarim acigi

**Geri cekilen:** "Kaydet dugmesinin `disabled` yapilmasi erisilebilirlik hatasidir"
maddesi. Gecersiz formda dugmenin devre disi birakilmasi tek basina hata degildir.

**Korunan ve yeniden ifade edilen** (`cari-hesap-mockup.html`):

- Satir 912-915: `role="tablist"` ve `role="tab"` var; `aria-controls` ve `role="tabpanel"` yok, ok tusuyla gezinme yok. Sekme semantigi yarim.
- Satir 1237-1242: `tabindex="0"` verilmis tablo satirlari tiklaniyor ve Enter/Space dinliyor, ancak `role` yok; ekran okuyucu etkilesimli oldugunu duyurmaz.
- Satir 1330-1336: Iskonto siniri asildiginda hata metni yaziliyor fakat `aria-live` veya `role="alert"` yok. **Asil sorun budur:** dugmenin kapali olmasi degil, **kapali olma nedeninin erisilebilir bicimde duyurulmamasidir**.
- Satir 919: `sr-only` sinifi CSS'te tanimli degil, ayni ogede `hidden` var. Olu isaretleme.

---

## 4. Yeni ve guncellenen dusuk siddetli bulgular

| # | Bulgu | Kanit | Aksiyon |
|---|---|---|---|
| D1 | Ana hafiza bolum 6 klasor agaci guncel degil: `docs/reviews`, `tools/installers`, `src` altindaki dort klasor ve `uu sport ltd/` yok; "src/ ... su an bos" yaziyor | Satir 317-337 ile gercek agac | Agac yeniden uretilsin |
| D2 | `.gitignore` yalniz yol bazli; `*.xlsx`, `*.xls`, `*.pdf`, `*.pem`, `*.p12`, `*.crt`, `id_rsa` kaliplari yok | `.gitignore` satir 31-38 | Uzanti bazli savunma ve commit oncesi kanca |
| D3 | `postgresql-18.6-4-windows-x64.exe` 412 MB, Obsidian kasasi icinde. Git disinda ama kasa indeksleme ve klasor yedegini etkiler | `tools/installers/` | Prosedur bolum 13'un ongordugu repo disi klasor |
| D4 | Lisans uygunlugu **yalniz ciroyla** karara baglanamaz. Surum 1.0'da 2025 cirosuna dayanilarak "buyuk ihtimalle kapsam ici" denmisti; bu yetersiz olcuttur | Spesifikasyon satir 35 | Visual Studio Community ve C# Dev Kit icin **guncel kurulus ve kullanim sartlari** kontrol edilsin, sonuc ADR'ye yazilsin |
| **D6** | `UUSport-Gorsel-Ornekler/` proje kokunde, **takip edilmiyor ve `.gitignore` kapsaminda degil**. `git status` `??` ile gosteriyor. Bir `git add .` ile commit'e girer | `git status --short` | Ya `.gitignore`'a eklensin ya da bilincli olarak commit edilsin |
| **D7** | Denetim surerken depo paralel olarak degistirildi: `.vscode/settings.json` icine `markdown.validate.enabled` eklenmis. Bu raporun satir numaralari sabit bir commit'e degil, calisma agacinin o anki haline dayaniyor | `git diff .vscode/settings.json` | Sonraki denetimler sabit bir commit uzerinde yapilsin |

---

## 5. Dogrulanan durum

**Komutla dogrulanmis olumlu bulgular:**

- Git gecmisi temiz: `git rev-list --objects --all` ciktisinda `uu sport ltd/` altindaki hicbir dosya, hicbir `.xlsx`, `.pdf` veya anahtar yok. `.gitignore` satir 36'daki `uu sport ltd/` girdisi etkili.
- PostgreSQL 18 kullanici modu kumesi calisiyor, `Database cluster state: in production`, veri sayfasi saglama (checksum) **etkin**.
- Ag yuzeyi kapali: yalniz `127.0.0.1` ve `::1`, her yolda `scram-sha-256`.
- Windows servisi yok — ana hafizanin iddiasiyla uyumlu.
- pgAdmin 4 9.17 kurulu — ana hafizanin iddiasiyla uyumlu.
- `initialize` ve `repair` betikleri parolayi dogru sekilde (parola dosyasi / stdin) isliyor.
- Kaynak HTML dosyalarinda TL isareti dogru kodlanmis (29 ve 22 adet U+20BA, sifir hatali karakter).
- `cari-hesap-mockup.html` rakamlari aritmetik olarak tutarli: Borc 11.200, Alacak 19.500, net 8.300 avans; dort ozet karti uyusuyor. Dort PNG, HTML'in gercek ciktisidir.
- Satis ve tahsilat cift gelir olarak sayilmiyor; borc/alacak ayrimi dogru.
- Vergi simulasyonu resmi beyan gibi sunulmuyor: rapor belgesi bolum 3, 12 ve 14 gerekli ayrimlari ve uyarilari iceriyor.
- Durum yalniz renkle anlatilmiyor; rozetlerde metin ve simge birlikte.
- Apple Design Skill dosyasi mevcut (23 KB).

**Gercekten yok:** uygulama kodu, UUSport veritabani semasi, migration, seed, test,
release paketi, canli kurulum, ADR. **Canliya alindi izlenimi veren hicbir ifade
bulunmadi.** Kurulum paketinin veya betigin diskte bulunmasi canli kurulum kaniti
sayilmamistir; ilk canli hedef bilgisayar bu denetimde hic incelenmemistir.

---

## 6. Dogrulanamayan iddialar

1. Excel'in 17 sayfa icerdigi, `Sayfa1 (2)`nin guncel liste oldugu, `2025 FIYAT`ta 58 formulun 29'unda `#REF!` bulundugu. **Dosya acilmadi.**
2. Excel'in gercek kisisel veri icerip icermedigi. **Dosya acilmadi**; ihlal iddia edilmiyor.
3. Betiklerdeki `--icu-locale=tr-TR` degerinin mevcut veritabanlarinda etkin olup olmadigi (bkz. O2).
4. `%TEMP%\install-postgresql.log` icinde parola bulunup bulunmadigi. **Icerik okunmadi.**
5. Kaynak dosyalarin baska surucu, ag paylasimi, bulut eslemesi veya yedekte kopyasi olup olmadigi.
6. Kaynak dosyalari kimin, ne zaman tasidigi.
7. Ilk canli hedef bilgisayarin donanimi (Windows 11 Pro / i3-10100 / 8 GB RAM) ve 8 GB RAM'in yeterliligi. **Makine incelenmedi.**
8. Surum 0.10 PDF'in icerigi. **Acilmadi.**
9. 2025 cirosu. Yalniz spesifikasyondan alinti.
10. `Sayfa1 (2)` seciminin dogrulugu — bu bir `[KULLANICI KARARI]`dir, teknik olarak dogrulanamaz ve karar olarak kabul edilmistir.

---

## 7. Ortam kurgusu (korunan)

`[KULLANICI KARARI]` Asagidaki kurgu degismemistir:

1. Gelistirme ortami ayridir. Bu denetim `BAKON-DEV01` uzerinde yapilmistir.
2. Ilk canli kurulum, kullanicinin mevcut normal Windows bilgisayarina yapilacaktir. **Bu makine henuz kurulmamis ve incelenmemistir.**
3. Daha sonra canli sistem, yeni kiralanacak ayri bir RDP/sunucuya tasinacaktir.
4. Gelistirme RDP'si ile gelecekteki canli RDP ayni ortam degildir; gelistirme ortami hicbir asamada canli veri hedefi olmaz.
5. Kurulum paketinin veya betigin bulunmasi, canli kurulumun tamamlandigini kanitlamaz.
6. Gelistirme ve testte yalniz maskeli veya sentetik veri kullanilir.

---

## 8. Ilk calisan surum kapsami (guncellendi)

`[KULLANICI KARARI]` Oncelik: Excel'den verileri alip muhasebeyi kullanmaya baslamak.
Surum 1.0'daki dikey dilim bu onceligi karsilamiyordu; **aktarim adimlari eklenmistir.**

**Cekirdek:**
1. Tek kiraci, kullanici girisi, iki rol (yonetici, muhasebe)
2. Cari karti ve hareket turleri
3. Stok/hizmet cikisi ve tahsilat
4. Uc gorunum: Toplam / Belgeli-Resmi / Muhasebe Bekleyen
5. Append-only denetim kaydi
6. Gunluk tahsilat ve bekleyenler listesi

**Excel aktarimi (ayni surumde, eksik birakilamaz):**

7. **Sutun eslestirme ve aktarim onizlemesi** — kaynak sayfa `Sayfa1 (2)` `[KULLANICI KARARI]`; benzer isimli sayfalar otomatik birlestirilmez
8. **Satir dogrulama ve hata listesi** — alan, satir numarasi ve hata koduyla
9. **Tekrarli aktarimi engelleme** — dosya hash'i ve is anahtari bazli idempotency
10. **Acilis bakiyelerinin muhasebe onayi** — onaysiz acilis bakiyesi ana tablolara girmez
11. **Aktarim kaydi (`batch`) ve kontrollu duzeltme** — dry-run ile canli sonucu ayri; hatali aktarim ters kayitla geri alinir, silinerek degil

Gelistirme ve testte **maskeli veya sentetik veri** kullanilir.

**Ilk surumun disinda birakilmasi onerilenler:** simulasyon motoru, mobil uygulamalar,
performans modulu, coklu kiraci devreye alma, banka ekstresi otomatik eslestirme,
e-belge **gonderimi** (taslak uretimi kalabilir — bkz. Y7).

---

## 9. Oncelikli duzeltme sirasi

### 9.1 Kodlama oncesi zorunlular

Bunlar sema yazildiktan sonra degistirilmesi en pahali olan kararlardir:

1. **K1** — Fatura/cikis/tahsilat arasindaki tutar sorumlulugu ve yedi senaryo (kismi, toplu, iskonto/fiyat farki, iade, iptal, tekrar gonderim, taslak-resmi ayrimi) yazili kurala baglansin.
2. **Y3** — Para alani tipi ve yuvarlama politikasi ADR ile kapatilsin.
3. **Y6** — Teknoloji yigini ADR'si yazilsin; API, masaustu, web ve mobil **ayri ayri** karara baglansin. Klasor silinmesin.
4. **O2** — Harmanlama politikasi standarda yazilsin. UUSport veritabani henuz yok; bu karar simdi bedelsiz alinabilir.
5. **Y7** — E-belge entegratorunun ilk surumdeki yeri karara baglansin.
6. **K3-duzeltmesi + D1** — Ana hafiza duzeltilsin: PostgreSQL satirlari gelistirme makinesi olarak isaretlensin, `::1` eklensin, klasor agaci yenilensin.
7. **Y8 + D7** — Kaynak veri incelemesine tarih/arac/kapsam kaydi eklensin; sonraki denetimler sabit commit uzerinde yapilsin.

### 9.2 Canliya gecis oncesi zorunlular

Bunlar gelistirme sirasinda bekletilebilir, canlidan once kapatilmalidir:

1. **Y4** — Yurt ici barindirma sarti operasyon ve yedekleme belgelerine islensin; ortam envanterine "yerlesim ulkesi" alani girsin. **RDP kiralamadan once.**
2. **Y1 + Y2** — Tek kurulum yolu secilsin; parola komut satirindan cikarilsin; `postgres` parolasi yenilensin ve `%TEMP%` kurulum logu incelenip silinsin.
3. **O4-YENI** — Canli ortamda veri ve gizli bilgi klasorleri icin kalitim kesme ve en az yetki, prosedur bolum 12'nin kontrol maddesi olsun.
4. **O3** — `initialize` betigine `listen_addresses` ve `pg_hba.conf` teyidi eklensin.
5. **K2 + D2 + D6** — Kaynak dosyalarin yeri karara baglansin; `.gitignore`'a uzanti bazli savunma ve `UUSport-Gorsel-Ornekler/` eklensin.
6. **D3 + D4** — Kurulum dosyalari repo disina alinsin; lisans sartlari guncel metinden teyit edilsin.

### 9.3 Arayuz calismasi sirasinda

**Y5** (fatura onizlemesinde matrah/iskonto/KDV/genel toplam), **O5** (tahsilat
ekraninda tarih, kasa/banka hesabi, tahsil eden), **O6** (filtre ve toplam ayrimi),
**O7** (maket mesajlari), **O8** (sekme semantigi, klavye, erisilebilir hata bildirimi),
**O1** (bozuk `td` etiketleri).

---

## 10. Denetim yontemine iliskin not

Surum 1.0'daki en agir hata (K3), bir ortam dogrulama komutu reddedildiginde
kullanicinin kisa beyaninin komut ciktisi yerine gecirilmesinden kaynaklandi.
`[DOGRU]` etiketini curutmek icin dosya, komut veya calisan ortam kaniti gerekir;
bu kural, ana hafizayi yazan icin oldugu kadar denetleyen icin de baglayicidir.
Capraz degerlendirmedeki itirazlar da kanitsiz kabul edilmemis, her biri bu makinede
komutla yeniden sinanmistir.
