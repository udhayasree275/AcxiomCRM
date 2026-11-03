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
public class FollowUpsController : Controller
{
    private readonly IFollowUpService _followUpService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public FollowUpsController(
        IFollowUpService followUpService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _followUpService = followUpService;
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

    private async Task PopulateDropDownsAsync(FollowUpEditViewModel model, string userId, string role)
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
        model.SalesExecutives = users.Select(u => new SelectListItem
        {
            Value = u.Id,
            Text = $"{u.FullName} ({u.RoleName})"
        }).ToList();
    }

    public async Task<IActionResult> Index(string? status, string? type, string? assignedTo, string? search, string filterTab = "all")
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        var items = await _followUpService.GetFollowUpsAsync(userId, role, status, type, assignedTo, search, filterTab);

        var viewModel = new FollowUpListViewModel
        {
            FollowUps = items,
            StatusFilter = status,
            TypeFilter = type,
            AssignedFilter = assignedTo,
            SearchTerm = search,
            ActiveTab = filterTab
        };

        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        viewModel.SalesExecutives = users.Select(u => new SelectListItem { Value = u.Id, Text = u.FullName }).ToList();

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        var viewModel = new FollowUpEditViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            FollowUpDate = DateTime.UtcNow.AddDays(1),
            AssignedTo = userId
        };

        await PopulateDropDownsAsync(viewModel, userId, role);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpEditViewModel model)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        // Business validation: Follow-up date cannot be earlier than today for a new/planned follow-up
        if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(model.FollowUpDate), "Follow-up date cannot be earlier than today.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(model, userId, role);
            return View(model);
        }

        var followUp = new FollowUp
        {
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            FollowUpDate = model.FollowUpDate,
            FollowUpType = model.FollowUpType,
            Subject = model.Subject,
            Remarks = model.Remarks,
            Status = model.Status,
            AssignedTo = string.IsNullOrEmpty(model.AssignedTo) ? userId : model.AssignedTo
        };

        var (success, error, created) = await _followUpService.CreateFollowUpAsync(followUp, userId, userEmail, ipAddress);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Error scheduling follow-up.");
            await PopulateDropDownsAsync(model, userId, role);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Follow-up scheduled for {created!.FollowUpDate:yyyy-MM-dd}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        var fu = await _followUpService.GetByIdAsync(id, userId, role);
        if (fu == null) return NotFound();

        var viewModel = new FollowUpEditViewModel
        {
            FollowUpId = fu.FollowUpId,
            CustomerId = fu.CustomerId,
            LeadId = fu.LeadId,
            FollowUpDate = fu.FollowUpDate,
            FollowUpType = fu.FollowUpType,
            Subject = fu.Subject,
            Remarks = fu.Remarks,
            Status = fu.Status,
            AssignedTo = fu.AssignedTo
        };

        await PopulateDropDownsAsync(viewModel, userId, role);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(FollowUpEditViewModel model)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(model.FollowUpDate), "Follow-up date cannot be earlier than today for a planned activity.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(model, userId, role);
            return View(model);
        }

        var fu = new FollowUp
        {
            FollowUpId = model.FollowUpId,
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            FollowUpDate = model.FollowUpDate,
            FollowUpType = model.FollowUpType,
            Subject = model.Subject,
            Remarks = model.Remarks,
            Status = model.Status,
            AssignedTo = model.AssignedTo
        };

        var (success, error) = await _followUpService.UpdateFollowUpAsync(fu, userId, userEmail, role, ipAddress);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Error updating follow-up.");
            await PopulateDropDownsAsync(model, userId, role);
            return View(model);
        }

        TempData["SuccessMessage"] = "Follow-up updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? remarks)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var (success, error) = await _followUpService.CompleteFollowUpAsync(id, remarks, userId, userEmail, role, ipAddress);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to complete follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = "Follow-up marked as Completed.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? remarks)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var (success, error) = await _followUpService.RescheduleFollowUpAsync(id, newDate, remarks, userId, userEmail, role, ipAddress);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to reschedule follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Follow-up rescheduled to {newDate:yyyy-MM-dd}.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var (success, error) = await _followUpService.DeleteFollowUpAsync(id, userId, userEmail, role, ipAddress);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete follow-up.";
        }
        else
        {
            TempData["SuccessMessage"] = "Follow-up deleted.";
        }

        return RedirectToAction(nameof(Index));
    }
}
