using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Lead
{
    [Key]
    public int LeadId { get; set; }

    [Required]
    [StringLength(20)]
    public string LeadCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
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
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lead Source is required.")]
    [StringLength(50)]
    public string Source { get; set; } = "Website"; // Website, Referral, Cold Call, Advertisement, Partner, Other

    [Required(ErrorMessage = "Lead Status is required.")]
    [StringLength(30)]
    public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

    [Required]
    [StringLength(20)]
    public string Priority { get; set; } = "Medium"; // Low, Medium, High

    [Range(0.01, 1000000000.0, ErrorMessage = "Expected Value must be greater than 0.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal ExpectedValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public string? AssignedTo { get; set; }

    [ForeignKey(nameof(AssignedTo))]
    public virtual ApplicationUser? AssignedUser { get; set; }

    public int? ConvertedCustomerId { get; set; }

    [ForeignKey(nameof(ConvertedCustomerId))]
    public virtual Customer? ConvertedCustomer { get; set; }

    public int? ConvertedOpportunityId { get; set; }

    [ForeignKey(nameof(ConvertedOpportunityId))]
    public virtual Opportunity? ConvertedOpportunity { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
