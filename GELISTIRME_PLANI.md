# Modern Blog / Spa CMS Geliştirme Planı

Hazırlanma tarihi: 13 Ekim 2026  
Dayanak: Kullanıcının paylaştığı gelişim ve dönüşüm şartnamesi ile mevcut kaynak kodun incelenmesi.  
Süre varsayımı: Tek geliştirici, haftada yaklaşık 25–30 saat; 8 haftalık ana çalışma ve 2 haftalık düzeltme payı. Süreler tahmindir; ilk hafta sonunda yeniden değerlendirilir.

## 1. Amaç ve yaklaşım

Mevcut çalışan MVC sitesini; rollere göre erişimi denetlenen, içerik üretim ve onay süreci bulunan, kullanıcı etkileşimi sunan ve testlerle doğrulanan bir CMS'ye dönüştürmek.

Mevcut dört katman korunacak. CQRS dönüşümü modül modül yapılacak; tüm projeyi yeniden yazmak başlangıç hedefi değildir. Clean Architecture hedefi; iş kurallarının HTTP, dosya depolama ve önbellek ayrıntılarından ayrılması, bağımlılıkların gözden geçirilmesi ve controller'ların inceltilmesiyle uygulanacak. CQRS kullanılması tek başına bu hedefin tamamlandığı anlamına gelmez.

## 2. İncelenen mevcut durum

- **Çalışan temel:** .NET 9 MVC, Identity, EF Core, dört katman ve yerel başlatma altyapısı mevcut. Site önceki kontrolde HTTP 200 döndürdü.
- **Yetkilendirme kısmi:** Rol yönetiminde System Admin kontrolü var. Yönetim controller'larının çoğunda yalnızca `[Authorize]` bulunuyor. Rol bazlı ve kayıt sahipliğine dayalı kontroller tamamlanmalı.
- **Soft delete kısmi:** BaseEntity, migration, global sorgu filtreleri ve SaveChanges dönüşümü var. İncelenen audit kodu kullanıcı alanlarını otomatik doldurmuyor. Geri yükleme akışı tamamlanmalı. Blog silme işlemi görseli fiziksel siliyor; geri alma sırasında görsel kaybı önlenmeli.
- **CQRS kısmi:** Blog command/query handler'ları mevcut; AdminBlogController doğrudan servis ve IQueryable kullanıyor. Oluşturma handler'ı içeriği doğrudan yayımlıyor; yazar taslak süreciyle uyumlandırılmalı.
- **Cache kısmi:** IDistributedCache tabanlı servis mevcut, ancak Program.cs bellek içi sağlayıcı kaydediyor. Bu, gerçek Redis bağlantısı değildir. Kategori değişiminde eski kategorinin cache anahtarı da temizlenmeli; yönetimden yapılan değişiklikler aynı akışı kullanmalı.
- **SignalR kısmi:** Hub, iletişim formundan bildirim ve panel istemcisi mevcut. İncelenen hub'da yetkilendirme yok; bildirimler Clients.All ile gönderiliyor. Alıcı kapsamı sınırlandırılmalı.
- **Loglama mevcut:** Serilog konsol ve günlük dosya kaydı ile hata middleware'i mevcut. Hassas veri maskeleme, hata davranışı ve log erişimi doğrulanmalı.
- **Editör mevcut:** TinyMCE entegrasyonu var; çalışma biçimi ve sunucuda HTML temizleme kontrol edilmeli.
- **Test başlangıcı mevcut:** Blog handler'ları için dört test görülüyor. Testlerin geçtiği veya %70 kapsama ulaşıldığı bu incelemede doğrulanmadı.
- **Docker dosyaları mevcut:** Dockerfile ve compose bulunuyor; temiz ortamda çalıştırılarak doğrulanmalı.
- **Yeni modüller:** İncelenen kaynaklarda nested yorum, favori/tepki, Hangfire, WebP dönüşümü, API/Swagger, dil yönetimi ve PDF/Excel raporlama uygulaması tespit edilmedi.

Bu çalışma kaynak kod incelemesidir; bütün ekranların, veritabanındaki kullanıcıların ve dağıtım senaryolarının çalıştığına dair kabul testi değildir.

## 3. Haftalık uygulama planı

### Hafta 1 — Güvenlik ve erişim temeli

**İşler**
- Yönetim ekranı ve işlem bazında yetki matrisi hazırlamak: System Admin, Content Manager, Author, Moderator, Registered User ve anonim Visitor.
- Merkezi policy tanımları; kullanıcı/rol yönetimi, içerik yönetimi ve iletişim yönetimi izinleri.
- Yazarın yalnızca kendi kayıtlarına erişmesini iş katmanında denetlemek. Düzenlemede mevcut yazarı korumak.
- Visitor'ı anonim erişim olarak ele almak; giriş yapılmasını gerektiren bir kullanıcı hesabına dönüştürmemek.
- Development ortamında diğer roller için tekrar çalıştırılabilir örnek hesap oluşturmak; üretimde örnek hesap oluşturmamak.
- Kaynakta bulunan bağlantı ve servis sırlarını ortam ayarlarına taşımak; açıkta kalan kimlik bilgilerinin yenilenmesini takip etmek.
- Mevcut build ve test sonuçlarını başlangıç kaydı olarak almak; kullanılan framework ve paketlerin destek/güvenlik durumunu resmi kaynaklardan kontrol etmek.

**Tamamlanma ölçütü:** Üye yönetim işlemi yapamaz; yazar URL veya kayıt ID'si değiştirerek başka yazarın kaydını düzenleyemez; rol değiştirme yalnızca System Admin'e açıktır. İzin verilen ve reddedilen durumlar otomatik testlerle doğrulanır.

### Hafta 2 — Veri güvenliği ve içerik yaşam döngüsü

**İşler**
- CurrentUser üzerinden CreatedBy, UpdatedBy ve DeletedBy alanlarını doldurmak; zamanları UTC saklamak.
- Yetkili çöp kutusu, geri yükleme ve ilişkili kayıt davranışları oluşturmak.
- Soft delete sırasında görselleri korumak; kalıcı temizlik için ayrı saklama süresi belirlemek.
- Taslak → inceleme bekliyor → yayımlandı → reddedildi iş akışı; yazar gönderir, içerik yöneticisi onaylar.
- Alan bazlı audit bilgilerine ek olarak kritik işlem geçmişi kaydı oluşturmak: işlem, aktör, kayıt ve zaman. Kayıt içeriğine hassas veri yazmamak.
- HTML içerik temizleme, dosya türü/boyutu doğrulama ve editör davranışını tamamlamak.

**Tamamlanma ölçütü:** Silinen içerik ziyaretçiye görünmez; yetkili kişi görseliyle geri yükleyebilir. İşlemi yapan kullanıcı kaydedilir. Taslak ve reddedilmiş içerik doğrudan adresle de halka açık değildir.

### Hafta 3 — CQRS ve Redis

**Bağımlılık:** Yetki ve yayın kuralları tamamlanmış olmalı.

**İşler**
- Blog ekranlarını mevcut command/query akışına geçirmek; sonra kategori, hizmet ve galeriye yaymak.
- Doğrulama, yetkilendirme, işlem sonucu ve iptal sinyali yönetimini ortaklaştırmak.
- Halka açık ve yönetim sorgularını ayrı tutmak; DTO kullanarak kullanıcı nesnelerinin cache'e taşınmasını önlemek.
- Gerçek Redis sağlayıcısını yapılandırmak; bellek içi geliştirme seçeneğini açıkça ayırmak.
- İçerik oluşturma, düzenleme, kategori değişimi, yayınlama, silme ve geri yükleme için cache temizleme kuralları.
- Liste sorgularına sayfalama; ölçüme göre gerekli indeksler ve sorgu düzenlemeleri.

**Tamamlanma ölçütü:** Yönetimden güncellenen içerik sonraki okumada doğru görünür; eski/yeni kategori listeleri tutarlıdır. Redis kesintisi için belirlenen davranış test edilir. Aynı veri ve koşullarda cache öncesi/sonrası sorgu sayısı ve yanıt süresi raporlanır.

### Hafta 4 — Yorum, moderasyon ve canlı bildirim

**İşler**
- Comment modeli: blog, kullanıcı, üst yorum, metin, moderasyon durumu ve audit alanları.
- Yanıt derinliği sınırı, aynı blog içinde parent kontrolü, spam/küfür kontrolü ve moderatör onayı.
- Üyenin kendi yorumunu düzenleme/silme akışı; düzenlenen yorumun yeniden onaya alınması.
- NotificationHub erişimini policy ile sınırlamak; bildirimleri izinli gruplara yönlendirmek ve istemcinin keyfi yayın yapmasını engellemek.
- Yeni mesaj ve yorum bildirimleri; yeniden bağlanma ve okunmamış bildirimlerin kalıcı saklanması.

**Tamamlanma ölçütü:** Onaysız yorum görünmez; başka kullanıcının yorumu değiştirilemez; anonim ve yetkisiz kullanıcılar yönetim bildirimlerini alamaz. Bağlantı kopup geldiğinde okunmamış bildirimler korunur.

### Hafta 5 — Üye özellikleri ve arka plan işleri

**İşler**
- Profil düzenleme, favoriler ve beğeni/tepki özellikleri; kullanıcı–blog ilişkilerinde tekrarlı kayıtları engellemek.
- Moderatörün hesap engelleme kapsamını rol yönetiminden ayırmak; engelin mevcut oturuma etkisini test etmek.
- E-posta işlerini Hangfire kuyruğuna taşımak; tekrar deneme ve iş durumunu izlemek.
- Yeniden denenen işlerin aynı işlemi tekrarlamasını önlemek; iş dashboard'unu System Admin ile sınırlamak.
- Saklama süresine uygun eski kayıt/dosya temizliğini zamanlanmış görev haline getirmek.

**Tamamlanma ölçütü:** Aynı kullanıcı aynı blog için tekrarlı favori oluşturamaz; engellenmiş kullanıcı içerik etkileşimi yapamaz. E-posta servisi kapalıyken mesaj veritabanına kaydedilir ve gönderim işi izlenebilir şekilde yeniden denenir.

### Hafta 6 — Görsel, dil ve raporlama

**İşler**
- Görselleri boyutlandırıp WebP'ye dönüştürmek; hatalı dosya ve aşırı büyük görselleri reddetmek.
- Depolama arayüzü ve yerel sağlayıcı; seçilecek bulut servisi için sağlayıcı, bağlantı ayarları ve erişim kapsamı.
- Türkçe/İngilizce arayüz, doğrulama mesajları, tarih ve sayı biçimleri; dil tercihini saklamak.
- Dinamik blog içeriğinin çevirisini arayüz çevirisinden ayrı ele almak; çevrilmiş alanlar ve eksik çeviri davranışı belirlemek.
- İletişim, kullanıcı ve istatistik raporlarında filtreleme; yetkili PDF/Excel dışa aktarımı.

**Tamamlanma ölçütü:** Görsel doğru görüntülenir ve dosya boyutları örneklerle karşılaştırılır. Dil değişiminde arayüz tutarlı çalışır. Rapor yalnızca izin verilen alanları ve seçilen kayıtları içerir; Türkçe karakterler doğru görünür.

**Karar noktası:** Bulut servisi ve rapor kütüphaneleri seçilirken kullanım şartları ve maliyet değerlendirilecek. Bağlantı sağlanamazsa bulut entegrasyonu tamamlanmış sayılmayacak.

### Hafta 7 — API ve dağıtım

**İşler**
- `/api/v1` altında blog, kategori, hizmet ve gerekli üye işlemleri; mevcut iş kurallarını yeniden kullanmak.
- API kimlik doğrulama yöntemi, rol/sahiplik denetimi, sayfalama, standart hata yanıtları ve istek sınırları.
- OpenAPI/Swagger sözleşmesi ve örnek yanıtlar; üretimde dokümantasyon erişimini sınırlandırmak.
- Docker/compose: gerçek uygulama portu, SQL Server/Redis kalıcı volume'leri, sağlık kontrolleri ve hazır olma beklemeleri.
- Üretim migration sürecini kontrollü dağıtım adımı yapmak; örnek veri ile üretim verisini ayırmak.
- CI içinde build, test ve kapsam raporu; ortam değişkenleriyle kurulum dokümanı.

**Tamamlanma ölçütü:** Temiz bir ortamda belgelenmiş adımlarla sistem açılır; yeniden başlatmada veriler korunur. API'de anonim okuma ve yetkili yazma sınırları test edilir. Hatalı API çağrıları HTML hata sayfasına dönüşmez.

### Hafta 8 — Kabul testleri ve staj teslimi

**İşler**
- Tüm rollerle uçtan uca senaryolar: giriş, içerik üretimi/onayı, yorum, bildirim, silme/geri alma ve rapor.
- SQL Server üzerinde ilişkiler, sorgu filtreleri ve migration entegrasyon testleri.
- Kapsama ölçümü; BusinessLayer ve yetki kurallarında en az %70 satır kapsamını teslim kapısı yapmak. Kapsam dışı alanları raporda belirtmek.
- Hedeflenen performans senaryolarını ölçmek; hata loglarında kişisel veri/parola bulunmadığını kontrol etmek.
- Yedek alma ve geri yükleme denemesi; mobil ekran ve erişilebilirlik kontrolleri.
- Şartname maddelerini test ve ekran kanıtlarına bağlamak; kurulum, kullanıcı kılavuzu, mimari açıklaması ve demo akışı hazırlamak.

**Tamamlanma ölçütü:** Kritik erişim ve veri kaybı hatası açık kalmaz; CI başarılıdır; kapsam raporu ve kabul testleri teslim edilir. Özellikler yalnızca dosyaları bulunduğu için tamamlandı olarak işaretlenmez.

### Hafta 9–10 — Düzeltme payı

Entegrasyon hataları, bulut bağlantısı, migration/dağıtım sorunları ve kabul testi bulguları için ayrılır. Yeni özellik eklemek yerine mevcut kapsamın doğrulanması önceliklidir.

## 4. İlk beş çalışma görevi

1. Başlangıç build/test sonuçlarını ve yönetim işlem listesini kaydet.
2. Ekran–rol–işlem matrisi oluştur ve merkezi policy'leri uygula.
3. Yazar sahipliği kontrolünü ve düzenleme sırasında yazarın korunmasını tamamla.
4. SignalR erişimini kapatılacak açık erişimler açısından düzelt; bildirimleri yetkili alıcılara sınırla.
5. Audit kullanıcı alanlarını doldur; soft delete sırasında görsel kaybını önle.

Her görev için çıktı: uygulanmış değişiklik, ilgili kabul testi ve kısa doğrulama kaydı. Testler son haftaya bırakılmayacak.

## 5. Süre altı haftayla sınırlıysa

Tam şartname kapsamı için altı hafta taahhüt edilmez. Öncelik: erişim güvenliği, audit/geri alma, yayın akışı, blog CQRS, Redis, güvenli bildirim ve temel yorum moderasyonu. Bu kapsamın testleri, kurulum ve demo belgeleri de aynı süreye dahildir.

Hangfire, gelişmiş üye etkileşimleri, bulut depolama, içerik çevirileri ve kapsamlı raporlama ikinci teslim paketine ayrılır. Bunlar şartnameden çıkarılmaz; ertelenen maddeler olarak açıkça gösterilir. API ve Docker kapsamı kalan kapasiteye göre ayrıca netleştirilir.

## 6. Teslim kontrolü

- Her şartname maddesi: mevcut durum, yapılan iş, kabul senaryosu ve kanıt.
- Altı erişim profiliyle izin ve ret testleri; anonim Visitor için hesap gerekmez.
- Veri kaybı olmadan migration, silme ve geri yükleme.
- Redis, SignalR ve arka plan işlerinde hata/kesinti davranışı.
- %70 hedefi için ölçülmüş ve kapsamı tanımlanmış rapor.
- Temiz ortam kurulumu, çalışır demo ve staj gelişim raporu.

Bu doküman geliştirme planıdır. Uygulama kodu bu çalışma sırasında değiştirilmemiştir.
