using BusinessLayer.Dtos;

namespace BusinessLayer.Abstract;

public interface IReportService
{
    Task<ReportFile> GenerateAsync(ReportKind kind, ReportFormat format, ReportFilter filter, CancellationToken cancellationToken = default);
}
