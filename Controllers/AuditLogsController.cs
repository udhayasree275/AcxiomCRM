using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
public class AuditLogsController : Controller
{
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuditLogsController(IAuditService auditService, UserManager<ApplicationUser> userManager)
    {
        _auditService = auditService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(
        string? search, string? entity, string? action, string? user,
        DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        int pageSize = 15;
        var logs = await _auditService.GetLogsAsync(search, entity, action, user, fromDate, toDate, page, pageSize);
        int totalCount = await _auditService.GetTotalCountAsync(search, entity, action, user, fromDate, toDate);

        var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();

        var viewModel = new AuditLogListViewModel
        {
            AuditLogs = logs,
            SearchTerm = search,
            EntityFilter = entity,
            ActionFilter = action,
            UserFilter = user,
            FromDate = fromDate,
            ToDate = toDate,
            CurrentPage = page,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Users = users.Select(u => new SelectListItem { Value = u.Email, Text = $"{u.FullName} ({u.Email})" }).ToList()
        };

        return View(viewModel);
    }
}
