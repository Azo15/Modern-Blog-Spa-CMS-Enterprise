using BusinessLayer.Abstract;
using Hangfire;

namespace BusinessLayer.Jobs;

public sealed class HangfireAppointmentEmailQueue(IBackgroundJobClient client) : IAppointmentEmailQueue
{
    public string Enqueue(int deliveryId) => client.Enqueue<AppointmentEmailJob>(job => job.SendAsync(deliveryId, CancellationToken.None));
}
