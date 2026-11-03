using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels;

// Authentication DTOs
public class ApiLoginRequestDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class ApiLoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

// Customer DTOs
public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedDate { get; set; }
    public string? OwnerName { get; set; }
}

public class CreateCustomerDto
{
    [Required(ErrorMessage = "Customer Name is required.")]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit phone number.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company Name is required.")]
    [StringLength(100)]
    public string CompanyName { get; set; } = string.Empty;

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = "Active";
    public string? OwnerId { get; set; }
}

public class UpdateCustomerDto : CreateCustomerDto
{
}

// Lead DTOs
public class LeadDto
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public decimal ExpectedValue { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? AssignedUserName { get; set; }
    public string? Notes { get; set; }
}

public class CreateLeadDto
{
    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(100)]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit phone number.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company Name is required.")]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    public string Source { get; set; } = "Website";

    [Required]
    public string Status { get; set; } = "New";

    [Required]
    public string Priority { get; set; } = "Medium";

    [Range(0.01, 1000000000.0, ErrorMessage = "Expected Value must be greater than 0.")]
    public decimal ExpectedValue { get; set; }

    public string? AssignedTo { get; set; }
    public string? Notes { get; set; }
}

// Opportunity DTOs
public class OpportunityDto
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = string.Empty;
    public int Probability { get; set; }
    public decimal WeightedAmount { get; set; }
    public DateTime ExpectedCloseDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string? AssignedUserName { get; set; }
    public string? Notes { get; set; }
}

public class CreateOpportunityDto
{
    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(150)]
    public string OpportunityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer is required.")]
    public int CustomerId { get; set; }

    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, 1000000000.0, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    [Required]
    public string Stage { get; set; } = "Qualification";

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 25;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    public DateTime ExpectedCloseDate { get; set; }

    public string Status { get; set; } = "Open";
    public string? AssignedTo { get; set; }
    public string? Notes { get; set; }
}

// FollowUp DTOs
public class FollowUpDto
{
    public int FollowUpId { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? LeadId { get; set; }
    public string? LeadName { get; set; }
    public DateTime FollowUpDate { get; set; }
    public string FollowUpType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AssignedUserName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateFollowUpDto
{
    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Follow-up Date is required.")]
    public DateTime FollowUpDate { get; set; }

    [Required]
    public string FollowUpType { get; set; } = "Call";

    [Required]
    [StringLength(150)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Remarks { get; set; } = string.Empty;

    public string Status { get; set; } = "Planned";

    [Required]
    public string AssignedTo { get; set; } = string.Empty;
}

// Pipeline Report DTO
public class PipelineReportDto
{
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }
    public List<PipelineReportItem> StageSummary { get; set; } = new();
}
