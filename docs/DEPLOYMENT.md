# Docker ile dağıtım

Bu dağıtım .NET 9, Linux x64, SQL Server 2022 ve Redis 7.4 kullanır. SQL Server Linux imajı için x64 işlemci ve yeterli Docker/WSL belleği gerekir. Windows'ta Docker Desktop'ın Linux container motoru çalışıyor olmalıdır. SQL Server Developer lisansı yalnızca geliştirme/demo içindir; gerçek canlı ortamda uygun lisanslı sürüm seçilmelidir.

## İlk kurulum

Proje kökünde `.env.example` dosyasını `.env` olarak kopyalayın. SA, migrator ve uygulama için **birbirinden farklı** rastgele SQL parolaları ile en az 32 baytlık JWT anahtarı girin. SQL parolaları 16–128 karakter olmalı ve büyük/küçük harf, sayı ve sembol içermelidir; çift tırnak, noktalı virgül ve satır sonu kullanmayın. `.env` Git'e ve Docker build context'ine dahil edilmez.

İlk yönetici için `BOOTSTRAP_ADMIN_EMAIL` ve güçlü `BOOTSTRAP_ADMIN_PASSWORD` değerlerini doldurun. Parola en az 14 karakter ve büyük/küçük harf, sayı ve sembol içermelidir. Bu değerler yalnızca yönetici bulunmayan yeni veritabanında kullanılır; var olan hesaplar yükseltilmez ve yönetici parolası tekrar yazılmaz. Production ortamında `admin@spa.com` ve diğer Development test hesapları oluşturulmaz.

```powershell
Copy-Item .env.example .env
# .env içindeki boş, zorunlu değerleri doldurun.
docker compose config --quiet
docker compose up -d --build
```

Modern Compose için `docker compose`; eski uyumlu kurulumlarda `docker-compose` komutunu kullanabilirsiniz. İlk imaj oluşturulduktan sonra başlatma komutu `docker compose up -d` olur.

Yerel demo adresi `http://localhost:5158` olur. Mevcut Visual Studio/yerel uygulamanın 5157 portuyla çakışmaz. `.env` içindeki `SWAGGER_ENABLED=true` seçeneği Swagger arayüzünü açar. Production varsayılanında Swagger kapalıdır. JWT kullanmak için kendi oluşturduğunuz ilk yöneticinin kimlik bilgileriyle `/api/v1/auth/login` çağrısı yapın. Başlangıç veritabanı boştur; içerikleri panel/API üzerinden oluşturun.

## Başlangıç sırası ve veritabanı yetkileri

Compose üç kalıcı temel servisi (`mssql`, `redis`, `webui`) ve tek sefer çalışan `migrate` yardımcı servisini tanımlar. SQL `SELECT 1`, Redis `PING` healthcheck'leri başarılı olmadan bağımlı servisler başlamaz. `webui`, ayrıca `migrate` işleminin başarıyla tamamlanmasını bekler. Migration hatasında web uygulaması çalıştırılmaz.

`migrate`, uygulama imajını `--initialize-database` ile çalıştırır. `DeploymentDatabaseInitializer` şu işlemleri sırayla yapar:

1. Kısa ömürlü SA bağlantısıyla `xspa_db`, `modern_blog_migrator` ve `modern_blog_app` login'lerini hazırlar. Parolalar SQL parametreleri ve `QUOTENAME` ile işlenir; SQL metnine ham olarak eklenmez.
2. Migrator hesabıyla EF Core migration'larını uygular ve Hangfire SQL şemasını hazırlar.
3. Uygulama hesabına veri okuma/yazma ve Hangfire şemasında çalıştırma yetkisini verir. Uygulama hesabına `db_owner` veya `db_ddladmin` verilmez.
4. Identity rollerini ve gerekiyorsa kullanıcı tarafından tanımlanan ilk yöneticiyi oluşturur.

`webui` yalnızca `modern_blog_app` bağlantısını alır. SA/migrator parolaları uzun süre çalışan uygulama container'ına aktarılmaz. Production normal başlangıcında EF migration veya Hangfire DDL kurulumu yapılmaz. Deployment kimlik bilgileri ve container environment değerleri Docker yöneticilerine açıktır; gerçek canlı dağıtımda secret manager/Docker secrets ile korunan dağıtım mekanizmasına taşınmalıdır.

Production bağımsız veritabanı kullanılıyorsa bootstrap bağlantısını vermeden, migration yetkili `ConnectionStrings__DefaultConnection` ile `dotnet WebUI.dll --initialize-database` çalıştırılabilir. Normal web servisinin bağlantısı ayrı, sınırlı yetkili hesabı kullanmalıdır. Harici Hangfire veritabanı seçilirse onun runtime hesabının izinlerini DBA ayrıca hazırlamalıdır.

## Kalıcılık ve güncelleme

`sql_data` veritabanını, `redis_data` Redis AOF verisini, `app_logs` günlükleri, `app_uploads` blog/avatarlara ait WebP dosyalarını, `data_protection_keys` Cookie/antiforgery şifreleme anahtarlarını korur. Yeniden imaj oluşturmak veya container değiştirmek bu volume'ları silmez. Soft-delete edilen blog görselleri upload volume'unda korunur.

Güncellemeden önce SQL veritabanının ve upload/Data Protection volume'larının yedeğini alın. Var olan çalışır kurulumda yeni migration'ı kontrollü uygulamak için:

```powershell
docker compose build
docker compose run --rm migrate
docker compose up -d webui
docker compose ps
```

`docker compose down` container'ları durdurur; kalıcı volume'ları korur. **`docker compose down -v` veritabanını ve dosyaları siler**; normal güncellemede kullanılmaz. İlk oluşturulan Data Protection anahtarlarının volume sahipliği .NET imajındaki root olmayan `app` kullanıcıya aittir; harici mevcut volume taşındığında sahiplik/izinler ayrıca korunmalıdır.

## HTTP demo ve HTTPS canlı dağıtım

Compose tüm host portlarını `127.0.0.1` üzerine bağlar. `LOCAL_HTTP=true` yalnızca yerel HTTP demosu için açık bir istisnadır; Cookie'nin yerel HTTP'de çalışmasını ve HTTPS yönlendirmesinin localhost üzerinde başarısız olmamasını sağlar. Gerçek canlı ortamda `LOCAL_HTTP=false` kullanın. TLS'i reverse proxy/load balancer üzerinde sonlandırın; uygulama yalnızca tanımlı güvenilir proxy adreslerinden gelen forwarded header'ları kabul etmelidir. Web servisinin container portu 8080'dir. SQL/Redis host portlarını gerçek dağıtımda kaldırın veya yönetim ağı/firewall üzerinden sınırlandırın.

HTTPS yapılandırıldığında güvenli Cookie, HSTS ve HTTPS yönlendirmesi geçerli olur. Proxy ve public hostname yapılandırmasına göre `AllowedHosts` ile güvenilir proxy allowlist'ini de sınırlandırın. `/health/ready` yalnızca sade durum bilgisi döndürür; SQL bağlantısı başarısızsa 503 üretir. Redis kesintisi uygulamanın güvenli in-memory fallback davranışıyla ele alınır.

## Ortam değişkenleri

`.env.example` Compose'un tüm kullanıcı tarafından ayarlanabilir değişkenlerini listeler:

- Zorunlu: `MSSQL_SA_PASSWORD`, `MSSQL_MIGRATOR_PASSWORD`, `MSSQL_APP_PASSWORD`, `JWT_SIGNING_KEY`.
- JWT: `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_ACCESS_TOKEN_MINUTES`.
- İlk yönetici: `BOOTSTRAP_ADMIN_EMAIL`, `BOOTSTRAP_ADMIN_PASSWORD` birlikte verilir veya birlikte boş bırakılır.
- Portlar ve sürüm: `WEBUI_HOST_PORT`, `MSSQL_HOST_PORT`, `REDIS_HOST_PORT`, `MSSQL_PID`.
- Demo/dokümantasyon: `LOCAL_HTTP`, `SWAGGER_ENABLED`.
- SMTP: `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_SENDER_EMAIL`, `SMTP_RECIPIENT_EMAIL`.
- CAPTCHA: `RECAPTCHA_SITE_KEY`, `RECAPTCHA_SECRET_KEY`, `RECAPTCHA_HOSTNAME`.

Compose bunları .NET'in `__` ayraçlı ayarlarına çevirir. Redis adresi servis adıyla `redis:6379`; EF/Hangfire bağlantıları Compose ağı içinde `mssql,1433` olur. SMTP yapılandırılmazsa iletişim mesajı kalıcı kaydedilir; gönderim işleri retry sonunda Hangfire Failed durumunda görünür. CAPTCHA ayarları eksikse public iletişim formu güvenli şekilde reddedilir.

## Kontrol ve sorun giderme

```powershell
docker compose config --quiet
docker compose ps
docker compose logs --tail 100 migrate
docker compose logs --tail 100 webui
curl.exe --fail http://localhost:5158/health/ready
```

`docker compose config` seçeneği `--quiet` olmadan environment içinde parolaları gösterebilir; çıktısını paylaşmayın. SQL healthcheck Docker SQL Server 2022 imajındaki `/opt/mssql-tools18/bin/sqlcmd` yolunu kullanır. İlk açılışta SQL Server'ın başlaması biraz sürebilir; sağlık kontrolü buna süre tanır. Migration çıkış kodu sıfır değilse önce ilgili güvenli log kaydını inceleyin. Veritabanı yedeği olmadan düşürme/reset işlemi yapılmaz.

Dağıtım doğrulama kanıtları Aşama 4 raporunda bulunur. Docker daemon kullanılabilir değilse yalnızca Compose yapılandırması ve Linux publish doğrulanmış sayılır; bunlar gerçek imaj build/container smoke testinin yerine geçmez.

## Resmî referanslar

- [Microsoft .NET container imajları ve çok aşamalı derleme](https://learn.microsoft.com/en-us/dotnet/core/docker/build-container)
- [SQL Server Linux container kurulumu](https://learn.microsoft.com/en-us/sql/linux/install-upgrade/quickstart-install-docker?view=sql-server-ver17)
- [Docker Compose healthcheck ve başlangıç sırası](https://docs.docker.com/compose/how-tos/startup-order/)
