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

Dosya LocalDB'yi başlatır ve uygulamayı Development ortamında yalnızca localhost üzerinden HTTP ile çalıştırır. Sertifika onayı gerekmez. İlk çalıştırmada paket indirme ve veritabanı hazırlığı biraz sürebilir. HTTPS yönlendirmesi ve zorunlu güvenli çerezler Production ortamında korunur.

- Site: http://localhost:5157
- Yönetici girişi: http://localhost:5157/dashboard/giris
- Durdurma: terminalde Ctrl+C.

Development ayarları `(localdb)\MSSQLLocalDB` sunucusundaki `xspa_db` veritabanını kullanır. Development başlangıcında bekleyen EF Core migration'ları uygulanır; ardından mevcut örnek veri yükleyicisi çalışır. Var olan veritabanı silinmez. Boş tablolara örnek içerik eklenir.

Örnek veri yükleyicisinin oluşturduğu yerel hesap: `admin@spa.com` / `Admin123!`. Hesap önceden mevcutsa mevcut şifresi korunur.

Visual Studio ile çalıştırmak için `WebUI.sln` dosyasını açın, WebUI'yi başlangıç projesi seçin ve **Development** profilini kullanın. Production profili farklı bir veritabanı bağlantısı kullanır.

## Kontrol ve kapsam

```powershell
dotnet build WebUI.sln
```

Derleme başarılıdır; mevcut kodda nullable ve paket güvenlik uyarıları bulunur. Örnek verilerde belirtilen bazı görseller depoda bulunmayabilir. E-posta ve reCAPTCHA gibi dış servisler kendi geçerli ayarlarına ihtiyaç duyar; yerel başlatma bunları yapılandırmaz.
