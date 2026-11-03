using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardDataAsync(string userId, string userName, string userRole, string dateFilter, DateTime? customStart, DateTime? customEnd);
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(string userId, string userName, string userRole, string dateFilter, DateTime? customStart, DateTime? customEnd)
    {
        var model = new DashboardViewModel
        {
            CurrentUserRole = userRole,
            CurrentUserName = userName,
            DateFilter = dateFilter,
            StartDate = customStart,
            EndDate = customEnd
        };

        // Determine date bounds
        DateTime now = DateTime.UtcNow;
        DateTime? startDate = null;
        DateTime? endDate = null;

        switch (dateFilter.ToLower())
        {
            case "today":
                startDate = now.Date;
                endDate = now.Date.AddDays(1).AddTicks(-1);
                break;
            case "thisweek":
                int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                startDate = now.Date.AddDays(-1 * diff);
                endDate = startDate.Value.AddDays(7).AddTicks(-1);
                break;
            case "thismonth":
                startDate = new DateTime(now.Year, now.Month, 1);
                endDate = startDate.Value.AddMonths(1).AddTicks(-1);
                break;
            case "custom":
                startDate = customStart?.Date;
                endDate = customEnd?.Date.AddDays(1).AddTicks(-1);
                break;
            case "alltime":
            default:
                startDate = null;
                endDate = null;
                break;
        }

        // Base queries scoped by role
        var customersQuery = _context.Customers.AsQueryable();
        var leadsQuery = _context.Leads.AsQueryable();
        var oppsQuery = _context.Opportunities.Include(o => o.Customer).AsQueryable();
        var followUpsQuery = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).AsQueryable();
        var activitiesQuery = _context.Activities.Include(a => a.Customer).Include(a => a.Lead).AsQueryable();

        if (userRole == AppRoles.SalesExecutive)
        {
            customersQuery = customersQuery.Where(c => c.OwnerId == userId);
            leadsQuery = leadsQuery.Where(l => l.AssignedTo == userId);
            oppsQuery = oppsQuery.Where(o => o.AssignedTo == userId);
            followUpsQuery = followUpsQuery.Where(f => f.AssignedTo == userId);
            activitiesQuery = activitiesQuery.Where(a => a.AssignedTo == userId);
        }

        // Apply date filtering to queries if requested
        if (startDate.HasValue)
        {
            customersQuery = customersQuery.Where(c => c.CreatedDate >= startDate.Value);
            leadsQuery = leadsQuery.Where(l => l.CreatedDate >= startDate.Value);
            oppsQuery = oppsQuery.Where(o => o.CreatedDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            customersQuery = customersQuery.Where(c => c.CreatedDate <= endDate.Value);
            leadsQuery = leadsQuery.Where(l => l.CreatedDate <= endDate.Value);
            oppsQuery = oppsQuery.Where(o => o.CreatedDate <= endDate.Value);
        }

        // Compute KPIs
        model.TotalCustomers = await customersQuery.CountAsync();
        model.TotalLeads = await leadsQuery.CountAsync();
        model.OpenLeads = await leadsQuery.CountAsync(l => l.Status != "Converted" && l.Status != "Lost");

        model.TotalOpportunities = await oppsQuery.CountAsync();
        model.OpenOpportunities = await oppsQuery.CountAsync(o => o.Status == "Open");
        model.WonOpportunities = await oppsQuery.CountAsync(o => o.Status == "Won");
        model.LostOpportunities = await oppsQuery.CountAsync(o => o.Status == "Lost");

        var openOpps = await oppsQuery.Where(o => o.Status == "Open").ToListAsync();
        model.TotalPipelineValue = openOpps.Sum(o => o.Amount);
        model.WeightedPipelineValue = openOpps.Sum(o => o.WeightedAmount);

        // Chart 1: Lead Status Breakdown
        string[] leadStatuses = ["New", "Contacted", "Qualified", "Lost", "Converted"];
        var leadStatusGroups = await leadsQuery
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var status in leadStatuses)
        {
            model.LeadStatusLabels.Add(status);
            var match = leadStatusGroups.FirstOrDefault(g => g.Status == status);
            model.LeadStatusCounts.Add(match?.Count ?? 0);
        }

        // Chart 2: Opportunity Pipeline by Stage
        string[] pipelineStages = ["Qualification", "Proposal", "Negotiation", "Won", "Lost"];
        var oppStageData = await oppsQuery
            .Select(o => new { o.Stage, o.Amount })
            .ToListAsync();

        var stageGroups = oppStageData
            .GroupBy(o => o.Stage)
            .Select(g => new { Stage = g.Key, Count = g.Count(), Amount = g.Sum(x => x.Amount) })
            .ToList();

        foreach (var stage in pipelineStages)
        {
            model.PipelineStageLabels.Add(stage);
            var match = stageGroups.FirstOrDefault(g => g.Stage == stage);
            model.PipelineStageCounts.Add(match?.Count ?? 0);
            model.PipelineStageAmounts.Add(match?.Amount ?? 0);
        }

        // Chart 3: Monthly Sales / Outcomes (last 6 months)
        var sixMonthsAgo = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
        var wonOppsInPeriod = await oppsQuery
            .Where(o => o.Status == "Won" && o.CreatedDate >= sixMonthsAgo)
            .Select(o => new { o.CreatedDate, o.Amount })
            .ToListAsync();

        for (int i = 5; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            var mStart = new DateTime(monthDate.Year, monthDate.Month, 1);
            var mEnd = mStart.AddMonths(1).AddTicks(-1);
            string monthLabel = monthDate.ToString("MMM yyyy");

            var wonAmountInMonth = wonOppsInPeriod
                .Where(o => o.CreatedDate >= mStart && o.CreatedDate <= mEnd)
                .Sum(o => o.Amount);

            model.MonthlySalesLabels.Add(monthLabel);
            model.MonthlySalesAmounts.Add(wonAmountInMonth);
        }

        // Upcoming and Overdue Follow-ups
        var today = DateTime.UtcNow.Date;
        model.UpcomingFollowUps = await followUpsQuery
            .Where(f => f.Status == "Planned" && f.FollowUpDate >= today)
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToListAsync();

        model.OverdueFollowUps = await followUpsQuery
            .Where(f => (f.Status == "Planned" || f.Status == "Missed") && f.FollowUpDate < today)
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToListAsync();

        // Recent Activities
        model.RecentActivities = await activitiesQuery
            .OrderByDescending(a => a.ActivityDate)
            .Take(5)
            .ToListAsync();

        return model;
    }
}
