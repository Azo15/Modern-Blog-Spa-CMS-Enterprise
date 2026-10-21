namespace BusinessLayer.Dtos;
public sealed record VisitedPageCount(string Path, int Count);
public sealed record VisitorDaySnapshot(DateOnly Date, int TotalPageViews, int UniqueVisitors, int MobileVisits,
    int DesktopVisits, IReadOnlyList<VisitedPageCount> TopPages);
