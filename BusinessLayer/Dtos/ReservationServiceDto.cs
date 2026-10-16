namespace BusinessLayer.Dtos;

public sealed record ReservationServiceDto(int Id, string Title, string? Image, int? DurationMinutes);
