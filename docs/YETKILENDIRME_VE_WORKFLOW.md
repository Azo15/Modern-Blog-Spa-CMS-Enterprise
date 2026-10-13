# Yetkilendirme, soft delete ve blog yaşam döngüsü — Uygulama raporu

Tarih: 13 Ekim 2026. Mevcut .NET 9 N-Tier MVC uygulaması üzerinde uygulanmıştır.

## Davranış ve erişim matrisi

- **System Admin:** Kullanıcı/rol/sistem ayarları, bütün içerikler, yayın onayı, blog çöp kutusu ve iletişim yönetimi.
- **Content Manager:** Bütün bloglar, kategoriler, hizmetler, galeri ve diğer site içerikleri; yayın onayı, blog çöp kutusu ve iletişim yönetimi. Sistem rol yönetimine erişemez.
- **Author:** Yalnızca kendi bloglarını yönetim listesinde görür; kendi kaydını düzenler, siler ve incelemeye gönderir. Kendi kaydını bile yayımlayamaz/reddedemez. Yönetimden başka yazara ait kayda ID ile erişim 403 döner.
- **Moderator:** Gelen mesajları yönetir ve yetkili bildirim kanalına bağlanır; blog, rol ve sistem ayarlarını yönetemez.
- **Registered User:** Halka açık içeriğe erişir; yönetim ekranlarına erişemez. Yeni kayıt olan kullanıcıya bu rol atanır.
- **Visitor:** Anonim erişim profili. Identity hesabı veya rolü oluşturulmaz. Public policy hem anonim hem oturum açmış okuyuculara izin verir.

Policy kayıtları Program.cs içinden `AddAuthorization(AccessPolicies.Configure)` ile yapılır. `RequireAdmin`, `RequireContentManager`, `RequireModerator`, `RequireBlogAuthor`, `ManageBlogResource`, `RequireMember` ve `Visitor` tek merkezde tanımlıdır. Controller'larda sabit rol listeleri kullanılmaz. Sidebar aynı authorization servisinden karar alır; bağlantı gizlemek sunucu erişim kontrolünün yerini tutmaz.

## İçerik yaşam döngüsü

1. Yeni blog, yönetici oluştursa bile **Draft** başlar. İstemciden gönderilen yazar/yayın/silme/audit alanları kullanılmaz.
2. Yazar kendi kaydını **PendingReview** durumuna getirir.
3. Content Manager veya System Admin **Published** ya da **Rejected** kararını verir; gerekirse taslağa döndürür.
4. Yazar yayımlanmış içeriğini düzenlerse durum **Draft** olur; tekrar onay gerekir. Yönetici düzenlemelerinde orijinal yazar korunur.
5. Public liste, kategori, detay ve ana sayfa blog sorguları yalnızca `PublishStatus == Published && !IsDeleted` kayıtları sunar.
6. Silme, çöp kutusuna taşır; görsel dosyası silinmez. Content Manager/System Admin geri yükler; içerik görseliyle **Draft** döner.

`IsPublish` eski ekranlarla uyumluluk için tutulur ve iş akışı tarafından yeni durumla eşitlenir. Blogların halka açık sorgularında belirleyici alan `PublishStatus`'tur. Yazı URL'si düzenlemede korunur.

HTML; oluşturma, düzenleme ve yayımlama sırasında Ganss.Xss.HtmlSanitizer ile temizlenir. Önceden kaydedilmiş HTML için detay görünümünde de temizleme uygulanır. TinyMCE biçimlendirmesi korunurken script, olay işleyicileri ve javascript bağlantıları temizlenir.

## Katmanlara göre eklenen ve güncellenen dosyalar

### EntityLayer

- `Enum/PublishStatus.cs` — Yeni enum: Draft=0, PendingReview=1, Published=2, Rejected=3.
- `Concrete/Blog.cs` — Varsayılan Draft yayın durumu.
- `Concrete/BaseEntity.cs` — Varsayılan oluşturma zamanı UTC.

### DataAccesLayer

- `Abstract/ICurrentUserService.cs` — Aktif principal, kullanıcı ID ve e-posta sözleşmesi. DAL'ın BusinessLayer'a bağımlı olmasını önlemek için veri katmanındaki soyutlamadır.
- `Concrete/AppDbContext.cs` — CurrentUser enjeksiyonu; ekleme/güncelleme/silmede UTC zaman ve aktör alanları. Senkron, asenkron ve bool parametreli SaveChanges overload'ları aynı denetimi uygular. Güncellemede oluşturma audit alanları korunur. Deleted state ve doğrudan IsDeleted değişimi soft delete alanlarını doldurur.
- `Repository/GenericRepository.cs` — Tarihleri repository'de tekrar yazma kaldırıldı; audit tek merkezden yürütülür. Liste sorgularında tracking parametresi uygulanır.
- `Migrations/20261013111704_AddBlogPublishStatus.cs` — Yeni kolon ve eski yayın durumlarının taşınması.
- `Migrations/20261013111704_AddBlogPublishStatus.Designer.cs` — Migration model tanımı.
- `Migrations/AppDbContextModelSnapshot.cs` — Güncel model.

### BusinessLayer

- `Security/AccessPolicies.cs` — Rol sabitleri ve merkezi policy matrisi.
- `Security/BlogOwnerAuthorizationHandler.cs` — BlogOwnerRequirement ve resource-based handler. Kimliği doğrulanmış yönetici/içerik yöneticisi veya kaydın sahibi Author izin alır.
- `Abstract/IBlogWorkflowService.cs` — Oluşturma, düzenleme, silme, durum değişimi, çöp kutusu ve geri yükleme sözleşmesi.
- `Concrete/BlogWorkflowService.cs` — İşlemlerde imperative authorization, yazarın korunması, sunucu tarafında temizleme, yayın kuralları ve cache temizleme. Eski ve yeni kategori anahtarları temizlenir.
- `Abstract/IHtmlContentSanitizer.cs` — HTML temizleme sözleşmesi.
- `Concrete/HtmlContentSanitizer.cs` — Ganss.Xss uygulaması.
- `CQRS/Handlers/BlogHandlers.cs` — Public query filtreleri; command handler'ları güvenli workflow servisine yönlendirilir. MVC ve CQRS yazma işlemleri aynı kuralları uygular.
- `Concrete/CacheManager.cs` — EF navigation döngülerini cache serileştirmesinde güvenli şekilde atlar.
- `Dtos/BlogDtos.cs` — Oluşturmada Slug istemciden zorunlu istenmez; güvenli iş akışında başlıktan sunucuda üretilir.
- `Container/Additions.cs` — Workflow ve sanitizer kayıtları.
- `BusinessLayer.csproj` — HtmlSanitizer 9.2.1039 paketi.

### WebUI

- `Services/CurrentUserService.cs` — IHttpContextAccessor üzerinden aktif kullanıcının ID/e-posta bilgileri. Anonim veya HTTP bağlamı olmayan işlerde kimlik uydurulmaz.
- `Program.cs` — Policy, handler, current-user servis kayıtları; yalnız Development örnek veri; bütün ortamlarda gerçek rollerin oluşturulması; yetki reddinde HTTP 403; bağlantı yoksa açıklayıcı hata.
- `Data/DataSeeder.cs` — Beş gerçek rol; yalnız Development için beş onaylı test hesabı; idempotent kullanıcı/rol oluşturma ve IdentityResult kontrolü. Eski Visitor rolü kaldırılır. Örnek içerik Production'da eklenmez.
- `Data/AppDbContextFactory.cs` — Tasarım zamanı EF işlemleri için User Secrets/ortam değişkeni okuyan factory; uygulama başlangıcı ve seeder çalıştırılmaz.
- `Controllers/AdminBlogController.cs` — Listeyi sahiplikle sınırlandırma, GET/POST kaynak kontrolü, güvenli workflow çağrıları, SubmitForReview/Review/Trash/Restore. İstemcinin Image/IsPublish alanları güvenilmez; silmede dosya silme çağrısı yoktur.
- `Controllers/AdminUserRoleController.cs`, `AdminUserListController.cs`, `AdminSiteInfoController.cs` — RequireAdmin.
- `Controllers/AdminAboutController.cs`, `AdminAboutCardController.cs`, `AdminCatagoryController.cs`, `AdminContactController.cs`, `AdminGalaryController.cs`, `AdminServiceController.cs`, `AdminSliderController.cs` — RequireContentManager.
- `Controllers/AdminMessageController.cs` — RequireModerator.
- `Controllers/DashboardController.cs` — Girişten sonra role uygun yönlendirme, kayıt sırasında Registered User atama, giriş/kayıt/çıkış POST işlemlerinde CSRF kontrolü.
- `Controllers/BlogController.cs` — Public policy ve slug detayında yayın/silme filtresi.
- `ViewComponents/Default/_HomeBlog.cs` — Ana sayfada yayın/silme filtresi.
- `Views/Admin/_Sidebar.cshtml` — Policy tabanlı menü.
- `Views/AdminBlog/Index.cshtml` — Durum, incelemeye gönderme, yönetici yayın/reddetme/taslak butonları ve çöp kutusu bağlantısı.
- `Views/AdminBlog/Trash.cshtml` — Silinen kayıtlar, silen aktör/zaman ve anti-forgery korumalı geri yükleme formu.
- `Views/Shared/Delete.cshtml` — Bloglarda çöp kutusuna taşıma açıklaması.
- `Views/Blog/Post.cshtml` — Eski HTML için de render öncesi sanitization.
- `Middleware/GlobalExceptionMiddleware.cs` — Servis katmanından gelen yetki ihlalini 403 olarak ele alır.
- `Hubs/NotificationHub.cs`, `Controllers/ContactController.cs`, `Views/Admin/_Script.cshtml` — Yönetim bildirimleri RequireModerator ile korunur; yetkili grup kullanılır, istemcinin keyfi yayın metodu kaldırılır, toast HTML olarak yorumlanmaz.
- `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json` — Sabit bağlantı/parola/reCAPTCHA anahtarları kaldırıldı.
- `WebUI.csproj` — UserSecretsId.

### Testler ve kök dosyalar

- `BusinessLayer.Tests/AuthorizationTests.cs` — Altı profil, merkezi policy matrisi, kendi/başka yazara ait kaynak, bozuk/eksik ID ve anonim kimliğin sahte rol claim'i.
- `BusinessLayer.Tests/ControllerSecurityTests.cs` — Başka yazara ait ID ile edit GET/POST, delete GET/POST ve submit 403; workflow/dosya işlemi çağrılmaz. Production seeder veri servislerine bile erişmez.
- `BusinessLayer.Tests/BlogWorkflowTests.cs` — Gerçek SQLite ilişkisel veritabanında yetki, yazarın korunması, UTC/aktör, yayın onayı, tekrar inceleme, soft delete/restore, görselin diskte korunması, kategori cache temizliği, HTML temizleme ve public yayın filtreleri.
- `BusinessLayer.Tests/DevelopmentSeederTests.cs` — Seeder iki kez çalıştırılır; beş rol ve beş hesap, doğru parola, e-posta onayı, tek doğru rol ve Visitor bulunmaması doğrulanır.
- `BusinessLayer.Tests/CurrentUserServiceTests.cs` — Aktif kullanıcı, anonim kimlik ve HTTP bağlamı olmayan iş.
- `BusinessLayer.Tests/BlogHandlersTests.cs` — Public cache testleri yeni yayın durumuyla uyumlu hale getirildi; yazma güvenliği gerçek workflow testlerinde doğrulanır.
- `BusinessLayer.Tests/BusinessLayer.Tests.csproj` — WebUI referansı ve EF Core SQLite 9.0.5.
- `.config/dotnet-tools.json` — Tekrarlanabilir dotnet-ef 9.0.5 yerel araç kaydı.
- `docker-compose.yml` — SQL parolası MSSQL_SA_PASSWORD ortam değişkeninden; sabit parola yok.
- `.gitignore` — Doğrulama çıktıları ve .env hariç tutulur.
- `README.md` — Yeni bağlantı kurulumu ve geliştirme hesapları.

## Migration ve konfigürasyon adımları

Yerel bağlantı örneği (parola içermeyen Windows entegre kimlik doğrulaması):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=xspa_db;Integrated Security=True;TrustServerCertificate=True;" --project WebUI
dotnet tool restore
dotnet ef database update --project DataAccesLayer --startup-project WebUI
```

Bu çalışma sırasında migration yerel `xspa_db` üzerinde başarıyla uygulandı. Yeni kolonun varsayılanı Draft'tır; migration eski IsPublish=true kayıtları Published, diğerlerini Draft yapar. Eski içerik ve görseller silinmez. Migration snapshot ile uyumludur.

Üretimde bağlantı `ConnectionStrings__DefaultConnection` ortam değişkeniyle verilir; parola depoya yazılmaz. Üretim migration'ı dağıtım adımı olarak yukarıdaki EF komutuyla, hedef bağlantı açıkça yapılandırıldıktan sonra uygulanır. Uygulama yalnız Development başlangıcında otomatik migration çalıştırır.

reCAPTCHA için `GoogleReCaptcha__SiteKey` ve `GoogleReCaptcha__SecretKey` ortam değişkenleri veya geliştirme User Secrets kullanılabilir. Gerçek değerler bu rapora yazılmamıştır. Daha önce kaynakta yer alan uzak veritabanı ve servis kimlik bilgilerinin sağlayıcı tarafında yenilenmesi ayrıca gereklidir; dosyadan kaldırmak geçmişteki sızıntıyı geri almaz.

## Test hesapları

Yalnız Development: admin@spa.com (System Admin), editor@spa.com (Content Manager), author@spa.com (Author), moderator@spa.com (Moderator), user@spa.com (Registered User).

Hepsinin parolası `P@ssword123!`; EmailConfirmed=true. Bu beş hesabın parolası ve rolü Development başlangıcında deterministik hale getirilir. Bu adresler gerçek kullanıcılar için kullanılmamalıdır. Production'da kullanıcı/parola/örnek içerik tohumlama çağrılmaz; SeedAsync içinde de ayrı ortam kontrolü vardır. Üretimde ilk yönetici güvenli bir operasyonel süreçle oluşturulmalıdır.

## Doğrulama

```powershell
dotnet test BusinessLayer.Tests/BusinessLayer.Tests.csproj --logger "trx;LogFileName=security-tests.trx"
dotnet ef migrations has-pending-model-changes --project DataAccesLayer --startup-project WebUI
```

Son doğrulamada **40 test geçti; hata ve atlanan test yok**.

Testler policy handler'larını gerçek AuthorizationService ile, veri işlemlerini ilişkisel SQLite üzerinde çalıştırır. SQL Server migration uygulaması ayrıca yerel LocalDB ile doğrulanır. SQLite testleri SQL Server'a özgü bütün davranışların kanıtı değildir.

Gerçek HTTP yaşam döngüsü de doğrulandı: taslak ve inceleme bekleyen içerik 404; yazarın yayımlama isteği 403; yöneticinin yayımlaması sonrası detay 200; yazar düzenlemesi sonrası tekrar 404; silme, çöp kutusu ve taslak geri yükleme başarılı. Bildirim negotiate endpoint'i anonim için giriş yönlendirmesi, Author için 403, Admin için 200 döndürdü. Bu kontrol için oluşturulan 1002 ID'li test blogu çöp kutusunda bırakıldı.

Gerçek HTTP kontrolleri: beş hesapla giriş başarılı; yönetim sayfaları policy matrisine uygun 200/403 döndürür. Author için başka yazara ait Edit/1 ve Delete/1 adresleri 403'tür. Anonim kullanıcı yönetim isteğinde girişe yönlendirilir; public site/blog listesi 200 döndürür.

Mevcut projede nullable/deprecated API uyarıları ile AutoMapper 13.0.1 ve MailKit 4.13.0 paket güvenlik uyarıları sürmektedir. Bu değişiklikte paket yükseltme ve diğer CMS modülleri kapsam dışındadır; üretim tesliminden önce paket güncellemesi ayrıca ele alınmalıdır. Test kapsamı yüzdesi ölçülmedi; %70 iddiası yoktur.

Referanslar: [ASP.NET Core resource-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based) ve [HtmlSanitizer resmi deposu](https://github.com/mganss/HtmlSanitizer).
