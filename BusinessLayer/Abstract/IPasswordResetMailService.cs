namespace BusinessLayer.Abstract;
public interface IPasswordResetMailService
{
    Task SendAsync(string recipient, string resetUrl, CancellationToken ct);
}
