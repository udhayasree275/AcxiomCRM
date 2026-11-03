using Microsoft.AspNetCore.Identity;

namespace AcxiomCRM.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = "SalesExecutive";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Customer> AssignedCustomers { get; set; } = new List<Customer>();
    public virtual ICollection<Lead> AssignedLeads { get; set; } = new List<Lead>();
    public virtual ICollection<Opportunity> AssignedOpportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> AssignedFollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> AssignedActivities { get; set; } = new List<Activity>();
}

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string SalesExecutive = "SalesExecutive";
}
