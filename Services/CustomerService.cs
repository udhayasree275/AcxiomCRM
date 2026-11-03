using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface ICustomerService
{
    Task<(List<Customer> Items, int TotalCount)> GetCustomersAsync(string? userId, string userRole, string? search, string? status, string? ownerId, int page, int pageSize);
    Task<Customer?> GetByIdAsync(int id, string? userId, string userRole);
    Task<(bool Success, string? Error, Customer? Customer)> CreateCustomerAsync(Customer customer, string currentUserId, string currentUserEmail, string? ipAddress);
    Task<(bool Success, string? Error)> UpdateCustomerAsync(Customer customer, string currentUserId, string currentUserEmail, string userRole, string? ipAddress);
    Task<(bool Success, string? Error)> DeleteCustomerAsync(int id, string currentUserId, string currentUserEmail, string userRole, string? ipAddress);
    Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null);
    Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null);
}

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public CustomerService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<(List<Customer> Items, int TotalCount)> GetCustomersAsync(string? userId, string userRole, string? search, string? status, string? ownerId, int page, int pageSize)
    {
        var query = _context.Customers.Include(c => c.Owner).AsQueryable();

        // Scope by Role
        if (userRole == AppRoles.SalesExecutive)
        {
            query = query.Where(c => c.OwnerId == userId);
        }
        else if (!string.IsNullOrWhiteSpace(ownerId))
        {
            query = query.Where(c => c.OwnerId == ownerId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(c =>
                c.CustomerName.Contains(search) ||
                c.Email.Contains(search) ||
                c.Phone.Contains(search) ||
                c.CompanyName.Contains(search) ||
                c.CustomerCode.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status == status);
        }

        int totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Customer?> GetByIdAsync(int id, string? userId, string userRole)
    {
        var customer = await _context.Customers
            .Include(c => c.Owner)
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
            .Include(c => c.Activities)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null) return null;

        // Scope Check
        if (userRole == AppRoles.SalesExecutive && customer.OwnerId != userId)
        {
            return null; // Not authorized for this specific customer
        }

        return customer;
    }

    public async Task<(bool Success, string? Error, Customer? Customer)> CreateCustomerAsync(Customer customer, string currentUserId, string currentUserEmail, string? ipAddress)
    {
        // Business Validation: Uniqueness checks
        if (!await IsEmailUniqueAsync(customer.Email))
        {
            return (false, "A customer with this email address already exists.", null);
        }

        if (!await IsPhoneUniqueAsync(customer.Phone))
        {
            return (false, "A customer with this phone number already exists.", null);
        }

        bool duplicateNameCompany = await _context.Customers.AnyAsync(c =>
            c.CustomerName.ToLower() == customer.CustomerName.ToLower() &&
            c.CompanyName.ToLower() == customer.CompanyName.ToLower());

        if (duplicateNameCompany)
        {
            return (false, "A customer with the same name and company already exists.", null);
        }

        // Generate CustomerCode
        int count = await _context.Customers.CountAsync();
        customer.CustomerCode = $"CUST-{1001 + count}";
        customer.CreatedDate = DateTime.UtcNow;
        customer.CreatedBy = currentUserEmail;

        if (string.IsNullOrEmpty(customer.OwnerId))
        {
            customer.OwnerId = currentUserId;
        }

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        // Audit Log
        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Create",
            "Customer",
            customer.CustomerId.ToString(),
            null,
            $"Created Customer {customer.CustomerName} ({customer.CustomerCode})",
            ipAddress);

        return (true, null, customer);
    }

    public async Task<(bool Success, string? Error)> UpdateCustomerAsync(Customer customer, string currentUserId, string currentUserEmail, string userRole, string? ipAddress)
    {
        var existing = await _context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
        if (existing == null)
        {
            return (false, "Customer not found.");
        }

        // Scope Check
        if (userRole == AppRoles.SalesExecutive && existing.OwnerId != currentUserId)
        {
            return (false, "You are not authorized to update this customer.");
        }

        // Business Validation: Uniqueness
        if (!await IsEmailUniqueAsync(customer.Email, customer.CustomerId))
        {
            return (false, "A customer with this email address already exists.");
        }

        if (!await IsPhoneUniqueAsync(customer.Phone, customer.CustomerId))
        {
            return (false, "A customer with this phone number already exists.");
        }

        string oldValue = $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}";

        existing.CustomerName = customer.CustomerName;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.CompanyName = customer.CompanyName;
        existing.Address = customer.Address;
        existing.City = customer.City;
        existing.State = customer.State;
        existing.Status = customer.Status;
        existing.ModifiedDate = DateTime.UtcNow;

        if (userRole != AppRoles.SalesExecutive && !string.IsNullOrEmpty(customer.OwnerId))
        {
            existing.OwnerId = customer.OwnerId;
        }

        await _context.SaveChangesAsync();

        string newValue = $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}";

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Update",
            "Customer",
            existing.CustomerId.ToString(),
            oldValue,
            newValue,
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteCustomerAsync(int id, string currentUserId, string currentUserEmail, string userRole, string? ipAddress)
    {
        var existing = await _context.Customers
            .Include(c => c.Opportunities)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (existing == null)
        {
            return (false, "Customer not found.");
        }

        // Sales Executives cannot delete customers (only Admin/Manager or Deactivate)
        if (userRole == AppRoles.SalesExecutive)
        {
            return (false, "Sales executives do not have permission to delete customers. Please deactivate instead.");
        }

        // If customer has opportunities, deactivate rather than hard delete to preserve financial history
        if (existing.Opportunities.Any())
        {
            existing.Status = "Inactive";
            existing.ModifiedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                currentUserEmail,
                "Deactivate",
                "Customer",
                id.ToString(),
                "Active",
                "Deactivated (contains related opportunities)",
                ipAddress);

            return (true, null);
        }

        _context.Customers.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Delete",
            "Customer",
            id.ToString(),
            $"Customer {existing.CustomerName} ({existing.CustomerCode})",
            null,
            ipAddress);

        return (true, null);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null)
    {
        var query = _context.Customers.Where(c => c.Email.ToLower() == email.Trim().ToLower());
        if (excludeCustomerId.HasValue)
        {
            query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
        }
        return !await query.AnyAsync();
    }

    public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null)
    {
        var query = _context.Customers.Where(c => c.Phone == phone.Trim());
        if (excludeCustomerId.HasValue)
        {
            query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
        }
        return !await query.AnyAsync();
    }
}
