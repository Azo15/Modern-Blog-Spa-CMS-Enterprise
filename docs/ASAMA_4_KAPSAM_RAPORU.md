# Aşama 4 test ve kapsam raporu

14 Ekim 2026, Release derlemesi, `coverlet.collector` 6.0.2.

- xUnit: **267 geçti, 0 başarısız, 0 atlanan**.
- BusinessLayer line coverage: **%88,665**, 1056 / 1191 üretim satırı.
- BusinessLayer branch coverage: **%77,435**, 477 / 616 dal.
- Zorunlu kalite eşiği: satır kapsamı ≥%70; sonuç başarılı.
- Ölçüm: bütün `[BusinessLayer]*` assembly; üretim sınıfı/dosya/attribute exclusions kullanılmadı. WebUI veya test assembly bu yüzdeye dahil değildir.
- Yerel uygulama: 38 canlı HTTP kontrolü başarılı.
- Docker Production uygulaması: 37 canlı HTTP kontrolü başarılı.
- Docker altyapısı: 19 kontrol başarılı; Redis outage/recovery, SQL yetkileri, non-root/volume izinleri, görsel koruma ve gerçek log maskelemesi dahil.

Kalite kapısını tekrar çalıştırma:

```powershell
pwsh -NoProfile -File scripts/Test-QualityGate.ps1 -MinimumCoverage 70
```

Bu komut yeni run ID altında TRX, Cobertura, JSON kapsam verisi ve summary üretir; test hatası veya eşik altı kapsam başarısız exit ile sonuçlanır. Önceki ölçüm yeniden kullanılmaz.

Son koşu: `.artifacts/quality/7115c4dc60ba4cfaa3a0b2622b8d3f9c`.

Teslimat kopyaları:

- `.artifacts/stage4-final/coverage.cobertura.xml`: araçlarla açılabilir satır/dal kapsamı.
- `.artifacts/stage4-final/coverage.json`: ayrıntılı coverlet verisi.
- `.artifacts/stage4-final/tests.trx`: test sonuçları.
- `.artifacts/stage4-final/coverage-summary.json`: kalite kapısı kararı.
- `.artifacts/stage4-final/acceptance-summary.json`: gerçek uygulama ve altyapı kontrollerinin özeti.

`ApiEndpointTests`, `ApiAuthTests`, `ApiPublicQueryTests` API sözleşmesi, sahiplik, altı profil, cookie CSRF, JWT iptali, rate limit ve RFC 7807 hatalarını doğrular. `LogPrivacyTests`, `LegacyImageSecurityTests`, `BackgroundRecoverySecurityTests` yeni güvenlik düzeltmelerini kapsar. Önceki workflow/yorum/cache/Hangfire/Skia/rapor/i18n/etkileşim testleri aynı koşuda çalıştırıldı.

Linux Docker build ve gerçek Linux container kabul kontrolleri yerelde tamamlandı. GitHub workflow hazırdır; uzak CI koşusunun geçtiği iddia edilmez. Kapsam oranı, bağımsız güvenlik denetimi veya tüm olası kullanım yollarının doğruluğu anlamına gelmez.
