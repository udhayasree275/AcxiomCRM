using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels;

public class OpportunityListViewModel
{
    public List<Opportunity> Opportunities { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? StageFilter { get; set; }
    public string? StatusFilter { get; set; }
    public string? OwnerFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public List<SelectListItem> Customers { get; set; } = new();
    public List<SelectListItem> SalesExecutives { get; set; } = new();
}

public class OpportunityEditViewModel
{
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer is required.")]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Display(Name = "Originating Lead (Optional)")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Opportunity Amount is required.")]
    [Range(0.01, 1000000000.0, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Display(Name = "Amount ($)")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Stage is required.")]
    public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    [Display(Name = "Probability (%)")]
    public int Probability { get; set; } = 25;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.UtcNow.AddDays(30);

    [Required]
    public string Status { get; set; } = "Open"; // Open, Won, Lost

    [Display(Name = "Owner / Sales Executive")]
    public string? AssignedTo { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public List<SelectListItem> Customers { get; set; } = new();
    public List<SelectListItem> Leads { get; set; } = new();
    public List<SelectListItem> SalesExecutives { get; set; } = new();
}
