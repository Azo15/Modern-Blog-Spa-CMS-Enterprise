# XSPA Ön Yüz ve Etkileşim Uygulama Raporu

> **Tarihsel not — 16 Ekim 2026:** Bu belge 15 Ekim'deki ilk ön yüz teslimini kaydeder. Aşağıdaki WhatsApp taslağına dayalı VIP akışı ve “migration gerekmiyor” bilgisi o aşamaya aittir. Güncel site içi rezervasyon, yeni randevu tabloları, concierge ve hizmet galerisi çalışmaları [Online Rezervasyon Uygulama Raporu](ONLINE_REZERVASYON_UYGULAMA_RAPORU.md) içinde belgelenmiştir.

15 Ekim 2026 · Mevcut ASP.NET Core MVC uygulamasına uygulandı.

## Sonuç

Tekrarlanan örnek fotoğraflar değiştirildi. 29 blog, 12 hizmet ve 12 galeri kaydı ayrı fotoğraflarla sunulur. Hero, hakkımızda ve VIP seçimleri de bağımsız kaynaklar kullanır. Toplam 65 yerel WebP dosyası 5.150.332 bayttır; kaynak fotoğraf kimlikleri ve SHA256 değerleri benzersizdir. Aynı kaydın kartı ve detay sayfası kendi fotoğrafını kullanmaya devam eder; marka işareti dekoratif olarak tekrarlanabilir.

İletişim formu, tema seçimi, beğeni/favori giriş uyarısı, açılır yorum formları, AJAX bülten ilgisi kaydı ve üç adımlı VIP ön talep modalı çalışır. Çalışma saatleri haftanın her günü 09:00–00:00 olarak korunmuştur.

## Güncellenen dosyalar

WebUI servis ve kontrolcüleri:

- `WebUI/Services/PublicMediaService.cs`: Genel anahtar sözcük eşlemesi yerine başlığa özel bağımsız medya; mevcut ortak seed dosyalarının düzeltilmesi; gerçek kullanıcı yüklemelerinin korunması; kimliğe bağlı yer tutucu.
- `WebUI/Controllers/PublicMediaController.cs`: Sadece izin verilen tür ve 24 haneli hexadecimal anahtar kabul eden güvenli SVG yer tutucu. Kullanıcı metni SVG içine yazılmaz.
- `WebUI/Data/DataSeeder.cs`: Development kayıtlarının benzersiz medya eşlemeleri; aynı başlıklı ek kayıtta bağımsız yer tutucu; yeniden çalıştırılabilir onarım.
- `WebUI/Controllers/ContactController.cs`: Kalıcı iletişim/bülten kayıtları, hata yanıtları, Managers bildirimi ve ortam/yapılandırma temelli CAPTCHA davranışı.
- `WebUI/Models/VipReservationViewModel.cs`, `WebUI/ViewComponents/Default/_VipReservation.cs`: Yalnızca yayımlanmış hizmetlerden seçilen modal seçenekleri. Hizmet yayından kaldırıldığında diğer hizmetlerin fotoğrafı kaymaz.

Razor ve tarayıcı dosyaları:

- `WebUI/Views/Shared/Components/_Navbar/Default.cshtml`: Projedeki `_Navbar` görünümü; güneş/ay teması ve VIP tetikleyicisi.
- `WebUI/Views/Shared/Components/_WelCome/Default.cshtml`: Üç hero ritüel düğmesi aynı VIP akışını açar.
- `WebUI/Views/Shared/Components/_VipReservation/Default.cshtml`: Hizmet, gün/saat, isim/telefon adımları ve erişilebilir modal.
- `WebUI/Views/Contact/Index.cshtml`: Anahtarsız ortamda tıklanabilir form ve lüks SweetAlert görünümü.
- `WebUI/Views/Shared/Components/BlogInteraction/Default.cshtml`: Altın detaylı favori, başparmak, kalp ve alkış düğmeleri; gerçek sayaçlar; anonim kullanıcıya giriş modalı.
- `WebUI/Views/Blog/_Comments.cshtml`: Yanıt ve kendi yorumunu düzenleme alanlarında `aria-expanded`, `aria-controls`, `hidden` ve `inert` ile açılır paneller. Formlar CSRF ve mevcut sahiplik denetimlerini kullanır.
- `WebUI/Views/Shared/Components/_Footer/Default.cshtml`: AJAX bülten formu ve yerinde başarı/hata durumu.
- `WebUI/Views/Shared/_UILayout.cshtml`, `WebUI/Views/LayoutPartial/_Header.cshtml`, `WebUI/Views/LayoutPartial/_Script.cshtml`: Lotus yükleme görünümü, ilk boyamadan önce tema tercihi, ortak modal ve sürümlü tarayıcı dosyaları.
- `WebUI/Views/Blog/Post.cshtml`, `WebUI/Views/Blog/_BlogCardPartial.cshtml`, `WebUI/Views/Service/ServiceDetails.cshtml`, `WebUI/Views/Service/_ServiceCardPartial.cshtml`, `WebUI/Views/Galary/_GalleryCardPartial.cshtml`: Kimliğe bağlı medya çağrıları.
- `WebUI/wwwroot/css/site.css`, `WebUI/wwwroot/js/site.js`: Obsidyen/zümrüt koyu tema, fildişi açık tema, localStorage tercihi, altın düğmeler, yanıt animasyonu, bildirimler, VIP akışı, ince altın scrollbar ve fare ile sınırlı 3D tilt.

## Formların gerçek davranışı

İletişim gönderiminde mevcut `IContactSubmissionService`, ContactForm ile ContactEmailDelivery kaydını atomik olarak saklar. E-posta işi mevcut Hangfire kuyruğuna alınır. Kuyruk/SMTP kesintisi mesaj kaydını kaybettirmez. SignalR yalnız `NotificationHub.ModerationGroup`/Managers grubunu bilgilendirir; bildirim kesilirse kaydedilmiş mesajın başarı yanıtı korunur.

Kullanıcının istediği koşul uygulanmıştır: Development ortamında veya reCAPTCHA site/secret anahtarlarından biri eksikse CAPTCHA geçişi kullanılır. Her iki anahtarı tanımlanmış Production ortamında sunucu doğrulaması zorunludur. CSRF, alan doğrulaması ve IP hız sınırı devam eder.

Bülten AJAX isteği `POST /iletisim/bulten` adresine FormData ve CSRF token ile gider. Başarı JSON'u yalnız kalıcı yazma tamamlandıktan sonra döner. Bülten ilgisi ContactForm ve teslimat niyeti olarak yönetim panelinde saklanır; toplu e-posta dağıtımı bu değişikliğin kapsamına eklenmemiştir. Aynı e-posta için başarılı kayıt sonrasında 10 dakika boyunca bellek önbelleğinden tekrar yanıtı verilir; IP için 60 saniye sınırı uygulanır. Veritabanı hatasında 503, geçersiz e-postada 400, hız sınırında 429 gelir. JavaScript kapalıysa gönderim yönlendirme ve SweetAlert bilgisiyle tamamlanır.

VIP akışı hizmet seçmeden, geçmiş tarih seçerek, tek karakterli isimle veya 10–15 rakam sınırına uymayan telefonla ilerlemez. Gün ve saat Europe/Istanbul üzerinden değerlendirilir; farklı ülkedeki tarayıcıda seçilen gün değişmez. Tamamlandığında `https://wa.me/905388787000?text=...` içinde kodlanmış bir mesaj taslağı açılır. Mesajı ziyaretçi gönderir; randevunun uygunluğunu işletme teyit eder. Teşekkür metni kesinleşmiş randevu iddiası içermez. Pop-up engellenirse teşekkür penceresindeki bağlantıyla taslağa yeniden ulaşılır. Bootstrap kapanma olayı tamamlandıktan sonra SweetAlert açılarak klavye odağı korunur.

Kullanıcı metinleri `textContent`/SweetAlert `text` ile gösterilir. Hareket azaltma tercihi tilt, konfeti, kaydırma ve yükleme animasyonlarında dikkate alınır. Lotus görünümü JavaScript olmadan sayfayı örtmez ve süre sınırıyla kapanır.

## Veritabanı ve kurulum

Yeni entity veya şema alanı eklenmedi; **EF Core migration gerekmiyor**. Mevcut migration'lar uygulanmış olmalıdır. Development başlangıcında DataSeeder eski örnek medya yollarını onarır. Production'da demo hesap/seed işlemi çalıştırılmaz; görünüm servisi mevcut örnek medya yollarını sunum sırasında eşler. Gerçek yüklenen dosyalar ve soft-delete görselleri korunur.

Yeni NuGet veya ön yüz CDN bağımlılığı eklenmedi. Bootstrap, SweetAlert2 ve yerel fontlar mevcut projeden kullanılır. Fotoğraflar uygulama çalışırken dış kaynaktan indirilmez. Kaynaklar ve lisans bağlantıları `docs/PUBLIC_MEDIA_CREDITS.md` ile `WebUI/wwwroot/img/media-sources.json` içindedir. Yeniden hazırlama aracı `scripts/Prepare-PublicMedia.py` dosyasıdır.

## Testler ve kanıt

- **288 xUnit testi başarılı**, başarısız/atlanan yok. BusinessLayer kapsamı **%89,14**: 1075/1206 satır; %70 kalite kapısı geçti.
- `BusinessLayer.Tests/ContactFlowTests.cs`: Development/eksik anahtar geçiş matrisi, tam yapılandırılmış Production CAPTCHA reddi, gerçek kalıcı kayıt ve outbox, yalnız Managers bildirimi, SignalR hatasında kayıt korunması, AJAX kayıt, kuyruk kapalıyken veri korunması, tekrar yanıtı, geçersiz e-posta ve kalıcılık hatasında başarısız yanıt.
- `BusinessLayer.Tests/PublicMediaTests.cs`: Ortak eski görselin farklı fotoğraflara ayrılması, gerçek yüklemelerin korunması, güvenli yol denetimi, bağımsız yer tutucular, manifest kaynak/hash benzersizliği. `DevelopmentSeederTests` yeniden çalıştırılabilir medya onarımını doğrular. Medya/seeder odaklı paket 9/9 geçti.
- `BusinessLayer.Tests/ApiEndpointTests.cs`: Bülten CSRF kontrolü ve yeni kalıcı kayıt davranışı güncellendi.
- **71 otomatik tarayıcı kontrolü başarılı**: altı masaüstü sayfa, açık/koyu tema ve tercih kalıcılığı, mobil taşma denetimleri, görsel yükleme/tekrar denetimi, anonim giriş yönlendirmesi, etkin iletişim düğmesi, AJAX başarı/hata, VIP adımları ve geçersiz veri reddi, Honolulu saat diliminde doğru Türkiye tarihi, WhatsApp taslak içeriği, teşekkür odağı ve hareket azaltma. JavaScript hatası yok.
- Son WebUI derlemesi: **0 hata**, mevcut nullable/legacy migration kaynaklı 31 uyarı.

Tam test kaynağı `.artifacts/quality/2c79b9ef27764bd3b8e359609e2c7f7e/`, tarayıcı sonuçları ve ekran görüntüleri `.artifacts/public-experience/` altındadır. Tekrar çalıştırma: `scripts/Test-QualityGate.ps1` ve Playwright kurulu Node ortamında `node scripts/Test-PublicExperience.cjs`. Tarayıcı testinin `BASE_URL` ve `BROWSER_CHANNEL` seçenekleri vardır. Browser bülten taşıması yalnız UI testi için taklit edilmiştir; kalıcı kayıt xUnit entegrasyon testinde gerçek SQLite ile doğrulanır. WhatsApp açılışı testte yakalanır; dışarıya mesaj gönderilmez.

Yerel geliştirme ön izlemesi: http://localhost:5157/ .

## Detay görselleri ek düzeltmesi

Blog kapaklarının sabit 460px/300px kırpılması ve hizmet detaylarının zorunlu kare/oval çerçevesi detay sayfaları için kaldırıldı. Fotoğraflar doğal en-boy oranında, tamamı görünecek şekilde, en fazla ekran yüksekliğinin %68'i/640px sınırında sunulur. Gerçek kaynakla uyuşmayan HTML boyutları kaldırıldı. Liste kartları ve hakkımızda tasarımı korunur.

İki blog ve dört hizmet detayında 1920×1080, 1280×900 ve 390×844 ekranlarla toplam 18 görünüm kontrol edildi: kaynak oranı korunuyor, büyütme/kırpma ve yatay taşma yok, sayfalar 200 dönüyor. Bu ön yüz düzeltmesinde backend testleri yeniden çalıştırılmadı. Kanıtlar `.artifacts/detail-image-fix/` dizinindedir.
