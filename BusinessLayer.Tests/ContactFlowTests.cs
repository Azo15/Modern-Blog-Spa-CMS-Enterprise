using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using BusinessLayer;
using BusinessLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebUI.Controllers;
using WebUI.Hubs;
using WebUI.Models;
using WebUI.Services;

namespace BusinessLayer.Tests;

public sealed class ContactFlowTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly MemoryCache cache = new(new MemoryCacheOptions());
        public Mock<IContactSubmissionService> Submission { get; } = new();
        public Mock<IReCaptchaVerifier> Captcha { get; } = new();
        public Mock<IHubClients> Clients { get; } = new();
        public Mock<IClientProxy> Managers { get; } = new();
        public ContactController Controller { get; }

        public Fixture(string environment, bool siteKey = true, bool secretKey = true)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GoogleReCaptcha:SiteKey"] = siteKey ? "test-site" : null,
                ["GoogleReCaptcha:SecretKey"] = secretKey ? "test-secret" : null
            }).Build();
            var localizer = new Mock<IStringLocalizer<SharedResource>>();
            localizer.Setup(l => l[It.IsAny<string>()]).Returns((string key) => new LocalizedString(key, key));
            var hub = new Mock<IHubContext<NotificationHub>>();
            Clients.Setup(c => c.Group(NotificationHub.ModerationGroup)).Returns(Managers.Object);
            Managers.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            hub.Setup(h => h.Clients).Returns(Clients.Object);
            Submission.Setup(s => s.SubmitAsync(It.IsAny<ContactForm>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ContactSubmissionResult(1, true));
            var host = Mock.Of<IWebHostEnvironment>(e => e.EnvironmentName == environment);
            Controller = new ContactController(Submission.Object, Mock.Of<IContactService>(), Captcha.Object,
                NullLogger<ContactController>.Instance, configuration, cache, hub.Object, localizer.Object, host);
            var http = new DefaultHttpContext();
            http.Connection.RemoteIpAddress = IPAddress.Loopback;
            http.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            Controller.ControllerContext = new ControllerContext { HttpContext = http };
            Controller.TempData = new TempDataDictionary(http, Mock.Of<ITempDataProvider>());
        }
        public void Dispose() { Controller.Dispose(); cache.Dispose(); }
    }

    private static ContactPageViewModel Message() => new()
    {
        ContactForm = new ContactForm { FullName = "Test Visitor", Email = "visitor@example.test", Subject = "Appointment question", Message = "I would like more information." }
    };

    [Theory]
    [InlineData("Development", true, true)]
    [InlineData("Production", false, false)]
    [InlineData("Production", true, false)]
    [InlineData("Production", false, true)]
    public async Task AuthorizedCaptchaBypassPersistsAndNotifiesOnlyManagers(string environment, bool siteKey, bool secretKey)
    {
        using var fixture = new Fixture(environment, siteKey, secretKey);
        Assert.IsType<RedirectToActionResult>(await fixture.Controller.SendMessage(Message()));
        fixture.Submission.Verify(s => s.SubmitAsync(It.IsAny<ContactForm>(), It.IsAny<CancellationToken>()), Times.Once);
        fixture.Captcha.VerifyNoOtherCalls();
        fixture.Clients.Verify(c => c.Group(NotificationHub.ModerationGroup), Times.Once);
        fixture.Managers.Verify(p => p.SendCoreAsync("ReceiveNotification", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
        fixture.Clients.VerifyNoOtherCalls();
        Assert.Equal("success", fixture.Controller.TempData["Swal_Icon"]);
    }

    [Fact]
    public async Task ConfiguredProductionCaptchaFailureCannotSaveOrNotify()
    {
        using var fixture = new Fixture("Production");
        fixture.Captcha.Setup(c => c.VerifyAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Assert.IsType<ViewResult>(await fixture.Controller.SendMessage(Message()));
        fixture.Submission.VerifyNoOtherCalls(); fixture.Clients.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NotificationFailurePreservesSavedContactAndSuccessAcknowledgement()
    {
        using var fixture = new Fixture("Development");
        fixture.Managers.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("Disconnected"));
        Assert.IsType<RedirectToActionResult>(await fixture.Controller.SendMessage(Message()));
        Assert.Equal("success", fixture.Controller.TempData["Swal_Icon"]);
        fixture.Submission.Verify(s => s.SubmitAsync(It.IsAny<ContactForm>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NewsletterFailureNeverReturnsASuccessAcknowledgement()
    {
        using var fixture = new Fixture("Development");
        fixture.Controller.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
        fixture.Submission.Setup(s => s.SubmitAsync(It.IsAny<ContactForm>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("Database unavailable"));
        var response = Assert.IsType<ObjectResult>(await fixture.Controller.NewsletterInterest("reader@example.test"));
        Assert.Equal(503, response.StatusCode);
        Assert.False(JsonSerializer.SerializeToElement(response.Value).GetProperty("success").GetBoolean());
        fixture.Clients.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ContactFormWithoutCaptchaKeysPersistsMessageAndDeliveryIntent()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
        var page = WebUtility.HtmlDecode(await client.GetStringAsync("/iletisim"));
        Assert.Contains("Bize bir not bırakın.", page);
        var contactForm = Regex.Match(page, "<form\\b[^>]*action=\"/iletisim\"[\\s\\S]*?</form>").Value;
        Assert.NotEmpty(contactForm);
        Assert.Matches("<button[^>]*type=\"submit\"[^>]*>\\s*Mesajı Gönder\\s*<i", contactForm);
        Assert.DoesNotContain("Randevu talebi gönder", contactForm);
        Assert.DoesNotContain("/randevu", contactForm);
        Assert.Contains("href=\"/randevu\">Online Randevu Al", page);
        Assert.Contains("WhatsApp Danışma Hattı", page);
        var token = await Csrf(client);
        var response = await client.PostAsync("/iletisim", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["ContactForm.FullName"] = "Test Visitor", ["ContactForm.Email"] = "visitor@example.test",
            ["ContactForm.Subject"] = "Appointment question", ["ContactForm.Message"] = "Please tell me more about your rituals.",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("visitor@example.test", Assert.Single(await db.ContactForms.ToListAsync()).Email);
        Assert.Single(await db.Set<ContactEmailDelivery>().ToListAsync());
        Assert.Empty(await db.Appointments.ToListAsync());
        var confirmation = await client.GetStringAsync(response.Headers.Location!);
        Assert.Contains("success", confirmation);
        var messageJson = Regex.Match(confirmation, """const message=("(?:\\.|[^"\\])*");""").Groups[1].Value;
        var message = JsonSerializer.Deserialize<string>(messageJson);
        Assert.Equal("Mesajınız kaydedildi. E-posta bildirimi sıraya alındı.", message);
        Assert.DoesNotContain("randevu", message!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NewsletterAjaxPersistsBeforeSuccessEvenWhenBackgroundQueueIsUnavailableAndRetriesAreIdempotent()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync();
        Mock.Get(factory.Services.GetRequiredService<IContactEmailQueue>()).Setup(q => q.Enqueue(It.IsAny<int>())).Throws(new IOException("Queue unavailable"));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
        var token = await Csrf(client);
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var response = await client.PostAsync("/iletisim/bulten", new FormUrlEncodedContent(new Dictionary<string, string>
                { ["email"] = "reader@example.test", ["__RequestVerificationToken"] = token }));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.GetProperty("success").GetBoolean());
        }
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = Assert.Single(await db.ContactForms.ToListAsync());
        Assert.Equal("reader@example.test", stored.Email);
        var delivery = Assert.Single(await db.Set<ContactEmailDelivery>().ToListAsync());
        Assert.Equal(stored.Id, delivery.ContactFormId); Assert.Null(delivery.SentAtUtc);
        Assert.Null(delivery.HangfireJobId);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("")]
    public async Task InvalidNewsletterCannotWriteToDatabase(string email)
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), HandleCookies = true });
        var token = await Csrf(client); client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var response = await client.PostAsync("/iletisim/bulten", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["email"] = email, ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<AppDbContext>().ContactForms.ToListAsync());
    }

    private static async Task<string> Csrf(HttpClient client)
    {
        var page = await client.GetStringAsync("/iletisim");
        var token = WebUtility.HtmlDecode(Regex.Match(page, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        Assert.NotEmpty(token); return token;
    }
}
