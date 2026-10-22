# Prompt 5 doğrulama kaydı

18 Ekim 2026: .NET 9 çözümü `dotnet build --no-restore` ile doğrulandı: **0 hata, 0 uyarı**.

Rol sadeleşmesinden sonra tekrar eden teori girdileri kaldırıldı. Razor menü kapsamı düzeltildi; girişte yalnızca yerel ReturnUrl kabul edilir. Güncel ayrıntılı çıktı `.artifacts/prompt5-build.log` dosyasındadır.

Son doğrulama:

- .NET: 396 test başarılı, başarısız veya atlanan test yok.
- JavaScript: 25 test başarılı.
- BusinessLayer kapsamı: %87,59 (1334/1523 satır).
- Taze derleme: 0 hata, 0 uyarı (`--artifacts-path .artifacts/prompt5-final-build`).
- Eski rol matrisinin artık geçerli olmayan yazar/moderatör beklentileri üç role göre güncellendi; müşteri erişim engelleri, özgün yazarın korunması, soft-delete, audit ve yayın filtreleri korunarak test edildi.
- Yeni testler: üretim kullanıcılarını çoğaltmadan eski rollerin taşınması, telefon/KVKK/parola doğrulaması, kayıt sırasında rol yükseltmenin engellenmesi, personelin rol atayamaması, Google doğrulanmamış e-postasının reddi, Google doğrulanmış e-postası ile mevcut hesaba otomatik bağlantı, rolün korunması ve pasif/kilitli/doğrulanmamış hesaplarda bağlantının reddi, Google müşteri kaydı ve güvenli ReturnUrl, AI yetki/şema/HTML temizliği/hata durumları, menü akordeonu.
- Google callback testleri korunan harici oturum test biletleriyle çalışır; gerçek Google hesabına veya dış AI servisine bağlanmaz.
- Tarayıcı: yeni sekmeli giriş/kayıt, Google yapılandırma hatası, personel girişinin işletme özetine gitmesi, tek açık menü grubu, personelden rol bağlantısının gizlenmesi, AI modalı ve eksik AI ayarı geri bildirimi doğrulandı.
- Eski admin Bootstrap paketi yerel standart Bootstrap bundle ile değiştirildi; TinyMCE yerel asset yolu ve `.min` soneki düzeltildi. Böylece modal ve zengin metin editörü birlikte açılır.

İstenen 18. commit mesajındaki `374` sayısı aynen korunmuştur; gerçek güncel test sayıları yukarıdadır.
