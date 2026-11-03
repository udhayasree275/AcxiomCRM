using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class FollowUp
{
    [Key]
    public int FollowUpId { get; set; }

    public int? CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    public int? LeadId { get; set; }

    [ForeignKey(nameof(LeadId))]
    public virtual Lead? Lead { get; set; }

    [Required(ErrorMessage = "Follow-up Date is required.")]
    public DateTime FollowUpDate { get; set; } = DateTime.UtcNow.AddDays(1);

    [Required(ErrorMessage = "Follow-up Type is required.")]
    [StringLength(30)]
    public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(150)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Remarks are required.")]
    [StringLength(1000)]
    public string Remarks { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

    [Required(ErrorMessage = "Assigned User is required.")]
    public string AssignedTo { get; set; } = string.Empty;

    [ForeignKey(nameof(AssignedTo))]
    public virtual ApplicationUser? AssignedUser { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedDate { get; set; }
}
