using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.ViewModels;

public class CustomerListViewModel
{
    public List<Customer> Customers { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public string? OwnerFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public List<SelectListItem> SalesExecutives { get; set; } = new();
}

public class CustomerEditViewModel
{
    public int CustomerId { get; set; }

    [Display(Name = "Customer Code")]
    public string CustomerCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer Name is required.")]
    [StringLength(100, ErrorMessage = "Customer Name cannot exceed 100 characters.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

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

    [StringLength(250)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? City { get; set; }

    [StringLength(50)]
    public string? State { get; set; }

    [Required]
    public string Status { get; set; } = "Active";

    [Display(Name = "Assigned Sales Executive")]
    public string? OwnerId { get; set; }

    public List<SelectListItem> SalesExecutives { get; set; } = new();
}

public class CustomerDetailViewModel
{
    public Customer Customer { get; set; } = null!;
    public List<Opportunity> Opportunities { get; set; } = new();
    public List<FollowUp> FollowUps { get; set; } = new();
    public List<Activity> Activities { get; set; } = new();
}
