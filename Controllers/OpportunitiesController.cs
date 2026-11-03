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
public class OpportunitiesController : Controller
{
    private readonly IOpportunityService _opportunityService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public OpportunitiesController(
        IOpportunityService opportunityService,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context)
    {
        _opportunityService = opportunityService;
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

    private async Task PopulateDropDownsAsync(OpportunityEditViewModel model, string userId, string role)
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

    public async Task<IActionResult> Index(string? search, string? stage, string? status, string? ownerId, int page = 1)
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        int pageSize = 10;
        var (items, totalCount, totalAmount, weightedAmount) = await _opportunityService.GetOpportunitiesAsync(
            userId, role, search, stage, status, ownerId, page, pageSize);

        var viewModel = new OpportunityListViewModel
        {
            Opportunities = items,
            SearchTerm = search,
            StageFilter = stage,
            StatusFilter = status,
            OwnerFilter = ownerId,
            CurrentPage = page,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            TotalPipelineValue = totalAmount,
            WeightedPipelineValue = weightedAmount
        };

        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        viewModel.SalesExecutives = users.Select(u => new SelectListItem { Value = u.Id, Text = u.FullName }).ToList();

        return View(viewModel);
    }

    public async Task<IActionResult> Details(int id)
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        var opp = await _opportunityService.GetByIdAsync(id, userId, role);
        if (opp == null) return NotFound();

        return View(opp);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        var viewModel = new OpportunityEditViewModel
        {
            CustomerId = customerId ?? 0,
            LeadId = leadId,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
        };

        await PopulateDropDownsAsync(viewModel, userId, role);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityEditViewModel model)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        // Server-Side Business Validations
        if (model.Amount <= 0)
        {
            ModelState.AddModelError(nameof(model.Amount), "Opportunity Amount must be greater than 0.");
        }
        if (model.Probability < 0 || model.Probability > 100)
        {
            ModelState.AddModelError(nameof(model.Probability), "Probability must be between 0 and 100.");
        }
        if (model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(model.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(model, userId, role);
            return View(model);
        }

        var opportunity = new Opportunity
        {
            OpportunityName = model.OpportunityName,
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            Amount = model.Amount,
            Stage = model.Stage,
            Probability = model.Probability,
            ExpectedCloseDate = model.ExpectedCloseDate,
            Status = model.Status,
            AssignedTo = string.IsNullOrEmpty(model.AssignedTo) ? userId : model.AssignedTo,
            Notes = model.Notes
        };

        var (success, error, createdOpp) = await _opportunityService.CreateOpportunityAsync(opportunity, userId, userEmail, ipAddress);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Error creating opportunity.");
            await PopulateDropDownsAsync(model, userId, role);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Opportunity '{createdOpp!.OpportunityName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        var opp = await _opportunityService.GetByIdAsync(id, userId, role);
        if (opp == null) return NotFound();

        var viewModel = new OpportunityEditViewModel
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            LeadId = opp.LeadId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            AssignedTo = opp.AssignedTo,
            Notes = opp.Notes
        };

        await PopulateDropDownsAsync(viewModel, userId, role);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(OpportunityEditViewModel model)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        // Business Validations
        if (model.Amount <= 0)
        {
            ModelState.AddModelError(nameof(model.Amount), "Opportunity Amount must be greater than 0.");
        }
        if (model.Probability < 0 || model.Probability > 100)
        {
            ModelState.AddModelError(nameof(model.Probability), "Probability must be between 0 and 100.");
        }
        if (model.Stage != "Won" && model.Stage != "Lost" && model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            ModelState.AddModelError(nameof(model.ExpectedCloseDate), "Expected Close Date cannot be in the past for an active opportunity.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(model, userId, role);
            return View(model);
        }

        var opp = new Opportunity
        {
            OpportunityId = model.OpportunityId,
            OpportunityName = model.OpportunityName,
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            Amount = model.Amount,
            Stage = model.Stage,
            Probability = model.Probability,
            ExpectedCloseDate = model.ExpectedCloseDate,
            Status = model.Status,
            AssignedTo = model.AssignedTo,
            Notes = model.Notes
        };

        var (success, error) = await _opportunityService.UpdateOpportunityAsync(opp, userId, userEmail, role, ipAddress);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Error updating opportunity.");
            await PopulateDropDownsAsync(model, userId, role);
            return View(model);
        }

        TempData["SuccessMessage"] = "Opportunity updated successfully.";
        return RedirectToAction(nameof(Details), new { id = model.OpportunityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStage(int id, string stage)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var (success, error) = await _opportunityService.UpdateStageAsync(id, stage, userId, userEmail, role, ipAddress);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to update opportunity stage.";
        }
        else
        {
            TempData["SuccessMessage"] = $"Opportunity moved to '{stage}'.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var (success, error) = await _opportunityService.DeleteOpportunityAsync(id, userId, userEmail, role, ipAddress);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete opportunity.";
        }
        else
        {
            TempData["SuccessMessage"] = "Opportunity deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
