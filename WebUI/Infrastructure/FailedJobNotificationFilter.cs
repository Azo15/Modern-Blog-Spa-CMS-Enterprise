using BusinessLayer.Abstract;
using EntityLayer.Enum;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
namespace WebUI.Infrastructure;
public sealed class FailedJobNotificationFilter(IServiceScopeFactory scopes,ILogger<FailedJobNotificationFilter> logger) : JobFilterAttribute,IApplyStateFilter
{
    public void OnStateApplied(ApplyStateContext context,IWriteOnlyTransaction transaction)
    {
        // AutomaticRetry elects ScheduledState until retries are exhausted.
        if (context.NewState is not FailedState) return;
        try
        {
            using var scope = scopes.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
            service.PublishAsync(new($"job-failed:{context.BackgroundJob.Id}","Arka plan işi başarısız","Bir arka plan işi yeniden denemelerden sonra tamamlanamadı. Arka Plan İşleri sayfasını kontrol edin.",NotificationType.SystemFailure)).GetAwaiter().GetResult();
        }
        catch (Exception error) { logger.LogWarning("Failed-job notification unavailable ({ErrorType}).",error.GetType().Name); }
    }
    public void OnStateUnapplied(ApplyStateContext context,IWriteOnlyTransaction transaction) { }
}
