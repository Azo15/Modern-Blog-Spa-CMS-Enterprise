# XSPA — Prompt 5 uygulama raporu

18 Ekim 2026. İstenen 19 adım sıralı, bağımsız commit ve push olarak tamamlandı. Push hedefi `github/master`; başlangıç `055e512`.

## Roller ve mevcut kullanıcılar

Identity artık yalnızca `System Admin`, `Staff`, `Customer` rollerini kullanır. Türkçe yönetim arayüzü bunları Sistem Yöneticisi, Personel ve Müşteri olarak gösterir. Anonim ziyaretçi bir Identity rolü değildir.

- RequireAdmin yalnızca System Admin içindir; rol atama ekranı ve POST işlemi bu policy ile korunur.
- Panel, blog yönetimi, yorum/hesap moderasyonu, SMTP/site ayarları, kullanıcı listesi, raporlar ve Hangfire: Admin ve Staff.
- Üye işlemleri: üç rol. Mevcut kilitli/pasif hesap kontrolleri devam eder.
- Personel sistem yöneticisini silemez veya kilitleyemez. Son sistem yöneticisinin rolü kaldırılamaz.
- Staff içerikleri yazarlar arasında yönetebilir; güncelleme özgün `AppUserId` değerini değiştirmez. Müşteriler panelden içerik oluşturamaz veya yönetemez.

`DataSeeder.SeedRolesAsync` önce üç rolü oluşturur, eski Content Manager / Author / Moderator üyeliklerini Staff'a, Registered User üyeliğini Customer'a taşır, ardından eski rolleri kaldırır. Birden fazla eski rolü olan hesap en yüksek seviyeyi korur. Parola değişmez; yetki değişiklikleri security stamp ile mevcut oturumlara yansır. Üretimde yeni demo hesabı açılmaz. Beş eski geliştirme test hesabı korunur; üç editöryal hesap aynı Staff rolündedir.

Rol atamasında gelen rol adı ve ID'si veritabanıyla karşılaştırılır; tek temel rol kabul edilir. Sadece menüyü gizlemekle yetinilmedi; sunucu yetkisi de korunur.

## Kayıt ve Google

`SignUpViewModel`: ad, soyad, e-posta, telefon, parola/tekrar, KVKK okuma onayı. Telefon ve parola eşleşmesi sunucuda doğrulanır. Hesap her zaman Customer rolüyle açılır; gönderilen rol alanları kabul edilmez. Rol ataması başarısızsa yarım kayıt geri alınır. Karşılama mesajı giriş ekranında gösterilir. Giriş ve kayıt ekranları TR/EN sekmeli tasarım, altın/zümrüt renkler ve Google düğmesi kullanır.

`AppUser.PrivacyAcknowledgedAtUtc` ve `PrivacyNoticeVersion` için migration:

`20261018041651_CustomerPrivacyAcknowledgement`

Development açılışı mevcut migration akışını kullanır. Diğer ortamlarda dağıtım sürecine uygun olarak:

```powershell
dotnet ef database update --project DataAccesLayer --startup-project WebUI
```

Google entegrasyonu `Microsoft.AspNetCore.Authentication.Google` 9.0.21 ile, yalnızca iki ayar da varsa kaydedilir:

- `Authentication__Google__ClientId`
- `Authentication__Google__ClientSecret`

User Secrets karşılıkları `Authentication:Google:ClientId` ve `Authentication:Google:ClientSecret` alanlarıdır. Google Cloud'da yerel yönlendirme adresi `http://localhost:5157/signin-google`; üretimde kendi HTTPS alan adınızın `/signin-google` adresini kaydedin.

Akış: CSRF korumalı POST `/account/google/start` → sağlayıcı → `/signin-google` middleware → `/account/google/callback`. Yeni hesap için doğrulanmış Google e-postası, telefon ve KVKK okuma onayı gerekir; yalnızca Customer atanır. Eşleşen mevcut e-posta otomatik bağlanmaz. Bağlı hesaplarda pasiflik/kilit ve Identity oturum kontrolleri uygulanır. Harici tokenlar saklanmaz. ReturnUrl yalnızca yerel adreslere dönebilir. Anahtar yoksa sunucu hatası yerine açık bilgilendirme gösterilir.

## Sidebar ve editör

Yeni dosyalar `admin-sidebar.css`, `admin-sidebar.js`, `_SidebarGroup.cshtml`, `SidebarGroupModel.cs`. Altı grup, FontAwesome 6 ikonları, AI rozeti, aktif sayfa vurgusu ve tek açık grup davranışı eklendi. JavaScript devreye girmediğinde bağlantılar kaybolmaz; kapanan gruplar klavye odağına kapatılır. Personel menüsünde rol atama bağlantısı bulunmaz. Admin/Staff girişleri işletme özetine gider.

Tarayıcı doğrulamasında eski şablonun Bootstrap paketi modalı çalıştırmadığı için mevcut yerel standart Bootstrap bundle kullanıldı. TinyMCE'nin yerel `base_url` ve `.min` soneki tanımlandı; Create/Edit ekranındaki anahtarsız CDN tekrarı kaldırıldı. Zengin metin editörü ve modal birlikte doğrulandı.

## AI blog üretimi

BusinessLayer:

- `IAiBlogService`, `AiBlogService`, `AiBlogPrompt`, `AiBlogDraftDto`
- `GenerateAiBlogCommand`, `GenerateAiBlogHandler`, `GenerateAiBlogValidator`

WebUI:

- `AdminBlogController.GenerateWithAi`: policy, CSRF, küçük istek boyutu ve kullanıcı başına dakikada üç üretim sınırı
- `_AiBlogModal.cshtml`, `ai-blog.js`: konu, kategori, dil/ton, yükleniyor durumu, hata geri bildirimi ve forma aktarım

Servis OpenAI Responses API kullanır. Konu ve kategori dışında müşteri kayıtları gönderilmez. API anahtarı ve model kaynak kodunda bulunmaz:

- `Ai__ApiKey` / User Secret `Ai:ApiKey`
- `Ai__Model` / User Secret `Ai:Model`

Model hesabınızın erişebildiği Responses ve Structured Outputs uyumlu bir model olmalıdır; model veya ücretli kota varsayılmadı. Anahtarlar sohbete yazılmamalıdır.

Sabit HTTPS sağlayıcı adresi, JSON şemalı başlık/alt başlık/özet/HTML yanıtı, yanıt boyutu sınırı, zaman aşımı ve `store=false` kullanılır. Yanıt durumu ve alan uzunlukları doğrulanır; HTML sunucuda temizlenir. Ham sağlayıcı hataları kullanıcıya veya loglara aktarılmaz. Eksik ayar veya sağlayıcı hatasında sahte makale üretilmez.

Create/Edit formunda mevcut metin varsa değiştirme izni istenir. Başlık, alt başlık, kategori ve temizlenmiş içerik form/TinyMCE alanlarına aktarılır; özet ayrı bir bildirimde gösterilir. Liste ekranından gelen taslak aynı sekmede geçici sessionStorage ile Create ekranına taşınır ve okunduğunda silinir. Aktarım kendi başına veritabanına yazmaz veya yayımlamaz; normal Kaydet ve yayın akışı geçerlidir. Üretilen sağlık/SEO iddiaları editör tarafından doğrulanmalıdır.

## Doğrulama ve sınırlar

[Doğrulama kaydı](PROMPT5-VALIDATION.md): 0 hata, 0 uyarı; 396 .NET ve 25 JavaScript testi; BusinessLayer kapsamı %87,59. Rol sadeleştirmesinden doğan yinelenen test girdileri kaldırıldı; artık geçerli olmayan eski Author/Moderator beklentileri yeni üç seviyeye uyarlandı.

Gerçek Google Client ID/Secret ve AI anahtarı/model ortamda tanımlı değildir. Canlı sağlayıcı oturumu veya ücretli AI üretimi yapılmış gibi raporlanmadı. Google callback güvenliği korunan test oturum biletleriyle; AI başarı, sanitization, bozuk yanıt ve hata durumları sahte HTTP transport ile doğrulandı. Tarayıcıda anahtarsız hata durumları ve gerçek ekranlar ayrıca kontrol edildi.

Görsel kanıtlar `.artifacts/prompt5-proof/`, test çıktıları `.artifacts/prompt5-quality.log`, derleme çıktısı `.artifacts/prompt5-build.log`, commit listesi `.artifacts/prompt5-log20.txt` içindedir. İstenen 18. commit mesajındaki `374` sayısı aynen korundu; gerçek güncel sonuç 396 .NET + 25 JavaScript testidir.

Resmî uygulama kaynakları:

- [ASP.NET Core Google dış giriş kurulumu](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-9.0)
- [OpenAI Structured Outputs](https://developers.openai.com/api/docs/guides/structured-outputs?api-mode=responses)
