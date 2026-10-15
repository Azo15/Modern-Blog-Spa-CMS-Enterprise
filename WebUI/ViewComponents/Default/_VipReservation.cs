using BusinessLayer.Abstract;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.ViewComponents.Default;

public sealed class _VipReservation(IServiceService services, PublicMediaService media) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var published = await services.GetListAsync(
            filter: service => service.IsPublish,
            orderBy: query => query.OrderBy(service => service.Id),
            take: 6);
        var options = published.Select((service, index) => new VipServiceOption(
            service.Id,
            service.Title ?? "Spa ritüeli",
            media.Image(null, $"img/vip-{index + 1}.webp"))).ToArray();
        return View(new VipReservationViewModel(options));
    }
}
