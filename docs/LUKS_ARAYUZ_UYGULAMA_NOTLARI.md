# XSPA lüks arayüz uygulama notları

14 Ekim 2026. ASP.NET Core MVC ön yüz yenilemesi.

## Uygulanan tasarım

Zümrüt/gece yeşili `#081C15`, `#0D2B22`, `#1B4332`; şampanya altını `#D4AF37`, `#E5C158`, `#C5A880`; fildişi/krem `#FDFBF7`, `#F7F4EB`. Başlıklarda Playfair Display, metinlerde Outfit kullanıldı. Fontlar yerel dosyalardan sunulur; Google Fonts'a çalışma anında bağımlılık yoktur.

Üç görselli Swiper hero, şeffaflıktan cam etkili koyu zemine geçen sabit menü, altı imza ritüeli kartı, iyi yaşam günlüğü, galeri/lightbox, yorum alanları, floating label iletişim formu, açılabilir Google haritası, footer ve üyelik ekranları yenilendi. Mobil menü Escape ile kapanır. Slaytlar duraklatılabilir; `prefers-reduced-motion` otomatik geçişleri, AOS ve dekoratif animasyonları kapatır. Kart görselleri hover'da %4 büyür; altın butonlarda shimmer, WhatsApp'ta pulse, ikonlarda hafif dönme/büyüme bulunur.

Telefon: **+90 538 878 70 00**; tıklama `tel:+905388787000`. WhatsApp: `https://wa.me/905388787000`. Header, hero, iletişim, footer ve sabit hızlı iletişim düğmesi bu bilgileri kullanır. Mevcut Contact kayıtlarında adres/e-posta korunarak telefon güncellendi. Development seeder tekrar çalıştırılabilir ve geçerli yüklenmiş fotoğrafları korur.

İşletme sahibinin verdiği çalışma saatleri: **haftanın her günü 09:00–00:00**. İletişim kartında ve footer'da Türkçe/İngilizce gösterilir; ortak değer `WebUI/Models/BusinessDisplayInfo.cs` içindedir.

## Medya seti ve kaynaklar

`WebUI/wwwroot/img/` altında **30 WebP** dosyası: üç hero, altı hizmet, on blog konusu, sekiz galeri, iki portre ve bir hikâye fotoğrafı. Toplam yaklaşık 2,8 MB. Hero dosyaları en çok 2200px, kart dosyaları en çok 1200px genişlikte hazırlanmıştır. Yüklenen gerçek kullanıcı/blog fotoğrafları `wwwroot/uploads/` üzerinden korunur; mevcut ve geçerli dosyalar stock fotoğrafla değiştirilmez.

Fotoğraflar [Pexels lisansı](https://www.pexels.com/license/) kapsamında indirildi; yerel WebP formatına dönüştürüldü. Her dosyanın fotoğraf ID'si, kaynak/download URL'si, lisansı, ölçüleri ve boyutu `wwwroot/img/media-sources.json` dosyasındadır. Bunlar örnek spa atmosferi görselleridir; gerçek işletmenin mekân/personel fotoğrafları olduğu iddia edilmez. Portre dosyaları kullanılabilir medya setine dahildir; gerçek kullanıcıların mevcut profil görselleri korunur. Yazar rozetleri kayıtlı yazar adına dayanır.

Font kaynakları/lisansları `wwwroot/fonts/SOURCES.md`, `outfit-OFL.txt` ve `playfairdisplay-OFL.txt` içindedir. Mevcut **Font Awesome Free 6.7.2** ve yerel SVG lotus marka işareti kullanıldı; ücretli Pro ikon bağımlılığı eklenmedi.

## Dosyalar

- `wwwroot/css/site.css`, `wwwroot/css/login.css`: responsive tasarım sistemi, tipografi, renkler, kartlar, menü, form, yorum, footer, animasyon ve hareket azaltma kuralları.
- `wwwroot/js/site.js`, `wwwroot/js/slider.js`: güvenli bileşen başlatma, menü/scroll/klavye etkileşimleri, Swiper, AOS, Fancybox ve pause/reduced-motion davranışı.
- `Views/LayoutPartial/_Header.cshtml`, `Views/Shared/_UILayout.cshtml`, `_LoginLayout.cshtml`, `_ViewImports.cshtml`: ortak çerçeve, yerel font/stil/script dosyaları, semantik main ve içeriğe geç bağlantısı.
- `Views/Shared/Components/_Navbar`, `_WelCome`, `_HomeService`, `_HomeBlog`, `_Galary`, `_Footer`, `BlogInteraction`: ortak menü ve ana sayfa bileşenleri.
- `Views/Default`, `Views/About`, `Views/Service`, `Views/Blog`, `Views/Galary`, `Views/Contact`, `Views/Profile`: public sayfalar, detaylar, filtreler, yorum/etkileşim ve profil görünümü.
- `Services/PublicMediaService.cs`: geçerli yerel/yüklenmiş görseli korur; eksik eski dosyaları konuya uygun yerel fotoğrafa yönlendirir.
- `Data/DataSeeder.cs`: telefon ve eksik örnek medya düzeltmesi; production test hesapları oluşturmaz.
- `Models/BusinessDisplayInfo.cs`: işletmenin verdiği ortak saat bilgisi.
- `Controllers/ServiceController.cs`: yalnız yayımlanmış hizmetlerin güvenli detay rotası `/hizmetler/{id}`; kartlar gerçek detaylara gider.
- `Controllers/BlogController.cs`: mevcut public CQRS sorgusu üzerinden kategori ve arama filtreleri.
- `Controllers/ContactController.cs`: bülten ilgisini CSRF korumalı iletişim formuna dolduran doğrulama akışı.
- `BusinessLayer/Dtos/BlogReadDtos.cs`, `CQRS/Handlers/BlogHandlers.cs`: public DTO'da yaklaşık okuma süresi ve gerçek Like/Heart toplamları. Cache'de makale bilgisi bulunsa da toplamlar tek toplu sorguyla güncel okunur; kullanıcı/Identity entity'si dönmez. DTO cache anahtarı sürümlendirildi.
- `BusinessLayer/Container/Additions.cs`, `Controllers/AdminAboutCardController.cs`, `Views/AdminAboutCard`: mevcut yönetim menüsündeki eksik servis/görünüm bağlantıları tamamlandı; altı profil için erişim sınırı doğrulandı.

## Güvenlik ve form davranışı

Yorum/favori/reaksiyon ve dil değiştirme formlarının CSRF, rol, aktif hesap ve sahiplik denetimleri korunur. Blog/About rich text sunucu sanitizer'ından geçer. Galeri caption'ları, lightbox içinde HTML olarak yorumlanmaması için metin olarak kodlanır. Mesaj bildirimleri `text` üzerinden gösterilir.

Bülten formu, verilen e-postayla iletişim formunu açar. Kullanıcı talebini tamamlayıp doğrulamadan kayıt veya e-posta oluşturmaz; olmayan bir abonelik kaydı için başarı mesajı verilmez. Mesaj formu mevcut CAPTCHA ve SMTP/Hangfire altyapısını kullanır. Bu geliştirme ortamında CAPTCHA anahtarı tanımlı değilse form uygun bilgiyle kapalı gösterilir; telefon ve WhatsApp aktiftir. Güvenlik doğrulaması atlanmadı.

## Doğrulama

- **269 xUnit testi geçti**, 0 başarısız/atlanan. BusinessLayer kapsamı **%89,14**, 1075 / 1206 satır; %70 kapısı başarılı.
- Tepki toplamlarının cache varken güncel kalması, bülten CSRF/onay akışı, galeri caption kodlaması, telefon güncellemesinde adres/e-posta koruma ve AboutCard yönetim erişimi doğrulandı.
- Masaüstünde altı public sayfa 200 döndü; kırık görsel, yatay taşma ve JavaScript hatası görülmedi.
- **28 responsive/etkileşim kontrolü**: sekiz mobil sayfa, menü, dil cookie'si, duraklatma/reduced-motion, galeri lightbox ve inert caption, bülten doldurma, hizmet detayı ve gerçek Linux tema/görsel erişimi.
- **38 canlı HTTP kontrolü** yeniden geçti: üyelik, favori/reaksiyon, rol/lockout, CSRF/dil, raporlar ve Hangfire sınırları.
- Windows ve Linux Docker derlemeleri başarılı; SQL/Redis/webui healthy, migrate exit 0. EF model değişikliği yok; yeni migration gerekmedi.

Kanıtlar `.artifacts/luxury-final/` altında saklanır. Cobertura/TRX son koşu kaynağı `.artifacts/quality/19b07250a3244b08803f0d2f98556585/` içindedir. Ekran görüntüleri `.artifacts/luxury-*.png`, browser sonuçları `.artifacts/luxury-browser-results.json` ve `.artifacts/luxury-interaction-results.json` dosyalarındadır.

Çalışan geliştirme adresi `http://localhost:5157`; Docker adresi `http://localhost:5158`.
