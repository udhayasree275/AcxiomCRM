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
public class LeadsController : Controller
{
    private readonly ILeadService _leadService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeadsController(ILeadService leadService, UserManager<ApplicationUser> userManager)
    {
        _leadService = leadService;
        _userManager = userManager;
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

    private async Task<List<SelectListItem>> GetSalesExecutivesListAsync()
    {
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        return users.Select(u => new SelectListItem
        {
            Value = u.Id,
            Text = $"{u.FullName} ({u.RoleName})"
        }).ToList();
    }

    public async Task<IActionResult> Index(string? search, string? status, string? priority, string? assignedTo, int page = 1)
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        int pageSize = 10;
        var (items, totalCount) = await _leadService.GetLeadsAsync(userId, role, search, status, priority, assignedTo, page, pageSize);

        var viewModel = new LeadListViewModel
        {
            Leads = items,
            SearchTerm = search,
            StatusFilter = status,
            PriorityFilter = priority,
            AssignedFilter = assignedTo,
            CurrentPage = page,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            SalesExecutives = await GetSalesExecutivesListAsync()
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Details(int id)
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        var lead = await _leadService.GetByIdAsync(id, userId, role);
        if (lead == null)
        {
            return NotFound();
        }

        var viewModel = new LeadDetailViewModel
        {
            Lead = lead,
            FollowUps = lead.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList(),
            Activities = lead.Activities.OrderByDescending(a => a.ActivityDate).ToList()
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var viewModel = new LeadEditViewModel
        {
            SalesExecutives = await GetSalesExecutivesListAsync()
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadEditViewModel model)
    {
        var (userId, userEmail, _) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        if (!ModelState.IsValid)
        {
            model.SalesExecutives = await GetSalesExecutivesListAsync();
            return View(model);
        }

        var lead = new Lead
        {
            LeadName = model.LeadName,
            Email = model.Email,
            Phone = model.Phone,
            CompanyName = model.CompanyName,
            Source = model.Source,
            Status = model.Status,
            Priority = model.Priority,
            ExpectedValue = model.ExpectedValue,
            AssignedTo = string.IsNullOrEmpty(model.AssignedTo) ? userId : model.AssignedTo,
            Notes = model.Notes
        };

        var (success, error, createdLead) = await _leadService.CreateLeadAsync(lead, userId, userEmail, ipAddress);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Error creating lead.");
            model.SalesExecutives = await GetSalesExecutivesListAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = $"Lead '{createdLead!.LeadName}' ({createdLead.LeadCode}) created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        var lead = await _leadService.GetByIdAsync(id, userId, role);
        if (lead == null)
        {
            return NotFound();
        }

        var viewModel = new LeadEditViewModel
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            AssignedTo = lead.AssignedTo,
            Notes = lead.Notes,
            SalesExecutives = await GetSalesExecutivesListAsync()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LeadEditViewModel model)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        if (!ModelState.IsValid)
        {
            model.SalesExecutives = await GetSalesExecutivesListAsync();
            return View(model);
        }

        var lead = new Lead
        {
            LeadId = model.LeadId,
            LeadCode = model.LeadCode,
            LeadName = model.LeadName,
            Email = model.Email,
            Phone = model.Phone,
            CompanyName = model.CompanyName,
            Source = model.Source,
            Status = model.Status,
            Priority = model.Priority,
            ExpectedValue = model.ExpectedValue,
            AssignedTo = model.AssignedTo,
            Notes = model.Notes
        };

        var (success, error) = await _leadService.UpdateLeadAsync(lead, userId, userEmail, role, ipAddress);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Error updating lead.");
            model.SalesExecutives = await GetSalesExecutivesListAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = "Lead updated successfully.";
        return RedirectToAction(nameof(Details), new { id = model.LeadId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var (success, error) = await _leadService.DeleteLeadAsync(id, userId, userEmail, role, ipAddress);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete lead.";
        }
        else
        {
            TempData["SuccessMessage"] = "Lead deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Convert(int id)
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        var lead = await _leadService.GetByIdAsync(id, userId, role);
        if (lead == null)
        {
            return NotFound();
        }

        if (lead.Status == "Converted")
        {
            TempData["ErrorMessage"] = "This lead is already converted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var viewModel = new LeadConvertViewModel
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            CompanyName = lead.CompanyName,
            Email = lead.Email,
            Phone = lead.Phone,
            CreateOpportunity = true,
            OpportunityName = $"{lead.CompanyName} - Expansion Deal",
            OpportunityAmount = lead.ExpectedValue,
            OpportunityStage = "Qualification",
            OpportunityProbability = 30,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(LeadConvertViewModel model)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, error, customerId, opportunityId) = await _leadService.ConvertLeadAsync(model, userId, userEmail, role, ipAddress);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Conversion failed.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Lead successfully converted! Customer created (ID: {customerId}){(opportunityId.HasValue ? $" and Opportunity created (ID: {opportunityId})" : "")}.";
        return RedirectToAction("Details", "Customers", new { id = customerId });
    }
}
