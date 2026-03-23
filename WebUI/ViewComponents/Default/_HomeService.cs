using BusinessLayer.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.ViewComponents.Default
{
    public class _HomeService : ViewComponent
    {
        private readonly IServiceService _serviceService;

        public _HomeService(IServiceService serviceService)
        {
            _serviceService = serviceService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var data = await _serviceService.GetListAsync(
                filter: x => x.IsPublish == true,
                orderBy: q => q.OrderByDescending(x => x.CreatedDate),
                take: 8
            );
            return View(data);
        }
    }
}
