using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class LeadsApiController : ControllerBase
{
    private readonly ILeadService _leadService;

    public LeadsApiController(ILeadService leadService)
    {
        _leadService = leadService;
    }

    private (string UserId, string UserEmail, string UserRole) GetUserContext()
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        string userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "";
        string role = User.FindFirstValue(ClaimTypes.Role) ?? AppRoles.SalesExecutive;
        return (userId, userEmail, role);
    }

    [HttpGet]
    public async Task<IActionResult> GetLeads([FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? priority, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (userId, _, role) = GetUserContext();
        var (items, totalCount) = await _leadService.GetLeadsAsync(userId, role, search, status, priority, null, page, pageSize);

        var dtos = items.Select(l => new LeadDto
        {
            LeadId = l.LeadId,
            LeadCode = l.LeadCode,
            LeadName = l.LeadName,
            Email = l.Email,
            Phone = l.Phone,
            CompanyName = l.CompanyName,
            Source = l.Source,
            Status = l.Status,
            Priority = l.Priority,
            ExpectedValue = l.ExpectedValue,
            CreatedDate = l.CreatedDate,
            AssignedUserName = l.AssignedUser?.FullName,
            Notes = l.Notes
        });

        return Ok(new { total = totalCount, page, pageSize, data = dtos });
    }

    [HttpPost]
    public async Task<IActionResult> CreateLead([FromBody] CreateLeadDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var (userId, userEmail, _) = GetUserContext();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var lead = new Lead
        {
            LeadName = dto.LeadName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Source = dto.Source,
            Status = dto.Status,
            Priority = dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            AssignedTo = string.IsNullOrEmpty(dto.AssignedTo) ? userId : dto.AssignedTo,
            Notes = dto.Notes
        };

        var (success, error, created) = await _leadService.CreateLeadAsync(lead, userId, userEmail, ipAddress);
        if (!success)
        {
            return BadRequest(new { message = error });
        }

        var resultDto = new LeadDto
        {
            LeadId = created!.LeadId,
            LeadCode = created.LeadCode,
            LeadName = created.LeadName,
            Email = created.Email,
            Phone = created.Phone,
            CompanyName = created.CompanyName,
            Source = created.Source,
            Status = created.Status,
            Priority = created.Priority,
            ExpectedValue = created.ExpectedValue,
            CreatedDate = created.CreatedDate,
            Notes = created.Notes
        };

        return StatusCode(StatusCodes.Status201Created, resultDto);
    }
}
