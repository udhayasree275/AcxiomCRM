using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels;

public class FollowUpListViewModel
{
    public List<FollowUp> FollowUps { get; set; } = new();
    public string? StatusFilter { get; set; }
    public string? TypeFilter { get; set; }
    public string? AssignedFilter { get; set; }
    public string? SearchTerm { get; set; }
    public string ActiveTab { get; set; } = "all"; // all, upcoming, overdue, completed
    public List<SelectListItem> SalesExecutives { get; set; } = new();
}

public class FollowUpEditViewModel
{
    public int FollowUpId { get; set; }

    [Display(Name = "Related Customer (Optional)")]
    public int? CustomerId { get; set; }

    [Display(Name = "Related Lead (Optional)")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Follow-up Date is required.")]
    [DataType(DataType.DateTime)]
    [Display(Name = "Follow-up Date & Time")]
    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);

    [Required(ErrorMessage = "Follow-up Type is required.")]
    [Display(Name = "Follow-up Type")]
    public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(150)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Remarks/Notes are required.")]
    [StringLength(1000)]
    public string Remarks { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

    [Required(ErrorMessage = "Assigned User is required.")]
    [Display(Name = "Assigned To")]
    public string AssignedTo { get; set; } = string.Empty;

    public List<SelectListItem> Customers { get; set; } = new();
    public List<SelectListItem> Leads { get; set; } = new();
    public List<SelectListItem> SalesExecutives { get; set; } = new();
}
