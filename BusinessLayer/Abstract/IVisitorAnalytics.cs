using BusinessLayer.Dtos;
namespace BusinessLayer.Abstract;
public interface IVisitorAnalytics
{
    Task RecordAsync(DateOnly day, string visitorHash, string path, bool mobile, DateTime createdUtc, CancellationToken ct);
    Task<VisitorDaySnapshot> GetDayAsync(DateOnly day, CancellationToken ct);
    Task FinalizeAsync(DateOnly day, CancellationToken ct);
}
