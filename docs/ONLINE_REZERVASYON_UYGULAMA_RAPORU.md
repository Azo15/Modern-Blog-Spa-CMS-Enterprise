# XSPA Online Rezervasyon ve Ritüel Deneyimi Uygulama Raporu

16 Ekim 2026 · ASP.NET Core MVC / .NET 9

## Tamamlanan davranış

Navbar, hero, hizmet detayı ve footer üzerindeki randevu düğmeleri artık site içindeki üç adımlı rezervasyon formunu açar. Ziyaretçi hizmet, Türkiye saatine göre gün/saat, 1–4 misafir ve iletişim bilgilerini seçer; isteğe bağlı not ekleyebilir. Hizmet detayından açılan form ilgili hizmeti seçili getirir. `GET /randevu?serviceId=1` de aynı yerel akışa yönlendirir.

`POST /randevu` başarılı olduğunda randevu ve e-posta teslimat niyeti aynı veritabanı yazımında saklanır. İlk kayıt **201 Created**, aynı talebin güvenli tekrarı **200 OK** döndürür. Form, veritabanında üretilen `#XSPA-…` referansını gösterir. Randevu **Pending / işletme onayı bekleniyor** durumundadır; kesinleşmiş rezervasyon veya otomatik müsaitlik garantisi verilmez. İşletme `/AdminAppointment/Index` üzerinden manuel onaylar ya da iptal eder.

WhatsApp, formun altında ve iletişim bölümünde ikincil danışma seçeneği olarak korunur. Bir randevu talebini kaydetmek için WhatsApp açılması veya ziyaretçinin dışarıya mesaj göndermesi gerekmez.

## Concierge, görseller ve ortak arayüz

- İletişim sayfasındaki basık, tam genişlikte harita şeridi kaldırıldı. Harita artık `container` içinde iki kolonlu, yuvarlatılmış bir lokasyon ve konsiyerj kartıdır. Sol panel açık adresi, kullanıcının verdiği ulaşım/vale/otopark metinlerini, her gün **09:00–00:00** saatlerini ve Google Haritalar yol tarifi düğmesini içerir. Sağdaki iframe altın çerçeve ve gölgeyle sunulur; mobilde alt alta yerleşir. Harita ve yol tarifi mevcut adres metnini kullanır; ayrıca doğrulanmış koordinat eklenmemiştir.
- Mevcut **12 örnek hizmetin her birinde 3 farklı WebP fotoğraf** bulunur: hizmetin kendi kapağı ve iki bağımsız hazırlık/uygulama fotoğrafı. Toplam 24 yeni ritüel fotoğrafı eklendi; mevcut setle birlikte 89 bağımsız fotoğraf vardır. Kaynaklar ve SHA256 değerleri `WebUI/wwwroot/img/media-sources.json`, lisans/kaynak açıklamaları `docs/PUBLIC_MEDIA_CREDITS.md` içindedir.
- Hizmet detayında thumbnail seçimi, yumuşak geçiş ve Fancybox tam ekran görüntüleme çalışır. Ana fotoğrafın özgün oranı korunur; sabit kırpma uygulanmaz. Thumbnail kırpmaları yalnız önizleme içindir. Özel kullanıcı yüklemeleri korunur; tanınmayan yeni hizmetlere başka bir hizmetin fotoğraf seti otomatik atanmaz.
- Hizmet detayı; altın çerçeveli fiyat/süre kartı, üç aşamalı ritüel yolculuğu ve ortak FAQ bileşeniyle tamamlandı. FAQ hem hizmet detayında hem iletişim sayfasında çalışır. İptal için doğrulanmamış ücret veya süre politikası uydurulmaz; ziyaretçi rezervasyon koduyla işletmeye yönlendirilir.
- `/hizmetler` üzerinde tüm ritüeller, masaj, hamam/kese, cilt/yüz ve sauna/detoks sekmeleri sayfa yenilenmeden filtreler; sonuç sayısı erişilebilir durum alanında güncellenir.
- Ana sayfada dört anlatımlı misafir deneyimi slider'ı vardır. Bölüm ve her kart **temsili deneyim** olarak açıkça etiketlenmiştir; gerçek müşteri yorumu, Google değerlendirmesi veya doğrulanmış puan ortalaması gibi sunulmaz.
- Yeni bileşenlerde Türkçe/İngilizce arayüz, açık/koyu tema, klavye odağı, `aria-*` durumları, mobil düzen ve azaltılmış hareket tercihi korunur. Rezervasyon içeriği kaydırılırken alt işlem düğmeleri görünür kalır.

## Katmanlara göre dosyalar

### EntityLayer

- `Concrete/Appointment.cs`: `FullName`, `Phone`, `Email`, `ServiceId`, `AppointmentDate`, `TimeSlot`, `GuestCount`, `Notes`, `Status`, `ReferenceCode`, `ClientRequestId` ve hizmet başlığı anlık kopyası. `BaseEntity` üzerinden UTC audit ve soft-delete altyapısını kullanır.
- `Enum/AppointmentStatus.cs`: `Pending = 0`, `Confirmed = 1`, `Cancelled = 2`.
- `Concrete/AppointmentEmailDelivery.cs`: Kalıcı e-posta niyeti; sabit Message-Id, gönderilme zamanı, deneme sayısı, Hangfire iş kimliği, kiralama belirteci ve hata durumu.

### DataAccesLayer

- `Concrete/AppDbContext.cs`: `Appointments` ve `AppointmentEmailDeliveries` DbSet kayıtları.
- `Configurations/AppointmentConfiguration.cs`: Zorunlu alanlar ve uzunluklar, `date` kolonu, soft-delete filtresi, `Status` eşzamanlılık belirteci, hizmet/teslimat ilişkilerinde `Restrict`.
- `Migrations/20261015173153_AddOnlineAppointments.cs` ve `.Designer.cs`; `Migrations/AppDbContextModelSnapshot.cs`.

`ReferenceCode` ve `ClientRequestId` üzerinde ayrı benzersiz indeksler vardır. Bir randevuya tek teslimat niyeti `AppointmentId` benzersiz indeksiyle korunur. Durum/tarih/saat indeksi yönetim listelerini destekler. Migration mevcut içerik tablolarını silmez; iki yeni tablo ve indekslerini ekler.

### BusinessLayer

- `CQRS/Commands/AppointmentCommands.cs`: Oluşturma ve durum değiştirme komutları.
- `CQRS/Queries/AppointmentQueries.cs`, `Dtos/AppointmentDtos.cs`: Yönetim sorgusu, sayfalanmış yönetim DTO'su ve kayıt makbuzu.
- `CQRS/Handlers/AppointmentHandlers.cs`: Kalıcı kayıt, tekrar kontrolü, kuyruk ve bildirim çağrısı; yönetici sorgusu/onay/iptal akışı.
- `ValidationRules/AppointmentValidators.cs`: Alan, tarih, saat, misafir sayısı, sayfalama ve durum doğrulaması; ortak `AppointmentSchedule` saatleri.
- `Abstract/IAppointmentEmailQueue.cs`, `IAppointmentMailService.cs`: Kuyruk ve gönderim arayüzleri.
- `Jobs/HangfireAppointmentEmailQueue.cs`, `AppointmentEmailJob.cs`, `AppointmentEmailRecoveryJob.cs`: Hangfire gönderimi, yeniden deneme ve kesinti sonrası kurtarma.
- `Concrete/AppointmentMailService.cs`: Metin/HTML e-posta; kullanıcı alanlarında HTML encoding; mevcut yapılandırmadan SMTP okuma.
- `Container/Additions.cs`: Yeni servis ve job bağımlılıklarının kaydı. Yönetim bildirimi mevcut `IManagerNotifier` arayüzü üzerinden yapılır.

### WebUI

- `Controllers/AppointmentController.cs`, `Models/AppointmentRequest.cs`: Halka açık giriş, yalnız ziyaretçinin değiştirebileceği alanların bağlanması, CSRF, doğrulama, 201/200/400/409/503 yanıtları.
- `Controllers/AdminAppointmentController.cs`, `Views/AdminAppointment/Index.cshtml`: Durum filtresi ve sayfalama; ad/telefon/e-posta/not/ref kodu; onay ve iptal formları.
- `Infrastructure/ApiRateLimitRegistration.cs`: Randevu POST isteklerine IP başına dakika sınırı.
- `Program.cs`: Mevcut Hangfire/SignalR altyapısıyla bütünleşme; e-posta kurtarma recurring job'ı.
- `Models/VipReservationViewModel.cs`, `ViewComponents/Default/_VipReservation.cs`, `Views/Shared/Components/_VipReservation/Default.cshtml`: Yayımlanmış hizmetler, yerel rezervasyon formu, CSRF token, istemci talep kimliği ve başarı makbuzu.
- `Views/Shared/Components/_Navbar/Default.cshtml`, `_WelCome/Default.cshtml`, `_Footer/Default.cshtml`: Ortak rezervasyon modalı girişleri.
- `Views/Admin/_Sidebar.cshtml`, `Views/Shared/Components/_TopHeader/Default.cshtml`, `Views/Admin/_Script.cshtml`: Policy ile filtrelenen yönetim bağlantısı, ses etkinleştirme düğmesi ve güvenli SignalR bildirimi.
- `Services/PublicMediaService.cs`: Bağımsız hizmet galerisi eşlemesi; gerçek yüklemelerin korunması.
- `Views/Service/ServiceDetails.cshtml`, `Views/Service/Index.cshtml`, `Views/Service/_ServiceCardPartial.cshtml`: Hizmet galerisi, yolculuk, fiyat/süre ve canlı kategori filtresi.
- `Views/Contact/Index.cshtml`, `Views/Shared/_RitualFaq.cshtml`, `Views/Shared/_GuestExperiences.cshtml`, `Views/Default/Index.cshtml`: Concierge kartı, ortak FAQ ve temsili deneyimler.
- `wwwroot/css/site.css`, `wwwroot/js/site.js`: Yeni bileşenler, galeri geçişi, filtre/slider, rezervasyon doğrulaması ve AJAX gönderimi.
- `wwwroot/img/ritual/*.webp`, `wwwroot/img/media-sources.json`, `scripts/Prepare-PublicMedia.py`: 24 yeni yerel ritüel fotoğrafı ve yeniden hazırlama kaydı.

### Testler

- `BusinessLayer.Tests/AppointmentTests.cs`: SQLite üzerinde handler, doğrulama, transaction/outbox, yetki, durum, benzersiz kısıt, idempotency, kuyruk kesintisi/kurtarma ve e-posta teslimat testleri.
- `BusinessLayer.Tests/AppointmentEndpointTests.cs`: Gerçek HTTP uçları, antiforgery, hız sınırı, kalıcılık, replay conflict, yönetim policy'leri ve yerel rezervasyon yönlendirmesi.
- `BusinessLayer.Tests/PublicMediaTests.cs`: Hizmet fotoğraf setleri, kaynak/hash benzersizliği ve medya davranışı.
- `scripts/Test-PublicExperience.cjs`: Ana tarayıcı regresyon paketi. Ek salt-okunur görsel inceleme `.artifacts/concierge-review/review.cjs` ile kaydedildi.

## Güvenlik ve iş kuralları

Randevu oluşturmak için hesap gerekmez; yalnız yayımlanmış ve silinmemiş hizmetler kabul edilir. Gün/saat Europe/Istanbul üzerinden değerlendirilir. Tarih bugün ile önümüzdeki 90 gün arasında olmalı ve seçilen saat henüz geçmemiş olmalıdır. Saat seçenekleri `10:00`, `11:30`, `14:00`, `16:30`, `18:00`, `20:00`, `22:00` olarak ortaktır. Misafir sayısı 1–4; ad 2–120, telefon 10–15 rakam, not en fazla 2000 karakterdir. Sunucu doğrulaması MediatR pipeline'ında da çalışır.

Tüm yazma formları CSRF korumalıdır. `POST /randevu` için gövde sınırı 16 KB, IP başına sabit pencere sınırı **10 istek/dakika**, kuyruk uzunluğu sıfırdır; aşım **429** döndürür. Bu hız sınırlayıcı uygulama örneğinin belleğinde çalışır; çok örnekli dağıtımda ortak sınır istenirse ağ geçidi veya dağıtık sınırlayıcı gerekir.

Tarayıcı aynı yanıtı alamadığı talebi tekrar gönderirken aynı GUID'i korur. Aynı GUID ve aynı normalize içerik mevcut referansla 200 döner; yeni randevu, teslimat niyeti veya bildirim oluşturulmaz. Aynı GUID ile farklı içerik **409 Conflict** döndürür; önceki kayıt değiştirilmez ve kullanıcıya mevcut kodla ekibe ulaşması söylenir. Başarıdan sonra modal kapatıldığında yeni bir talep kimliği hazırlanır.

Yönetim listesi ve durum komutları hem controller hem handler katmanında `RequireContentManager` policy'siyle korunur: **System Admin ve Content Manager** erişebilir. Author, Moderator ve Registered User için bu yönetim yetkisi verilmemiştir. İptal edilmiş talep yeniden onaylanamaz; yeni talep gerekir. Aynı duruma tekrar geçiş idempotenttir; eşzamanlı durum güncellemeleri için concurrency kontrolü vardır.

SignalR bildirimi `Clients.All` kullanmaz; mevcut yetkili `Managers` grubuna gider. Başlık `Yeni Rezervasyon`, içerik müşteri/hizmet/tarih/saat bilgisidir. Paneldeki **“Bildirim sesini aç”** düğmesine basıldıktan sonra yeni rezervasyonda kısa ses çalar; tarayıcının kullanıcı etkileşimi gerektiren ses kuralı korunur. Toastr HTML escape eder; rezervasyon referansı ve form mesajları `textContent` ile yerleştirilir.

## E-posta kuyruğu ve işletim sınırları

Randevu ile `AppointmentEmailDelivery` aynı `SaveChangesAsync` çağrısında saklanır. Hangfire erişilemezse kayıt korunur. `recover-appointment-emails` görevi 10 dakikada bir gönderilmemiş niyetleri tarar; aktif kiralama, deneme sayısı ve son kuyruğa alma zamanını kontrol ederek yeniden kuyruklar.

`AppointmentEmailJob` üzerinde `[AutomaticRetry(Attempts = 3)]` vardır. Veritabanındaki 5 dakikalık kiralama eşzamanlı işçilerin aynı teslimatı göndermesini engeller; başarılı teslimatta `SentAtUtc` kaydedilir. Gönderilmiş kayıt normal yeniden denemelerde atlanır. SMTP hatası kiralamayı serbest bırakır ve hata türünü saklar. Kurtarma taraması dört denemenin altındaki kayıtları değerlendirir.

**Gerçek SMTP sunucusuna e-posta teslimi bu çalışmada doğrulanmadı.** SMTP ayarları ve alıcı kabulü ayrıca gerçek ortamda denenmelidir. Testlerde gönderici ve kuyruk kontrollü taklitlerle başarı/kesinti davranışı doğrulandı. SMTP kabulü ile veritabanında “gönderildi” yazımı tek atomik işlem olamayacağından, bu iki adım arasındaki çökmede mutlak tek teslimat garantisi verilmez; sabit Message-Id ve kiralama olağan tekrarları bastırır.

## Migration ve kurulum

Yeni migration **`20261015173153_AddOnlineAppointments`** repoda hazırdır; aynı isimle tekrar migration oluşturulmaz. Bağlantı `ConnectionStrings__DefaultConnection` ortam değişkeni veya WebUI User Secrets üzerinden alınır. Development başlangıcında mevcut `MigrateAsync` akışı migration'ı uygular; Production için açık dağıtım adımı kullanılmalıdır.

Çözüm kökünden hedef veritabanına uygulama:

```powershell
dotnet ef database update 20261015173153_AddOnlineAppointments --context AppDbContext --project DataAccesLayer/DataAccesLayer.csproj --startup-project WebUI/WebUI.csproj
```

Önceki migration'dan itibaren ekleme yapan, migration geçmişiyle korunan idempotent SQL dosyası **`.artifacts/OnlineAppointments.sql`** hazırdır. Yeniden üretme komutu:

```powershell
dotnet ef migrations script 20261013214716_AddStage3MemberInteractionsAndJobs 20261015173153_AddOnlineAppointments --idempotent --context AppDbContext --project DataAccesLayer/DataAccesLayer.csproj --startup-project WebUI/WebUI.csproj --output .artifacts/OnlineAppointments.sql
```

Bu artımlı SQL, önceki migration'ları uygulanmış veritabanı içindir. `Appointments`, `AppointmentEmailDeliveries`, ilgili indeksler ve migration geçmişi kaydını ekler. Daha önce aynı migration uygulanmışsa ilgili blokları atlar; mevcut blog/hizmet/kullanıcı kayıtlarını temizleyen bir işlem içermez. `Down` işlemi yeni rezervasyon tablolarını kaldırdığı için canlı randevu verisi varken geri alma rutin yükseltme adımı olarak kullanılmamalıdır.

Mevcut Hangfire SQL bağlantısı, yöneticilere açık dashboard ve SMTP yapılandırması kullanılır. Bu çalışma bağlantı parolalarını veya SMTP sırlarını teslim raporuna eklemez.

## Doğrulama sonuçları

- **360/360 xUnit testi geçti**; başarısız veya atlanan test yok. Son çalıştırma `.artifacts/quality/33ab998621374ccb80aef91d8e0e9ab9/tests.trx` içindedir.
- BusinessLayer satır kapsamı **%86,9936 — 1224/1407 satır**. Ölçüm tüm BusinessLayer derlemesini kapsar; %70 kalite eşiği geçildi. Kanıt aynı dizindeki `coverage-summary.json` ve Cobertura raporudur.
- Ana tarayıcı paketi **114/114 kontrol** geçti; sonuçlar `.artifacts/public-experience/results.json`, yürütme kaydı `.artifacts/online-reservation-browser.log` içindedir. Rezervasyon adımları, geçersiz veri reddi, Türkiye tarihi, gerçek hizmet ön seçimi, başarı kartı/referans gösterimi, modal yeniden kullanımı, thumbnail/lightbox, filtreler, FAQ, temsili yorum slider'ı, açık/koyu tema, mobil taşma ve azaltılmış hareket incelendi. JavaScript hatası yok.
- Ek concierge/görsel incelemede **38/38 salt-okunur kontrol** geçti. 1440×1000 masaüstü, 390×844 mobil ve 390×500 kısa mobil görünüm incelendi; harita yüksekliği, FAQ, fotoğraf oranı, lightbox, hizmet ön seçimi ve not alanına kaydırırken görünür kalan modal alt düğmeleri doğrulandı. Yerel rezervasyon POST'u yapılmadı. Kanıtlar `.artifacts/concierge-review/results.json` ve aynı dizindeki PNG dosyalarıdır. Google karoları ayrı incelemede yüklendi; konsol/ağ hata kaydı oluşmadı.
- Son normal WebUI derlemesi **0 hata**, mevcut nullable kaynaklı **31 uyarı** ile tamamlandı. Yeniden başlatılan yerel uygulamada gerçek LocalDB üzerinden yönetim okuması başarılıdır: yetkili yönetici için `/AdminAppointment/Index` **200**, başlık ve tablo/boş durum görünür; anonim istek **302** ile girişe yönlenir. Ana sayfa **200** döner. Bu salt-okunur canlı kontrolün kaydı `.artifacts/online-reservation-live-smoke.json` içindedir; canlı rezervasyon oluşturma testi olarak değerlendirilmez.

**Tarayıcı paketindeki rezervasyon POST yanıtı UI testi için taklit edilmiştir.** Bu paket gerçek veritabanı kaydı veya gerçek e-posta tesliminin kanıtı olarak sunulmaz. Gerçek kalıcılık, SQLite ilişkisel veritabanı kullanan handler/HTTP entegrasyon testlerinde doğrulanır: Pending randevu ve outbox'ın saklanması, kuyruk kapalıyken veri kaybı olmaması, aynı GUID ile tek kayıt, değişmiş içerikte 409, ilişkisel benzersiz indeksler ve yönetim yetkileri bu testlerde çalışır. Tarayıcı incelemelerinde dışarıya WhatsApp mesajı veya gerçek müşteriye e-posta gönderilmedi.

Tekrar çalıştırma:

```powershell
./scripts/Test-QualityGate.ps1
node scripts/Test-PublicExperience.cjs
```

Tarayıcı komutu Playwright bulunan Node ortamında çalıştırılır; gerekirse `NODE_PATH`, `BASE_URL` ve `BROWSER_CHANNEL` ortam değişkenleri kullanılır. Sonuç dizinleri her paketin kendi kanıtlarını içerir; 114 ve 38 kontrol ayrı turlardır ve bazı senaryoları tekrarlar.

## Teslim dosyaları

Kaynak kodlar yukarıdaki katman dosyalarındadır. Teslim çıktıları:

- `.artifacts/teslim/XSPA-Online-Rezervasyon-Tam-Kodlar.md`: İstenen entity, migration, controller, Razor, CSS ve JavaScript dosyalarının eksiksiz kod dökümü.
- `.artifacts/teslim/XSPA-Online-Rezervasyon-2026-10-16.zip`: Mevcut projeye uygulanacak kaynak dosyaları, görseller, testler, bu rapor, SHA256 manifesti ve `deployment/OnlineAppointments.sql`.

Bu rapor, önceki WhatsApp taslağına dayalı VIP akışının yerine geçen güncel yerel rezervasyon davranışını açıklar. Önceki aşamanın tarihsel kaydı `docs/ON_YUZ_ETKILESIM_UYGULAMA_RAPORU.md` içinde korunmuştur.
