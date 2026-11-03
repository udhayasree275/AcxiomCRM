using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels;

public class AuditLogListViewModel
{
    public List<AuditLog> AuditLogs { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? EntityFilter { get; set; }
    public string? ActionFilter { get; set; }
    public string? UserFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public List<SelectListItem> Users { get; set; } = new();
}
