using DataAccesLayer.Concrete;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebUI.Infrastructure;
namespace BusinessLayer.Tests;
public sealed class FailedJobNotificationTests
{
    [Fact]
    public async Task TerminalFailuresCreateOneSafeAlertAndScheduledRetriesDoNot()
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync();
        var transaction = Mock.Of<IWriteOnlyTransaction>();
        var job = new BackgroundJob("private-job-id",Job.FromExpression(() => Console.WriteLine("private-job-argument")),DateTime.UtcNow);
        ApplyStateContext Context(IState state) => new(Mock.Of<JobStorage>(),Mock.Of<IStorageConnection>(),transaction,job,state,"Processing");
        var filter = new FailedJobNotificationFilter(f.Services.GetRequiredService<IServiceScopeFactory>(),NullLogger<FailedJobNotificationFilter>.Instance);
        filter.OnStateApplied(Context(new ScheduledState(TimeSpan.FromMinutes(1))),transaction);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(0,await db.Notifications.CountAsync());
        var failure = Context(new FailedState(new InvalidOperationException("private-password")));
        filter.OnStateApplied(failure,transaction); filter.OnStateApplied(failure,transaction);
        Assert.Equal(5,await db.Notifications.CountAsync());
        Assert.False(await db.Notifications.AnyAsync(n => n.Content.Contains("private") || n.Title.Contains("private")));
    }
}
