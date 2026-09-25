# Claude Code icin UU Sport bagimsiz tam denetim promptu

Asagidaki metni `C:\UUSport` klasoru VS Code'da acikken Claude Code'a ver:

---

UU Sport / PSG Academy Samsun projesini bastan sona **bagimsiz ve salt okunur** denetle. Amacin Codex'in onceki kararlarini onaylamak degil; dosyalara dayanarak yanlislari, eksikleri, celiskileri ve riskleri bulmak. Simdilik kod yazma, dosya degistirme, SQL calistirma, kurulum/yayin yapma, Git commit olusturma. Bulgulari yalniz sohbette raporla.

Ilk olarak `C:\UUSport\AGENTS.md`, `CLAUDE.md` ve `docs\project-memory\UUSPORT_ANA_HAFIZA.md` dosyalarini oku. Sonra asagidaki proje belgelerini denetle:

- `docs\designs\MUHASEBE_CARI_STOK_BELGE_AKISI_TASLAK.md`
- `docs\designs\MUHASEBE_RAPORLARI_VE_ODEME_TAKVIMI_TASLAK.md`
- `docs\designs\cari-hesap-mockup.html` ve mevcut PNG goruntuleri
- `docs\operations\UUSPORT_GELISTIRME_VERI_VE_CANLIYA_ALMA_PROSEDURU.md`
- `docs\standards\UUSPORT_POSTGRESQL_ADLANDIRMA_VE_MIGRATION_STANDARDI.md`
- `README.md`, `.gitignore`, `tools\` altindaki betikler, `docs\decisions\`, `src\`, `database\`, `tests\` ve `deployment\` klasorlerinin gercek durumu
- `docs\designs\UUSPORT_MUHASEBE_KABA_ONIZLEME.html` statik taslagini da incele. Masaustundeki `C:\Users\Ensaradmin\Desktop\UUSport-Gorsel-Ornekler\UUSport-Onizleme.html` ayni icerigin yerel kopyasidir; her iki dosya uygulama olarak kabul edilmez.

Kaynak urun belgesi `C:\Users\Ensaradmin\Desktop\uu sport ltd\psg-academy-samsun-sistem-spesifikasyonu.md` surum 0.11. Bu dosyayi salt okunur incele; eski PDF v0.10 ile celisirse v0.11'i esas al. Kaynak Excel'deki guncel ogrenci sayfasi kullanici kararina gore yalniz `Sayfa1 (2)`dir. **Gercek ogrenci, cocuk, veli veya odeme verisini modele, sohbet ciktisina, Git'e ya da test ortamina alma.** Repo icinde `uu sport ltd\PSG aktif tutulan basit excel.xlsx` kopyasi gorunuyor. Tum `uu sport ltd/` klasoru `.gitignore` ile dislandi; bunu ve depolama riskini dosyalari acmadan dogrula. Kisi kayitlarini okuma veya paylasma. Gerekirse veri iceren dosyanin ayrica insan tarafindan kontrollu incelenmesi gerektigini raporla.

Kararlastirilan yerlesim sirasi su: (1) gelistirme icin ayri kiralik RDP, yalniz sentetik/maskeli veri; (2) ilk canli kurulum mevcut normal Windows bilgisayari, donanim uygunsa yerel sanal makine; (3) daha sonra canli uygulama ve PostgreSQL'in yeni kiralanacak **ayri canli RDP'ye** tasinmasi. Gelistirme RDP'si hicbir zaman canli veri hedefi degil. Mevcut bilgisayarin paylasilan goruntusunde Windows 11 Pro, Intel i3-10100 ve 8 GB RAM goruldu; disk ve sanallastirma ayari teyit edilmedi. Bu siraya aykiri veya fazla kesin yazilmis ifadeleri bul.

Ozellikle su konulari denetle:

1. **Kanit ve durum:** Hangi ozellik gercekten calisiyor, hangisi sadece belge/mockup? PostgreSQL yerel gelistirme kurulumunun varligi ile UUSport semasinin varligini ayir. Release, EXE/MSI paketi veya canli kurulum varmis gibi bir ifade varsa isaretle.
2. **Mimari:** Node.js/TypeScript/React ile ASP.NET Core/React/WPF yonleri arasindaki belirsizlik; ADR eksikligi; masaustu kullaniminin tarayici/PWA mi yoksa ayri istemci mi olacagi; API, PostgreSQL, dosya depolama ve rol sinirlari.
3. **Muhasebe is kurallari:** Tek cari; Toplam/Belgeli/Resmi/Muhasebe Bekleyen gorunumleri; stok veya hizmet cikisi; tahsilat; iskonto yetkisi; fatura taslagi; ters kayit; belge durumlari. Ozellikle tahsilat ile satisin ciroda iki kez sayilmasi, belge durumu ile resmi muhasebe durumunun karismasi, vergi simulasyonunun resmi beyan gibi sunulmasi risklerini ara.
4. **Excel aktarma:** `Sayfa1 (2)` secimi, kaynak dosyanin korunmasi, tekrarlanan aktarim, satir hata raporu, acilis bakiyesi mutabakati, geri alma/ters kayit ve kisisel veri siniri. Gercek veriyi acma.
5. **Tasarim:** Apple Design Skill ilkelerinin web/Windows'a uygun cevirisi, ekranlarin gunluk muhasebe kullanimi icin anlasilirligi, yaratici kimlik, erisilebilirlik ve mockup'ta calisiyor gorunup gercekte ornek olan islemler. Ekran goruntulerini ve HTML'yi ayri ayri degerlendir.
6. **Kurulum ve tasima:** Gelistirme RDP -> mevcut bilgisayarda canli -> ayri canli RDP. Ilk bilgisayarin 8 GB RAM siniri, sanal makine secimi, Windows/IIS/.NET/PostgreSQL onkosullari, teslim edilecek resmi EXE/MSI ve komutlar, servis otomatik baslama, elektrik/internet, HTTPS, dis erisim, yedek, geri yukleme provasi, veri tutarliligi, kesinti ve buluta geciste kesim plani. Prosedurde uygulanmasi imkansiz veya eksik kontrol var mi?
7. **Gizlilik ve guvenlik:** Gercek veri barindiran dosyalarin repo ve Obsidian icindeki varligi, `.gitignore` yeterliligi, Git gecmisinde veri/anahtar olup olmadigi, gelistirme-canli ayrimi, yetki ve denetim kaydi. Gizli veri veya kisi isimlerini rapora kopyalama.
8. **Uygulama sirasi:** Muhasebeyi hizla kullanima alma hedefi icin asgari dikey dilim nedir? Kodlamadan once mutlaka karara baglanmasi gereken konular hangileri? Orantili ve uygulanabilir bir sonraki adim sirasi oner.

Her bulguda **onem derecesi**, **dosya yolu ve satir/bolum**, **somut kanit**, **etki** ve **onerilen duzeltme** ver. Once kritik/yuksek bulgulari yaz. Ardindan `Dogrulanan durum`, `Dogrulanamayan iddialar`, `Acik kararlar` ve `Onerilen sonraki 5 adim` bolumlerini ekle. Kullanici karari, onerisi ve senin cikarimini ayri etiketle. Bir iddiayi dogrulayamiyorsan dogru kabul etme. Gereksiz yeni teknoloji veya servis onermeden mevcut hedefe odaklan.

---
