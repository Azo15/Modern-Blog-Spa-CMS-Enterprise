namespace BusinessLayer.Abstract;
public interface IPasswordResetMailService
{
    Task SendGoogleAccountAsync(string recipient, string resetUrl, CancellationToken ct);
    Task SendAsync(string recipient, string resetUrl, CancellationToken ct);
}
