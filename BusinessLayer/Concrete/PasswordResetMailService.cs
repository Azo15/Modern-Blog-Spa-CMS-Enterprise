using System.Net;
using BusinessLayer.Abstract;
using MimeKit;
namespace BusinessLayer.Concrete;
public sealed class PasswordResetMailService(IContactReplyMailService smtp) : IPasswordResetMailService
{
    public async Task SendAsync(string recipient, string resetUrl, CancellationToken ct)
    {
        using var mail = CreateMessage(recipient, resetUrl);
        await smtp.SendMessageAsync(mail, ct);
    }
    public async Task SendGoogleAccountAsync(string recipient, string resetUrl, CancellationToken ct)
    {
        using var mail = CreateGoogleAccountMessage(recipient, resetUrl);
        await smtp.SendMessageAsync(mail, ct);
    }
    public static MimeMessage CreateMessage(string recipient, string resetUrl) => CreateMessage(recipient, resetUrl, false);
    public static MimeMessage CreateGoogleAccountMessage(string recipient, string resetUrl) => CreateMessage(recipient, resetUrl, true);
    private static MimeMessage CreateMessage(string recipient, string resetUrl, bool googleAccount)
    {
        var link = WebUtility.HtmlEncode(resetUrl);
        var introduction = googleAccount
            ? "Bu hesabı Google ile oluşturdunuz; henüz ayrı bir XSPA parolanız yok. Giriş ekranındaki Google ile Devam Et düğmesini kullanabilir veya aşağıdaki güvenli bağlantıyla ilk parolanızı belirleyebilirsiniz. Google bağlantınız korunur."
            : "Parolanızı güvenle yenilemek için aşağıdaki bağlantıyı kullanın.";
        var heading = googleAccount ? "Google hesabınızla devam edin" : "Yeni bir başlangıç";
        var mail = new MimeMessage { Subject = googleAccount ? "XSPA · Google hesabınız için parola belirleyin" : "XSPA · Parolanızı sıfırlayın" };
        mail.To.Add(MailboxAddress.Parse(recipient));
        mail.Body = new BodyBuilder {
            TextBody = $"{introduction}\n\nXSPA parola sıfırlama bağlantınız: {resetUrl}\nBağlantı 1 saat geçerlidir ve bir kez kullanılabilir. Bu isteği siz yapmadıysanız mesajı dikkate almayın.",
            HtmlBody = $"<html lang='tr'><body style='background:#f8f5ec;color:#173b2e;font:16px Arial;padding:24px'><main style='max-width:580px;margin:auto;border:1px solid #c9ab61;padding:32px'><h1 style='color:#173b2e;font-family:Georgia'>XSPA</h1><h2>{heading}</h2><p>{introduction}</p><a href='{link}' style='display:inline-block;background:#173b2e;color:#f0d58d;padding:16px;text-decoration:none'>Parolamı sıfırla →</a><p>Bağlantı 1 saat geçerlidir ve bir kez kullanılabilir. Bu isteği siz yapmadıysanız mesajı dikkate almayın.</p></main></body></html>"
        }.ToMessageBody();
        return mail;
    }
}
