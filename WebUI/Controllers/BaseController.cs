using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WebUI.Controllers
{
    public class BaseController:Controller
    {
        // TempData kullanarak toast mesajı ayarlamak için bir metot
        protected void SetToastMessage(string message, string type = "info")
        {
            TempData["ToastMessage"] = message;
            TempData["ToastType"] = type;
        }

        protected void SetSweetAlertMessage(string title, string message, string icon = "info")
        {
            TempData["Swal_Title"] = title;
            TempData["Swal_Message"] = message;
            TempData["Swal_Icon"] = icon;
        }

        // Gelen datayı JSON formatında ekrana basmayı sağlar kullanım sekli su sekilde. öncelikle bu controlleri miras alaması gerekir.
        // return var_dump(data) şeklinde çağırılır.
        protected IActionResult var_dump(object data)
        {
            return Content(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }), "application/json");
        }
    }
}
