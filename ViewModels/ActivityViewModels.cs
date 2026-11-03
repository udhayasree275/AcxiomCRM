using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels;

public class ActivityListViewModel
{
    public List<Activity> Activities { get; set; } = new();
    public string? TypeFilter { get; set; }
    public string? StatusFilter { get; set; }
    public string? AssignedFilter { get; set; }
    public string? SearchTerm { get; set; }
    public List<SelectListItem> Users { get; set; } = new();
}

public class ActivityEditViewModel
{
    public int ActivityId { get; set; }

    [Required(ErrorMessage = "Activity Type is required.")]
    [Display(Name = "Activity Type")]
    public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Activity Date is required.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Activity Date")]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Related Customer (Optional)")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead (Optional)")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Assigned User is required.")]
    [Display(Name = "Assigned To")]
    public string AssignedTo { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "Completed"; // Planned, In Progress, Completed, Cancelled

    public List<SelectListItem> Customers { get; set; } = new();
    public List<SelectListItem> Leads { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();
}
