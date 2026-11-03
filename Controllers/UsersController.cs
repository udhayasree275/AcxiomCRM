using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
    }

    private List<SelectListItem> GetRolesList()
    {
        return new List<SelectListItem>
        {
            new SelectListItem { Value = AppRoles.Admin, Text = AppRoles.Admin },
            new SelectListItem { Value = AppRoles.Manager, Text = AppRoles.Manager },
            new SelectListItem { Value = AppRoles.SalesExecutive, Text = AppRoles.SalesExecutive }
        };
    }

    public async Task<IActionResult> Index(string? search, string? role)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.RoleName == role);
        }

        var users = await query.OrderByDescending(u => u.CreatedDate).ToListAsync();
        var userItems = new List<UserItemViewModel>();

        foreach (var u in users)
        {
            bool isLocked = await _userManager.IsLockedOutAsync(u);
            userItems.Add(new UserItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? "",
                Role = u.RoleName,
                IsActive = u.IsActive,
                IsLockedOut = isLocked,
                LockoutEnd = u.LockoutEnd,
                AccessFailedCount = u.AccessFailedCount,
                CreatedDate = u.CreatedDate
            });
        }

        return View(new UserListViewModel
        {
            Users = userItems,
            SearchTerm = search,
            RoleFilter = role
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new UserCreateViewModel { Roles = GetRolesList() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Roles = GetRolesList();
            return View(model);
        }

        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        string adminId = _userManager.GetUserId(User) ?? "";
        string adminEmail = User.Identity?.Name ?? "";

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(model.Email), "A user with this email address already exists.");
            model.Roles = GetRolesList();
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            RoleName = model.Role,
            IsActive = model.IsActive,
            CreatedDate = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);

            await _auditService.LogAsync(
                adminId,
                adminEmail,
                "Create",
                "User",
                user.Id,
                null,
                $"Created user '{user.FullName}' ({user.Email}) with role '{model.Role}'",
                ipAddress);

            TempData["SuccessMessage"] = $"User '{user.FullName}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var err in result.Errors)
        {
            ModelState.AddModelError(string.Empty, err.Description);
        }

        model.Roles = GetRolesList();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var viewModel = new UserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? "",
            Role = user.RoleName,
            IsActive = user.IsActive,
            Roles = GetRolesList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Roles = GetRolesList();
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        string adminId = _userManager.GetUserId(User) ?? "";
        string adminEmail = User.Identity?.Name ?? "";

        string oldValues = $"Name: {user.FullName}, Role: {user.RoleName}, Active: {user.IsActive}";

        user.FullName = model.FullName;
        user.IsActive = model.IsActive;

        if (user.RoleName != model.Role)
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);
            user.RoleName = model.Role;

            await _auditService.LogAsync(
                adminId,
                adminEmail,
                "Role Change",
                "User",
                user.Id,
                string.Join(",", currentRoles),
                model.Role,
                ipAddress);
        }

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            string newValues = $"Name: {user.FullName}, Role: {user.RoleName}, Active: {user.IsActive}";
            await _auditService.LogAsync(
                adminId,
                adminEmail,
                "Update",
                "User",
                user.Id,
                oldValues,
                newValues,
                ipAddress);

            TempData["SuccessMessage"] = "User updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var err in result.Errors)
        {
            ModelState.AddModelError(string.Empty, err.Description);
        }

        model.Roles = GetRolesList();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        string adminId = _userManager.GetUserId(User) ?? "";
        string adminEmail = User.Identity?.Name ?? "";
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        bool oldStatus = user.IsActive;
        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        await _auditService.LogAsync(
            adminId,
            adminEmail,
            "Security",
            "User",
            user.Id,
            $"Active: {oldStatus}",
            $"Active: {user.IsActive}",
            ipAddress);

        TempData["SuccessMessage"] = $"User {(user.IsActive ? "Activated" : "Deactivated")} successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        string adminId = _userManager.GetUserId(User) ?? "";
        string adminEmail = User.Identity?.Name ?? "";
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        await _auditService.LogAsync(
            adminId,
            adminEmail,
            "Security",
            "User",
            user.Id,
            "Locked Out",
            "Account unlocked by Administrator",
            ipAddress);

        TempData["SuccessMessage"] = $"Account for {user.Email} has been unlocked.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        return View(new ResetPasswordViewModel
        {
            UserId = user.Id,
            UserEmail = user.Email ?? ""
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null) return NotFound();

        string adminId = _userManager.GetUserId(User) ?? "";
        string adminEmail = User.Identity?.Name ?? "";
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

        if (result.Succeeded)
        {
            // Also reset lockout count if any
            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.SetLockoutEndDateAsync(user, null);

            await _auditService.LogAsync(
                adminId,
                adminEmail,
                "Password Reset",
                "User",
                user.Id,
                null,
                $"Password reset by Administrator for {user.Email}",
                ipAddress);

            TempData["SuccessMessage"] = $"Password for {user.Email} has been reset successfully.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var err in result.Errors)
        {
            ModelState.AddModelError(string.Empty, err.Description);
        }

        return View(model);
    }
}
