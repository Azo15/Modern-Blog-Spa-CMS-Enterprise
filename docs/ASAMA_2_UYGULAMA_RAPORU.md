# Aşama 2 — CQRS, Redis, SignalR ve yorum moderasyonu

Tarih: 13 Ekim 2026. Aşama 1'deki policy, sahiplik, audit ve yayın iş akışı üzerine uygulanmıştır.

## 1. Uygulanan davranış

### CQRS ve doğrulama

AdminBlogController yalnızca ISender alır. Blog okuma/yazma, kategori seçenekleri, çöp kutusu ve durum değişimleri MediatR üzerinden yürütülür; controller servis veya IQueryable tüketmez. Dosya saklama handler üzerinden bir arayüze taşınmıştır. Yetki denetimi dosya yüklemeden önce yapılır.

Query'ler:
- GetBlogsQuery: Yetkili yönetim listesi; Author için yalnız kendi kayıtları.
- GetBlogByIdQuery: Yönetim detay DTO'su; başka yazara ait kayıtta imperative authorization başarısız olur ve HTTP 403 döner.
- GetPublicBlogsQuery: Veritabanında sıralama/sayfalama; yalnız Published ve silinmemiş kayıtlar; BlogListDto.
- GetPublicBlogBySlugQuery: Halka açık, temizlenmiş BlogDetailDto. Her okumada yayın durumu denetlenir.
- GetTrashedBlogsQuery: System Admin/Content Manager için sayfalanmış BlogAdminDto.
- GetCategoryOptionsQuery: Form seçenekleri; Identity nesnesi içermeyen DTO.

Command'lar: CreateBlogCommand, UpdateBlogCommand, DeleteBlogCommand, ChangeBlogStatusCommand ve RestoreBlogCommand. Aşama 1'in BlogWorkflowService kuralları korunur: taslak başlangıç, yazarın değiştirilmemesi, yazar düzenlemesinde yeniden onay, yalnız yönetici/içerik yöneticisinin yayımlaması, görseli koruyan soft delete ve taslak geri yükleme.

ValidationBehavior her isteğin FluentValidation kurallarını handler'dan önce çalıştırır. Aynı scoped DbContext'i kullanabilecek doğrulayıcılar sırayla çalışır. Geçersiz sayfa/boyut, kimlik, başlık, kategori, durum veya içerik doğrulama hatası üretir. Formlar doğrulama mesajlarını gösterir; diğer isteklerde hata HTTP 400'e çevrilir. Imperative yetki reddi HTTP 403'tür. Hata middleware'i DeveloperExceptionPage'in içinde konumlandırılarak geliştirmede de doğru durum kodları korunmuştur.

PaginatedList<T>: Items, TotalCount, PageIndex, PageSize, TotalPages, HasPreviousPage ve HasNextPage. Sayfa indeksleri 1'den başlar; blog/moderasyon sayfa boyutu üst sınırı 100, halka açık yorumların ana yorum sayfa boyutu üst sınırı 50'dir. Blog sıralamasında Id ile ikinci sıralama sayfalamayı tutarlı kılar.

### Hibrit cache

ConnectionStrings:Redis boşsa AddDistributedMemoryCache kullanılır. Tanımlıysa AddStackExchangeRedisCache, spa: fiziksel anahtar alanı ve kısa bağlantı/işlem timeout'larıyla kaydedilir. Redis'e bağlantı başlangıçta zorunlu değildir. CacheManager, Redis/timeout/socket hatalarında ayrı ve 50 MB sınırındaki bellek fallback'ine geçer; beş saniyelik deneme aralığı bağlantı hatalarının her cache işleminde tekrarlanmasını azaltır.

Mantıksal anahtar alanları: blogs_all, blogs_cat_{categoryId}, blog_detail_{blogId}. Sayfa, boyut, hariç tutulan blog ve veri güncelliği bilgileri anahtara eklenir. RemoveByPrefixAsync bu alanın sürüm belirtecini değiştirir. Bütün eski sayfalar erişilemez hale gelir; TTL ile temizlenir. Redis KEYS/SCAN veya yalnız o process'in bildiği bir anahtar listesi kullanılmaz. Aynı Redis'e bağlanan diğer uygulama instance'ları da yeni sürümü görür.

Blog oluşturma/güncelleme/yayınlama/silme/geri yükleme, genel listeyi ve ilgili kategorileri geçersiz kılar. Kategori değişiminde eski ve yeni kategori alanları birlikte temizlenir. Redis kesilirken temizleme belirteçleri tutulur; bağlantı geri geldiğinde cache okumadan önce uygulanır. Kesinti sırasında yazılan verinin eski uzak değeri geri getirmemesi için uzak alan da geçersiz kılınır. Redis ulaşılabilirken bir miss, eski yerel değerin tekrar sunulmasına yol açmaz. Bozuk JSON cache miss olarak ele alınır.

Halka açık cache yalnız PaginatedList<BlogListDto> saklar. BlogListDto/BlogDetailDto; AppUser entity'si, e-posta, parola hash'i, security stamp veya audit kullanıcı alanlarını içermez. BlogAdminDto ise yetkili yönetim sorgularına özeldir ve halka açık cache'e yazılmaz.

Redis kesintisinde farklı sunucuların bellek cache'leri ortak değildir. Bu nedenle halka açık liste için hafif bir SQL count/son değişiklik/son ID kontrolü cache anahtarına katılır; eski yayımlanmış kayıtların silme/taslağa alma sonrasında yeniden görünmesi önlenir. Cache hit sırasında da bu metadata sorgusu yapılır. Bu tercih doğruluğu korur; yüksek yükte bu sorgunun maliyeti ayrıca ölçülmelidir. Yük/kapsama yüzdesi ölçüldüğüne dair bir iddia yoktur.

### Güvenli bildirimler

NotificationHub RequireModerator ile korunur. System Admin, Content Manager ve Moderator bildirim alabilir; anonim, Registered User ve Author bağlanamaz. OnConnectedAsync yalnız Managers grubuna ekler. İstemciye açık broadcast metodu yoktur. İletişim ve yorum bildirimleri yalnız Clients.Group("Managers") ile gönderilir.

İstemcide withAutomaticReconnect korunur. Projedeki Toastr kütüphanesinin escapeHtml seçeneği etkinleştirilmiştir; mesajlar HTML olarak çalıştırılmaz. Yorumlar Razor tarafından düz metin olarak encode edilir. Canlı yorum bildirimi gönderilemese de kaydedilmiş yorum kaybolmaz; gönderim hatası loglanır.

### Yorum ve moderasyon

- Comment, BaseEntity'den türer; soft delete, UTC ve aktif kullanıcı audit bilgileri aynı SaveChanges altyapısıyla uygulanır.
- Blog, kullanıcı ve üst yorum foreign key'lerinde Restrict vardır; fiziksel cascade delete yoktur.
- İçerik 1–1000 karakter; yeni yorum onaysız başlar. Kullanıcı kimliği oturumdan alınır; istemciden kullanıcı ID veya onay değeri kabul edilmez.
- Yorum yalnız yayındaki bloga eklenir. Yanıt aynı blogdaki onaylı ana yoruma eklenir. Yanıta tekrar yanıt verilerek üçüncü seviye oluşturulamaz.
- Yorum yazarı kendi yorumunu güncelleyebilir/silebilir. Düzenleme tekrar onay gerektirir. Başka kullanıcıya ait kayda müdahale 403'tür.
- Yalnız System Admin ve Moderator onaylar/reddeder. RequireCommentModerator ek policy'si, Aşama 1'in iletişim/bildirim için Content Manager'a da izin veren RequireModerator policy'sinden ayrılır. AdminCommentController iki policy'yi de uygular; Content Manager yorum moderasyonuna erişemez.
- Onaylı ana yorumlar ve onaylı yanıtlar halka açık görünür. Ana yorum onaysız/silinmişse onaylı çocukları da halka açık görünmez.
- Bekleyen yorumlar paneli blogun yayın durumu değişse de kuyruğu korur; yayımlanmamış blogun yorumu onaylanamaz, fakat moderatör silebilir.
- Ana yorum silindiğinde yanıtları da uygulama düzeyinde soft delete edilir; audit korunur.
- IProfanityFilterService temel kelime eşleştirmesiyle uygunsuz içeriği kaydetmeden reddeder. Liste Moderation:BlockedWords ile yapılandırılır. Bu temel bir filtredir; kapsamlı dil/bağlam analizi değildir.
- Halka açık yorum listesi ana yorumlara göre sayfalanır; yanıtlar seçilen ana yorumlar için tek ek SQL sorgusunda alınır. Kullanıcı son 20 kendi yorumunu, onay bekleyenler dahil, düzenleme bölümünde görür.

## 2. Katmanlara göre dosyalar

### EntityLayer
- Concrete/Comment.cs — Yeni entity, parent/children ilişkisi, içerik sınırı ve onay durumu.
- Concrete/Blog.cs — Comments navigation koleksiyonu.

### DataAccesLayer
- Concrete/AppDbContext.cs — Comments DbSet; Blog/Identity/self-reference Restrict ilişkileri; IsApproved varsayılanı; içerik sınırı; global soft-delete filtresi; sorgu indeksleri.
- Migrations/20261013120212_AddComments.cs ve .Designer.cs — Comments tablosu, audit alanları, foreign key'ler ve indeksler.
- Migrations/20261013120743_BackfillMissingBlogSlugs.cs ve .Designer.cs — Eski veride yalnız boş blog adreslerini tamamlama. Geçerli adresler ve içerikler korunur; olası adres çakışmasına karşı ek benzersiz değer üretilir.
- Migrations/AppDbContextModelSnapshot.cs — Güncel model.

### BusinessLayer
- CQRS/Behaviors/ValidationBehavior.cs — Yeni pipeline.
- CQRS/Queries/BlogQueries.cs — Blog query sözleşmeleri.
- CQRS/Commands/BlogCommands.cs — Blog command sözleşmeleri; yüklenen dosya handler'a taşınır.
- CQRS/Handlers/BlogHandlers.cs — DTO projeksiyonları, sahiplik, sayfalama, cache ve command handler'ları.
- CQRS/Queries/CommentQueries.cs — Public, bekleyen ve kendi yorumları sorguları.
- CQRS/Commands/CommentCommands.cs — Oluşturma, güncelleme, silme ve onay.
- CQRS/Handlers/CommentHandlers.cs — Derinlik, parent/blog uygunluğu, sahiplik, onay ve bildirim kuralları.
- Dtos/BlogReadDtos.cs — BlogListDto, BlogDetailDto, BlogAdminDto, CategoryOptionDto.
- Dtos/PaginatedList.cs — Ortak sayfalama sonucu.
- Dtos/CommentDtos.cs — Public thread, pending ve kullanıcıya ait yorum DTO'ları.
- ValidationRules/BlogRequestValidators.cs — Blog command/query doğrulamaları.
- ValidationRules/CommentRequestValidators.cs — Yorum command/query doğrulamaları.
- Abstract/ICacheService.cs — RemoveByPrefixAsync.
- Concrete/CacheManager.cs — Sürüm alanları, Redis kesintisi, fallback ve toparlanma.
- Concrete/CacheFallbackStore.cs — Ayrı ve sınırlı bellek cache'i.
- Concrete/BlogWorkflowService.cs — Genel/eski/yeni kategori alanı temizleme.
- Abstract/IProfanityFilterService.cs, Concrete/ProfanityFilterService.cs — Yapılandırılabilir temel filtre.
- Abstract/IManagerNotifier.cs — İş katmanı bildirim sözleşmesi.
- Abstract/IBlogImageStorage.cs — Dosya saklama sözleşmesi.
- Security/AccessPolicies.cs — Yorum moderasyonu policy'si.
- Container/Additions.cs — Validator, açık generic pipeline, cache ve filtre servis kayıtları.
- BusinessLayer.csproj — Microsoft.Extensions.Caching.StackExchangeRedis 9.0.5.

### WebUI
- Program.cs — Yapılandırmaya duyarlı Redis/bellek kaydı, image/notifier servisleri ve hata middleware sırası.
- Controllers/AdminBlogController.cs — Yalnız ISender üzerinden çalışan yönetim.
- Controllers/BlogController.cs — Public DTO, sayfalama, yorumlar ve ilgili bloglar.
- Controllers/CommentController.cs — Member policy'si, anti-forgery ve yorum sahibi işlemleri.
- Controllers/AdminCommentController.cs — Yetkili kuyruk, Approve ve Delete.
- Controllers/AdminUserListController.cs — Yorum ilişkileri nedeniyle Restrict'e takılan kullanıcı silme isteği güvenli mesajla karşılanır; silme başarısızsa kullanıcı görseli korunur.
- Services/BlogImageStorage.cs — Yetki kontrolünden sonra dosya saklama; 5 MB ve JPG/PNG/GIF/WebP imza kontrolü. Blog silmede görsel silinmez.
- Services/ManagerNotifier.cs — Yalnız Managers grubu; hata kaydı.
- Hubs/NotificationHub.cs — Managers grup adı; yetkili bağlantı ve broadcast yüzeyinin kapalı tutulması.
- Middleware/GlobalExceptionMiddleware.cs — Validation 400 ve authorization 403 eşlemesi.
- Models/BlogPostViewModel.cs — Public DTO ve yorum sonuçları.
- ViewComponents/Default/_HomeBlog.cs — MediatR ve salt-okunur DTO.
- Views/AdminBlog/Index.cshtml, Trash.cshtml — Sayfalanmış yönetim DTO'ları.
- Views/Blog/Index.cshtml, Post.cshtml, _BlogCardPartial.cshtml — Public DTO ekranları.
- Views/Blog/_Comments.cshtml — Onaylı hiyerarşi, cevap formu, sayfalama ve kendi yorumunu düzenleme/silme.
- Views/AdminComment/Index.cshtml — Bekleyen yorumlar, onay ve red/silme.
- Views/Shared/Components/_HomeBlog/Default.cshtml — DTO tabanlı kartlar.
- Views/Admin/_Sidebar.cshtml — Yorum moderasyonu bağlantısı.
- Data/DataSeeder.cs — Yeni örnek bloglara adres üretimi.

Aşama 1'deki Controllers/ContactController.cs ve Views/Admin/_Script.cshtml, hub sabitini ve HTML kaçışlı otomatik yeniden bağlantıyı zaten kullanıyordu; yeni Managers grubuyla aynı güvenli akışı sürdürür.

### Testler ve kök yapılandırma
- BusinessLayer.Tests/Stage2Fixture.cs — SQLite, gerçek DI/MediatR/AuthorizationService ve test kimliği.
- BusinessLayer.Tests/BlogHandlersTests.cs — Sayfalanmış DTO, hassas alanların dışlanması, sahiplik, pipeline, cache alanları ve yayın/çöp kutusu akışı.
- BusinessLayer.Tests/CommentTests.cs — Oluşturma, parent/blog sınırı, üçüncü seviye, rol/sahiplik, profanity, yeniden onay, kuyruk ve soft delete.
- BusinessLayer.Tests/CacheManagerTests.cs — İki instance arasında sürüm temizleme; Redis down/recovery; offline yazma, eski fallback değeri ve bozuk JSON.
- BusinessLayer.Tests/NotificationHubTests.cs — Altı erişim profili, Managers üyeliği ve istemciye açık broadcast olmaması.
- BusinessLayer.Tests/ExceptionMappingTests.cs — 400/403 durum kodları.
- BusinessLayer.Tests/UserDeletionTests.cs — Restrict nedeniyle kullanıcı silinemediğinde görselin korunması.
- BusinessLayer.Tests/ControllerSecurityTests.cs — Controller'ın korumalı query'ye yönlendirmesi; üretim seed koruması.
- BusinessLayer.Tests/BlogWorkflowTests.cs — Aşama 1 güvenlik regresyon testlerinin yeni query/cache sözleşmelerine uyarlanması.
- docker-compose.yml — ConnectionStrings__Redis=redis:6379 anahtarı.
- README.md — İkinci aşama çalıştırma/doğrulama bağlantısı.

## 3. Migration ve çalışma adımları

Ön koşul: Aşama 1 migration'ları uygulanmış olmalı; SQL bağlantısı ortam değişkeni veya WebUI User Secrets üzerinden tanımlanmalı.

```powershell
dotnet tool restore
dotnet ef database update --project DataAccesLayer --startup-project WebUI
dotnet ef migrations has-pending-model-changes --project DataAccesLayer --startup-project WebUI
dotnet test BusinessLayer.Tests/BusinessLayer.Tests.csproj --logger "trx;LogFileName=stage2-tests.trx"
powershell -ExecutionPolicy Bypass -File .\Baslat.ps1
```

Yerel doğrulamada AddComments ve BackfillMissingBlogSlugs, xspa_db üzerinde başarıyla uygulandı. Model/migration farkı yok. Comments fiziksel cascade kullanmaz. Eksik blog adresi tamamlama geri alındığında düzeltilmiş adresleri silmez.

Redis isteğe bağlıdır:

```powershell
# Erişilebilir bir Redis sunucunuz varsa:
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379" --project WebUI

# Redis yapılandırmasını kaldırıp yalnız bellek kullanmak için:
dotnet user-secrets remove "ConnectionStrings:Redis" --project WebUI
```

Üretimde karşılığı ConnectionStrings__Redis ortam değişkenidir; gerçek parola/endpoint depoya yazılmamalıdır. SQL bağlantısı ConnectionStrings__DefaultConnection'dan okunur. Üretimde migration dağıtım adımıdır; geliştirme hesapları oluşturulmaz.

Kelime filtresi örneği, Moderation:BlockedWords altında string listesi veya Moderation__BlockedWords__0 gibi ortam değişkenleriyle tanımlanabilir.

Site: http://localhost:5157. Moderasyon: /AdminComment/Index. Giriş: /dashboard/giris. Aşama 1'deki Development hesapları ve P@ssword123! şifresi geçerlidir.

## 4. Test ve doğrulama sonuçları

- **68 xUnit testi geçti; hata ve atlanan test yok.** Kanıt: BusinessLayer.Tests/TestResults/stage2-tests.trx. SQL ilişkileri ve kurallar SQLite üzerinde gerçek sorgularla, migration'lar ayrıca SQL Server LocalDB üzerinde doğrulanmıştır.
- Gerçek HTTP: üye yorum oluşturur; onay öncesi anonim sayfada görünmez; Moderator onayı sonrası görünür. Onaylı yanıta üçüncü seviye eklenemez. Başka kullanıcının güncellemesi ve Content Manager moderasyon paneli 403'tür. Düzenlenen ana yorum yeniden onaya düşer ve yanıtlarıyla birlikte gizlenir. Red/silme soft delete olarak çalışır.
- HTML saldırı metni yorumda, kuyrukta ve public ekranda encode edilmiştir; script olarak render edilmemiştir. Anti-forgery token'ları gerçek HTTP işlemlerinde kullanılmıştır.
- Anonim hub isteği girişe yönlenir; Registered User 403, Moderator 200 alır. Client broadcast metodu yoktur.
- Geçersiz sayfa HTTP 400 döndürür. Imperative sahiplik hatası Development ortamında da 403'tür.
- Gerçek Redis SDK ile kapalı 127.0.0.1:6399 bağlantısı kullanılarak HTTP kontrolü yapılmıştır: birinci ve ikinci blog sayfaları 200 döndürmüş, RedisConnectionException bellek fallback'iyle karşılanmıştır.
- Test amaçlı oluşturulan yorumlar soft delete ile temizlenmiştir. Test için açılan 5158 portlu süreç durdurulmuştur; site 5157'de çalışır.

Yerel Docker motoru çalışmadığı için sağlıklı gerçek Redis sunucusu üzerinde bağlantı/expire/cluster yük testi yapılmamıştır. Sürüm ve toparlanma kuralları IDistributedCache test double'ı ile doğrulanmıştır. Canlı SignalR grupları bu uygulama instance'ına aittir; çok instance'lı yayın için ayrıca SignalR backplane gerekir. Önceki AutoMapper/MailKit güvenlik ve nullable uyarıları sürer; kapsam yüzdesi veya yük kapasitesi ölçülmemiştir.

Referanslar: [Microsoft distributed caching](https://learn.microsoft.com/aspnet/core/performance/caching/distributed), [StackExchange.Redis configuration](https://github.com/StackExchange/StackExchange.Redis/blob/main/docs/Configuration.md), [ASP.NET Core resource-based authorization](https://learn.microsoft.com/aspnet/core/security/authorization/resource-based).
