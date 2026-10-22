using Microsoft.Extensions.DependencyInjection;
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
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void HostedKeysDefaultToPrivateSiblingDirectory(string environment)
    {
        var root = Path.Combine(Path.GetTempPath(), "site99770", "wwwroot");
        Assert.Equal(Path.GetFullPath(Path.Combine(root, "..", ".xspa-data-protection")), WebUI.Infrastructure.PersistentProtectionKeys.ResolvePath(root, environment, null));
        Assert.Throws<InvalidOperationException>(() => WebUI.Infrastructure.PersistentProtectionKeys.ResolvePath(root, environment, "keys"));
        Assert.Throws<InvalidOperationException>(() => WebUI.Infrastructure.PersistentProtectionKeys.ResolvePath(root, environment, Path.Combine(root, "wwwroot", "keys")));
        Assert.Null(WebUI.Infrastructure.PersistentProtectionKeys.ResolvePath(root, "Testing", null));
    }
    [Fact]
    public void SavedSmtpPasswordSurvivesRecreationOfApplicationServices()
    {
        var folder = Path.Combine(Path.GetTempPath(), "xspa-key-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var environment = new Moq.Mock<Microsoft.Extensions.Hosting.IHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns(Path.Combine(folder, "wwwroot"));
        environment.SetupGet(x => x.EnvironmentName).Returns("Production");
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var keys = WebUI.Infrastructure.PersistentProtectionKeys.ResolvePath(environment.Object.ContentRootPath, "Production", null)!;
        try
        {
            string encrypted;
            var first = new Microsoft.Extensions.DependencyInjection.ServiceCollection(); first.AddLogging();
            WebUI.Infrastructure.PersistentProtectionKeys.AddPersistentProtection(first, environment.Object, config);
            using (var provider = first.BuildServiceProvider()) encrypted = SmtpSecret.Protect(provider.GetRequiredService<IDataProtectionProvider>(), "test-only-smtp-password");
            var second = new Microsoft.Extensions.DependencyInjection.ServiceCollection(); second.AddLogging();
            WebUI.Infrastructure.PersistentProtectionKeys.AddPersistentProtection(second, environment.Object, config);
            using (var provider = second.BuildServiceProvider()) Assert.Equal("test-only-smtp-password", SmtpSecret.Read(provider.GetRequiredService<IDataProtectionProvider>(), encrypted));
            Assert.NotEmpty(Directory.GetFiles(keys, "key-*.xml"));
        }
        finally
        {
            if (Directory.Exists(keys)) { foreach (var file in Directory.GetFiles(keys)) File.Delete(file); Directory.Delete(keys); }
            Directory.Delete(folder);
        }
    }
    [Fact]
    public void LostKeyIsReportedWithoutExposingOrReplacingStoredSecret()
    {
        var encrypted = SmtpSecret.Protect(new EphemeralDataProtectionProvider(), "test-secret");
        Assert.False(SmtpSecret.CanRead(new EphemeralDataProtectionProvider(), encrypted));
        Assert.False(SmtpSecret.CanRead(null, null));
        Assert.True(SmtpSecret.CanRead(null, "legacy-test-password"));
    }
}
