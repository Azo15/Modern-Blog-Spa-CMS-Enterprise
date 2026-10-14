using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using BusinessLayer.Jobs;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BusinessLayer.Tests;

public sealed class BackgroundRecoverySecurityTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        public AppDbContext Db { get; }
        public Mock<IContactEmailQueue> Queue { get; } = new();
        public Fixture()
        {
            connection.Open(); Db = new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options); Db.Database.EnsureCreated();
        }
        public async Task<int> Intent(string subject)
        {
            var delivery = new ContactEmailDelivery { ContactForm = new ContactForm { FullName = "Test", Email = "test@example.test", Subject = subject, Message = "Private contact" } };
            Db.Add(delivery); await Db.SaveChangesAsync(); Db.ChangeTracker.Clear(); return delivery.Id;
        }
        public void Dispose() { Db.Dispose(); connection.Dispose(); }
    }

    [Fact]
    public async Task CancelledWorkerReleasesLeaseAndStoresOnlySafeErrorType()
    {
        using var fixture = new Fixture(); var id = await fixture.Intent("Cancellation");
        using var stopped = new CancellationTokenSource();
        var mail = new Mock<IMailService>();
        mail.Setup(m => m.SendContactMailAsync(It.IsAny<ContactForm>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns((ContactForm form, string? messageId, CancellationToken ct) =>
            { stopped.Cancel(); return Task.FromException(new OperationCanceledException("password=do-not-store email=secret@example.test", ct)); });
        var job = new ContactEmailJob(fixture.Db, mail.Object, NullLogger<ContactEmailJob>.Instance);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => job.SendAsync(id, stopped.Token));
        var stored = await fixture.Db.Set<ContactEmailDelivery>().SingleAsync();
        Assert.Null(stored.LeaseToken); Assert.Null(stored.LeaseExpiresAtUtc); Assert.Null(stored.SentAtUtc);
        Assert.Equal(nameof(OperationCanceledException), stored.LastError);
        Assert.Equal(1, stored.AttemptCount); Assert.Single(await fixture.Db.ContactForms.ToListAsync());
    }

    [Fact]
    public async Task RecoveryContinuesPastOneQueueFailureAndKeepsItsIntentAvailable()
    {
        using var fixture = new Fixture(); var first = await fixture.Intent("First"); var second = await fixture.Intent("Second");
        fixture.Queue.Setup(q => q.Enqueue(first)).Throws(new InvalidOperationException("Temporary queue failure"));
        fixture.Queue.Setup(q => q.Enqueue(second)).Returns("second-job");
        await new ContactEmailRecoveryJob(fixture.Db, fixture.Queue.Object, NullLogger<ContactEmailRecoveryJob>.Instance).RecoverAsync(CancellationToken.None);
        var pending = await fixture.Db.Set<ContactEmailDelivery>().AsNoTracking().OrderBy(d => d.Id).ToListAsync();
        Assert.Null(pending[0].LastEnqueuedAtUtc); Assert.Null(pending[0].HangfireJobId);
        Assert.Equal("second-job", pending[1].HangfireJobId); Assert.NotNull(pending[1].LastEnqueuedAtUtc);
        Assert.Equal(2, await fixture.Db.ContactForms.CountAsync());
    }

    [Fact]
    public async Task CancellationAfterDurableSavePropagatesWithoutDiscardingDeliveryIntent()
    {
        using var fixture = new Fixture();
        fixture.Queue.Setup(q => q.Enqueue(It.IsAny<int>())).Throws(new OperationCanceledException());
        var service = new ContactSubmissionService(fixture.Db, fixture.Queue.Object, NullLogger<ContactSubmissionService>.Instance);
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.SubmitAsync(new ContactForm { FullName = "Test User", Email = "test@example.test", Subject = "Subject", Message = "Valid message content" }));
        Assert.Single(await fixture.Db.ContactForms.ToListAsync());
        Assert.Null((await fixture.Db.Set<ContactEmailDelivery>().SingleAsync()).LastEnqueuedAtUtc);
    }
}
