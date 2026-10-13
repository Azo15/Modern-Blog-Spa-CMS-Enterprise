using BusinessLayer.Abstract;
using Hangfire;

namespace BusinessLayer.Jobs;

public sealed class HangfireContactEmailQueue(IBackgroundJobClient client) : IContactEmailQueue
{
    public string Enqueue(int deliveryId)
        => client.Enqueue<ContactEmailJob>(job => job.SendAsync(deliveryId, CancellationToken.None));
}
