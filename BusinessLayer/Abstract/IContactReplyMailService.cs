using EntityLayer.Concrete;
namespace BusinessLayer.Abstract;
public interface IContactReplyMailService { Task SendAsync(ContactReply reply, CancellationToken ct); }
