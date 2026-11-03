using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Activity
{
    [Key]
    public int ActivityId { get; set; }

    [Required(ErrorMessage = "Activity Type is required.")]
    [StringLength(30)]
    public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
    public string Subject { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Activity Date is required.")]
    public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

    public int? CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }

    [ForeignKey(nameof(LeadId))]
    public virtual Lead? Lead { get; set; }

    [Required(ErrorMessage = "Assigned User is required.")]
    public string AssignedTo { get; set; } = string.Empty;

    [ForeignKey(nameof(AssignedTo))]
    public virtual ApplicationUser? AssignedUser { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Completed"; // Planned, In Progress, Completed, Cancelled

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
