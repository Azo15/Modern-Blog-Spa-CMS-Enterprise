using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using BusinessLayer.Abstract;
using BusinessLayer.Security;
using BusinessLayer.ValidationRules;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BusinessLayer.Tests;

public sealed class AppointmentEndpointTests
{
    private static HttpClient Client(PlatformWebFactory factory) => factory.CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });

    private static async Task AddService(PlatformWebFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Services.Add(new Service { Id = 1, Title = "Klasik İsveç Masajı", IsPublish = true });
        await db.SaveChangesAsync();
    }

    private static async Task<string> Csrf(HttpClient client, string page = "/")
    {
        var html = await client.GetStringAsync(page);
        var token = WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        Assert.NotEmpty(token); return token;
    }

    private static Dictionary<string, string> Request(string? csrf = null, Guid? id = null)
    {
        var values = new Dictionary<string, string>
        {
            ["ServiceId"] = "1", ["FullName"] = "Appointment Test Guest", ["Phone"] = "+90 538 878 70 00",
            ["Email"] = "reservation@example.test", ["AppointmentDate"] = DateOnly.FromDateTime(AppointmentSchedule.LocalNow).AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["TimeSlot"] = "14:00", ["GuestCount"] = "2", ["Notes"] = "Hassas cilt: çğışöüİ",
            ["ClientRequestId"] = (id ?? Guid.NewGuid()).ToString()
        };
        if (csrf is not null) values["__RequestVerificationToken"] = csrf;
        return values;
    }

    private static async Task Login(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = PlatformWebFactory.Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var wrapper = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wrapper.GetProperty("data").GetProperty("accessToken").GetString());
    }

    [Fact]
    public async Task AnonymousReservationRequiresCsrfBeforeSavingAnything()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); await AddService(factory);
        using var client = Client(factory);
        var denied = await client.PostAsync("/randevu", new FormUrlEncodedContent(Request()));
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Appointments.ToListAsync());
    }

    [Fact]
    public async Task PublicReservationFloodIsRateLimitedWithoutCreatingRecords()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync();
        using var client = Client(factory);
        for (var attempt = 0; attempt < 10; attempt++)
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/randevu", new FormUrlEncodedContent(Request()))).StatusCode);
        var rejected = await client.PostAsync("/randevu", new FormUrlEncodedContent(Request()));
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.Contains("Retry-After"));
        using var scope = factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Appointments.ToListAsync());
    }

    [Fact]
    public async Task AnonymousPostPersistsPendingReservationAndDurableEmailDespiteQueueFailure()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); await AddService(factory);
        Mock.Get(factory.Services.GetRequiredService<IAppointmentEmailQueue>()).Setup(queue => queue.Enqueue(It.IsAny<int>())).Throws(new IOException("Job storage unavailable"));
        using var client = Client(factory); var token = await Csrf(client);
        var fields = Request(token);
        fields["Status"] = "Confirmed"; fields["ReferenceCode"] = "#SPOOFED"; fields["CreatedBy"] = "1"; fields["Id"] = "9999";
        fields["IsDeleted"] = "true"; fields["SentAtUtc"] = DateTime.UtcNow.ToString("O");
        using var response = await client.PostAsync("/randevu", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.Equal("Pending", json.GetProperty("status").GetString());
        var reference = json.GetProperty("referenceCode").GetString(); Assert.StartsWith("#XSPA-", reference);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var appointment = Assert.Single(await db.Appointments.ToListAsync());
        Assert.Equal(AppointmentStatus.Pending, appointment.Status); Assert.Equal(reference, appointment.ReferenceCode);
        Assert.NotEqual(9999, appointment.Id); Assert.Null(appointment.CreatedBy); Assert.False(appointment.IsDeleted);
        Assert.Equal("Hassas cilt: çğışöüİ", appointment.Notes);
        var delivery = Assert.Single(await db.AppointmentEmailDeliveries.ToListAsync());
        Assert.Equal(appointment.Id, delivery.AppointmentId); Assert.Null(delivery.SentAtUtc); Assert.Null(delivery.HangfireJobId);
        using var retry = await client.PostAsync("/randevu", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        var retryJson = await retry.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(reference, retryJson.GetProperty("referenceCode").GetString());
        Assert.Equal(1, await db.Appointments.CountAsync()); Assert.Equal(1, await db.AppointmentEmailDeliveries.CountAsync());
    }

    [Theory]
    [InlineData("Notes")] [InlineData("GuestCount")] [InlineData("AppointmentDate")]
    public async Task ChangedReplayReturnsConflictWithOriginalReferenceAndKeepsOriginalReservation(string changedField)
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); await AddService(factory);
        using var client = Client(factory);
        var fields = Request(await Csrf(client));
        var originalDate = fields["AppointmentDate"];
        var originalGuests = int.Parse(fields["GuestCount"], CultureInfo.InvariantCulture);
        var originalNotes = fields["Notes"];
        using var created = await client.PostAsync("/randevu", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var receipt = await created.Content.ReadFromJsonAsync<JsonElement>();
        var reference = receipt.GetProperty("referenceCode").GetString()!;
        fields[changedField] = changedField switch
        {
            "Notes" => "A different health request", "GuestCount" => "4",
            _ => DateOnly.ParseExact(originalDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };

        using var replay = await client.PostAsync("/randevu", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.Conflict, replay.StatusCode);
        var conflict = await replay.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(conflict.GetProperty("success").GetBoolean());
        Assert.Contains(reference, conflict.GetProperty("message").GetString());
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = Assert.Single(await db.Appointments.AsNoTracking().ToListAsync());
        Assert.Equal(reference, saved.ReferenceCode);
        Assert.Equal(DateOnly.ParseExact(originalDate, "yyyy-MM-dd", CultureInfo.InvariantCulture), saved.AppointmentDate);
        Assert.Equal(originalGuests, saved.GuestCount); Assert.Equal(originalNotes, saved.Notes);
        Assert.Single(await db.AppointmentEmailDeliveries.ToListAsync());
        Mock.Get(factory.Services.GetRequiredService<IAppointmentEmailQueue>()).Verify(queue => queue.Enqueue(It.IsAny<int>()), Times.Once);
    }

    [Theory]
    [InlineData("Email", "invalid-email")]
    [InlineData("TimeSlot", "02:00")]
    [InlineData("GuestCount", "5")]
    [InlineData("AppointmentDate", "2000-01-01")]
    [InlineData("AppointmentDate", "bad-date")]
    [InlineData("ClientRequestId", "not-a-guid")]
    public async Task InvalidReservationReturnsUsefulValidationWithoutSaving(string field, string value)
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); await AddService(factory);
        using var client = Client(factory); var fields = Request(await Csrf(client)); fields[field] = value;
        var response = await client.PostAsync("/randevu", new FormUrlEncodedContent(fields));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.False(json.GetProperty("success").GetBoolean());
        Assert.True(json.GetProperty("errors").EnumerateObject().Any());
        using var scope = factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Appointments.ToListAsync());
    }

    [Theory]
    [InlineData("admin@acceptance.test", true)] [InlineData("editor@acceptance.test", true)]
    [InlineData("moderator@acceptance.test", true)] [InlineData("author@acceptance.test", true)]
    [InlineData("user@acceptance.test", false)] [InlineData(null, false)]
    public async Task ReservationAdminRoutesEnforcePoliciesAndCsrf(string? email, bool allowed)
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); await AddService(factory);
        using var client = Client(factory);
        var publicToken = await Csrf(client);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsync("/randevu", new FormUrlEncodedContent(Request(publicToken)))).StatusCode);
        if (email is not null) await Login(client, email);
        var page = await client.GetAsync("/AdminAppointment/Index");
        Assert.Equal(allowed ? HttpStatusCode.OK : email is null ? HttpStatusCode.Redirect : HttpStatusCode.Forbidden, page.StatusCode);
        var denied = await client.PostAsync("/AdminAppointment/Approve", new FormUrlEncodedContent(new Dictionary<string, string> { ["id"] = "1" }));
        Assert.Equal(allowed ? HttpStatusCode.BadRequest : email is null ? HttpStatusCode.Redirect : HttpStatusCode.Forbidden, denied.StatusCode);
        if (allowed)
        {
            var token = await Csrf(client, "/AdminAppointment/Index");
            var approved = await client.PostAsync("/AdminAppointment/Approve", new FormUrlEncodedContent(new Dictionary<string, string> { ["id"] = "1", ["__RequestVerificationToken"] = token }));
            Assert.Equal(HttpStatusCode.Redirect, approved.StatusCode);
            using var scope = factory.Services.CreateScope();
            Assert.Equal(AppointmentStatus.Confirmed, (await scope.ServiceProvider.GetRequiredService<AppDbContext>().Appointments.SingleAsync()).Status);
        }
    }

    [Fact]
    public async Task DirectReservationEntryPreservesSelectedServiceInLocalFlow()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); using var client = Client(factory);
        var response = await client.GetAsync("/randevu?serviceId=1");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/?reservation=1&serviceId=1", response.Headers.Location?.OriginalString);
    }
}
