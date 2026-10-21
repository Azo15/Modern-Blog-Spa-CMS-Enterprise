namespace BusinessLayer.Dtos;
public sealed record VisitorDashboardDto
{
    public int TodayUniqueVisitors { get; init; }
    public int YesterdayUniqueVisitors { get; init; }
    public decimal? UniqueVisitorChangePercent { get; init; }
    public long TotalPageViews { get; init; }
    public int TodayPageViews { get; init; }
    public int MobileVisits { get; init; }
    public int DesktopVisits { get; init; }
    public decimal MobilePercent { get; init; }
    public decimal DesktopPercent { get; init; }
    public int TotalMessages { get; init; }
    public int RepliedMessages { get; init; }
    public decimal MessageReplyRate { get; init; }
    public int TodayAppointmentRequests { get; init; }
    public decimal AppointmentConversionRate { get; init; }
    public IReadOnlyList<DashboardDay> Trend { get; init; } = [];
    public IReadOnlyList<VisitedPageCount> TopPages { get; init; } = [];
}
