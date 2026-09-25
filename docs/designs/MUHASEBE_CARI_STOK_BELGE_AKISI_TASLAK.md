# Muhasebe, Cari, Stok ve Belge Akisi Taslagi

**Durum:** `[ONERI]` Kullanici incelemesi ve mali musavir teyidi bekliyor  
**Tarih:** 23 Eylul 2026  
**Kapsam:** Hizli Muhasebe MVP

**Etkilesimli ekran taslagi:** [Cari hesap mockup](./cari-hesap-mockup.html)

**Gorsel onizlemeler:**

- [Ana cari ekrani](./cari-hesap-mockup.png)
- [Stok veya hizmet cikisi](./stok-hizmet-cikis-mockup.png)
- [Tahsilat alma](./tahsilat-al-mockup.png)
- [Fatura taslagi onizlemesi](./cari-hesap-fatura-dialog-mockup.png)

## 1. Is ihtiyaci

- Ayni veli/musteri icin tek cari kart kullanilir.
- Tahsilat, fatura kesilmeden once alinabilir.
- Fatura daha sonra kesildiginde onceki tahsilatla eslestirilir.
- Urun, fatura kesilmeden stoktan teslim edilebilir.
- Sonradan kesilen fatura, ilgili stok cikis satirlarina baglanir.
- Kullanici tek tikla belgeli/resmi gorunumu ve tum islemleri birlikte gosteren
  toplam gorunumu acabilir.
- Muhasebe bekleyen tahsilat, hizmet ve stok cikislari unutulmayacak ayri bir is
  listesinde tutulur.

## 2. Temel karar: iki cari degil, tek cari ve uc gorunum

Ayni kisi icin "resmi cari" ve "diger cari" adiyla iki ayri kart acilmaz. Tum
hareketler tek cari kimligi altinda saklanir; gorunum, hareketlerin belge durumuna
gore filtrelenir.

### Toplam

Tum operasyonel hareketleri gosterir:

- faturali ve fatura bekleyen hizmet/urun satislari,
- tahakkuklar ve indirimler,
- tahsilatlar, avanslar, iadeler ve mahsuplar,
- stok teslimleri ve iadeleri.

### Belgeli / Resmi

Resmi belgeye baglanmis hareketleri ve bunlarla eslestirilen tahsilatlari gosterir:

- e-Arsiv/e-Fatura ve iade belgeleri,
- belgeye dagitilmis tahsilatlar,
- muhasebe tarafindan kabul edilen kasa/banka/POS/avans kayitlari,
- GMS.NET aktarim durumu.

Bu gorunumun kesin muhasebe kapsami, mali musavirin hesap plani ve aktarim kuralina
gore teyit edilir.

### Muhasebe Bekleyen

Islem yapilmasi gereken kuyruktur:

- faturasi kesilmemis hizmet veya tahakkuk,
- faturasi kesilmemis stok cikisi,
- faturaya baglanmamis tahsilat/avans,
- kismi faturalanmis satirlar,
- belge hatasi veya eslesmeyen POS/banka hareketi.

Her sekmede adet ve tutar rozeti bulunur. Durum yalniz renkle anlatilmaz; metin ve
simge birlikte kullanilir.

## 3. Cari ekran taslagi

Ekranin tek isi, "Bu cariyle ilgili ne oldu, ne belgelendi ve ne bekliyor?"
sorusunu cevaplamaktir.

Ust bolum:

- Cari adi/kodu ve bagli sporcular
- Toplam operasyonel bakiye
- Belgeli/resmi bakiye
- Fatura bekleyen toplam
- Dagitilmamis tahsilat/avans

Gorunum secimi:

`Toplam | Belgeli / Resmi | Muhasebe Bekleyen`

Hareket tablosu:

| Alan | Aciklama |
|---|---|
| Tarih | Islem veya teslim tarihi |
| Islem | Tahakkuk, fatura, tahsilat, stok cikisi, iade, mahsup, avans |
| Belge | Fatura/irsaliye/makbuz/referans numarasi |
| Aciklama | Paket, urun, vize bedeli veya diger kalem |
| Borc | Cariyi borclandiran tutar |
| Alacak | Tahsilat, iade veya mahsup |
| Bakiye | Secili gorunume gore yuruyen bakiye |
| Belge durumu | Bekliyor, kismi, belgelendi, iptal/ters kayit |

Satir ayrintisi acildiginda stok teslimi, tahsilat dagitimi, fatura baglantisi ve
degisiklik gecmisi gorulur. Ana tablo gereksiz sutunlarla doldurulmaz.

### 3.1 Islem dugmeleri ve resmi islem akisi

Cari ekraninin sag ustunde yalniz iki ana islem bulunur:

- `Stok veya Hizmet Cikisi`
- `Tahsilat Al`

Bagimsiz bir `Fatura Hazirla` dugmesi bulunmaz. Fatura taslagi, ilgili satis veya
tahsilat islemi icinden baslatilir.

`Stok veya Hizmet Cikisi` secildiginde:

1. Kullanici `Stok` veya `Hizmet` turunu secer.
2. Stok secilmisse urun listesi; hizmet secilmisse hizmet katalogu acilir.
3. Adet/miktar, birim fiyat ve iskonto girilir.
4. Iskonto, oturumdaki kullanicinin yetki sinirini asamaz. Sinir asilirsa kayit
   dugmesi kapanir ve acik hata mesaji gosterilir.
5. `Resmi cikis olustur` secilmisse fatura onizlemesi acilir. Kullanici son
   kontrolu onaylarsa fatura yalnizca taslak olarak kaydedilir.
6. Secenek kapaliysa stok cikisi veya hizmet satisi kaydedilir ve
   `Muhasebe Bekleyen` listesine gonderilir.

`Tahsilat Al` ekraninda odeme araci, tutar ve referans girilir. `Resmi islem
olarak devam et` secilmisse belge/fatura eslestirme onizlemesi acilir. Secenek
kapaliysa tahsilat kaydedilir ve `Muhasebe Bekleyen` listesine gonderilir.

Resmi secenek kapali olsa da hareket silinmez veya gizlenmez; cari toplaminda,
denetim kaydinda ve muhasebe is listesinde gorunur.

## 4. Faturadan once tahsilat akisi

1. Tahsilat kaydedilir: tarih, tutar, arac, kasa/banka/POS, referans ve tahsil eden.
2. Acik fatura yoksa tutar `dagitilmamis tahsilat / avans` durumunda tutulur.
3. Kayit ayni anda `Muhasebe Bekleyen` is listesine girer; gorunmez veya kayipsiz
   bir kenar kayit olmaz.
4. Fatura hazirlandiginda sistem uygun tahsilatlari onerir.
5. Kullanici dagitim onizlemesini kontrol eder ve baglantiyi onaylar.
6. Tahsilat fatura tutarini asarsa kalan tutar avans olarak kalir; eksikse kalan
   borc caride gorunur.
7. Yanlis kayit silinmez; ters kayit ve yeni dogru kayitla duzeltilir.

## 5. Faturadan once stok cikisi akisi

1. Teslim aninda cari, urun, depo, miktar, teslim tarihi ve teslim eden kaydedilir.
2. Fiziksel stok hemen azalir.
3. Satis satiri `Fatura Bekliyor` durumunda cariye baglanir.
4. Sonradan fatura hazirlanirken bekleyen teslim satirlari secilir.
5. Bir fatura birden fazla cikisi toplayabilir; bir cikis da miktar bazinda kismi
   faturalanabilir.
6. Faturalanan miktar ve kalan miktar ayri izlenir.
7. Iade, ilk cikisa bagli ters stok ve cari hareketiyle kaydedilir.
8. Eksi stok ve faturasiz teslim yetkisi rol bazli kontrol edilir ve denetim
   kaydina yazilir.

## 6. Vize kalemi

Kullanilan ifade iki farkli islemi anlatabilir:

- `Visa/POS tahsilati` ise odeme araci olarak kaydedilir; komisyon ve valor ayri
  izlenir, kart bilgisi saklanmaz.
- `Sporcu lisans/vize bedeli` ise urun/hizmet katalog kalemi olarak cariye borc
  yazilir ve faturalanacak satira donusur.

Uygulamada bu iki anlam ayri alan ve adlarla tutulur; ayni "vize" etiketiyle
karistirilmaz.

## 7. Zorunlu kontroller

- Fatura bekleyen hareketler gunluk uyarida ve yaslandirma raporunda gorunur.
- Ayni teslim veya tahsilat ikinci kez faturaya baglanamaz.
- Kismi eslestirmede kalan tutar/miktar sifirlanmaz.
- Fatura iptali stok cikisini otomatik yok etmez; stok teslimi ve belge birbirinden
  bagimsiz fakat iliskili kayitlardir.
- Donem kilidi sonrasi kayit degistirilmez; duzeltme ters kayitla yapilir.
- GMS.NET'e hangi hareketin aktarilacagi ve banka ekstresinin hangi sistemde
  fislesecegi mali musavirle karara baglanir.
- Vergi, KDV ve fatura kesme zamani uygulamada sabit varsayilmaz; tarih aralikli
  kurallar mali musavir onayiyla tanimlanir.

## 8. Hizli Muhasebe MVP sirasi

1. Tek cari ve hareket turleri
2. Toplam / Belgeli-Resmi / Muhasebe Bekleyen gorunumleri
3. Tahsilat, avans ve faturaya dagitim
4. Stok cikisi, kismi faturalanma ve iade
5. Fatura bekleyenler gunluk is listesi
6. Gunluk tahsilat, gecikmis alacak ve stoktan cikmis-faturalanmamis raporlari
7. Mali musavir mutabakati ve GMS.NET aktarim eslemesi

Mobil uygulama, gelismis simulasyon ve ayrintili performans modulleri bu cekirdek
tamamlanana kadar muhasebe calismasinin onune alinmaz.
