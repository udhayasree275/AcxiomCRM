using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels;

public class LeadListViewModel
{
    public List<Lead> Leads { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public string? PriorityFilter { get; set; }
    public string? AssignedFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public List<SelectListItem> SalesExecutives { get; set; } = new();
}

public class LeadEditViewModel
{
    public int LeadId { get; set; }

    [Display(Name = "Lead Code")]
    public string LeadCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit phone number.")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company Name is required.")]
    [StringLength(100)]
    [Display(Name = "Company Name")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lead Source is required.")]
    public string Source { get; set; } = "Website";

    [Required(ErrorMessage = "Status is required.")]
    public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

    [Required(ErrorMessage = "Priority is required.")]
    public string Priority { get; set; } = "Medium";

    [Required(ErrorMessage = "Expected Value is required.")]
    [Range(0.01, 1000000000.0, ErrorMessage = "Expected Value must be greater than 0.")]
    [Display(Name = "Expected Value ($)")]
    public decimal ExpectedValue { get; set; }

    [Display(Name = "Assigned Sales Executive")]
    public string? AssignedTo { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public List<SelectListItem> SalesExecutives { get; set; } = new();
}

public class LeadDetailViewModel
{
    public Lead Lead { get; set; } = null!;
    public List<FollowUp> FollowUps { get; set; } = new();
    public List<Activity> Activities { get; set; } = new();
}

public class LeadConvertViewModel
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;

    [Required]
    public string LeadName { get; set; } = string.Empty;

    [Required]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    [Display(Name = "Also Create Opportunity?")]
    public bool CreateOpportunity { get; set; } = true;

    [Display(Name = "Opportunity Name")]
    public string? OpportunityName { get; set; }

    [Display(Name = "Opportunity Amount ($)")]
    [Range(0.01, 1000000000.0, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal OpportunityAmount { get; set; }

    public string OpportunityStage { get; set; } = "Qualification";

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int OpportunityProbability { get; set; } = 25;

    [DataType(DataType.Date)]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);
}
