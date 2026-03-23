using BusinessLayer.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebUI.ViewComponents.Default
{
    public class _Navbar: ViewComponent
    {
        private readonly ISiteTableService _siteTableService;

        public _Navbar(ISiteTableService siteTableService)
        {
            _siteTableService = siteTableService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var data = await _siteTableService.GetQueryable().FirstOrDefaultAsync();
            return View(data);
        }
    }
}
