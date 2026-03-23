using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebUI.ViewComponents.Default
{
    public class _Footer : ViewComponent
    {
        private readonly IContactService _contactService;
        private readonly IServiceService _serviceService;
        private readonly ISiteTableService _siteTableService;

        public _Footer(IContactService contactService, IServiceService serviceService, ISiteTableService siteTableService)
        {
            _contactService = contactService;
            _serviceService = serviceService;
            _siteTableService = siteTableService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var contact = await _contactService.FirstOrDefaultAsync(
                predicate: x => true,
                orderBy: q => q.OrderByDescending(x => x.CreatedDate)
            );

            var service = await _serviceService.GetListAsync(
                orderBy: q => q.OrderByDescending(x => x.CreatedDate),
                take: 5
            );

            var siteData = await _siteTableService.GetQueryable().FirstOrDefaultAsync();
            var siteLogo = siteData?.SiteLogo;
            var siteTitle = siteData?.SiteTitle;

            var data = new Models.FooterViewModel
            {
                Contact = contact ?? new(),
                Services = service ?? new List<Service>(),
                Logo = siteLogo,
                SiteTitle = siteTitle
            };

            return View(data);
        }
    }
}
