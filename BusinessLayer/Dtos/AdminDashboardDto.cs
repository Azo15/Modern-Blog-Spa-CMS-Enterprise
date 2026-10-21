namespace BusinessLayer.Dtos;
public sealed record DashboardDay(string Date, int Count);
public sealed record DashboardDistribution(string Service, int Count);
public sealed record AdminDashboardDto(int Today, int Yesterday, int Pending, int Customers, int Guests,
    int ActiveServices, decimal EstimatedMonthlyRevenue, int UnreadMessages, bool SmtpConfigured,
    IReadOnlyList<DashboardDay> Trend, IReadOnlyList<DashboardDistribution> Distribution,
    IReadOnlyList<AppointmentAdminDto> Recent)
{
    public VisitorDashboardDto Analytics { get; init; } = new();
}
