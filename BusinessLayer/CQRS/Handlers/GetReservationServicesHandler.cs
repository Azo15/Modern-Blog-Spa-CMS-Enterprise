using System.Globalization;
using System.Text.RegularExpressions;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Dtos;
using DataAccesLayer.Concrete;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.CQRS.Handlers;

public sealed class GetReservationServicesHandler(AppDbContext db)
    : IRequestHandler<GetReservationServicesQuery, IReadOnlyList<ReservationServiceDto>>
{
    public async Task<IReadOnlyList<ReservationServiceDto>> Handle(GetReservationServicesQuery request, CancellationToken cancellationToken)
    {
        var services = await db.Services.AsNoTracking()
            .Where(service => service.IsPublish && !service.IsDeleted)
            .OrderBy(service => service.Id)
            .Select(service => new { service.Id, service.Title, service.Image, service.Content })
            .ToListAsync(cancellationToken);

        return services.Select(service => new ReservationServiceDto(service.Id, service.Title ?? "Spa ritüeli",
            service.Image, Duration(service.Content))).ToArray();
    }

    private static int? Duration(string? content)
    {
        var match = Regex.Match(content ?? "", @"(?<!\d)(\d{1,3})\s*(?:dk|dakika|min(?:utes?)?)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
        return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) && minutes > 0
            ? minutes : null;
    }
}
