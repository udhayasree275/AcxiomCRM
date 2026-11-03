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
public class ReportsController : Controller
{
    private readonly IReportService _reportService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(IReportService reportService, UserManager<ApplicationUser> userManager)
    {
        _reportService = reportService;
        _userManager = userManager;
    }

    private async Task<(string UserId, string UserRole)> GetCurrentUserInfoAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        string role = AppRoles.SalesExecutive;
        if (user != null)
        {
            if (await _userManager.IsInRoleAsync(user, AppRoles.Admin)) role = AppRoles.Admin;
            else if (await _userManager.IsInRoleAsync(user, AppRoles.Manager)) role = AppRoles.Manager;
        }
        return (user?.Id ?? "", role);
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

    public async Task<IActionResult> Index(
        string type = "pipeline",
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? status = null,
        string? ownerId = null)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();

        var filter = new ReportFilterViewModel
        {
            ReportType = type.ToLower(),
            FromDate = fromDate,
            ToDate = toDate,
            Status = status,
            OwnerId = ownerId,
            SalesExecutives = await GetSalesExecutivesListAsync()
        };

        ViewBag.Filter = filter;
        ViewBag.UserRole = role;

        switch (filter.ReportType)
        {
            case "customer":
                var customers = await _reportService.GetCustomerReportAsync(userId, role, fromDate, toDate, status, ownerId);
                return View("CustomerReport", customers);

            case "lead":
                var leads = await _reportService.GetLeadReportAsync(userId, role, fromDate, toDate, status, null, ownerId);
                return View("LeadReport", leads);

            case "followup":
                var followUps = await _reportService.GetFollowUpReportAsync(userId, role, fromDate, toDate, status, null, ownerId);
                return View("FollowUpReport", followUps);

            case "opportunity":
                var opps = await _reportService.GetOpportunityReportAsync(userId, role, fromDate, toDate, null, status, ownerId);
                return View("OpportunityReport", opps);

            case "conversion":
                var conversion = await _reportService.GetConversionReportAsync(userId, role, fromDate, toDate);
                return View("ConversionReport", conversion);

            case "useractivity":
                if (role == AppRoles.SalesExecutive) return Forbid();
                var activity = await _reportService.GetUserActivityReportAsync(fromDate, toDate);
                return View("UserActivityReport", activity);

            case "audit":
                if (role == AppRoles.SalesExecutive) return Forbid();
                var audit = await _reportService.GetAuditReportAsync(fromDate, toDate, null, null);
                return View("AuditReport", audit);

            case "pipeline":
            default:
                var pipeline = await _reportService.GetPipelineReportAsync(userId, role, fromDate, toDate);
                return View("PipelineReport", pipeline);
        }
    }

    public async Task<IActionResult> ExportCsv(
        string type = "pipeline",
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? status = null,
        string? ownerId = null)
    {
        var (userId, role) = await GetCurrentUserInfoAsync();

        switch (type.ToLower())
        {
            case "customer":
                var customers = await _reportService.GetCustomerReportAsync(userId, role, fromDate, toDate, status, ownerId);
                var cCsv = _reportService.ExportToCsv(
                    customers,
                    c => [c.CustomerCode, c.CustomerName, c.Email, c.Phone, c.CompanyName, c.Status, c.Owner?.FullName ?? "None", c.CreatedDate.ToString("yyyy-MM-dd")],
                    ["Code", "Name", "Email", "Phone", "Company", "Status", "Owner", "Created Date"]
                );
                return File(cCsv, "text/csv", $"Customer_Report_{DateTime.UtcNow:yyyyMMdd}.csv");

            case "lead":
                var leads = await _reportService.GetLeadReportAsync(userId, role, fromDate, toDate, status, null, ownerId);
                var lCsv = _reportService.ExportToCsv(
                    leads,
                    l => [l.LeadCode, l.LeadName, l.Email, l.Phone, l.CompanyName, l.Source, l.Status, l.Priority, l.ExpectedValue.ToString("F2"), l.AssignedUser?.FullName ?? "None", l.CreatedDate.ToString("yyyy-MM-dd")],
                    ["Code", "Name", "Email", "Phone", "Company", "Source", "Status", "Priority", "Expected Value", "Assigned To", "Created Date"]
                );
                return File(lCsv, "text/csv", $"Lead_Report_{DateTime.UtcNow:yyyyMMdd}.csv");

            case "followup":
                var followUps = await _reportService.GetFollowUpReportAsync(userId, role, fromDate, toDate, status, null, ownerId);
                var fuCsv = _reportService.ExportToCsv(
                    followUps,
                    f => [f.FollowUpDate.ToString("yyyy-MM-dd HH:mm"), f.FollowUpType, f.Customer?.CustomerName ?? f.Lead?.LeadName ?? "None", f.Remarks ?? "", f.Status, f.AssignedUser?.FullName ?? "None"],
                    ["Follow-Up Date", "Type", "Contact / Lead", "Remarks", "Status", "Assigned To"]
                );
                return File(fuCsv, "text/csv", $"FollowUp_Report_{DateTime.UtcNow:yyyyMMdd}.csv");

            case "opportunity":
                var opps = await _reportService.GetOpportunityReportAsync(userId, role, fromDate, toDate, null, status, ownerId);
                var oCsv = _reportService.ExportToCsv(
                    opps,
                    o => [o.OpportunityName, o.Customer?.CustomerName ?? "None", o.Stage, o.Amount.ToString("F2"), o.Probability.ToString(), o.WeightedAmount.ToString("F2"), o.ExpectedCloseDate.ToString("yyyy-MM-dd"), o.AssignedUser?.FullName ?? "None"],
                    ["Opportunity", "Customer", "Stage", "Amount", "Probability (%)", "Weighted Amount", "Expected Close", "Owner"]
                );
                return File(oCsv, "text/csv", $"Opportunity_Report_{DateTime.UtcNow:yyyyMMdd}.csv");

            case "conversion":
                var conversion = await _reportService.GetConversionReportAsync(userId, role, fromDate, toDate);
                var convRows = new List<string[]>
                {
                    new[] { "Total Leads", conversion.TotalLeads.ToString() },
                    new[] { "Converted Leads", conversion.ConvertedLeads.ToString() },
                    new[] { "Lead Conversion Rate (%)", conversion.LeadConversionRate.ToString("F2") + "%" },
                    new[] { "Total Opportunities", conversion.TotalOpportunities.ToString() },
                    new[] { "Won Opportunities", conversion.WonOpportunities.ToString() },
                    new[] { "Opportunity Win Rate (%)", conversion.OpportunityWinRate.ToString("F2") + "%" },
                    new[] { "Won Revenue ($)", conversion.WonRevenue.ToString("F2") }
                };
                var convCsv = _reportService.ExportToCsv(
                    convRows,
                    r => r,
                    ["Metric", "Value"]
                );
                return File(convCsv, "text/csv", $"Conversion_Report_{DateTime.UtcNow:yyyyMMdd}.csv");

            case "useractivity":
                if (role == AppRoles.SalesExecutive) return Forbid();
                var activity = await _reportService.GetUserActivityReportAsync(fromDate, toDate);
                var actCsv = _reportService.ExportToCsv(
                    activity,
                    a => [a.UserName, a.Role, a.LeadsCount.ToString(), a.OpportunitiesCount.ToString(), a.FollowUpsCount.ToString(), a.ActivitiesCount.ToString(), a.LoginsCount.ToString()],
                    ["User Name", "Role", "Leads Managed", "Opportunities", "Follow-Ups", "Activities", "Logins"]
                );
                return File(actCsv, "text/csv", $"User_Activity_Report_{DateTime.UtcNow:yyyyMMdd}.csv");

            case "audit":
                if (role == AppRoles.SalesExecutive) return Forbid();
                var audit = await _reportService.GetAuditReportAsync(fromDate, toDate, null, null);
                var audCsv = _reportService.ExportToCsv(
                    audit,
                    a => [a.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"), a.UserEmail ?? "System", a.Action, a.EntityName, a.RecordId ?? "", a.IpAddress ?? ""],
                    ["Timestamp", "User Email", "Action", "Entity", "Record Id", "IP Address"]
                );
                return File(audCsv, "text/csv", $"Audit_Report_{DateTime.UtcNow:yyyyMMdd}.csv");

            case "pipeline":
            default:
                var pipeline = await _reportService.GetPipelineReportAsync(userId, role, fromDate, toDate);
                var pCsv = _reportService.ExportToCsv(
                    pipeline,
                    p => [p.Stage, p.OwnerName, p.Count.ToString(), p.TotalAmount.ToString("F2"), p.WeightedAmount.ToString("F2")],
                    ["Stage", "Owner", "Deal Count", "Total Amount", "Weighted Pipeline Amount"]
                );
                return File(pCsv, "text/csv", $"Pipeline_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
        }
    }
}
