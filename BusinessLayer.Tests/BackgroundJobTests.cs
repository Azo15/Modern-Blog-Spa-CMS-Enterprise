using System.Net;
using System.Net.Sockets;
using System.Reflection;
using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using BusinessLayer.Jobs;
using BusinessLayer.Security;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Hangfire;
using Hangfire.Common;
using Hangfire.Dashboard;
using Hangfire.States;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebUI.Security;
using WebUI.Services;

namespace BusinessLayer.Tests;

public sealed class BackgroundJobTests
{
    private sealed class JobDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public AppDbContext Db { get; }
        public Mock<IContactEmailQueue> Queue { get; } = new();
        public Mock<IMailService> Mail { get; } = new();
        public Fixture()
        {
            connection.Open();
            Db = new JobDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
            Db.Database.EnsureCreated();
            Queue.Setup(x => x.Enqueue(It.IsAny<int>())).Returns("test-job");
            Mail.Setup(x => x.SendContactMailAsync(It.IsAny<ContactForm>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }
        public ContactSubmissionService Submission() => new(Db, Queue.Object, NullLogger<ContactSubmissionService>.Instance);
        public ContactEmailJob Job() => new(Db, Mail.Object, NullLogger<ContactEmailJob>.Instance);
        public ContactEmailRecoveryJob Recovery() => new(Db, Queue.Object, NullLogger<ContactEmailRecoveryJob>.Instance);
        public async Task<ContactEmailDelivery> Submit()
        {
            await Submission().SubmitAsync(Message());
            Db.ChangeTracker.Clear();
            return await Db.Set<ContactEmailDelivery>().SingleAsync();
        }
        public void Dispose() { Db.Dispose(); connection.Dispose(); }
    }

    private static ContactForm Message() => new()
    {
        FullName = "İsmail Şahin", Email = "visitor@example.test", Subject = "İletişim testi", Message = "Türkçe mesaj: çğışöüİ"
    };

    [Fact]
    public async Task SubmissionPersistsMessageAndDeliveryIntentBeforeEnqueue()
    {
        using var fixture = new Fixture();
        fixture.Queue.Setup(q => q.Enqueue(It.IsAny<int>())).Callback<int>(id =>
        {
            Assert.Equal(1, fixture.Db.ContactForms.Count());
            Assert.True(fixture.Db.Set<ContactEmailDelivery>().Any(d => d.Id == id));
        }).Returns("persisted-job");
        var result = await fixture.Submission().SubmitAsync(Message());
        Assert.True(result.EmailQueued);
        var delivery = await fixture.Db.Set<ContactEmailDelivery>().AsNoTracking().SingleAsync();
        Assert.Equal(result.ContactFormId, delivery.ContactFormId);
        Assert.Equal("persisted-job", delivery.HangfireJobId);
        Assert.NotNull(delivery.LastEnqueuedAtUtc);
        fixture.Mail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnqueueOutageDoesNotLoseMessageAndRecoveryRequeuesIntent()
    {
        using var fixture = new Fixture();
        fixture.Queue.Setup(q => q.Enqueue(It.IsAny<int>())).Throws(new InvalidOperationException("Job storage unavailable"));
        Assert.False((await fixture.Submission().SubmitAsync(Message())).EmailQueued);
        Assert.Equal(1, await fixture.Db.ContactForms.CountAsync());
        Assert.Equal(1, await fixture.Db.Set<ContactEmailDelivery>().CountAsync());
        fixture.Queue.Setup(q => q.Enqueue(It.IsAny<int>())).Returns("recovered-job");
        await fixture.Recovery().RecoverAsync(CancellationToken.None);
        Assert.Equal("recovered-job", (await fixture.Db.Set<ContactEmailDelivery>().AsNoTracking().SingleAsync()).HangfireJobId);
    }

    [Fact]
    public async Task AnonymousPostedAuditAndModerationFieldsAreNotPersisted()
    {
        using var fixture = new Fixture();
        var form = Message(); form.Id = 987; form.CreatedBy = "spoofed"; form.IsDeleted = true;
        form.Status = EntityLayer.Enum.ReservationStatus.SoforBeklemede;
        var result = await fixture.Submission().SubmitAsync(form);
        var saved = await fixture.Db.ContactForms.SingleAsync();
        Assert.NotEqual(987, result.ContactFormId);
        Assert.Null(saved.CreatedBy);
        Assert.False(saved.IsDeleted);
    }

    [Fact]
    public async Task InvalidContactCannotCreateDeliveryIntent()
    {
        using var fixture = new Fixture();
        var form = Message(); form.Email = "invalid-email";
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => fixture.Submission().SubmitAsync(form));
        Assert.Empty(await fixture.Db.ContactForms.ToListAsync());
        fixture.Queue.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeliveryIntentIsUniquePerContactMessageInRelationalDatabase()
    {
        using var fixture = new Fixture();
        var delivery = await fixture.Submit();
        fixture.Db.Set<ContactEmailDelivery>().Add(new ContactEmailDelivery { ContactFormId = delivery.ContactFormId });
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task ClosedSmtpFailsBackgroundJobButKeepsPersistedVisitorMessage()
    {
        using var fixture = new Fixture();
        var delivery = await fixture.Submit();
        // Bound without Listen: reserve a local port that cannot accept SMTP; never use configured external SMTP.
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)socket.LocalEndPoint!).Port;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "127.0.0.1", ["Smtp:Port"] = port.ToString(), ["Smtp:Username"] = "test",
            ["Smtp:Password"] = "test-only", ["Smtp:SenderEmail"] = "sender@example.test"
        }).Build();
        var site = new Mock<ISiteTableService>(); site.Setup(x => x.GetQueryable()).Returns(fixture.Db.SiteTables);
        var mail = new MailService(site.Object, configuration);
        var job = new ContactEmailJob(fixture.Db, mail, NullLogger<ContactEmailJob>.Instance);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await Assert.ThrowsAnyAsync<Exception>(() => job.SendAsync(delivery.Id, deadline.Token));
        var stored = await fixture.Db.Set<ContactEmailDelivery>().AsNoTracking().SingleAsync();
        Assert.Null(stored.SentAtUtc); Assert.Null(stored.LeaseToken); Assert.NotNull(stored.LastError);
        Assert.Equal(1, stored.AttemptCount);
        Assert.Equal("Türkçe mesaj: çğışöüİ", (await fixture.Db.ContactForms.SingleAsync()).Message);
    }

    [Fact]
    public async Task SuccessfulDeliveryIsIdempotentAndUsesStableMessageId()
    {
        using var fixture = new Fixture();
        var delivery = await fixture.Submit();
        await fixture.Job().SendAsync(delivery.Id, CancellationToken.None);
        await fixture.Job().SendAsync(delivery.Id, CancellationToken.None);
        fixture.Mail.Verify(x => x.SendContactMailAsync(It.IsAny<ContactForm>(), delivery.MessageId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull((await fixture.Db.Set<ContactEmailDelivery>().AsNoTracking().SingleAsync()).SentAtUtc);
    }

    [Fact]
    public async Task LiveLeasePreventsConcurrentDelivery()
    {
        using var fixture = new Fixture();
        var delivery = await fixture.Submit();
        delivery.LeaseToken = "other-worker"; delivery.LeaseExpiresAtUtc = DateTime.UtcNow.AddMinutes(5);
        await fixture.Db.SaveChangesAsync(); fixture.Db.ChangeTracker.Clear();
        await fixture.Job().SendAsync(delivery.Id, CancellationToken.None);
        fixture.Mail.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExpiredLeaseCanBeReclaimedAfterWorkerCrash()
    {
        using var fixture = new Fixture();
        var delivery = await fixture.Submit();
        delivery.LeaseToken = "dead-worker"; delivery.LeaseExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await fixture.Db.SaveChangesAsync(); fixture.Db.ChangeTracker.Clear();
        await fixture.Job().SendAsync(delivery.Id, CancellationToken.None);
        Assert.NotNull((await fixture.Db.Set<ContactEmailDelivery>().AsNoTracking().SingleAsync()).SentAtUtc);
    }

    [Theory]
    [InlineData("sent")]
    [InlineData("lease")]
    [InlineData("recent")]
    [InlineData("exhausted")]
    public async Task RecoverySkipsSentActiveRecentAndRetryExhaustedDeliveries(string reason)
    {
        using var fixture = new Fixture();
        var delivery = await fixture.Submit();
        delivery.LastEnqueuedAtUtc = DateTime.UtcNow.AddHours(-2);
        if (reason == "sent") delivery.SentAtUtc = DateTime.UtcNow;
        if (reason == "lease") delivery.LeaseExpiresAtUtc = DateTime.UtcNow.AddMinutes(2);
        if (reason == "recent") delivery.LastEnqueuedAtUtc = DateTime.UtcNow;
        if (reason == "exhausted") delivery.AttemptCount = 4;
        await fixture.Db.SaveChangesAsync(); fixture.Queue.Invocations.Clear();
        await fixture.Recovery().RecoverAsync(CancellationToken.None);
        fixture.Queue.VerifyNoOtherCalls();
    }

    [Fact]
    public void EmailJobDeclaresExactlyThreeAutomaticRetries()
    {
        var attribute = typeof(ContactEmailJob).GetMethod(nameof(ContactEmailJob.SendAsync))!.GetCustomAttribute<AutomaticRetryAttribute>();
        Assert.NotNull(attribute); Assert.Equal(3, attribute.Attempts);
    }

    [Fact]
    public void QueueStoresOnlyDeliveryIdentifierAndUsesInjectedHangfireClient()
    {
        var client = new Mock<IBackgroundJobClient>();
        client.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>())).Returns("job-42");
        Assert.Equal("job-42", new HangfireContactEmailQueue(client.Object).Enqueue(42));
        client.Verify(x => x.Create(It.Is<Job>(job => job.Type == typeof(ContactEmailJob) && (int)job.Args[0] == 42),
            It.IsAny<EnqueuedState>()), Times.Once);
    }

    [Theory]
    [InlineData(AppRoles.Admin, true)]
    [InlineData(AppRoles.Staff, false)]
    
    
    [InlineData(AppRoles.Customer, false)]
    [InlineData(null, false)]
    public void HangfireDashboardAllowsOnlyAuthenticatedSystemAdmin(string? role, bool allowed)
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services, User = AuthorizationTests.Principal(role, authenticated: role != null) };
        var context = new AspNetCoreDashboardContext(Mock.Of<JobStorage>(), new DashboardOptions(), http);
        Assert.Equal(allowed, new HangfireCustomAuthorizationFilter().Authorize(context));
    }

    [Fact]
    public void AnonymousRoleClaimCannotAccessHangfireDashboard()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services, User = AuthorizationTests.Principal(AppRoles.Admin, authenticated: false) };
        var context = new AspNetCoreDashboardContext(Mock.Of<JobStorage>(), new DashboardOptions(), http);
        Assert.False(new HangfireCustomAuthorizationFilter().Authorize(context));
    }

    [Fact]
    public async Task MaintenanceArchivesOnlyOldUnreferencedTrashAndPreservesImagesAndLogs()
    {
        using var fixture = new Fixture();
        var root = Path.Combine(Path.GetTempPath(), "modern-blog-maintenance-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "logs")); Directory.CreateDirectory(Path.Combine(root, "wwwroot", "blog"));
        var image = Path.Combine(root, "wwwroot", "blog", "restorable.jpg");
        var oldLog = Path.Combine(root, "logs", "log-20000101.txt");
        var activeLog = Path.Combine(root, "logs", "log-active.txt");
        await File.WriteAllTextAsync(image, "preserved-image"); await File.WriteAllTextAsync(oldLog, "old-log");
        await File.WriteAllTextAsync(activeLog, "active-log"); File.SetLastWriteTimeUtc(oldLog, DateTime.UtcNow.AddDays(-31));
        File.SetLastWriteTimeUtc(activeLog, DateTime.UtcNow.AddDays(-31));
        using var heldLog = new FileStream(activeLog, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        try
        {
            fixture.Db.Users.Add(new AppUser { Id = 1, UserName = "test" });
            fixture.Db.Blogs.AddRange(new Blog { Id = 1, Title = "Unreferenced", Image = "/blog/restorable.jpg" },
                new Blog { Id = 2, Title = "With history" }, new Blog { Id = 3, Title = "Recent" });
            await fixture.Db.SaveChangesAsync();
            fixture.Db.Comments.Add(new Comment { BlogId = 2, AppUserId = 1, Content = "Keep history" });
            await fixture.Db.SaveChangesAsync();
            // ExecuteUpdate bypasses audit and simulates genuinely historic deletion timestamps.
            await fixture.Db.Blogs.Where(b => b.Id < 3).ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDeleted, true)
                .SetProperty(b => b.DeletedDate, DateTime.UtcNow.AddDays(-31)));
            await fixture.Db.Blogs.Where(b => b.Id == 3).ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDeleted, true)
                .SetProperty(b => b.DeletedDate, DateTime.UtcNow.AddDays(-1)));
            fixture.Db.ChangeTracker.Clear();
            var environment = Mock.Of<IHostEnvironment>(e => e.ContentRootPath == root);
            var job = new TrashMaintenanceJob(fixture.Db, environment, NullLogger<TrashMaintenanceJob>.Instance);
            await job.RunAsync(CancellationToken.None); await job.RunAsync(CancellationToken.None);
            var archive = Assert.Single(await fixture.Db.Set<TrashArchiveEntry>().ToListAsync());
            Assert.Equal(nameof(Blog), archive.EntityType); Assert.Equal(1, archive.EntityId);
            Assert.Equal(3, await fixture.Db.Blogs.IgnoreQueryFilters().CountAsync());
            Assert.True(File.Exists(image)); Assert.True(File.Exists(activeLog));
            Assert.True(File.Exists(Path.Combine(root, "logs", "archive", "log-20000101.txt")));
        }
        finally
        {
            heldLog.Dispose();
            // Test-created absolute directory, constrained to temp root before recursive cleanup.
            if (Path.GetFullPath(root).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
                Directory.Delete(root, recursive: true);
        }
    }

    private sealed class CaptchaHandler(string response) : HttpMessageHandler
    {
        public string? Body { get; private set; }
        public Uri? Uri { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri; Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new(HttpStatusCode.OK) { Content = new StringContent(response) };
        }
    }

    [Theory]
    [InlineData("{\"success\":false}", false)]
    [InlineData("{\"success\":true,\"hostname\":\"example.test\"}", true)]
    [InlineData("{\"success\":true,\"hostname\":\"attacker.test\"}", false)]
    [InlineData("{}", false)]
    [InlineData("broken-json", false)]
    public async Task CaptchaRemainsFailClosedAndPostsSecretOutsideUrl(string response, bool expected)
    {
        var handler = new CaptchaHandler(response); using var client = new HttpClient(handler);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GoogleReCaptcha:SecretKey"] = "test-secret", ["GoogleReCaptcha:ExpectedHostname"] = "example.test"
        }).Build();
        var verifier = new ReCaptchaVerifier(client, config, NullLogger<ReCaptchaVerifier>.Instance);
        Assert.Equal(expected, await verifier.VerifyAsync("test-token", "127.0.0.1"));
        Assert.DoesNotContain("test-secret", handler.Uri!.ToString());
        Assert.Contains("secret=test-secret", handler.Body);
    }

    [Fact]
    public async Task CaptchaWithoutSecretCannotBeBypassedInDevelopment()
    {
        var handler = new CaptchaHandler("{\"success\":true}"); using var client = new HttpClient(handler);
        var verifier = new ReCaptchaVerifier(client, new ConfigurationBuilder().Build(), NullLogger<ReCaptchaVerifier>.Instance);
        Assert.False(await verifier.VerifyAsync("test-token", null)); Assert.Null(handler.Uri);
    }
}
