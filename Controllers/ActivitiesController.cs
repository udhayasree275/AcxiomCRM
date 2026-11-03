using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly IActivityService _activityService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public ActivitiesController(
        IActivityService activityService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _activityService = activityService;
        _userManager = userManager;
        _context = context;
    }

    private async Task<(string UserId, string UserEmail, string UserRole)> GetCurrentUserInfoAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        string role = AppRoles.SalesExecutive;
        if (user != null)
        {
            if (await _userManager.IsInRoleAsync(user, AppRoles.Admin)) role = AppRoles.Admin;
            else if (await _userManager.IsInRoleAsync(user, AppRoles.Manager)) role = AppRoles.Manager;
        }
        return (user?.Id ?? "", user?.Email ?? "", role);
    }

    private async Task PopulateDropDownsAsync(ActivityEditViewModel model, string userId, string role)
    {
        var customersQ = _context.Customers.Where(c => c.Status == "Active");
        if (role == AppRoles.SalesExecutive) customersQ = customersQ.Where(c => c.OwnerId == userId);
        var customers = await customersQ.OrderBy(c => c.CustomerName).ToListAsync();

        model.Customers = customers.Select(c => new SelectListItem
        {
            Value = c.CustomerId.ToString(),
            Text = $"{c.CustomerName} ({c.CustomerCode})"
        }).ToList();

        var leadsQ = _context.Leads.AsQueryable();
        if (role == AppRoles.SalesExecutive) leadsQ = leadsQ.Where(l => l.AssignedTo == userId);
        var leads = await leadsQ.OrderBy(l => l.LeadName).ToListAsync();

        model.Leads = leads.Select(l => new SelectListItem
        {
            Value = l.LeadId.ToString(),
            Text = $"{l.LeadName} ({l.CompanyName})"
        }).ToList();

        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        model.Users = users.Select(u => new SelectListItem
        {
            Value = u.Id,
            Text = $"{u.FullName} ({u.RoleName})"
        }).ToList();
    }

    public async Task<IActionResult> Index(string? type, string? status, string? assignedTo, string? search)
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        var items = await _activityService.GetActivitiesAsync(userId, role, type, status, assignedTo, search);

        var viewModel = new ActivityListViewModel
        {
            Activities = items,
            TypeFilter = type,
            StatusFilter = status,
            AssignedFilter = assignedTo,
            SearchTerm = search
        };

        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        viewModel.Users = users.Select(u => new SelectListItem { Value = u.Id, Text = u.FullName }).ToList();

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        var viewModel = new ActivityEditViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            ActivityDate = DateTime.UtcNow,
            AssignedTo = userId
        };

        await PopulateDropDownsAsync(viewModel, userId, role);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityEditViewModel model)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(model, userId, role);
            return View(model);
        }

        var activity = new Activity
        {
            ActivityType = model.ActivityType,
            Subject = model.Subject,
            Description = model.Description,
            ActivityDate = model.ActivityDate,
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            AssignedTo = string.IsNullOrEmpty(model.AssignedTo) ? userId : model.AssignedTo,
            Status = model.Status
        };

        var (success, error, created) = await _activityService.LogActivityAsync(activity, userId, userEmail, ipAddress);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Error saving activity.");
            await PopulateDropDownsAsync(model, userId, role);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Activity '{created!.Subject}' logged successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        var act = await _activityService.GetByIdAsync(id, userId, role);
        if (act == null) return NotFound();

        var viewModel = new ActivityEditViewModel
        {
            ActivityId = act.ActivityId,
            ActivityType = act.ActivityType,
            Subject = act.Subject,
            Description = act.Description,
            ActivityDate = act.ActivityDate,
            CustomerId = act.CustomerId,
            LeadId = act.LeadId,
            AssignedTo = act.AssignedTo,
            Status = act.Status
        };

        await PopulateDropDownsAsync(viewModel, userId, role);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ActivityEditViewModel model)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(model, userId, role);
            return View(model);
        }

        var act = new Activity
        {
            ActivityId = model.ActivityId,
            ActivityType = model.ActivityType,
            Subject = model.Subject,
            Description = model.Description,
            ActivityDate = model.ActivityDate,
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            AssignedTo = model.AssignedTo,
            Status = model.Status
        };

        var (success, error) = await _activityService.UpdateActivityAsync(act, userId, userEmail, role, ipAddress);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Error updating activity.");
            await PopulateDropDownsAsync(model, userId, role);
            return View(model);
        }

        TempData["SuccessMessage"] = "Activity updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var (success, error) = await _activityService.DeleteActivityAsync(id, userId, userEmail, role, ipAddress);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete activity.";
        }
        else
        {
            TempData["SuccessMessage"] = "Activity deleted.";
        }

        return RedirectToAction(nameof(Index));
    }
}
