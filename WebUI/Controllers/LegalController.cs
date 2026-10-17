using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace WebUI.Controllers;
[AllowAnonymous]
public class LegalController : Controller
{
    private IActionResult Page(string view, string tr, string en, string descriptionTr, string descriptionEn)
    {
        var english = CultureInfo.CurrentUICulture.Name == "en-US";
        ViewData["Title"] = english ? en : tr;
        ViewData["Description"] = english ? descriptionEn : descriptionTr;
        return View(view);
    }
    [HttpGet("gizlilik-politikasi")]
    public IActionResult GizlilikPolitikasi() => Page("Privacy", "Gizlilik Politikası", "Privacy Policy", "XSPA kişisel bilgilerinizi nasıl kullanır ve korur?", "How XSPA uses and protects your personal information.");
    [HttpGet("kullanim-kosullari")]
    public IActionResult KullanimKosullari() => Page("Terms", "Kullanım Koşulları", "Terms of Use", "XSPA randevu talepleri ve tesis kullanım koşulları.", "XSPA booking requests and facility terms.");
    [HttpGet("kvkk")]
    public IActionResult Kvkk() => Page("Kvkk", "KVKK Aydınlatma Metni", "Personal Data Notice", "Randevu ve iletişim bilgilerinizin işlenmesine ilişkin bilgilendirme.", "Information about processing booking and contact details.");
}
