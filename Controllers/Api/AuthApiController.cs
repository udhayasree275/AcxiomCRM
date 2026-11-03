using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;

    public AuthApiController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        IAuditService auditService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _auditService = auditService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] ApiLoginRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null || !user.IsActive)
        {
            await _auditService.LogAsync(null, request.Email, "Failed Login", "API", null, null, "Invalid credentials or inactive user", ipAddress);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            await _auditService.LogAsync(user.Id, user.Email, "Lockout", "API", user.Id, null, "API Login locked out", ipAddress);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Account is locked out due to repeated failed logins." });
        }

        if (!result.Succeeded)
        {
            await _auditService.LogAsync(user.Id, user.Email, "Failed Login", "API", user.Id, null, "API invalid password", ipAddress);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // Generate JWT Token
        var roles = await _userManager.GetRolesAsync(user);
        var primaryRole = roles.FirstOrDefault() ?? user.RoleName;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Email ?? ""),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim(ClaimTypes.GivenName, user.FullName),
            new Claim(ClaimTypes.Role, primaryRole)
        };

        var secret = _configuration["JwtSettings:Secret"] ?? "AcxiomCRM_Super_Secret_Key_For_Jwt_Authentication_Token_2026_Secure!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(24);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"] ?? "AcxiomCRM",
            audience: _configuration["JwtSettings:Audience"] ?? "AcxiomCRMUsers",
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        await _auditService.LogAsync(user.Id, user.Email, "Login", "API", user.Id, null, "API JWT issued successfully", ipAddress);

        return Ok(new ApiLoginResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email ?? "",
            FullName = user.FullName,
            Role = primaryRole,
            ExpiresAt = expires
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        string? userEmail = User.Identity?.Name;
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        await _signInManager.SignOutAsync();
        await _auditService.LogAsync(userId, userEmail, "Logout", "API", userId, null, "API session terminated", ipAddress);

        return Ok(new { message = "Logged out successfully." });
    }
}
