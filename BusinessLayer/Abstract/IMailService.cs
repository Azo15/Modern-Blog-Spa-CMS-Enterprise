using EntityLayer.Concrete;

namespace BusinessLayer.Abstract
{
    public interface IMailService
    {
        Task SendContactMailAsync(ContactForm model, string? messageId = null, CancellationToken cancellationToken = default);
    }
}
