using System.Text;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IReportService
{
    Task<List<Customer>> GetCustomerReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, string? status, string? ownerId);
    Task<List<Lead>> GetLeadReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, string? status, string? source, string? ownerId);
    Task<List<FollowUp>> GetFollowUpReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, string? status, string? type, string? assignedTo);
    Task<List<Opportunity>> GetOpportunityReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, string? stage, string? status, string? ownerId);
    Task<List<PipelineReportItem>> GetPipelineReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate);
    Task<ConversionReportViewModel> GetConversionReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate);
    Task<List<UserActivityReportItem>> GetUserActivityReportAsync(DateTime? fromDate, DateTime? toDate);
    Task<List<AuditLog>> GetAuditReportAsync(DateTime? fromDate, DateTime? toDate, string? action, string? entityName);
    byte[] ExportToCsv<T>(IEnumerable<T> data, Func<T, string[]> rowSelector, string[] headers);
}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Customer>> GetCustomerReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, string? status, string? ownerId)
    {
        var query = _context.Customers.Include(c => c.Owner).AsQueryable();

        if (userRole == AppRoles.SalesExecutive)
        {
            query = query.Where(c => c.OwnerId == userId);
        }
        else if (!string.IsNullOrWhiteSpace(ownerId))
        {
            query = query.Where(c => c.OwnerId == ownerId);
        }

        if (fromDate.HasValue) query = query.Where(c => c.CreatedDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(c => c.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);

        return await query.OrderByDescending(c => c.CreatedDate).ToListAsync();
    }

    public async Task<List<Lead>> GetLeadReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, string? status, string? source, string? ownerId)
    {
        var query = _context.Leads.Include(l => l.AssignedUser).Include(l => l.ConvertedCustomer).AsQueryable();

        if (userRole == AppRoles.SalesExecutive)
        {
            query = query.Where(l => l.AssignedTo == userId);
        }
        else if (!string.IsNullOrWhiteSpace(ownerId))
        {
            query = query.Where(l => l.AssignedTo == ownerId);
        }

        if (fromDate.HasValue) query = query.Where(l => l.CreatedDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(l => l.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(l => l.Status == status);
        if (!string.IsNullOrWhiteSpace(source)) query = query.Where(l => l.Source == source);

        return await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
    }

    public async Task<List<FollowUp>> GetFollowUpReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, string? status, string? type, string? assignedTo)
    {
        var query = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).Include(f => f.AssignedUser).AsQueryable();

        if (userRole == AppRoles.SalesExecutive)
        {
            query = query.Where(f => f.AssignedTo == userId);
        }
        else if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            query = query.Where(f => f.AssignedTo == assignedTo);
        }

        if (fromDate.HasValue) query = query.Where(f => f.FollowUpDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(f => f.FollowUpDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(f => f.Status == status);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(f => f.FollowUpType == type);

        return await query.OrderByDescending(f => f.FollowUpDate).ToListAsync();
    }

    public async Task<List<Opportunity>> GetOpportunityReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate, string? stage, string? status, string? ownerId)
    {
        var query = _context.Opportunities.Include(o => o.Customer).Include(o => o.AssignedUser).AsQueryable();

        if (userRole == AppRoles.SalesExecutive)
        {
            query = query.Where(o => o.AssignedTo == userId);
        }
        else if (!string.IsNullOrWhiteSpace(ownerId))
        {
            query = query.Where(o => o.AssignedTo == ownerId);
        }

        if (fromDate.HasValue) query = query.Where(o => o.CreatedDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(o => o.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
        if (!string.IsNullOrWhiteSpace(stage)) query = query.Where(o => o.Stage == stage);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);

        return await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
    }

    public async Task<List<PipelineReportItem>> GetPipelineReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Opportunities.Include(o => o.AssignedUser).AsQueryable();

        if (userRole == AppRoles.SalesExecutive)
        {
            query = query.Where(o => o.AssignedTo == userId);
        }

        if (fromDate.HasValue) query = query.Where(o => o.CreatedDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(o => o.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));

        var opps = await query.ToListAsync();

        var result = opps
            .GroupBy(o => new { o.Stage, Owner = o.AssignedUser?.FullName ?? "Unassigned" })
            .Select(g => new PipelineReportItem
            {
                Stage = g.Key.Stage,
                OwnerName = g.Key.Owner,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.WeightedAmount)
            })
            .OrderBy(r => r.Stage)
            .ThenBy(r => r.OwnerName)
            .ToList();

        return result;
    }

    public async Task<ConversionReportViewModel> GetConversionReportAsync(string? userId, string userRole, DateTime? fromDate, DateTime? toDate)
    {
        var leadsQuery = _context.Leads.AsQueryable();
        var oppsQuery = _context.Opportunities.AsQueryable();

        if (userRole == AppRoles.SalesExecutive)
        {
            leadsQuery = leadsQuery.Where(l => l.AssignedTo == userId);
            oppsQuery = oppsQuery.Where(o => o.AssignedTo == userId);
        }

        if (fromDate.HasValue)
        {
            leadsQuery = leadsQuery.Where(l => l.CreatedDate >= fromDate.Value.Date);
            oppsQuery = oppsQuery.Where(o => o.CreatedDate >= fromDate.Value.Date);
        }
        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            leadsQuery = leadsQuery.Where(l => l.CreatedDate <= endOfDay);
            oppsQuery = oppsQuery.Where(o => o.CreatedDate <= endOfDay);
        }

        int totalLeads = await leadsQuery.CountAsync();
        int convertedLeads = await leadsQuery.CountAsync(l => l.Status == "Converted");
        int lostLeads = await leadsQuery.CountAsync(l => l.Status == "Lost" || l.Status == "Unqualified");
        int activeLeads = totalLeads - convertedLeads - lostLeads;

        int totalOpps = await oppsQuery.CountAsync();
        int wonOpps = await oppsQuery.CountAsync(o => o.Status == "Won");
        int lostOpps = await oppsQuery.CountAsync(o => o.Status == "Lost");
        int activeOpps = totalOpps - wonOpps - lostOpps;
        var wonAmounts = await oppsQuery.Where(o => o.Status == "Won").Select(o => o.Amount).ToListAsync();
        decimal wonRevenue = wonAmounts.Sum();

        decimal leadConvRate = totalLeads > 0 ? Math.Round((decimal)convertedLeads / totalLeads * 100m, 2) : 0m;
        decimal oppWinRate = totalOpps > 0 ? Math.Round((decimal)wonOpps / totalOpps * 100m, 2) : 0m;

        return new ConversionReportViewModel
        {
            TotalLeads = totalLeads,
            ConvertedLeads = convertedLeads,
            LostLeads = lostLeads,
            ActiveLeads = activeLeads,
            LeadConversionRate = leadConvRate,
            TotalOpportunities = totalOpps,
            WonOpportunities = wonOpps,
            LostOpportunities = lostOpps,
            ActiveOpportunities = activeOpps,
            OpportunityWinRate = oppWinRate,
            WonRevenue = wonRevenue
        };
    }

    public async Task<List<UserActivityReportItem>> GetUserActivityReportAsync(DateTime? fromDate, DateTime? toDate)
    {
        var users = await _context.Users.ToListAsync();
        var report = new List<UserActivityReportItem>();

        foreach (var u in users)
        {
            var leadsQ = _context.Leads.Where(l => l.AssignedTo == u.Id);
            var oppsQ = _context.Opportunities.Where(o => o.AssignedTo == u.Id);
            var fuQ = _context.FollowUps.Where(f => f.AssignedTo == u.Id);
            var actQ = _context.Activities.Where(a => a.AssignedTo == u.Id);
            var logQ = _context.AuditLogs.Where(l => l.UserId == u.Id && l.Action == "Login");

            if (fromDate.HasValue)
            {
                leadsQ = leadsQ.Where(l => l.CreatedDate >= fromDate.Value.Date);
                oppsQ = oppsQ.Where(o => o.CreatedDate >= fromDate.Value.Date);
                fuQ = fuQ.Where(f => f.CreatedDate >= fromDate.Value.Date);
                actQ = actQ.Where(a => a.CreatedDate >= fromDate.Value.Date);
                logQ = logQ.Where(l => l.CreatedDate >= fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                var end = toDate.Value.Date.AddDays(1).AddTicks(-1);
                leadsQ = leadsQ.Where(l => l.CreatedDate <= end);
                oppsQ = oppsQ.Where(o => o.CreatedDate <= end);
                fuQ = fuQ.Where(f => f.CreatedDate <= end);
                actQ = actQ.Where(a => a.CreatedDate <= end);
                logQ = logQ.Where(l => l.CreatedDate <= end);
            }

            report.Add(new UserActivityReportItem
            {
                UserId = u.Id,
                UserName = u.FullName,
                Role = u.RoleName,
                LeadsCount = await leadsQ.CountAsync(),
                OpportunitiesCount = await oppsQ.CountAsync(),
                FollowUpsCount = await fuQ.CountAsync(),
                ActivitiesCount = await actQ.CountAsync(),
                LoginsCount = await logQ.CountAsync()
            });
        }

        return report.OrderByDescending(r => r.ActivitiesCount + r.OpportunitiesCount).ToList();
    }

    public async Task<List<AuditLog>> GetAuditReportAsync(DateTime? fromDate, DateTime? toDate, string? action, string? entityName)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (fromDate.HasValue) query = query.Where(a => a.CreatedDate >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(a => a.CreatedDate <= toDate.Value.Date.AddDays(1).AddTicks(-1));
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(entityName)) query = query.Where(a => a.EntityName == entityName);

        return await query.OrderByDescending(a => a.CreatedDate).Take(200).ToListAsync();
    }

    public byte[] ExportToCsv<T>(IEnumerable<T> data, Func<T, string[]> rowSelector, string[] headers)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

        foreach (var item in data)
        {
            var row = rowSelector(item);
            sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string? field)
    {
        if (string.IsNullOrEmpty(field)) return "\"\"";
        return $"\"{field.Replace("\"", "\"\"")}\"";
    }
}
