using System.Text.Json;

namespace WebUI.Services;

public sealed class ReCaptchaVerifier(HttpClient client, IConfiguration configuration,
    ILogger<ReCaptchaVerifier> logger) : IReCaptchaVerifier
{
    public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default)
    {
        var secret = configuration["GoogleReCaptcha:SecretKey"];
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(secret)) return false;
        try
        {
            using var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = secret, ["response"] = token, ["remoteip"] = remoteIp ?? string.Empty
            });
            using var response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", body, cancellationToken);
            if (!response.IsSuccessStatusCode) return false;
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!json.RootElement.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.True)
                return false;
            var expectedHost = configuration["GoogleReCaptcha:ExpectedHostname"];
            return string.IsNullOrWhiteSpace(expectedHost) ||
                json.RootElement.TryGetProperty("hostname", out var hostname) &&
                string.Equals(hostname.GetString(), expectedHost, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogWarning("reCAPTCHA verification failed: {FailureType}", ex.GetType().Name);
            return false;
        }
    }
}
