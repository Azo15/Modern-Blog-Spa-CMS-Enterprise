# Modern Blog / Spa Sitesi — Yerel Çalıştırma

## Gereksinimler

- Windows, .NET 9 SDK ve SQL Server Express LocalDB.
- İlk çalıştırmada NuGet paketlerini indirmek için internet bağlantısı.
- Node.js veya npm kurulumu gerekmez.

## Başlatma

Proje klasöründe PowerShell açıp çalıştırın:

```powershell
powershell -ExecutionPolicy Bypass -File .\Baslat.ps1
```

Bağlantıyı önce User Secrets ile tanımlayın:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\MSSQLLocalDB;Database=xspa_db;Integrated Security=True;TrustServerCertificate=True;" --project WebUI
```

Alternatif: `ConnectionStrings__DefaultConnection` ortam değişkeni. Bağlantı bilgileri depodaki JSON dosyalarında tutulmaz.

Dosya LocalDB'yi başlatır ve uygulamayı Development ortamında yalnızca localhost üzerinden HTTP ile çalıştırır. Sertifika onayı gerekmez. İlk çalıştırmada paket indirme ve veritabanı hazırlığı biraz sürebilir. HTTPS yönlendirmesi ve zorunlu güvenli çerezler Production ortamında korunur.

- Site: http://localhost:5157
- Yönetici girişi: http://localhost:5157/dashboard/giris
- Durdurma: terminalde Ctrl+C.

Yukarıdaki yerel User Secret `(localdb)\MSSQLLocalDB` sunucusundaki `xspa_db` veritabanını kullanır. Development başlangıcında bekleyen EF Core migration'ları uygulanır; ardından mevcut örnek veri yükleyicisi çalışır. Var olan veritabanı silinmez. Boş tablolara örnek içerik eklenir.

Yalnızca Development ortamında `admin@spa.com`, `editor@spa.com`, `author@spa.com`, `moderator@spa.com` ve `user@spa.com` hesapları oluşturulur. Şifreleri `P@ssword123!`, e-posta onayları aktiftir. Bu beş örnek hesabın şifresi ve rolü her geliştirme başlangıcında standart hale getirilir; başka hesapların şifresi değiştirilmez. Visitor için hesap oluşturulmaz. Production ortamında örnek kullanıcı/içerik eklenmez.

Visual Studio ile çalıştırmak için `WebUI.sln` dosyasını açın, WebUI'yi başlangıç projesi seçin ve **Development** profilini kullanın. Production bağlantısı `ConnectionStrings__DefaultConnection` ortam değişkeninden alınır.

## Kontrol ve kapsam

```powershell
dotnet build WebUI.sln
```

Derleme başarılıdır; mevcut kodda nullable ve paket güvenlik uyarıları bulunur. Örnek verilerde belirtilen bazı görseller depoda bulunmayabilir. E-posta ve reCAPTCHA gibi dış servisler kendi geçerli ayarlarına ihtiyaç duyar; yerel başlatma bunları yapılandırmaz.

Yetkilendirme, migration ve test ayrıntıları: [uygulama raporu](docs/YETKILENDIRME_VE_WORKFLOW.md).

## Aşama 2

Blog işlemleri MediatR ve FluentValidation pipeline üzerinden yürür. Halka açık bloglar salt-okunur DTO ve veritabanı sayfalaması kullanır. `ConnectionStrings:Redis` tanımlıysa Redis, yoksa bellek cache kullanılır; bağlantı kesintisinde bellek fallback devreye girer.

Yorumlar üyeler tarafından oluşturulur, System Admin/Moderator onayıyla görünür. Ana yorum ve yanıt olmak üzere iki seviye desteklenir. Moderasyon paneli: `/AdminComment/Index`.

Dosyalar, migration ve testler: [Aşama 2 uygulama raporu](docs/ASAMA_2_UYGULAMA_RAPORU.md).
