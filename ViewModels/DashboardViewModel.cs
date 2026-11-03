using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class DashboardViewModel
{
    // KPIs
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }

    // Date Filters: "Today", "ThisWeek", "ThisMonth", "Custom", "AllTime"
    public string DateFilter { get; set; } = "ThisMonth";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // User context
    public string CurrentUserRole { get; set; } = string.Empty;
    public string CurrentUserName { get; set; } = string.Empty;

    // Chart.js data: Lead Status
    public List<string> LeadStatusLabels { get; set; } = new();
    public List<int> LeadStatusCounts { get; set; } = new();

    // Chart.js data: Opportunity Pipeline Stages
    public List<string> PipelineStageLabels { get; set; } = new();
    public List<int> PipelineStageCounts { get; set; } = new();
    public List<decimal> PipelineStageAmounts { get; set; } = new();

    // Chart.js data: Monthly Sales trend
    public List<string> MonthlySalesLabels { get; set; } = new();
    public List<decimal> MonthlySalesAmounts { get; set; } = new();

    // Upcoming / Overdue Follow-ups
    public List<FollowUp> UpcomingFollowUps { get; set; } = new();
    public List<FollowUp> OverdueFollowUps { get; set; } = new();

    // Recent Activities
    public List<Activity> RecentActivities { get; set; } = new();
}
