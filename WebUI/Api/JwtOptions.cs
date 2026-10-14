namespace WebUI.Api;

/// <summary>JWT settings supplied through environment variables or local User Secrets.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "modern-blog-platform";
    public string Audience { get; set; } = "modern-blog-clients";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 30;
}

public static class ApiAuthentication
{
    public const string HybridScheme = "PlatformCookieOrBearer";
    public const string BearerIdentityType = "PlatformBearer";
    public const string SecurityVersionClaim = "security_version";
}
