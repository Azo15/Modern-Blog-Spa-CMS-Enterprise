using BusinessLayer.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebUI.ViewComponents
{
    public class _WelCome : ViewComponent
    {
        private readonly ISliderService _sliderService;
        private readonly IContactService _contactService;

        public _WelCome(ISliderService sliderService, IContactService contactService)
        {
            _sliderService = sliderService;
            _contactService = contactService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var data = await _sliderService.GetListAsync();

            var contactData = await _contactService.GetQueryable()
                .OrderByDescending(x => x.CreatedDate)
                .FirstOrDefaultAsync();

            ViewBag.Phone = contactData?.Phone?.Trim().Replace(" ", "");
            return View(data);
        }
    }
}
