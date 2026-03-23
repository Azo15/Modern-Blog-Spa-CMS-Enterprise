using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Web;

namespace BusinessLayer.Concrete
{
    public class MailService : IMailService
    {
        private readonly ISiteTableService _siteTableService;

        public MailService(ISiteTableService siteTableService)
        {
            _siteTableService = siteTableService;
        }

        public async Task SendContactMailAsync(ContactForm model)
        {
            var smtpInfo = await _siteTableService.GetQueryable().OrderByDescending(m=>m.CreatedDate).FirstOrDefaultAsync();

            // Null veya eksik kontrolü
            if (smtpInfo == null || string.IsNullOrEmpty(smtpInfo.Host) ||
                string.IsNullOrEmpty(smtpInfo.Mail) || string.IsNullOrEmpty(smtpInfo.Password) ||
                string.IsNullOrEmpty(smtpInfo.Port))
            {
                throw new InvalidOperationException("SMTP ayarları eksik veya tanımlı değil.");
            }

            if (!int.TryParse(smtpInfo.Port, out int port))
            {
                throw new InvalidOperationException("SMTP port bilgisi geçersiz.");
            }

            var emailMessage = new MimeMessage();

            // Gönderen
            emailMessage.From.Add(new MailboxAddress("Askar Profile İletişim", smtpInfo.Mail));

            // Alıcı
            emailMessage.To.Add(MailboxAddress.Parse(smtpInfo.Mail));

            // Cevaplanacak kişi
            emailMessage.ReplyTo.Add(new MailboxAddress(model.FullName, model.Email));

            emailMessage.Subject = $"İletişim Formu: {model.Subject}";

            // HTML içeriği
            string body = $@"
                <!DOCTYPE html>
                <html lang=""tr"">
                <head>
                    <meta charset=""UTF-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <title>İletişim Mesajı</title>
                    <style>
                        body {{
                            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                            background-color: #f9f9f9;
                            margin: 0;
                            padding: 0
                        }}
                        .email-wrapper {{
                            max-width: 600px;
                            margin: 20px auto;
                            background-color: #ffffff;
                            border-radius: 8px;
                            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
                            overflow: hidden
                        }}
                        .email-header {{
                            background: linear-gradient(135deg, rgba(3, 77, 151, 0.7), rgba(95, 115, 147, 0.7));
                            padding: 20px;
                            text-align: center
                        }}
                        .email-header img {{
                            max-width: 150px
                        }}
                        .email-body {{
                            padding: 30px;
                            color: #333
                        }}
                        .email-body h2 {{
                            margin-top: 0;
                            font-size: 22px;
                            color: #004080;
                            text-align: center
                        }}
                        .email-body table {{
                            width: 100%;
                            margin-top: 20px;
                            font-size: 16px;
                            border-collapse: collapse
                        }}
                        .email-body td {{
                            padding: 10px 5px;
                            vertical-align: top
                        }}
                        .email-body td.label {{
                            font-weight: bold;
                            width: 120px;
                            color: #666
                        }}
                        .email-body td.message-content {{
                            background: linear-gradient(135deg, rgba(3, 77, 151, 0.2), rgba(95, 115, 147, 0.2));
                            backdrop-filter: blur(8px);
                            -webkit-backdrop-filter: blur(8px);
                            border-radius: 10px;
                            padding: 15px;
                            box-shadow: 0 4px 12px rgba(0,0,0,0.1);
                            color: #222;
                            white-space: pre-wrap;
                        }}
                        .email-footer {{
                            background-color: #f1f1f1;
                            padding: 15px;
                            text-align: center;
                            font-size: 13px;
                            color: #999;
                        }}
                        @media screen and (max-width: 600px) {{
                            .email-body {{
                                padding: 20px;
                            }}
                        }}
                    </style>
                </head>
                <body>
                    <div class=""email-wrapper"">
                        <div class=""email-header"">
                            <img src=""https://askarprofilo.netlify.app/img/logo.png"" alt=""Logo"">
                        </div>
                        <div class=""email-body"">
                            <h2>Rezervasyon Mesajı</h2>
                            <table>
                                <tr><td class=""label"">Ad Soyad:</td><td>{HttpUtility.HtmlEncode(model.FullName)}</td></tr>
                                <tr><td class=""label"">E-posta:</td><td><a href=""mailto:{model.Email}"" style=""color: #004080;"">{HttpUtility.HtmlEncode(model.Email)}</a></td></tr>
                                <tr><td class=""label"">Telefon:</td><td>{HttpUtility.HtmlEncode(model.Phone ?? "Belirtilmemiş")}</td></tr>
                                <tr><td class=""label"">Konu:</td><td>{HttpUtility.HtmlEncode(model.Subject)}</td></tr>
                                <tr><td class=""label"" style=""vertical-align: top;"">Mesaj:</td><td class=""message-content"">{HttpUtility.HtmlEncode(model.Message).Replace(Environment.NewLine, "<br />")}</td></tr>
                            </table>
                        </div>
                        <div class=""email-footer"">
                            Bu e-posta, *** rezervasyon formu aracılığıyla gönderilmiştir.<br />
                            &copy; {DateTime.Now.Year} ***. Tüm hakları saklıdır.
                        </div>
                    </div>
                </body>
                </html>";

            emailMessage.Body = new TextPart("html") { Text = body };

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(smtpInfo.Host, port, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpInfo.Mail, smtpInfo.Password);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Gelişmiş loglama varsa burada logla
                throw new InvalidOperationException("E-posta gönderimi sırasında hata oluştu.", ex);
            }
        }
    }
}
