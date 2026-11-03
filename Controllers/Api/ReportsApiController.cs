using System.Security.Claims;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ReportsApiController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsApiController(IReportService reportService)
    {
        _reportService = reportService;
    }

    private (string UserId, string UserRole) GetUserContext()
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        string role = User.FindFirstValue(ClaimTypes.Role) ?? AppRoles.SalesExecutive;
        return (userId, role);
    }

    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipelineReport([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var (userId, role) = GetUserContext();
        var items = await _reportService.GetPipelineReportAsync(userId, role, fromDate, toDate);

        var response = new PipelineReportDto
        {
            TotalPipelineValue = items.Sum(i => i.TotalAmount),
            WeightedPipelineValue = items.Sum(i => i.WeightedAmount),
            StageSummary = items
        };

        return Ok(response);
    }
}
