using EntityLayer.Concrete;
namespace BusinessLayer.Abstract;
public interface IContactReplyMailService
{
    Task EnsureConfiguredAsync(CancellationToken ct);
    Task SendAsync(ContactReply reply, CancellationToken ct);
}
