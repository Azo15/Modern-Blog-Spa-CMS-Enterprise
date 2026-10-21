using EntityLayer.Concrete;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Tests;

public sealed class MessageAnalyticsSchemaTests
{
    [Fact]
    public void SqlServerMigrationSnapshotMatchesCurrentModel()
    {
        using var db = new DataAccesLayer.Concrete.AppDbContext(new DbContextOptionsBuilder<DataAccesLayer.Concrete.AppDbContext>()
            .UseSqlServer("Server=localhost;Database=SchemaOnly;Integrated Security=true;TrustServerCertificate=true").Options);
        Assert.False(db.Database.HasPendingModelChanges());
    }
    [Fact]
    public async Task ReplyHistorySurvivesSoftDeletionAndDuplicateRequestIsRejected()
    {
        using var f = new Stage2Fixture();
        var message = new ContactForm { Email = "guest@example.test" }; f.Db.ContactForms.Add(message); await f.Db.SaveChangesAsync();
        var request = Guid.NewGuid();
        ContactReply Reply() => new() { ContactFormId = message.Id, ReplyMessage = "Thank you", RepliedByUserId = "1", RepliedByUserName = "Team", RecipientEmail = message.Email!, Subject = "Your request", ClientRequestId = request };
        f.Db.ContactReplies.Add(Reply()); await f.Db.SaveChangesAsync();
        var duplicate = Reply(); f.Db.ContactReplies.Add(duplicate);
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync()); f.Db.Entry(duplicate).State = EntityState.Detached;
        f.Db.ContactForms.Remove(message); await f.Db.SaveChangesAsync();
        Assert.Empty(await f.Db.ContactReplies.ToListAsync()); Assert.Single(await f.Db.ContactReplies.IgnoreQueryFilters().ToListAsync());
    }

    [Fact]
    public async Task DailySummaryHasOneRowPerDateAndRawLogsSupportHardRetentionDeletion()
    {
        using var f = new Stage2Fixture(); var date = new DateOnly(2026, 10, 20);
        f.Db.DailyVisitorStats.Add(new DailyVisitorStat { Date = date, TotalPageViews = 2, UniqueVisitors = 1 });
        f.Db.VisitorLogs.AddRange(new VisitorLog { Date = date, VisitorHash = new string('a', 64), Path = "/ana-sayfa" }, new VisitorLog { Date = date, VisitorHash = new string('a', 64), Path = "/hizmetler" });
        await f.Db.SaveChangesAsync(); f.Db.ChangeTracker.Clear();
        f.Db.DailyVisitorStats.Add(new DailyVisitorStat { Date = date });
        await Assert.ThrowsAsync<DbUpdateException>(() => f.Db.SaveChangesAsync()); f.Db.ChangeTracker.Clear();
        await f.Db.VisitorLogs.ExecuteDeleteAsync(); Assert.Empty(await f.Db.VisitorLogs.ToListAsync()); Assert.Single(await f.Db.DailyVisitorStats.ToListAsync());
    }
}
