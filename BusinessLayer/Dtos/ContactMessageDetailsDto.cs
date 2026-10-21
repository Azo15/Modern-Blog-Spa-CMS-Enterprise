using EntityLayer.Concrete;
namespace BusinessLayer.Dtos;
public sealed record ContactReplyDto(int Id, string ReplyMessage, string RepliedByUserName, DateTime RepliedAt, bool IsMailSent, int AttemptCount, string? LastError = null)
{
    public string DeliveryStatus => IsMailSent ? "E-posta SMTP sunucusuna iletildi" : LastError switch
    {
        "SmtpConfigurationException" or "InvalidOperationException" => "Gönderilemedi: SMTP ayarlarını kontrol edin ve uygulama şifresini kaydedin.",
        "AuthenticationException" or "SaslException" => "Gmail/SMTP oturum açmayı reddetti. Kullanıcı adı ve uygulama şifresini kontrol edin.",
        "CryptographicException" => "Kayıtlı SMTP şifresi okunamıyor; uygulama şifresini yeniden kaydedin.",
        not null => AttemptCount >= 4 ? "Gönderim başarısız. Ayarları kontrol edip Arka Plan İşleri üzerinden yeniden deneyin." : "Gönderim denemesi başarısız; otomatik yeniden denenecek.",
        _ => AttemptCount >= 4 ? "Gönderim başarısız; yeniden deneme gerekli." : "Gönderim kuyruğunda; henüz e-posta sunucusuna iletilmedi."
    };
}
public sealed record ContactMessageDetailsDto(ContactForm Message, IReadOnlyList<ContactReplyDto> Replies, int Page, int TotalReplies);
