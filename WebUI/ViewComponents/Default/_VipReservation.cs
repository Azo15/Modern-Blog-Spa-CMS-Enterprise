using BusinessLayer.CQRS.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using WebUI.Services;

namespace WebUI.ViewComponents.Default;

public sealed class _VipReservation(ISender sender, PublicMediaService media) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var published = await sender.Send(new GetReservationServicesQuery(), HttpContext.RequestAborted);
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
                    media.ServiceImage(service.Title, service.Image, service.Id), service.DurationMinutes);
            }).ToArray();
        return View(new VipReservationViewModel(options));
    }
}
