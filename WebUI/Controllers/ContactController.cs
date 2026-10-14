using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using WebUI.Hubs;
using WebUI.Models;
using WebUI.Services;
using BusinessLayer;
using Microsoft.Extensions.Localization;

namespace WebUI.Controllers
{
    [AllowAnonymous]
    [Route("iletisim")]
    public class ContactController : BaseController
    {
        private readonly IContactSubmissionService _submissionService;
        private readonly IContactService _contactService;
        private readonly IReCaptchaVerifier _captcha;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ILogger<ContactController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ContactController(IContactSubmissionService submissionService, IContactService contactService, IReCaptchaVerifier captcha, ILogger<ContactController> logger, IConfiguration configuration, IMemoryCache cache, IHubContext<NotificationHub> hubContext, IStringLocalizer<SharedResource> localizer)
        {
            _submissionService = submissionService;
            _contactService = contactService;
            _captcha = captcha;
            _localizer = localizer;
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
            if (!ModelState.IsValid || model.ContactForm == null)
            {
                SetSweetAlertMessage(_localizer["Contact.ErrorTitle"], _localizer["Contact.ValidationRequired"], "error");
                var vm = await BuildViewModel();
                vm.ContactForm = model.ContactForm;
                return View("Index", vm);
            }

            var ip = GetClientIp();

            if (IsIpOnCooldown(ip))
            {
                _logger.LogWarning("Spam engellendi - IP: {IP}", ip);
                SetSweetAlertMessage(_localizer["Contact.RateLimitTitle"], _localizer["Contact.RateLimit"], "warning");
                var vm = await BuildViewModel();
                vm.ContactForm = model.ContactForm;
                return View("Index", vm);
            }

            var recaptchaToken = Request.Form["g-recaptcha-response"];
            if (!await _captcha.VerifyAsync(recaptchaToken, ip, HttpContext.RequestAborted))
            {
                SetSweetAlertMessage(_localizer["Contact.CaptchaTitle"], _localizer["Contact.CaptchaRequired"], "info");
                var vm = await BuildViewModel();
                vm.ContactForm = model.ContactForm;
                return View("Index", vm);
            }

            try
            {
                await _submissionService.SubmitAsync(model.ContactForm, HttpContext.RequestAborted);
                SetIpCooldown(ip, TimeSpan.FromSeconds(60));

                // 📌 SignalR Canlı Bildirim (WebSocket)
                try
                {
                    await _hubContext.Clients.Group(NotificationHub.ModerationGroup).SendAsync("ReceiveNotification",
                        "Yeni İletişim Mesajı!",
                        $"{model.ContactForm.FullName} isimli ziyaretçiden yeni mesaj geldi: \"{model.ContactForm.Subject}\"",
                        "info", HttpContext.RequestAborted);
                }
                catch (Exception notificationError)
                {
                    _logger.LogWarning(notificationError, "Contact saved; live manager notification was unavailable.");
                }
                _logger.LogInformation("Contact message persisted for background email delivery.");
                SetSweetAlertMessage(_localizer["Contact.SuccessTitle"], _localizer["Contact.Success"], "success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İletişim mesajı kaydedilirken hata oluştu.");
                SetSweetAlertMessage(_localizer["Contact.ErrorTitle"], _localizer["Contact.SaveFailed"], "error");
            }

            return RedirectToAction("Index");
        }

        [HttpPost("bulten"), ValidateAntiForgeryToken]
        public async Task<IActionResult> NewsletterInterest(string? email)
        {
            var vm = await BuildViewModel();
            if (string.IsNullOrWhiteSpace(email) || email.Length > 256 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            {
                ModelState.AddModelError("ContactForm.Email", _localizer["Contact.ValidationRequired"]);
                return View("Index", vm);
            }
            var en = System.Globalization.CultureInfo.CurrentUICulture.Name == "en-US";
            vm.ContactForm!.Email = email.Trim();
            vm.ContactForm.Subject = en ? "Wellbeing newsletter request" : "İyi yaşam bülteni talebi";
            vm.ContactForm.Message = en ? "I would like information about your wellbeing newsletter. Please contact me about available updates." : "İyi yaşam bülteniniz hakkında bilgi almak istiyorum. Uygun güncellemeler için benimle iletişime geçebilirsiniz.";
            ViewData["NewsletterNotice"] = en ? "Your email is ready. Complete the contact form to confirm your request." : "E-posta adresiniz hazır. Talebinizi onaylamak için iletişim formunu tamamlayın.";
            return View("Index", vm);
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

    }
}
