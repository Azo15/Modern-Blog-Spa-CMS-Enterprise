using System.ComponentModel.DataAnnotations;
using BusinessLayer.Concrete;
using Microsoft.AspNetCore.DataProtection;
using WebUI.Models;
namespace BusinessLayer.Tests;
public sealed class SmtpSettingsTests
{
    [Fact]
    public void PasswordIsEncryptedAndRoundTripsWithMatchingKeyRing()
    {
        var protection = new EphemeralDataProtectionProvider();
        var encrypted = SmtpSecret.Protect(protection, "smtp-test-password");
        Assert.DoesNotContain("smtp-test-password", encrypted);
        Assert.Equal("smtp-test-password", SmtpSecret.Read(protection, encrypted));
        Assert.Throws<System.Security.Cryptography.CryptographicException>(() => SmtpSecret.Read(new EphemeralDataProtectionProvider(), encrypted));
    }
    [Theory]
    [InlineData(25, true)] [InlineData(587, false)] [InlineData(465, false)]
    public void InsecureSmtpSettingsAreRejected(int port, bool ssl)
    {
        var model = new SmtpSettingsForm { Host="smtp.example.test",Username="user",Mail="sender@example.test",Port=port,EnableSsl=ssl };
        Assert.False(Validator.TryValidateObject(model,new ValidationContext(model),new List<ValidationResult>(),true));
    }
}
