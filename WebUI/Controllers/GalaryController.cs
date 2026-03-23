using BusinessLayer.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{

    [AllowAnonymous]
    public class GalaryController : Controller
    {
        private readonly IGalaryService _galaryService;

        public GalaryController(IGalaryService galaryService)
        {
            _galaryService = galaryService;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _galaryService.GetListAsync(
                orderBy: q => q.OrderByDescending(g => g.UpdatedDate)
            );
            return View(data);
        }
    }
}
