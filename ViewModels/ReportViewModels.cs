using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels;

public class ReportFilterViewModel
{
    public string ReportType { get; set; } = "pipeline"; // customer, lead, followup, opportunity, pipeline, conversion, useractivity, audit
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Status { get; set; }
    public string? OwnerId { get; set; }
    public List<SelectListItem> SalesExecutives { get; set; } = new();
}

public class PipelineReportItem
{
    public string Stage { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class ConversionReportViewModel
{
    public int TotalLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public int LostLeads { get; set; }
    public int ActiveLeads { get; set; }
    public decimal LeadConversionRate { get; set; }

    public int TotalOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public int ActiveOpportunities { get; set; }
    public decimal OpportunityWinRate { get; set; }
    public decimal WonRevenue { get; set; }
}

public class UserActivityReportItem
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int LeadsCount { get; set; }
    public int OpportunitiesCount { get; set; }
    public int FollowUpsCount { get; set; }
    public int ActivitiesCount { get; set; }
    public int LoginsCount { get; set; }
}
