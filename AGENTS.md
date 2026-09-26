# UU Sport - Ajan Calisma Kurallari

Bu depo UU Sport / PSG Academy Samsun yonetim platformunun ortak calisma alanidir.

## Tek Ana Hafiza

Calismaya baslamadan once su dosyayi oku:

`docs/project-memory/UUSPORT_ANA_HAFIZA.md`

Bu dosya projenin guncel durumu, dogrulanmis kurulumlari, alinmis kararlari ve acik
konulari icin tek ana hafizadir. Urun ve is kurallarinin ayrintili kaynagi, ana
hafizada belirtilen `psg-academy-samsun-sistem-spesifikasyonu.md` surum 0.11'dir.

Oncelik sirasi:

1. Kullanicinin en son acik talimati
2. Onaylanmis ADR dosyalari (`docs/decisions`)
3. Bu ana hafizadaki guncel proje durumu
4. Surum 0.11 sistem spesifikasyonu
5. Diger notlar ve tarihsel sohbetler

## Calisma Kurallari

- Turkce calis.
- Once mevcut durumu ve ilgili belgeleri oku; dogrulanmamis bilgiyi karar gibi yazma.
- Gercek sporcu, cocuk veya veli verisini repoya, test ortamlarina ya da yapay zeka
  araclarina koyma. Gelistirme ve testte maskeli veya sentetik veri kullan.
- Masaustu, web ve mobil istemciler PostgreSQL'e dogrudan baglanmaz; merkezi HTTPS
  API uzerinden calisir.
- Teknoloji yigini ADR-001 ile kesindir: ASP.NET Core API + React web + React Native
  (Expo) mobil + PostgreSQL; masaustu istemci yoktur. Mimari degisiklik yeni ADR ister.
- Para, yuvarlama ve KDV: ADR-002. Turkce metin ve harmanlama: ADR-003. Cari hareket
  modeli: ADR-004 (cariye yalniz satir, tahsilat/odeme ve onayli acilis bakiyesi yazar).
- Guvenlik kurallari: `docs/standards/UUSPORT_GUVENLIK_STANDARDI.md`.
- Kod degisikliginden sonra `tools/build-uusport.ps1` calistirilir; testler gecmeden is
  bitmis sayilmaz. Arayuz degisikligi calisan programin ekran goruntusuyle dogrulanir.
- Excel, PDF, resim, veri aktarimi, release, canliya alma veya geri yukleme
  calismasinda once
  `docs/operations/UUSPORT_GELISTIRME_VERI_VE_CANLIYA_ALMA_PROSEDURU.md`
  dosyasini oku.
- Schema, SQL veya migration calismasinda once
  `docs/standards/UUSPORT_POSTGRESQL_ADLANDIRMA_VE_MIGRATION_STANDARDI.md`
  dosyasini oku.
- UI/UX tasarimi, mockup, ekran incelemesi veya arayuz kodu calismasinda ana
  hafizada tanimlanan `Apple Design Skill` kullanilir. Ilkeler hedef platforma
  uyarlanir; Windows veya web arayuzu macOS taklidi haline getirilmez.
- Veritabani degisikligini surumlu migration olmadan yapma.
- Canli veriyi gelistirme/teste kopyalama. Canliyi temsil eden maskeli veya
  sentetik veri ve ayni sema/is kurallariyla simulasyon kullan.
- Parola, anahtar ve baglanti gizli bilgilerini Git'e yazma.
- Kaynak klasordeki Excel, PDF ve MD belgelerini salt okunur kabul et; kullanici
  onayi olmadan degistirme veya birlestirme.
- Canliya yayin, veri aktarimi, SQL yazma islemi ve geri yukleme once planlanir ve
  kullanici onayiyla uygulanir.
- Release klasorunun veya paketin varligini canli kurulum kaniti sayma. Surum,
  Git commit'i, migration seviyesi, hash ve smoke test ayri dogrulanir.
- Onaylanan karar veya tamamlanan kurulum sonrasinda ana hafizayi ve degisiklik
  gecmisini guncelle. Yazmadan once dosyanin son halini yeniden oku.
- Claude ve Codex ayni dosyada calisabilir: ayni gun baska ajanin yazdigi satiri ezme,
  birlestir. Canli surum iddiasi yalniz canlidaki surum dosyasi veya kurulum raporuyla
  yazilir; rapor yoklugu "kurulmadi" demek degildir (ENS dersi).
- Claude ve Codex canli ortama dogrudan baglanmaz.

## Model ve efor secimi

Her isin basinda bir satir yazilir: `Model: ... · Efor: ... · Gerekce: ...`. Karar
hisse degil is turune dayanir:

| Is turu | Model | Efor |
|---|---|---|
| ADR, muhasebe/vergi kurali, sema ve RLS tasarimi, guvenlik incelemesi, denetim | Opus | yuksek |
| API ucu, ekran, form, migration, test, hata ayiklama, Excel aktarim kodu | Sonnet | orta |
| Belge guncelleme, yeniden adlandirma, bicim, log okuma | Haiku | dusuk |

Model degistirmek onbellegi sifirlar; isler model bazinda oturumlara gruplanir.
Kullanici modeli ve eforu acikca belirlediyse o gecerlidir.
