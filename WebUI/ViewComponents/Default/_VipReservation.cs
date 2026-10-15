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
            take: 50);
        var preferred = new[] { "Klasik İsveç Masajı", "Türk Hamamı Ritüeli", "Altın Yüz Maskesi", "Derin Doku Masajı", "Refleksoloji", "Sauna & Buhar Odası" };
        var options = published.OrderBy(service =>
            {
                var index = Array.IndexOf(preferred, service.Title);
                return index < 0 ? preferred.Length : index;
            }).ThenBy(service => service.Id).Select(service =>
            {
                var photoIndex = Array.IndexOf(preferred, service.Title);
                return new VipServiceOption(service.Id, service.Title ?? "Spa ritüeli",
                    photoIndex >= 0 ? media.Image(null, $"img/vip-{photoIndex + 1}.webp") :
                    media.ServiceImage(service.Title, service.Image, service.Id));
            }).ToArray();
        return View(new VipReservationViewModel(options));
    }
}
