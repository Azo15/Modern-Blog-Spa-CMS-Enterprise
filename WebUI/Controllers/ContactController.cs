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
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using WebUI.Infrastructure;

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
        private readonly IWebHostEnvironment _environment;

        public ContactController(IContactSubmissionService submissionService, IContactService contactService, IReCaptchaVerifier captcha, ILogger<ContactController> logger, IConfiguration configuration, IMemoryCache cache, IHubContext<NotificationHub> hubContext, IStringLocalizer<SharedResource> localizer, IWebHostEnvironment environment)
        {
            _submissionService = submissionService;
            _contactService = contactService;
            _captcha = captcha;
            _localizer = localizer;
            _logger = logger;
            _configuration = configuration;
            _cache = cache;
            _hubContext = hubContext;
            _environment = environment;
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
                if (IsAjaxRequest) return ContactFailure(_localizer["Contact.ValidationRequired"]);
                SetSweetAlertMessage(_localizer["Contact.ErrorTitle"], _localizer["Contact.ValidationRequired"], "error");
                var vm = await BuildViewModel();
                vm.ContactForm = model.ContactForm;
                return View("Index", vm);
            }

            var ip = GetClientIp();

            if (IsIpOnCooldown(ip))
            {
                if (IsAjaxRequest) { Response.Headers.RetryAfter = "60"; return ContactFailure(_localizer["Contact.RateLimit"], StatusCodes.Status429TooManyRequests); }
                _logger.LogWarning("Spam engellendi - IP: {IP}", ip);
                SetSweetAlertMessage(_localizer["Contact.RateLimitTitle"], _localizer["Contact.RateLimit"], "warning");
                var vm = await BuildViewModel();
                vm.ContactForm = model.ContactForm;
                return View("Index", vm);
            }

            var recaptchaToken = Request.Form["g-recaptcha-response"];
            if (RequiresCaptcha && !await _captcha.VerifyAsync(recaptchaToken, ip, HttpContext.RequestAborted))
            {
                if (IsAjaxRequest) return ContactFailure(_localizer["Contact.CaptchaRequired"]);
                SetSweetAlertMessage(_localizer["Contact.CaptchaTitle"], _localizer["Contact.CaptchaRequired"], "info");
                var vm = await BuildViewModel();
                vm.ContactForm = model.ContactForm;
                return View("Index", vm);
            }

            try
            {
                await _submissionService.SubmitAsync(model.ContactForm, HttpContext.RequestAborted);
                SetIpCooldown(ip, TimeSpan.FromSeconds(60));

                await NotifyManagersAsync(model.ContactForm);
                _logger.LogInformation("Contact message persisted for background email delivery.");
                if (IsAjaxRequest) return Json(new { success = true, message = _localizer["Contact.Success"].Value });
                SetSweetAlertMessage(_localizer["Contact.SuccessTitle"], _localizer["Contact.Success"], "success");
            }
            catch (FluentValidation.ValidationException validation)
            {
                foreach (var error in validation.Errors)
                    ModelState.AddModelError($"ContactForm.{error.PropertyName}", error.ErrorMessage);
                if (IsAjaxRequest) return ContactFailure(_localizer["Contact.ValidationRequired"]);
                var vm = await BuildViewModel();
                vm.ContactForm = model.ContactForm;
                return View("Index", vm);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "İletişim mesajı kaydedilirken hata oluştu.");
                if (IsAjaxRequest) return ContactFailure(_localizer["Contact.SaveFailed"], StatusCodes.Status503ServiceUnavailable);
                SetSweetAlertMessage(_localizer["Contact.ErrorTitle"], _localizer["Contact.SaveFailed"], "error");
            }

            return RedirectToAction("Index");
        }

        [HttpPost("bulten"), ValidateAntiForgeryToken]
        public async Task<IActionResult> NewsletterInterest(string? email)
        {
            var en = System.Globalization.CultureInfo.CurrentUICulture.Name == "en-US";
            var normalizedEmail = email?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedEmail) || normalizedEmail.Length > 254 || !new EmailAddressAttribute().IsValid(normalizedEmail))
            {
                if (IsAjaxRequest)
                    return BadRequest(new { success = false, message = en ? "Please enter a valid email address." : "Lütfen geçerli bir e-posta adresi girin.", errors = new Dictionary<string, string[]> { ["email"] = [en ? "Please enter a valid email address." : "Geçerli bir e-posta adresi girin."] } });
                ModelState.AddModelError("ContactForm.Email", _localizer["Contact.ValidationRequired"]);
                return View("Index", await BuildViewModel());
            }

            var message = en ? "Thank you for your interest. Your request has been received." : "İlginiz için teşekkürler, kaydınız alındı.";
            var emailKey = "newsletter-received:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail.ToUpperInvariant())));
            // A browser retry receives the same acknowledgement only after a successful durable write.
            if (_cache.TryGetValue(emailKey, out _)) return NewsletterResponse(true, message);
            var cooldownKey = $"newsletter-cooldown:{GetClientIp()}";
            if (_cache.TryGetValue(cooldownKey, out _))
            {
                Response.Headers.RetryAfter = "60";
                return NewsletterResponse(false, en ? "Please wait a minute before submitting another request." : "Yeni bir talep göndermeden önce bir dakika bekleyin.", StatusCodes.Status429TooManyRequests);
            }

            var form = new ContactForm
            {
                FullName = en ? "Newsletter visitor" : "Bülten ziyaretçisi", Email = normalizedEmail,
                Subject = en ? "Wellbeing newsletter request" : "İyi yaşam bülteni talebi",
                Message = en ? "I would like to receive information about your wellbeing newsletter and published articles." : "İyi yaşam bülteni ve yayımlanan yazılar hakkında bilgilendirme almak istiyorum."
            };
            try
            {
                // The existing submission service atomically stores both the request and its email delivery intent.
                // A temporary mail/job outage cannot discard a visitor's registration.
                await _submissionService.SubmitAsync(form, HttpContext.RequestAborted);
                _cache.Set(emailKey, true, TimeSpan.FromMinutes(10));
                _cache.Set(cooldownKey, true, TimeSpan.FromSeconds(60));
                await NotifyManagersAsync(form);
                return NewsletterResponse(true, message);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                _logger.LogError(error, "Newsletter request could not be persisted.");
                return NewsletterResponse(false, en ? "We could not save your request. Please try again." : "Talebiniz kaydedilemedi. Lütfen tekrar deneyin.", StatusCodes.Status503ServiceUnavailable);
            }
        }

        private bool RequiresCaptcha => !_environment.IsDevelopment() &&
            !string.IsNullOrWhiteSpace(_configuration["GoogleReCaptcha:SiteKey"]) &&
            !string.IsNullOrWhiteSpace(_configuration["GoogleReCaptcha:SecretKey"]);

        private bool IsAjaxRequest => Request.WantsJson();

        private ObjectResult ContactFailure(string message, int status = StatusCodes.Status400BadRequest)
            => StatusCode(status, new { success = false, message,
                errors = ModelState.Where(entry => entry.Value?.Errors.Count > 0).ToDictionary(entry => entry.Key,
                    entry => entry.Value!.Errors.Select(error => string.IsNullOrEmpty(error.ErrorMessage) ? message : error.ErrorMessage).ToArray()) });

        private IActionResult NewsletterResponse(bool success, string message, int statusCode = StatusCodes.Status200OK)
        {
            if (IsAjaxRequest) return StatusCode(statusCode, new { success, message });
            SetSweetAlertMessage(_localizer[success ? "Contact.SuccessTitle" : "Contact.ErrorTitle"], message, success ? "success" : "warning");
            return RedirectToAction(nameof(Index));
        }

        private async Task NotifyManagersAsync(ContactForm form)
        {
            try
            {
                await _hubContext.Clients.Group(NotificationHub.ModerationGroup).SendAsync("ReceiveNotification",
                    "Yeni İletişim Mesajı!", $"{form.FullName} isimli ziyaretçiden yeni mesaj geldi: \"{form.Subject}\"",
                    "info", HttpContext.RequestAborted);
            }
            catch (Exception notificationError) when (notificationError is not OperationCanceledException)
            {
                _logger.LogWarning(notificationError, "Contact saved; live manager notification was unavailable.");
            }
        }

        // ViewModel'i her seferinde hazırla
        private async Task<ContactPageViewModel> BuildViewModel()
        {
            ViewData["SiteKey"] = RequiresCaptcha ? _configuration["GoogleReCaptcha:SiteKey"] : null;

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
