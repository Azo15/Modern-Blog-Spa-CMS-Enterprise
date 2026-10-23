using BusinessLayer.Abstract;
using BusinessLayer.Jobs;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace BusinessLayer.Tests;
public sealed class NotificationServiceTests
{
    [Fact]
    public async Task EventsReachOnlyActiveManagersAndDeduplicatePerRecipient()
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Users.Where(x => x.Id == 6).ExecuteUpdateAsync(u => u.SetProperty(x => x.IsActive,false));
        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var evt = new NotificationEvent("test:one","Hello","<script>untrusted</script>",NotificationType.SystemFailure);
        await service.PublishAsync(evt); await service.PublishAsync(evt);
        Assert.Equal(new[] {1,2,3,4},await db.Notifications.OrderBy(n => n.RecipientUserId).Select(n => n.RecipientUserId).ToArrayAsync());
        Assert.Equal(1,await service.CountAsync(1)); Assert.Equal(0,await service.CountAsync(5));
        var item = Assert.Single((await service.ListAsync(1)).Items);
        Assert.False(await service.ReadAsync(2,item.Id)); Assert.True(await service.ReadAsync(1,item.Id));
        Assert.Equal(0,await service.CountAsync(1)); Assert.Equal(1,await service.CountAsync(2));
    }
    [Fact]
    public async Task ReadAllUsesSnapshotBoundaryAndDoesNotReadNewArrivals()
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); using var scope = f.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
        await service.PublishAsync(new("test:first","First","First",NotificationType.ContactMessage,1));
        var snapshot = await service.ListAsync(1);
        await service.PublishAsync(new("test:second","Second","Second",NotificationType.ContactMessage,2));
        await service.ReadAllAsync(1,snapshot.LatestId);
        Assert.Equal(1,await service.CountAsync(1)); Assert.Equal(2,await service.CountAsync(2));
    }
    [Fact]
    public async Task SavedContactAndRecoveryCreateDurableRemindersAndSafeMailFailureAlerts()
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var submission = scope.ServiceProvider.GetRequiredService<IContactSubmissionService>();
        var saved = await submission.SubmitAsync(new ContactForm { FullName="Test Guest",Email="guest@example.test",Phone="+905380000000",Subject="Question",Message="A message for the team." });
        Assert.True(await db.Notifications.AnyAsync(n => n.Type == NotificationType.ContactMessage && n.RelatedRecordId == saved.ContactFormId));
        await db.ContactForms.Where(x => x.Id == saved.ContactFormId).ExecuteUpdateAsync(u => u.SetProperty(x => x.CreatedDate,DateTime.UtcNow.AddDays(-2)));
        await db.ContactEmailDeliveries.Where(x => x.ContactFormId == saved.ContactFormId).ExecuteUpdateAsync(u => u.SetProperty(x => x.LastError,"Secret must never reach notification content"));
        var job = scope.ServiceProvider.GetRequiredService<NotificationMaintenanceJob>(); await job.RunAsync(default);
        var count = await db.Notifications.CountAsync(); await job.RunAsync(default); Assert.Equal(count,await db.Notifications.CountAsync());
        Assert.True(await db.Notifications.AnyAsync(n => n.Type == NotificationType.MessageReminder && n.RelatedRecordId == saved.ContactFormId));
        Assert.True(await db.Notifications.AnyAsync(n => n.Type == NotificationType.EmailFailure));
        Assert.False(await db.Notifications.AnyAsync(n => n.Content.Contains("Secret")));
    }
}
