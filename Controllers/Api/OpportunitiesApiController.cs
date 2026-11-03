using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class OpportunitiesApiController : ControllerBase
{
    private readonly IOpportunityService _opportunityService;

    public OpportunitiesApiController(IOpportunityService opportunityService)
    {
        _opportunityService = opportunityService;
    }

    private (string UserId, string UserEmail, string UserRole) GetUserContext()
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        string userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name ?? "";
        string role = User.FindFirstValue(ClaimTypes.Role) ?? AppRoles.SalesExecutive;
        return (userId, userEmail, role);
    }

    [HttpGet]
    public async Task<IActionResult> GetOpportunities([FromQuery] string? search, [FromQuery] string? stage, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (userId, _, role) = GetUserContext();
        var (items, totalCount, totalAmount, weightedAmount) = await _opportunityService.GetOpportunitiesAsync(
            userId, role, search, stage, status, null, page, pageSize);

        var dtos = items.Select(o => new OpportunityDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.CustomerName,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            WeightedAmount = o.WeightedAmount,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            CreatedDate = o.CreatedDate,
            AssignedUserName = o.AssignedUser?.FullName,
            Notes = o.Notes
        });

        return Ok(new
        {
            total = totalCount,
            totalPipeline = totalAmount,
            weightedPipeline = weightedAmount,
            page,
            pageSize,
            data = dtos
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateOpportunity([FromBody] CreateOpportunityDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Server-Side Business Validations
        if (dto.Amount <= 0)
        {
            return BadRequest(new { message = "Opportunity Amount must be greater than 0." });
        }

        if (dto.Probability < 0 || dto.Probability > 100)
        {
            return BadRequest(new { message = "Probability must be between 0 and 100." });
        }

        if (dto.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            return BadRequest(new { message = "Expected Close Date cannot be in the past." });
        }

        var (userId, userEmail, _) = GetUserContext();
        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        var opp = new Opportunity
        {
            OpportunityName = dto.OpportunityName,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = dto.Status ?? "Open",
            AssignedTo = string.IsNullOrEmpty(dto.AssignedTo) ? userId : dto.AssignedTo,
            Notes = dto.Notes
        };

        var (success, error, created) = await _opportunityService.CreateOpportunityAsync(opp, userId, userEmail, ipAddress);
        if (!success)
        {
            return BadRequest(new { message = error });
        }

        var resultDto = new OpportunityDto
        {
            OpportunityId = created!.OpportunityId,
            OpportunityName = created.OpportunityName,
            CustomerId = created.CustomerId,
            Amount = created.Amount,
            Stage = created.Stage,
            Probability = created.Probability,
            WeightedAmount = created.WeightedAmount,
            ExpectedCloseDate = created.ExpectedCloseDate,
            Status = created.Status,
            CreatedDate = created.CreatedDate,
            Notes = created.Notes
        };

        return StatusCode(StatusCodes.Status201Created, resultDto);
    }
}
