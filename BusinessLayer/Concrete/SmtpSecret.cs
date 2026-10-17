using Microsoft.AspNetCore.DataProtection;
namespace BusinessLayer.Concrete;
public static class SmtpSecret
{
    private const string Prefix = "xspa:protected:v1:";
    public static string Protect(IDataProtectionProvider provider, string value) => Prefix + provider.CreateProtector("XSPA.SMTP.Password.v1").Protect(value);
    public static string? Read(IDataProtectionProvider? provider, string? value)
    {
        if (value?.StartsWith(Prefix, StringComparison.Ordinal) != true) return value;
        if (provider is null) throw new InvalidOperationException("SMTP parola koruma servisi bulunamadı.");
        return provider.CreateProtector("XSPA.SMTP.Password.v1").Unprotect(value[Prefix.Length..]);
    }
}
