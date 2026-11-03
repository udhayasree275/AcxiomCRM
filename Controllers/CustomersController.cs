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
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersController(ICustomerService customerService, UserManager<ApplicationUser> userManager)
    {
        _customerService = customerService;
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

    public async Task<IActionResult> Index(string? search, string? status, string? ownerId, int page = 1)
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        int pageSize = 10;
        var (items, totalCount) = await _customerService.GetCustomersAsync(userId, role, search, status, ownerId, page, pageSize);

        var viewModel = new CustomerListViewModel
        {
            Customers = items,
            SearchTerm = search,
            StatusFilter = status,
            OwnerFilter = ownerId,
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
        var customer = await _customerService.GetByIdAsync(id, userId, role);
        if (customer == null)
        {
            return NotFound();
        }

        var viewModel = new CustomerDetailViewModel
        {
            Customer = customer,
            Opportunities = customer.Opportunities.OrderByDescending(o => o.CreatedDate).ToList(),
            FollowUps = customer.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList(),
            Activities = customer.Activities.OrderByDescending(a => a.ActivityDate).ToList()
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var viewModel = new CustomerEditViewModel
        {
            SalesExecutives = await GetSalesExecutivesListAsync()
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerEditViewModel model)
    {
        var (userId, userEmail, _) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        if (!ModelState.IsValid)
        {
            model.SalesExecutives = await GetSalesExecutivesListAsync();
            return View(model);
        }

        var customer = new Customer
        {
            CustomerName = model.CustomerName,
            Email = model.Email,
            Phone = model.Phone,
            CompanyName = model.CompanyName,
            Address = model.Address,
            City = model.City,
            State = model.State,
            Status = model.Status,
            OwnerId = string.IsNullOrEmpty(model.OwnerId) ? userId : model.OwnerId
        };

        var (success, error, createdCustomer) = await _customerService.CreateCustomerAsync(customer, userId, userEmail, ipAddress);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Error creating customer.");
            model.SalesExecutives = await GetSalesExecutivesListAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = $"Customer '{createdCustomer!.CustomerName}' ({createdCustomer.CustomerCode}) created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (userId, _, role) = await GetCurrentUserInfoAsync();
        var customer = await _customerService.GetByIdAsync(id, userId, role);
        if (customer == null)
        {
            return NotFound();
        }

        var viewModel = new CustomerEditViewModel
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            OwnerId = customer.OwnerId,
            SalesExecutives = await GetSalesExecutivesListAsync()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomerEditViewModel model)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        if (!ModelState.IsValid)
        {
            model.SalesExecutives = await GetSalesExecutivesListAsync();
            return View(model);
        }

        var customer = new Customer
        {
            CustomerId = model.CustomerId,
            CustomerCode = model.CustomerCode,
            CustomerName = model.CustomerName,
            Email = model.Email,
            Phone = model.Phone,
            CompanyName = model.CompanyName,
            Address = model.Address,
            City = model.City,
            State = model.State,
            Status = model.Status,
            OwnerId = model.OwnerId
        };

        var (success, error) = await _customerService.UpdateCustomerAsync(customer, userId, userEmail, role, ipAddress);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Error updating customer.");
            model.SalesExecutives = await GetSalesExecutivesListAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = "Customer updated successfully.";
        return RedirectToAction(nameof(Details), new { id = model.CustomerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, userEmail, role) = await GetCurrentUserInfoAsync();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var (success, error) = await _customerService.DeleteCustomerAsync(id, userId, userEmail, role, ipAddress);
        if (!success)
        {
            TempData["ErrorMessage"] = error ?? "Failed to delete customer.";
        }
        else
        {
            TempData["SuccessMessage"] = "Customer deleted/deactivated successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
