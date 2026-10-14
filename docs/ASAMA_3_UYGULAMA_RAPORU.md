# Aşama 3 Uygulama Raporu — 5. ve 6. Hafta

Tarih: 14 Ekim 2026. Platform: .NET 9, MVC, Identity<int>, EF Core, MediatR.

Aşama 1 ve 2 altyapısı üzerinde favori/tepki, hesap engelleme, profil, kalıcı e-posta kuyruğu, bakım görevleri, WebP optimizasyonu, Türkçe/İngilizce arayüz ve Excel/PDF raporları uygulandı. Görsel dönüştürmede kullanıcının onayıyla ImageSharp yerine SkiaSharp kullanıldı. Aşama 1 ve 2 regresyon testleri korunuyor.

## 1. EntityLayer ve DataAccesLayer

Yeni entity dosyaları:

- `EntityLayer/Concrete/BlogFavorite.cs`: üye, blog, UTC oluşturma tarihi.
- `EntityLayer/Concrete/BlogReaction.cs`: üye, blog, tepki, UTC oluşturma tarihi.
- `EntityLayer/Enum/ReactionType.cs`: Like=1, Heart=2, Clap=3.
- `EntityLayer/Concrete/ContactEmailDelivery.cs`: kalıcı gönderim niyeti, sabit Message-ID, gönderilme tarihi, deneme sayısı, Hangfire job ID, yeniden kuyruklama tarihi, işlem lease'i ve hata türü.
- `EntityLayer/Concrete/TrashArchiveEntry.cs`: entity türü/ID, UTC arşiv tarihi ve JSON snapshot.

Yeni EF yapılandırmaları:

- `DataAccesLayer/Configurations/BlogFavoriteConfiguration.cs`
- `DataAccesLayer/Configurations/BlogReactionConfiguration.cs`
- `DataAccesLayer/Configurations/ContactEmailDeliveryConfiguration.cs`
- `DataAccesLayer/Configurations/TrashArchiveEntryConfiguration.cs`

Favori ve tepki tablolarında `(AppUserId, BlogId)` tekil indeksleri var. Tepki türü için `IN (1,2,3)` CHECK constraint bulunuyor. Blog/üye ilişkileri Restrict; ilişkili geçmiş cascade ile yok edilmiyor. Her iletişim mesajının yalnızca bir teslimat kaydı bulunabiliyor. Arşiv entity türü/ID çifti de tekil.

`DataAccesLayer/Concrete/AppDbContext.cs` dört DbSet'i ve assembly configuration kayıtlarını içeriyor. `CascadeDeleteTiming.OnSaveChanges`, takip edilen favori/tepki/yorumlar varken blogun soft-delete'e dönüştürülmesinin EF ilişki kontrollerinden önce gerçekleşmesini sağlıyor. Önceki UTC audit/global filter davranışı korunuyor. Favori ve tepkiler kullanıcı tarafından geri çekildiğinde fiziksel olarak kaldırılır; blog soft-delete olduğunda etkileşimler korunur ve halka açık sorgularda gizlenir.

## 2. Üye işlemleri, CQRS, hesap engeli ve profil

Yeni veya genişletilen BusinessLayer dosyaları:

- `Abstract/IActiveMemberService.cs`, `Security/ActiveMemberService.cs`
- `CQRS/Commands/MemberCommands.cs`, `CQRS/Queries/MemberQueries.cs`
- `CQRS/Handlers/MemberHandlers.cs`, `Dtos/MemberDtos.cs`
- `ValidationRules/MemberValidators.cs`
- `Exceptions/InteractionConflictException.cs`
- `Security/AccessPolicies.cs`, `CQRS/Handlers/CommentHandlers.cs`

Komutlar: `ToggleBlogFavoriteCommand`, `SetBlogReactionCommand`, `SetAccountLockoutCommand`. Sorgular: `GetUserFavoritesQuery`, `GetBlogInteractionQuery`, `GetModerationAccountsQuery`.

İş kuralları:

- Etkileşim yalnızca `Published && !IsDeleted` bloglara uygulanır. Üye ID'si oturumdan alınır.
- Favori ikinci işlemde geri çekilir. Aynı tepki tekrar seçildiğinde geri çekilir; farklı tepki mevcut kaydı değiştirir. `null` tepki geri çekme anlamına gelir.
- Bir üyenin blog başına bir favorisi ve bir tepkisi olabilir; uygulama kontrolü yanında veritabanı tekillik kısıtı vardır.
- Favoriler yalnızca oturumdaki üyeye ait, yayındaki blogların salt-okunur/sayfalanmış DTO listesini döndürür.
- Tepki yarışlarında başlangıçtaki istek niyeti korunarak en fazla üç yeniden yükleme/kaydetme denemesi yapılır. Süren çakışma HTTP 409'a çevrilir. Eşzamanlı hesap değişiminde daha yeni hesap bilgisi ezilmez.
- `RequireActiveMember`, her yetkili istekte veritabanındaki `IsActive`, `LockoutEnabled`, `LockoutEnd` alanlarını kontrol eder. Yorum ve etkileşim handler'ları da kontrolü uygular.
- `RequireAccountModerator` yalnızca System Admin ve Moderator içindir. Content Manager, Author, Registered User ve Visitor hesap kilitleyemez.
- Kilitlemede `LockoutEnabled=true`, gelecekte bir `LockoutEnd` ve yeni SecurityStamp/ConcurrencyStamp yazılır. Mevcut cookie geçersizleşir; yeni giriş de reddedilir. Doğrudan komut kullanımında ayrıca aktif hesap kontrolü yapılır.
- Moderatör kendi hesabını veya System Admin hesabını kilitleyemez. Rol atama yalnızca `RequireAdmin` ile korunur. Kilit açmak rolleri değiştirmez. Kilit süresi 1–3650 gündür.

WebUI dosyaları:

- `Controllers/ProfileController.cs`, `Models/ProfileViewModels.cs`, `Views/Profile/Index.cshtml`: `/Profile`, ad/soyad, şifre değişimi, optimize avatar ve favoriler.
- `Controllers/BlogInteractionController.cs`, `ViewComponents/BlogInteractionViewComponent.cs`, `Models/ProfileViewModels.cs` içindeki `BlogInteractionViewModel`, `Views/Shared/Components/BlogInteraction/Default.cshtml`: blog detayındaki favori/Like/Heart/Clap formları.
- `Controllers/AdminAccountController.cs`, `Views/AdminAccount/Index.cshtml`: arama, sayfalama, kilitle/kilit aç.
- `Controllers/AdminUserRoleController.cs`: eski kilit aksiyonu da güvenli komuta yönlendirilir.
- `Controllers/AdminUserListController.cs`: görünmeyen/soft-delete yorumlar ve favori/tepki geçmişi olan hesabın silinmesi önceden engellenir; geçmiş/avatar korunur.
- `Controllers/DashboardController.cs`: kilitli/inaktif hesap yeni oturum açamaz; engellenen giriş yanlış şifre sayacını artırmaz.
- `Views/Blog/Post.cshtml`, `Views/Blog/_Comments.cshtml`, ortak navbar/sidebar/top header güncellendi.

Mutasyon formları POST + antiforgery kullanır. Profil hedef kullanıcı ID'si kabul etmez. Avatar yolu `uploads/avatars/{filename}.webp` olarak saklanır; profil, eski yönetim ekranları ve üst menü aynı kök-relative yolu kullanır.

## 3. Hangfire, e-posta ve bakım

Yeni arayüz/servisler:

- `BusinessLayer/Abstract/IContactSubmissionService.cs`
- `BusinessLayer/Abstract/IContactEmailQueue.cs`
- `BusinessLayer/Concrete/ContactSubmissionService.cs`
- `BusinessLayer/Jobs/HangfireContactEmailQueue.cs`
- `BusinessLayer/Jobs/ContactEmailJob.cs`
- `BusinessLayer/Jobs/ContactEmailRecoveryJob.cs`
- `BusinessLayer/Jobs/TrashMaintenanceJob.cs`
- `WebUI/Security/HangfireCustomAuthorizationFilter.cs`
- `WebUI/Services/IReCaptchaVerifier.cs`, `ReCaptchaVerifier.cs`

Güncellenenler: `BusinessLayer/Abstract/IMailService.cs`, `Concrete/MailService.cs`, `WebUI/Controllers/ContactController.cs`, `WebUI/Program.cs`, `BusinessLayer/Container/Additions.cs`.

İletişim verileri whitelist ile yeni entity'ye aktarılır; ziyaretçiden audit/kimlik alanları alınmaz. Mesaj ve gönderim niyeti aynı EF SaveChanges işlemiyle önce kalıcı olarak yazılır. Sonra gerçek Hangfire `IBackgroundJobClient.Enqueue` çağrılır. Kuyruk erişilemiyorsa iletişim kaydı ve niyet kaybolmaz; recovery görevi tekrar kuyruklar. SignalR bildirim hatası da kaydedilmiş mesajı başarısız saydırmaz.

`ContactEmailJob.SendAsync` üzerinde `[AutomaticRetry(Attempts=3)]` vardır. Gönderilmiş kayıt yeniden işlenmez. Atomik, beş dakikalık DB lease aynı teslimatın paralel işlenmesini sınırlar; SMTP deadline 90 saniyedir. SMTP hata türü saklanır; işlem tekrar denensin diye exception Hangfire'a iletilir. Lease worker kesintisinde zaman aşımıyla yeniden alınabilir.

`recover-contact-emails` her 10 dakikada çalışır. Gönderilmemiş, lease'i boş/süresi dolmuş, bir saattir kuyruklanmamış kayıtları inceler. İlk deneme + üç retry tüketilen kayıtlar kendiliğinden sonsuz kuyruklanmaz; dashboard'da incelenip yetkili yönetici tarafından tekrar denenebilir.

`nightly-trash-maintenance`, `Cron.Daily(2)` ile **Europe/Istanbul** saat diliminde 02:00'de çalışır. Windows karşılığı `Turkey Standard Time` kullanılır. 30 günden eski ilişkisiz silinmiş blog/kategori/galeri kayıtlarının JSON snapshot'ını arşivler; orijinal kayıtları ve görselleri korur. İlişkili bloglar ve blogun referans verdiği kategoriler atlanır. Yalnızca uygulamanın `logs/log-*.txt` günlüklerinden eski, kapalı ve yeniden yönlendirilmemiş dosyalar `logs/archive` altına taşınır; uploads dizini taranmaz.

SQL Server Hangfire storage kalıcıdır. `ConnectionStrings:Hangfire` verilmemişse `DefaultConnection` kullanılır. `/hangfire` dashboard yalnızca kimliği doğrulanmış System Admin'e açıktır; dashboard antiforgery açık kalır. Sunucu worker sayısı 1–4 arasında sınırlandırılır.

SMTP için işlem-sonrası DB işaretlemesi ile SMTP sunucusunun kabulü aynı transaction'a alınamaz. Sunucu e-postayı kabul ettikten hemen sonra süreç çökerse tekrar gönderim ihtimali vardır. Sabit Message-ID, gönderilmiş kaydı atlama ve lease bu riski azaltır; sistem uçtan uca kesin exactly-once garantisi vermez.

## 4. SkiaSharp WebP görsel pipeline'ı

- `BusinessLayer/Abstract/IImageOptimizationService.cs`
- `WebUI/Services/ImageOptimizationService.cs`
- `WebUI/Services/BlogImageStorage.cs`

Blog/avatarda en fazla 5 MiB giriş kabul edilir; beyan edilen boyut ve gerçekten okunan akış ayrı denetlenir. JPEG/PNG/GIF/WebP için uzantı, MIME, magic bytes ve decoder formatı birlikte doğrulanır. Script/EXE/SVG, yanlış MIME/imza ve bozuk decoder verisi reddedilir. Pixel sayısı 30 milyonla, tek boyut 12000px ile sınırlanır; büyük animasyon dizileri reddedilir, kabul edilen animasyonun ilk karesi kullanılır.

EXIF yönü uygulanır, en fazla 1200px genişlikte oran korunur, WebP kalite 75 ile yeniden kodlanır. Orijinal EXIF/GPS metadata yeni dosyaya aktarılmaz. Dosya adı GUID'dir; yalnızca `wwwroot/uploads/blogs` veya `avatars` kullanılır. Önceki JPG/PNG/WebP dosyaları soft-delete, geri yükleme veya yeni görsel yüklemesinde silinmez. Başarısız yeni yazmanın oluşturduğu yarım dosya temizlenir.

Paketler: SkiaSharp **4.153.1**, Linux için `SkiaSharp.NativeAssets.Linux.NoDependencies` **4.153.1**. ImageSharp veya SixLabors lisans anahtarı gerektiren görsel paketi kullanılmıyor.

## 5. Türkçe/İngilizce yerelleştirme

- `BusinessLayer/SharedResource.cs`
- `BusinessLayer/Resources/SharedResource.resx`
- `BusinessLayer/Resources/SharedResource.tr-TR.resx`
- `BusinessLayer/Resources/SharedResource.en-US.resx`
- `WebUI/Models/LanguageSettings.cs`
- `WebUI/Views/Shared/_LanguageSelector.cshtml`
- `WebUI/Services/LocalizedIdentityErrorDescriber.cs`
- `WebUI/Controllers/HomeController.cs`: `SetLanguage`.

`AddLocalization`, `IStringLocalizer`, View/DataAnnotations localization ve `CookieRequestCultureProvider` kuruldu. Varsayılan `tr-TR`, diğer desteklenen kültür `en-US`. Seçim bir yıl süreli, HttpOnly/SameSite Lax/essential cookie ile saklanır. `SetLanguage` yalnızca desteklenen kültürleri ve yerel dönüş URL'lerini kabul eder; POST antiforgery ile korunur. Yanıtta Content-Language ve HTML lang aktiftir.

Navbar/footer/sidebar, üye/moderasyon ekranları, blog/yayın/çöp kutusu/yorum/iletişim formları ve ortak CRUD başlık/düğmeleri kaynaklara bağlandı. Blog/yorum/kategori/iletişim/member FluentValidation mesajları, DataAnnotations ve temel Identity şifre/hesap hataları yerelleştirildi. Eski giriş/kayıt Razor dosyaları UTF-8'e dönüştürüldü. Dinamik blog makaleleri ve veritabanındaki tanıtım metinleri otomatik çevrilmez.

## 6. Excel/PDF raporları

- `BusinessLayer/Abstract/IReportService.cs`
- `BusinessLayer/Dtos/ReportDtos.cs`
- `BusinessLayer/Concrete/ReportService.cs`
- `BusinessLayer/Concrete/ReportDocumentWriter.cs`
- `BusinessLayer/Assets/Fonts/NotoSans-Regular.ttf`, `NotoSans-Bold.ttf`, `OFL.txt`
- `WebUI/Controllers/ReportsController.cs`

`Contacts` raporu RequireModerator; `Blogs`, `Interactions`, `Users` raporları RequireAdmin ister. Servis katmanı da yetki kontrolü yapar. `/Reports/{action}?format=Excel` veya `format=Pdf` ile indirilir. İletişim ve blog yönetim sayfalarında indirme düğmeleri; admin menüsünde kullanıcı raporu bağlantıları vardır.

Tarih aralığı UTC olarak başlangıç dahil/bitiş hariçtir. Blog/etkileşim raporları isteğe bağlı kullanıcı ve blog ID filtresini destekler; kullanıcı raporu kullanıcı filtresini destekler. Desteklenmeyen/ters/negatif filtreler reddedilir. İletişim raporu kullanıcı/blog filtresi kabul etmez. En fazla 10000 satır üretilir; aşım sessiz veri kırpmak yerine hata verir. Excel hücre metni sınırı da kontrol edilir.

ClosedXML **0.105.1**: başlık, filtre satırı, otomatik sütun genişliği, tablo filtresi, sabit başlık satırları ve güvenli metin hücreleri. Kullanıcı metni Excel formülüne çevrilmez.

PDFsharp-MigraDoc **6.2.4**: kurumsal tablo, tekrarlanan başlık, sayfa numarası, gömülü Noto Sans regular/bold. Türkçe `ç,ğ,ı,ö,ş,ü,İ` karakterleri doğrulandı. Font lisansı dağıtıma dahil edildi. Dört PDF ve dört Excel örneği render edilip görsel olarak kontrol edildi.

Dosyalar istek sırasında bellekte üretilir; disk/shared cache'e kaydedilmez. Yanıtlar `no-store`/`no-cache` içerir. Parola hash'i, security stamp, SMTP credential gibi hassas alanlar dışa aktarılmaz.

## 7. EF Core migration ve çalıştırma

Migration:

- `DataAccesLayer/Migrations/20261013214716_AddStage3MemberInteractionsAndJobs.cs`
- Aynı migration'ın `.Designer.cs` dosyası ve `AppDbContextModelSnapshot.cs` güncellendi.

```powershell
dotnet tool restore
dotnet restore
dotnet build WebUI/WebUI.csproj
dotnet ef database update --project DataAccesLayer --startup-project WebUI
dotnet ef migrations has-pending-model-changes --project DataAccesLayer --startup-project WebUI
dotnet run --project WebUI --launch-profile Development
```

Migration yerel geliştirme veritabanına uygulandı; son model kontrolünde bekleyen değişiklik yok. SQL script `.artifacts/stage3-migration.sql` içinde incelenebilir. Migration dört tabloyu ve indeks/FK/check constraint'leri oluşturur; Hangfire kendi SQL şemasını storage başlangıcında kurar. Production EF migration'ı dağıtım sürecinde ayrıca uygulanır; uygulama yalnızca Development ortamında otomatik migrate/seeding yapar. Down migration yeni tablolardaki verileri siler; veri içeren ortamda rollback öncesi yedek gerekir.

Yapılandırma kaynakları Environment Variables veya WebUI User Secrets:

- `ConnectionStrings__DefaultConnection`: zorunlu SQL bağlantısı.
- `ConnectionStrings__Hangfire`: isteğe bağlı ayrı Hangfire SQL storage.
- `ConnectionStrings__Redis`: Aşama 2 hibrit cache için isteğe bağlı.
- `Smtp__Host`, `Smtp__Port`, `Smtp__Username`, `Smtp__Password`, `Smtp__SenderEmail`, `Smtp__RecipientEmail`: mevcut yönetim SMTP ayarlarını override eder. TLS zorunludur; 465 SSL, diğer portlarda STARTTLS.
- `GoogleReCaptcha__SiteKey`, `GoogleReCaptcha__SecretKey`, isteğe bağlı `GoogleReCaptcha__ExpectedHostname`.

SMTP veya reCAPTCHA credential'ı kaynak koduna yazılmaz. Eksik/başarısız CAPTCHA doğrulaması iletişim formundan güvenlik atlatılarak mesaj gönderilmesine izin vermez. Yerel SMTP doğrulaması CAPTCHA'yı atlayan bir UI ayarı eklemeden servis katmanında, kapalı loopback portunda yapıldı.

Development test hesapları: admin/editor/author/moderator/user `@spa.com`; önceki Development seeder davranışı korunuyor. Ortak şifre `P@ssword123!`. Production'da bu hesaplar oluşturulmaz. Yerel uygulama: `http://localhost:5157`.

Hangfire recurring işlerinin zamanında çalışması için worker process'in sürekli çalışır olması gerekir; host kapalıyken 02:00 görevi o anda yürütülemez. Çok instance kullanımında aynı kalıcı SQL storage, paylaşılan görsel depolaması ve ortak Data Protection anahtarları yapılandırılmalıdır.

## 8. Testler ve doğrulama kanıtları

Son xUnit sonucu: **183 geçti, 0 başarısız, 0 atlandı**. Aşama 1/2 dahil toplamdır. Yeni/ilgili dosyalar:

- `MemberFeatureTests.cs` — 37 vaka: SQL model/tekillik, toggle/değiştirme/geri çekme, yayında olmayan içerik, sahiplik, lockout/expired lockout, rol matrisi, admin koruması, mevcut oturum/yeni giriş, soft-delete/restore ve hesap geçmişini koruma.
- `BackgroundJobTests.cs` — 29 vaka: transaction ile kayıt/niyet, kuyruk kesintisi, recovery, kapalı SMTP, retry niteliği, Message-ID/sent idempotency, eşzamanlı lease, arşivleme/dosya sınırları, dashboard ve CAPTCHA.
- `ImageReportTests.cs` — 29 vaka: WebP/1200px, magic/MIME/bozuk dosya, gerçek akış boyutu, klasör sınırı, önceki dosyanın korunması, rapor yetkileri/filtreleri/kültürleri, font/glyph, satır/hücre sınırı, güvenli Excel metni ve no-store.
- `InteractionConcurrencyTests.cs` — 4 vaka: değiştirilirken geri çekilen tepki, duplicate insert ardından geri çekme, süren yarışta kontrollü conflict, profil değişirken kilitlemede yeni kaydın korunması. EF interceptor gerçek SQLite satırlarını başka context üzerinden değiştirir.
- `LocalizationTests.cs` — 9 vaka: gerçek kaynaklar, culture cookie, harici redirect engeli, desteklenmeyen dil ve Identity hata mesajları.
- `ValidationLocalizationTests.cs` — 4 vaka: Türkçe/İngilizce gerçek kaynak ve karakter sınırları, aynı validator ile kültür değişimi, parametresiz kullanım.
- `ProfileAvatarTests.cs` — 2 vaka: yeni root-relative avatar yolu, yükleme olmadığında eski dosyanın korunması.
- `ExceptionMappingTests.cs` — 3 vaka: 400/403/409 ve conflict yanıtında DB ayrıntılarının gizlenmesi.
- `UserDeletionTests.cs` ve önceki yetki/blog/cache/yorum/SignalR/seeder testleri regresyon kapsamında geçiyor.

Çalıştırma:

```powershell
dotnet test BusinessLayer.Tests/BusinessLayer.Tests.csproj --artifacts-path .artifacts/stage3 --logger "trx;LogFileName=stage3.trx" --results-directory .artifacts/test-results
```

Ek doğrulamalar:

- **38 gerçek HTTP kontrolü**: public liste, antiforgery, cookie kültürü, güvenli redirect, admin/moderator/member erişimleri, dashboard, dört tür Excel/PDF, no-store, favori/tepki, hesap kilidi ve İngilizce yönetim formları/silme onayı. Test hesabı kilidi açıldı; test etkileşimleri geri çekildi.
- **4 gerçek SQL Server/Hangfire kontrolü**: favori tekil indeksinde SQL 2601/2627, mesaj/niyetin gerçek queue'dan önce kaydı, kapalı loopback SMTP'de Scheduled retry, veri korunması/gönderildi işaretinin yazılmaması. Ayrı, bu teste ait LocalDB veritabanı işlem sonunda kaldırıldı; dışarıya e-posta gönderilmedi.
- Migration uygulandı ve pending model changes kontrolü geçti.
- WebUI build hatasız. Önceden kalan nullable uyarıları ayrıca görülüyor.

Yerel kanıtlar: `.artifacts/test-results/stage3.trx`, `stage3-test.log`, `stage3-http-results.json`, `stage3-sql-results.json`, `stage3-migration.log`, `stage3-build.log`, `stage3-report-preview/pdf-qa.json` ve rapor önizlemeleri. Bu dosyalar `.artifacts` altında tutulur, Git'e dahil edilmez.

## 9. Paket güvenliği ve pratik sınırlar

MailKit **4.18.1** kullanıldı; eski STARTTLS advisory'sini içeren 4.13.0 kaldırıldı. Gelen kriptografi bağımlılığı `System.Security.Cryptography.Pkcs` **10.0.13** ile güncellendi. Çalışan uygulamada kullanılmayan AutoMapper 13.0.1 referansları, DI kaydı ve kullanılmayan MappingProfile kaldırıldı; CQRS DTO projeksiyonları açık eşlemeyi kullanıyor.

Son paket taramasında eski geliştirme/scaffolding araçlarının transitif `Microsoft.Build` 17.10.4 (High), `NuGet.Packaging` ve `NuGet.Protocol` 6.11.0 (Low) uyarıları sürüyor. Dağıtım öncesi bu araçlar, ortam SDK/runtime güncelliği ve production publish bağımlılıkları ayrıca gözden geçirilmelidir. Tarama kaydı `.artifacts/stage3-package-audit.log` içindedir.

Görsel tasarrufu görüntüye bağlıdır; her yüklemede dosyanın daha küçük olacağı garantisi yoktur. Kaynak ve arşiv/görsellerin saklanması için disk kapasitesi, yedekleme ve saklama politikası gerekir. Raporlar güvenli boyut sınırları içinde senkron istekle üretilir; çok büyük veri için sonraki aşamada ayrı rapor işi/streaming tasarlanabilir.

Teknik kaynaklar: [Hangfire ASP.NET Core](https://docs.hangfire.io/en/latest/getting-started/aspnet-core-applications.html), [recurring jobs](https://docs.hangfire.io/en/latest/background-methods/performing-recurrent-tasks.html), [ASP.NET Core localization](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization), [MailKit advisory](https://github.com/advisories/GHSA-9j88-vvj5-vhgr), [PKCS advisory](https://github.com/advisories/GHSA-846f-f764-76mx), [AutoMapper advisory](https://github.com/advisories/GHSA-rvv3-g6hj-g44x).
