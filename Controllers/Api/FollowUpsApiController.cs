using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/followups")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class FollowUpsApiController : ControllerBase
{
    private readonly IFollowUpService _followUpService;

    public FollowUpsApiController(IFollowUpService followUpService)
    {
        _followUpService = followUpService;
    }

    private (string UserId, string UserEmail, string UserRole) GetUserContext()
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        string userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "";
        string role = User.FindFirstValue(ClaimTypes.Role) ?? AppRoles.SalesExecutive;
        return (userId, userEmail, role);
    }

    [HttpGet]
    public async Task<IActionResult> GetFollowUps([FromQuery] string? status, [FromQuery] string? type, [FromQuery] string? search, [FromQuery] string filterTab = "all")
    {
        var (userId, _, role) = GetUserContext();
        var items = await _followUpService.GetFollowUpsAsync(userId, role, status, type, null, search, filterTab);

        var dtos = items.Select(f => new FollowUpDto
        {
            FollowUpId = f.FollowUpId,
            CustomerId = f.CustomerId,
            CustomerName = f.Customer?.CustomerName,
            LeadId = f.LeadId,
            LeadName = f.Lead?.LeadName,
            FollowUpDate = f.FollowUpDate,
            FollowUpType = f.FollowUpType,
            Subject = f.Subject,
            Remarks = f.Remarks,
            Status = f.Status,
            AssignedUserName = f.AssignedUser?.FullName,
            CreatedDate = f.CreatedDate
        });

        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> CreateFollowUp([FromBody] CreateFollowUpDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Business Rule
        if (dto.Status == "Planned" && dto.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new { message = "Follow-up date cannot be earlier than today." });
        }

        var (userId, userEmail, _) = GetUserContext();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var followUp = new FollowUp
        {
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            FollowUpDate = dto.FollowUpDate,
            FollowUpType = dto.FollowUpType,
            Subject = dto.Subject,
            Remarks = dto.Remarks,
            Status = dto.Status ?? "Planned",
            AssignedTo = string.IsNullOrEmpty(dto.AssignedTo) ? userId : dto.AssignedTo
        };

        var (success, error, created) = await _followUpService.CreateFollowUpAsync(followUp, userId, userEmail, ipAddress);
        if (!success)
        {
            return BadRequest(new { message = error });
        }

        return StatusCode(StatusCodes.Status201Created, new FollowUpDto
        {
            FollowUpId = created!.FollowUpId,
            CustomerId = created.CustomerId,
            LeadId = created.LeadId,
            FollowUpDate = created.FollowUpDate,
            FollowUpType = created.FollowUpType,
            Subject = created.Subject,
            Remarks = created.Remarks,
            Status = created.Status,
            CreatedDate = created.CreatedDate
        });
    }
}
