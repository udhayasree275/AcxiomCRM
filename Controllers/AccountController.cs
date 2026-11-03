using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        if (user == null)
        {
            await _auditService.LogAsync(null, model.Email, "Failed Login", "Auth", null, null, "User not found", ipAddress);
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        if (!user.IsActive)
        {
            await _auditService.LogAsync(user.Id, user.Email, "Failed Login", "Auth", user.Id, null, "Account is deactivated", ipAddress);
            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact an administrator.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _auditService.LogAsync(user.Id, user.Email, "Login", "Auth", user.Id, null, "User logged in successfully", ipAddress);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogAsync(user.Id, user.Email, "Lockout", "Auth", user.Id, null, "Account locked out due to repeated failed login attempts", ipAddress);
            ModelState.AddModelError(string.Empty, "This account is locked due to repeated failed login attempts. Please try again after 15 minutes or contact an administrator.");
            return View(model);
        }

        await _auditService.LogAsync(user.Id, user.Email, "Failed Login", "Auth", user.Id, null, $"Invalid password (Failed count: {user.AccessFailedCount + 1})", ipAddress);
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "A user with this email address already exists.");
            return View(model);
        }

        // Validate role selection: regular register defaults to SalesExecutive
        string assignedRole = model.Role == AppRoles.Manager ? AppRoles.Manager : AppRoles.SalesExecutive;

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            RoleName = assignedRole,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, assignedRole);

            await _auditService.LogAsync(user.Id, user.Email, "Register", "User", user.Id, null, $"User registered with role {assignedRole}", ipAddress);

            // Auto sign in
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Dashboard");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        string? userId = _userManager.GetUserId(User);
        string? userEmail = User.Identity?.Name;
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        await _signInManager.SignOutAsync();
        await _auditService.LogAsync(userId, userEmail, "Logout", "Auth", userId, null, "User logged out", ipAddress);

        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
