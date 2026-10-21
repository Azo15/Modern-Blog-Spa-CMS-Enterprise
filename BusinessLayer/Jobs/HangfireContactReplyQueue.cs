using BusinessLayer.Abstract;
using Hangfire;
namespace BusinessLayer.Jobs;
public sealed class HangfireContactReplyQueue(IBackgroundJobClient client) : IContactReplyQueue
{
    public string Enqueue(int replyId) => client.Enqueue<ContactReplyEmailJob>(job => job.SendAsync(replyId, CancellationToken.None));
}
