namespace WebUI.Models;

public sealed record VipServiceOption(int Id, string Title, string ImageUrl, int? DurationMinutes = null);

public sealed record VipReservationViewModel(IReadOnlyList<VipServiceOption> Services,
    string? FullName = null, string? Phone = null, string? Email = null);
