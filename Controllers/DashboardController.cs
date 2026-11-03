using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(IDashboardService dashboardService, UserManager<ApplicationUser> userManager)
    {
        _dashboardService = dashboardService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string dateFilter = "ThisMonth", DateTime? startDate = null, DateTime? endDate = null)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        string role = AppRoles.SalesExecutive;
        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            role = AppRoles.Admin;
        }
        else if (await _userManager.IsInRoleAsync(user, AppRoles.Manager))
        {
            role = AppRoles.Manager;
        }

        var model = await _dashboardService.GetDashboardDataAsync(user.Id, user.FullName, role, dateFilter, startDate, endDate);
        return View(model);
    }
}
