using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WebUI.Models;

namespace WebUI.Controllers
{
    [AllowAnonymous]
    public class ContactController : BaseController
    {
        private readonly IContactFormService _contactFormService;
        private readonly IMailService _mailService;
        private readonly ILogger<ContactController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        public ContactController(IContactFormService contactFormService, IMailService mailService, ILogger<ContactController> logger, IConfiguration configuration, IMemoryCache cache)
        {
            _contactFormService = contactFormService;
            _mailService = mailService;
            _logger = logger;
            _configuration = configuration;
            _cache = cache;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var siteKey = _configuration["GoogleReCaptcha:SiteKey"];
            ViewData["SiteKey"] = siteKey;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(ContactPageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var ip = GetClientIp();

            // 🛑 IP engelli mi?
            if (IsIpOnCooldown(ip))
            {
                _logger.LogWarning("Spam engellendi - IP: {IP}", ip);
                SetSweetAlertMessage("Uyarı", "Çok sık mesaj gönderiyorsunuz. Lütfen birkaç dakika sonra tekrar deneyin.", "warning");
                return View("Index", model);
            }

            // ✅ reCAPTCHA kontrolü
            var recaptchaToken = Request.Form["g-recaptcha-response"];
            if (!await IsReCaptchaValid(recaptchaToken))
            {
                SetSweetAlertMessage("Bilgi", "Lütfen robot olmadığınızı doğrulayın.", "info");
                return View("Index", model);
            }

            try
            {
                await _mailService.SendContactMailAsync(model.ContactForm);

                var contactFormEntity = new ContactForm
                {
                    FullName = model.ContactForm.FullName,
                    Email = model.ContactForm.Email,
                    Phone = model.ContactForm.Phone,
                    Subject = model.ContactForm.Subject,
                    Message = model.ContactForm.Message,
                    CreatedDate = DateTime.Now,
                };
                await _contactFormService.AddAsync(contactFormEntity);

                // ⏳ IP'yi 60 saniye engelle
                SetIpCooldown(ip, TimeSpan.FromSeconds(60));

                _logger.LogInformation("Mesaj başarıyla gönderildi: {Email}", model.ContactForm.Email);
                SetSweetAlertMessage("Başarılı!", "Mesajınız başarıyla gönderildi! En kısa sürede sizinle iletişime geçeceğiz.", "success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mail gönderme sırasında hata oluştu.");
                SetSweetAlertMessage("Hata", $"{ex} Mesajınız gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.", "error");
            }

            return RedirectToAction("Index");
        }

        // Yardımcı metodlar
        private string GetClientIp()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private bool IsIpOnCooldown(string ip)
        {
            return _cache.TryGetValue($"ip-blocked:{ip}", out _);
        }

        private void SetIpCooldown(string ip, TimeSpan duration)
        {
            _cache.Set($"ip-blocked:{ip}", true, duration);
        }

        private async Task<bool> IsReCaptchaValid(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;

            var secretKey = _configuration["GoogleReCaptcha:SecretKey"];
            using var client = new HttpClient();
            var response = await client.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}",
                null);
            var result = await response.Content.ReadAsStringAsync();

            return result.Contains("\"success\": true");
        }
    }
}
