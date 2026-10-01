using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using WebUI.Hubs;
using WebUI.Models;

namespace WebUI.Controllers
{
    [AllowAnonymous]
    [Route("iletisim")]
    public class ContactController : BaseController
    {
        private readonly IContactFormService _contactFormService;
        private readonly IContactService _contactService;
        private readonly IMailService _mailService;
        private readonly ILogger<ContactController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ContactController(IContactFormService contactFormService, IContactService contactService, IMailService mailService, ILogger<ContactController> logger, IConfiguration configuration, IMemoryCache cache, IHubContext<NotificationHub> hubContext)
        {
            _contactFormService = contactFormService;
            _contactService = contactService;
            _mailService = mailService;
            _logger = logger;
            _configuration = configuration;
            _cache = cache;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View(await BuildViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(ContactPageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                SetSweetAlertMessage("Hata", "Lütfen tüm alanları doldurun.", "error");
                var vm = await BuildViewModel();
                vm.ContactForm = model.ContactForm;
                return View("Index", vm);
            }

            var ip = GetClientIp();

            if (IsIpOnCooldown(ip))
            {
                _logger.LogWarning("Spam engellendi - IP: {IP}", ip);
                SetSweetAlertMessage("Uyarı", "Çok sık mesaj gönderiyorsunuz. Lütfen birkaç dakika sonra tekrar deneyin.", "warning");
                var vm = await BuildViewModel();
                vm.ContactForm = model.ContactForm;
                return View("Index", vm);
            }

            var recaptchaToken = Request.Form["g-recaptcha-response"];
            if (!await IsReCaptchaValid(recaptchaToken))
            {
                SetSweetAlertMessage("Bilgi", "Lütfen robot olmadığınızı doğrulayın.", "info");
                var vm = await BuildViewModel();
                vm.ContactForm = model.ContactForm;
                return View("Index", vm);
            }

            try
            {
                await _mailService.SendContactMailAsync(model.ContactForm);
                await _contactFormService.AddAsync(model.ContactForm);

                // 📌 SignalR Canlı Bildirim (WebSocket)
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", 
                    "Yeni İletişim Mesajı!", 
                    $"{model.ContactForm.FullName} isimli ziyaretçiden yeni mesaj geldi: \"{model.ContactForm.Subject}\"", 
                    "info");

                SetIpCooldown(ip, TimeSpan.FromSeconds(60));
                _logger.LogInformation("Mesaj başarıyla gönderildi: {Email}", model.ContactForm.Email);
                SetSweetAlertMessage("Başarılı!", "Mesajınız başarıyla gönderildi! En kısa sürede sizinle iletişime geçeceğiz.", "success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mail gönderme sırasında hata oluştu.");
                SetSweetAlertMessage("Hata", "Mesajınız gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.", "error");
            }

            return RedirectToAction("Index");
        }

        // ViewModel'i her seferinde hazırla
        private async Task<ContactPageViewModel> BuildViewModel()
        {
            ViewData["SiteKey"] = _configuration["GoogleReCaptcha:SiteKey"];

            return new ContactPageViewModel
            {
                Contact = await _contactService.LastOrDefaultAsync(
                    predicate: x => x.IsPublish == true,
                    orderBy: q => q.OrderBy(x => x.Id)
                ),
                ContactForm = new ContactForm()
            };
        }

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

            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("success").GetBoolean();
        }
    }
}