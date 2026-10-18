using BusinessLayer.Abstract;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers;

[Authorize]
public sealed class ReportsController(IReportService reports) : Controller
{
    [HttpGet, Authorize(Policy = AccessPolicies.Moderator)]
    public Task<IActionResult> Contacts(ReportFormat format = ReportFormat.Excel, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken cancellationToken = default)
        => Export(ReportKind.Contacts, format, new(fromUtc, toUtc), cancellationToken);

    [HttpGet, Authorize(Policy = AccessPolicies.ContentManager)]
    public Task<IActionResult> Blogs(ReportFormat format = ReportFormat.Excel, DateTime? fromUtc = null, DateTime? toUtc = null, int? userId = null, int? blogId = null, CancellationToken cancellationToken = default)
        => Export(ReportKind.Blogs, format, new(fromUtc, toUtc, userId, blogId), cancellationToken);

    [HttpGet, Authorize(Policy = AccessPolicies.ContentManager)]
    public Task<IActionResult> Interactions(ReportFormat format = ReportFormat.Excel, DateTime? fromUtc = null, DateTime? toUtc = null, int? userId = null, int? blogId = null, CancellationToken cancellationToken = default)
        => Export(ReportKind.Interactions, format, new(fromUtc, toUtc, userId, blogId), cancellationToken);

    [HttpGet, Authorize(Policy = AccessPolicies.ContentManager)]
    public Task<IActionResult> Users(ReportFormat format = ReportFormat.Excel, int? userId = null, CancellationToken cancellationToken = default)
        => Export(ReportKind.Users, format, new(UserId: userId), cancellationToken);

    private async Task<IActionResult> Export(ReportKind kind, ReportFormat format, ReportFilter filter, CancellationToken cancellationToken)
    {
        // Generated files may contain private contact/user data. Never persist them in shared caches.
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        var report = await reports.GenerateAsync(kind, format, filter, cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }
}
