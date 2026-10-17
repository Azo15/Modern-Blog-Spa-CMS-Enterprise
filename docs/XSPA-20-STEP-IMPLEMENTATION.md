# XSPA — 20 Adımlık Uygulama Raporu

18 Ekim 2026. Her adım kullanıcının belirttiği mesajla ayrı commit olarak `github/master` deposuna gönderilir. `origin` başka projenin kaynak deposudur; push hedefi değildir.

## Tamamlanan adımlar

1. `LegalController`: anonim, sabit Türkçe rotalar; TR/EN başlık ve meta açıklaması.
2. `Views/Legal/Privacy.cshtml`: veri akışı, güvenlik, tarayıcı tercihleri ve dış hizmetler.
3. `Views/Legal/Terms.cshtml`: ön talep/onay ayrımı, değişiklik ve tesis kullanımı.
4. `Views/Legal/Kvkk.cshtml`: toplama, amaç, hukuki sebepler, alıcı grupları ve başvuru hakları.
5. Footer: üç gerçek yasal bağlantı ve klavye/hover vurgusu.
6. `SiteTable.SmtpUsername`, `EnableSsl`, doğrulamalı `SmtpSettingsForm` ve migration.
7. `AdminSettingsController`: yalnızca RequireAdmin, CSRF, Data Protection ile şifreli parola; eski site ayarları SMTP alanlarını değiştiremez. İki posta servisi korunan parolayı çözer.
8. `Views/AdminSettings/Smtp.cshtml`: koyu yönetim arayüzü, TLS 465/587, boş bırakıldığında parolayı koruma, menü bağlantısı.
9. Test endpoint: yalnızca oturumdaki doğrulanmış yönetici adresi; CSRF, bir dakikalık süreç içi hız sınırı, güvenli hata mesajı ve AJAX geri bildirimi.
10. Talep ve onay için ayrı dayanıklı e-posta kayıtları; `(AppointmentId, IsConfirmation)` benzersizliği. Onay ve e-posta niyeti aynı işlemde saklanır. Kuyruk kesintisinde mevcut kurtarma işi devreye girer. İptal edilen randevuya kuyruktaki eski onay gönderilmez. Altın XSPA başlıklı, HTML encode uygulanmış şablon.
11. `GetAdminDashboardQuery`, DTO ve yetki kontrollü handler; ince `AdminDashboardController`. Türkiye saatine göre sayılar, iptal/silinmiş kayıt filtreleri, e-posta bazında farklı müşteriler ve güncel tarifeden tahmini aylık bedel.
12. Bekleyen randevulara bağlantılı dinamik uyarı.
13. Dört duyarlı kart; günlük randevuda gerçek dünkü değerle fark. Diğer kartlarda uydurma yüzde yerine gerçek kuyruk/aktif durum.
14. Yerel, bütünlüğü doğrulanmış Chart.js UMD ve lisans; 14 gün, sıfır kayıtlı günler dahil çizgi grafiği ve erişilebilir veri alternatifi.
15. En popüler sekiz hizmetin halka grafiği; boş veri açıklaması.
16. Son sekiz randevu, Türkçe durum rozetleri ve CSRF korumalı onay/iptal aksiyonları.
17. SMTP ayar durumu, bekleyen talepler, okunmamış mesajlar. `ContactForm.IsRead`, okundu işaretleme POST aksiyonu ve migration. SMTP yapılandırılmış olması teslimat testi olarak sunulmaz.
18. Duyuru bandı, localStorage ile kapatma tercihi ve yüksekliğe duyarlı sabit başlık boşluğu. Doğrulanmamış indirim kampanyası yerine gerçek çalışma saatleri kullanıldı.
19. Hakkımızda sayfasında tekrar kullanılabilir TR/EN SSS; rezervasyon, hazırlık ve hijyen soruları.
20. İsteğe bağlı GiftCode; HTML ve FluentValidation kuralları, normalize edilerek saklama, replay kontrolü ve yönetim ekranında görünüm. Kod otomatik indirim sağlamaz. Son doğrulamada mevcut nullable sözleşmeleri, DTO başlangıç değerleri, giriş denetimleri ve eski migration sınıf adı uyarıları gerçek kaynak düzeltmeleriyle giderildi.

## Migration ve çalıştırma

Yeni migrationlar:

- `20261017223122_AddSecureSmtpSettings`
- `20261017223357_AppointmentConfirmationOutbox`
- `20261017223453_ContactMessageReadState`
- `20261017223738_AppointmentVipGiftCode`

Development açılışında mevcut migration akışı yerel veritabanını günceller. Diğer ortamlarda yedekleme ve dağıtım sürecine göre:

```powershell
dotnet ef database update --project DataAccesLayer --startup-project WebUI
```

SMTP yönetimi `/AdminSettings/Smtp`; işletme özeti `/AdminDashboard`.
Ortam değişkenleri kayıtlı ayarların önüne geçer: `Smtp__Host`, `Smtp__Port`, `Smtp__Username`, `Smtp__Password`, `Smtp__SenderEmail`, iletişim alıcısı için isteğe bağlı `Smtp__RecipientEmail`. Secret değerlerini kaynak koduna veya rapora yazmayın. Üretimde Data Protection anahtarlarını kalıcı ve örnekler arasında ortak tutun; aksi halde eski şifreli parolalar çözülemez. Test endpoint hız sınırı süreç içidir; çok örnekli dağıtımda ortak gateway sınırı kullanılabilir.

## Doğrulama

- Çözüm derlemesi: 0 hata, 0 uyarı; uyarılar gizlenmedi.
- xUnit: 392 senaryo; mevcut yetki, CQRS, dosya ve etkileşim testlerine ek olarak kod saklama/ret/replay, onay idempotency, iptal sonrası onay engeli, parola şifreleme, güvensiz SMTP ayarı reddi, yasal rotalar, yönetici yetkileri, CSRF ve dashboard hesapları.
- JavaScript: 21 test; görünürlük, saat durumu ve geçişler.
- Kalite kapısı: BusinessLayer kapsamı en az %70; ayrıntılı güncel ölçüm `.artifacts/final20-quality.log` ve taze coverage raporunda.
- Tarayıcı: yönetici girişi, gerçek verili dashboard/grafikler, SMTP formu, SSS açılışı, 12 hizmetli modal, isteğe bağlı kodun geçersiz formatı reddetmesi, 390px mobil yasal bağlantılar ve taşma kontrolü. Görseller `.artifacts/final20/` altında.

## İşletmenin tamamlaması gereken bilgiler

SMTP kimlik bilgileri ortamda tanımlı değildir; gerçek teslimat yapılmış gibi raporlanmadı. Ayarlar girildikten sonra yönetici kendi adresine test gönderimini başlatabilir. Otomatik testler gerçek dış alıcılara posta göndermez.

KVKK metninde veri sorumlusunun gerçek ticari unvanı sağlanmadığı için uydurulmadı. İşletme yasal kimliğini, kesin saklama sürelerini ve kullanılan sağlayıcılara göre aktarım şartlarını yayın öncesinde netleştirmelidir. Sabit iptal cezası veya süre taahhüdü icat edilmedi.

Tahmini bedel bir rezervasyon başına güncel hizmet tarifesidir; kişi sayısıyla çarpılmaz, tahsilat veya sabitlenmiş tarihsel satış fiyatı değildir. SMTP ve veritabanı tek atomik işlem olmadığından, gönderimden hemen sonra süreç çökerse sağlayıcı tarafında Message-Id ile tekilleştirme hâlâ önemlidir.

## Resmî başvuru kaynakları

- [KVKK aydınlatma yükümlülüğü](https://www.kvkk.gov.tr/Icerik/2033/Aydinlatma-Yukumlulugu-)
- [Chart.js kurulum ve dağıtım](https://www.chartjs.org/docs/latest/getting-started/installation.html)

Commit kanıtı: son doğrulamada `git log --oneline -20` çıktısı `.artifacts/final20/git-log20.txt` dosyasına kaydedilir.
