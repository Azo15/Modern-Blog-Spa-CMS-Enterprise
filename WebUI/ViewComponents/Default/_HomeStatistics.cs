using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Models;

namespace WebUI.ViewComponents.Default;

public sealed class _HomeStatistics(
    IServiceService services,
    ISender sender,
    ILogger<_HomeStatistics> logger) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var cancellationToken = HttpContext.RequestAborted;
        int? serviceCount = null;
        int? blogCount = null;

        // Both services share the scoped DbContext; run these reads sequentially.
        try
        {
            serviceCount = await services.GetQueryable()
                .CountAsync(service => service.IsPublish, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Unable to load the home page published service count.");
        }

        try
        {
            var blogs = await sender.Send(new GetPublicBlogsQuery(1, 1), cancellationToken);
            blogCount = blogs.TotalCount;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Unable to load the home page published blog count.");
        }

        return View(new HomeStatisticsViewModel(serviceCount, blogCount));
    }
}
