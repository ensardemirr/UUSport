# Muhasebe Raporlari ve Odeme Takvimi Taslagi

**Durum:** `[KARAR]` Rapor kapsami  
**Durum:** `[ACIK]` Vergi formulleri, oranlari ve resmi tarihler mali musavir
teyidi bekliyor  
**Tarih:** 24 Eylul 2026  
**Kapsam:** Yonetim ve on muhasebe raporlari

Bu raporlar yonetim, nakit plani ve on muhasebe icindir. Mali musavirin resmi
beyaninin veya GMS.NET kayitlarinin yerine gecmez.

## 1. Ortak rapor filtresi

Butun raporlarda ayni filtre cubugu kullanilir:

- Donem: aylik, 3 aylik, 6 aylik, 9 aylik, yillik ve ozel tarih araligi
- Gorunum: gerceklesen, planlanan, gerceklesen + planlanan
- Belge kapsami: Belgeli/Resmi, Muhasebe Bekleyen, Toplam
- Tesis, sube ve masraf merkezi
- Odeme durumu: odendi, bekliyor, gecikti, kismi odendi
- Para birimi
- Senaryo: baz, iyimser, temkinli

Her raporda `veri tarihi`, secilen filtreler, senaryo ve son yenileme zamani
gorunur. Eksik veya teyitsiz veri sifir gibi gosterilmez; `eksik` veya
`teyit bekliyor` olarak isaretlenir.

## 2. Yonetici ozet ekrani

Ust kartlar:

- Kasa + banka kullanilabilir bakiye
- Bu ay toplam tahsilat
- Bu ay toplam odeme
- 30 gunluk kesin odeme
- 90 gunluk planlanan odeme
- Aylik KDV simulasyonu
- 3/6/9/12 aylik vergi simulasyonu
- Gecikmis borc ve alacak

Ana bloklar:

- Aylik gelir, gider ve net nakit grafigi
- Odeme turlerine gore aylik toplam
- Yaklasan 7, 15 ve 30 gunluk odemeler
- Kasa/banka bakiyesi ile odeme ihtiyacinin karsilastirmasi
- Eksik belge ve Muhasebe Bekleyen islem sayisi

## 3. Aylik KDV simulasyonu

Her ay icin:

- Hesaplanan KDV
- Indirilecek KDV
- Onceki aydan devreden KDV
- Odenecek KDV veya sonraki aya devreden KDV
- Belgeli/Resmi kayitlar
- Muhasebe Bekleyen islemlerin ayri olasi etkisi
- Veri ve belge eksikleri

Formul ozeti:

`hesaplanan KDV - indirilecek KDV - devreden KDV`

Oran, istisna, tevkifat, indirim hakki ve donem kurallari tarih aralikli parametre
olarak tutulur. Raporun ustunde belirgin `SIMULASYON - RESMI BEYAN DEGILDIR`
uyarisi bulunur.

## 4. Vergi simulasyonu

Kullanici secimiyle 3, 6, 9 ve 12 aylik gorunum:

- Gelir
- Gider
- Kanunen kabul edilmeyen gider
- Amortisman ve diger matrah duzeltmeleri
- Tahmini vergi matrahi
- Donem icinde odenen/mahsup edilecek vergi
- Tahmini kalan vergi
- Tahakkuk ayi ve odeme tarihi

Sirketin Ltd olmasi nedeniyle nihai vergi turu `kurumlar vergisi / gecici vergi`
olabilir. `Gelir vergisi` ifadesi kullanici talebi olarak korunur; hangi vergi
turlerinin hesaplanacagi mali musavirin sirket vergi profiliyle kesinlestirilir.

## 5. Aylik odeme takvimi

Tek takvimde su odemeler bulunur:

- Sirket kredi karti ekstre ve taksit odemeleri
- Kredi taksitleri
- Maas odemeleri
- Kira odemeleri
- Muhtasar ve prim hizmet beyannamesi odemeleri
- SGK odemeleri
- KDV ve diger vergi odemeleri
- Tedarikci odemeleri
- Cek/senet vadeleri
- Antrenor hakedisleri
- Diger duzenli ve tek seferlik odemeler

Her satir:

`odeme_turu, aciklama, tahakkuk_donemi, vade_tarihi, tutar, odenen, kalan,
odeme_durumu, banka/kasa, belge_durumu, masraf_merkezi`

Ozetler:

- Bu ay toplam odeme
- Odendi / kalan / geciken
- 3/6/9/12 aylik toplam odeme
- Kategori bazli toplam
- Kasa ve banka bazli cikis
- Kesin ve tahmini odemelerin ayri toplami

## 6. Kredi karti odemeleri

Sirket kredi kartlari ile musteriden alinan POS tahsilatlari karistirilmaz.

Sirket karti raporu:

- Kart/banka adi ve maskeli son dort hane
- Hesap kesim ve son odeme tarihi
- Donem harcamasi
- Taksitli islemler ve kalan taksit
- Faiz, komisyon ve diger masraf
- Odenen ve kalan ekstre
- Gecikme durumu

POS raporu:

- Brüt tahsilat
- Komisyon
- Net banka girisi
- Valor tarihi
- Bekleyen ve hesaba gecen tutar
- Fatura/belge eslestirme durumu

## 7. Kredi odemeleri

Her kredi icin:

- Banka ve kredi adi
- Baslangic ve bitis tarihi
- Ana para
- Faiz, vergi ve masraf
- Taksit tarihi ve taksit tutari
- Odenen taksit
- Kalan ana para
- Erken kapama veya yapilandirma bilgisi
- 3/6/9/12 aylik nakit cikisi

Ana para odemesi ile faiz gideri ayri raporlanir.

## 8. Maas, SGK ve muhtasar

Maas raporu:

- Brut ucret
- Net odeme
- Isci kesintileri
- Isveren SGK maliyeti
- Diger yan hak ve kesintiler
- Tahakkuk ayi ve fiili odeme tarihi
- Tesis/masraf merkezi

SGK raporu:

- Donem tahakkuku
- Tesvik/indirim
- Odenmesi gereken
- Odeme tarihi
- Odenen ve kalan

Muhtasar raporu:

- Ucret stopaji
- Serbest meslek stopaji
- Kira stopaji
- Diger stopajlar
- Beyan donemi, tahakkuk ve odeme tarihi

Personel bazli ayrinti yalniz yetkili kullanicida acilir. Yonetici ozetinde toplu
tutarlar gosterilir.

## 9. Kira ve sabit odemeler

- Tesis/mulk
- Karsi taraf turu
- Sozlesme baslangic/bitis
- Net/brut kira
- Stopaj ve varsa KDV
- Odeme periyodu ve vade
- Artis tarihi ve orani
- Odenen/kalan
- Depozito ve diger yukumlulukler

Kira formulu sozlesme turune ve mali musavir teyidine gore parametreyle calisir.

## 10. Toplam odemeler raporu

Secilen donemde tum cikislar tek tabloda ve kategorilere ayrilmis gosterilir:

- Kredi karti
- Kredi
- Maas
- SGK
- Muhtasar
- Kira
- KDV ve diger vergiler
- Tedarikci
- Antrenor/hakedis
- Cek/senet
- Diger

Her kategori icin:

- Gerceklesen
- Kesinlesmis gelecek odeme
- Tahmini odeme
- Toplam
- Onceki doneme gore tutar ve yuzde farki
- Butce varsa butce farki

## 11. Nakit yeterlilik ve uyarilar

Rapor, aylik toplam odemeyi beklenen tahsilat ve kullanilabilir kasa/banka ile
karsilastirir:

- Tahmini ay sonu bakiye
- En dusuk nakit noktasi
- Odemeyi karsilayamama riski
- Geciken odeme
- Belgesiz veya teyitsiz islem
- Fatura/belge eslesmeyen tahsilat
- Yaklasan vergi, SGK, kredi ve kira vadeleri

Uyari yalniz renkle verilmez; metin, ikon ve tutar birlikte gosterilir.

## 12. Gerceklesen ve simulasyon ayrimi

- Gerceklesen: kayitli ve onayli islem
- Planlanan: vadesi veya sozlesmesi bilinen islem
- Tahmini: varsayimla hesaplanan islem
- Muhasebe Bekleyen: kayitli fakat resmi belge/eslestirme islemi tamamlanmamis
- Resmi: mali musavir veya entegrasyon sureciyle belgelenmis

Rapor toplaminda bu gruplar sessizce birlestirilmez. Kullanici hangi toplami
gordugunu her zaman anlar.

## 13. Disa aktarma

- Excel
- PDF
- Aylik mali musavir paketi

Ciktida rapor adi, tarih araligi, veri tarihi, senaryo, belge kapsami ve
`simulasyon/resmi` durumu bulunur. Yetkisiz personel/maas ayrintisi ciktiya
eklenmez.

## 14. Mali musavirden alinacak kesin bilgiler

- Sirket icin uygulanacak gelir/kurumlar/gecici vergi turleri
- KDV oranlari, istisna, tevkifat ve indirim kurallari
- Muhtasar kalemleri ve beyan/odeme takvimi
- SGK tesvikleri ve tahakkuk kurali
- Kira net/brut ve stopaj davranisi
- Kredi ana para/faiz muhasebe eslemesi
- GMS.NET hesap kodlari ve aktarim sablonu
- Banka/POS hareketinin hangi sistemde fislesecegi

Bu bilgiler onaylanmadan raporlar resmi hesap sonucu olarak sunulmaz.

## 15. Ilk gelistirme sirasi

1. Ortak rapor filtresi ve donem modeli
2. Odeme takvimi ve toplam odemeler
3. Kredi karti, kredi, maas, kira, muhtasar ve SGK veri girisleri
4. Aylik KDV simulasyonu
5. 3/6/9/12 aylik vergi simulasyonu
6. Nakit yeterlilik ve uyari paneli
7. Excel/PDF ve mali musavir paketi
