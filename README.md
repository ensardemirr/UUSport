# UU Sport Yönetim Platformu

Bu depo; masaüstü uygulaması, merkezî API, web portalı, mobil uygulama ve PostgreSQL veritabanı değişiklikleri için ortak çalışma alanıdır.

## Ortak ana hafıza

Codex ve Claude Code aynı proje hafızasını kullanır:

- `AGENTS.md`: Codex ve diğer ajanlar için çalışma kuralları
- `CLAUDE.md`: Claude Code giriş noktası
- `docs/project-memory/UUSPORT_ANA_HAFIZA.md`: tek güncel proje hafızası
- `docs/operations/UUSPORT_GELISTIRME_VERI_VE_CANLIYA_ALMA_PROSEDURU.md`:
  kaynak dosya, release, migration, yedek ve canlıya alma prosedürü
- `docs/standards/UUSPORT_POSTGRESQL_ADLANDIRMA_VE_MIGRATION_STANDARDI.md`:
  PostgreSQL nesne adları, veri tipleri, RLS ve migration standardı
- `docs/reviews/CLAUDE_CODE_UUSPORT_TAM_DENETIM_PROMPTU_2026-09-25.md`:
  Claude Code için bağımsız, salt okunur proje denetimi isteği

Yeni bir çalışmaya başlamadan önce ana hafıza okunur. Onaylanan kararlar ve
tamamlanan kurulumlar aynı dosyanın değişiklik geçmişine eklenir.

## Durum

Proje başlangıç ve tasarım aşamasındadır. Geliştirmeye ara verilmiş, Claude Code
denetimi beklenmektedir. Ekranlar, MVP kapsamı ve veri modeli onaylanmadan
uygulama ya da veritabanı şeması oluşturulmayacaktır.

## Klasörler

- `docs`: kararlar, tasarımlar, operasyon prosedürleri, standartlar ve güvenli
  kaynak belgeler
- `src`: API, masaüstü, web ve mobil kaynak kodları
- `database`: migration ve başlangıç verisi tanımları
- `tests`: otomatik testler
- `deployment`: test ve canlı ortam dağıtım tanımları
- `tools`: yalnızca geliştirme yardımcıları ve kurulum dosyaları

Gerçek sporcu ve veli verileri geliştirme veya test ortamına konulmaz.

## Yerel PostgreSQL geliştirme ortamı

- Sürüm: PostgreSQL 18.6 (64 bit)
- Sunucu: yalnızca `localhost:5432`
- Veri: `%LOCALAPPDATA%\UUSport\PostgreSQL\18\data`
- Parola: `%LOCALAPPDATA%\UUSport\secrets` altında Windows DPAPI ile korunur
- Başlatma: `powershell -ExecutionPolicy Bypass -File .\tools\start-postgresql-dev.ps1`
- Durdurma: `powershell -ExecutionPolicy Bypass -File .\tools\stop-postgresql-dev.ps1`

Bu kurulum geliştirme içindir ve Windows servisi değildir. Proje veritabanı ile şema, mimari karar kaydı ve MVP kapsamı onaylandıktan sonra oluşturulacaktır.
