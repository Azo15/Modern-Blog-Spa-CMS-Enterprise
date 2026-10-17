using BusinessLayer.CQRS.Queries;
using BusinessLayer.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace WebUI.Controllers;
[Authorize(Policy = AccessPolicies.ContentManager)]
public sealed class AdminDashboardController(ISender sender) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken token) => View(await sender.Send(new GetAdminDashboardQuery(), token));
}
