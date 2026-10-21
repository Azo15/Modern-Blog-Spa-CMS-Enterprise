namespace EntityLayer.Concrete;

/// <summary>No raw IP, cookie, query string, referrer or full user agent is retained.</summary>
public sealed class VisitorLog
{
    public long Id { get; set; }
    public DateOnly Date { get; set; }
    public string VisitorHash { get; set; } = "";
    public string Path { get; set; } = "";
    public bool IsMobile { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
