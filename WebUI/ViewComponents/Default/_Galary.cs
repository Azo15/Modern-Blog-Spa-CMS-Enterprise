using BusinessLayer.Abstract;
using DataAccesLayer.Concrete;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace WebUI.ViewComponents.Default
{
    public class _Galary: ViewComponent
    {
        private readonly IGalaryService _galaryService;

        public _Galary(IGalaryService galaryService)
        {
            _galaryService = galaryService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var data = await _galaryService.GetListAsync(orderBy: q => q.OrderByDescending(x => x.CreatedDate), take: 16);
            return View(data);
        }
    }
}
