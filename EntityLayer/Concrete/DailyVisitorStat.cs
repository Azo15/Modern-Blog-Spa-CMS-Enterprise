namespace EntityLayer.Concrete;

public sealed class DailyVisitorStat
{
    public DateOnly Date { get; set; }
    public int TotalPageViews { get; set; }
    public int UniqueVisitors { get; set; }
    public int MobileVisits { get; set; }
    public int DesktopVisits { get; set; }
    public string? TopVisitedPagesJson { get; set; }
}
