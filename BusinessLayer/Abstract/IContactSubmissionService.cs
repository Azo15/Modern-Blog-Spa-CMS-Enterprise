using EntityLayer.Concrete;

namespace BusinessLayer.Abstract;

public sealed record ContactSubmissionResult(int ContactFormId, bool EmailQueued);

public interface IContactSubmissionService
{
    Task<ContactSubmissionResult> SubmitAsync(ContactForm form, CancellationToken cancellationToken = default);
}
