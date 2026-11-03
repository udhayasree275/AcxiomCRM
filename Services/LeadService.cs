using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface ILeadService
{
    Task<(List<Lead> Items, int TotalCount)> GetLeadsAsync(string? userId, string userRole, string? search, string? status, string? priority, string? assignedTo, int page, int pageSize);
    Task<Lead?> GetByIdAsync(int id, string? userId, string userRole);
    Task<(bool Success, string? Error, Lead? Lead)> CreateLeadAsync(Lead lead, string currentUserId, string currentUserEmail, string? ipAddress);
    Task<(bool Success, string? Error)> UpdateLeadAsync(Lead lead, string currentUserId, string currentUserEmail, string userRole, string? ipAddress);
    Task<(bool Success, string? Error)> DeleteLeadAsync(int id, string currentUserId, string currentUserEmail, string userRole, string? ipAddress);
    Task<(bool Success, string? Error, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(LeadConvertViewModel model, string currentUserId, string currentUserEmail, string userRole, string? ipAddress);
}

public class LeadService : ILeadService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ICustomerService _customerService;

    public LeadService(ApplicationDbContext context, IAuditService auditService, ICustomerService customerService)
    {
        _context = context;
        _auditService = auditService;
        _customerService = customerService;
    }

    public async Task<(List<Lead> Items, int TotalCount)> GetLeadsAsync(string? userId, string userRole, string? search, string? status, string? priority, string? assignedTo, int page, int pageSize)
    {
        var query = _context.Leads.Include(l => l.AssignedUser).AsQueryable();

        // Scope by Role
        if (userRole == AppRoles.SalesExecutive)
        {
            query = query.Where(l => l.AssignedTo == userId);
        }
        else if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            query = query.Where(l => l.AssignedTo == assignedTo);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(l =>
                l.LeadName.Contains(search) ||
                l.Email.Contains(search) ||
                l.Phone.Contains(search) ||
                l.CompanyName.Contains(search) ||
                l.LeadCode.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => l.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(priority))
        {
            query = query.Where(l => l.Priority == priority);
        }

        int totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Lead?> GetByIdAsync(int id, string? userId, string userRole)
    {
        var lead = await _context.Leads
            .Include(l => l.AssignedUser)
            .Include(l => l.ConvertedCustomer)
            .Include(l => l.ConvertedOpportunity)
            .Include(l => l.FollowUps)
            .Include(l => l.Activities)
            .FirstOrDefaultAsync(l => l.LeadId == id);

        if (lead == null) return null;

        if (userRole == AppRoles.SalesExecutive && lead.AssignedTo != userId)
        {
            return null;
        }

        return lead;
    }

    public async Task<(bool Success, string? Error, Lead? Lead)> CreateLeadAsync(Lead lead, string currentUserId, string currentUserEmail, string? ipAddress)
    {
        // Business Validation
        if (lead.ExpectedValue <= 0)
        {
            return (false, "Expected Value must be greater than 0.", null);
        }

        string[] validStatuses = ["New", "Contacted", "Qualified", "Unqualified", "Converted", "Lost"];
        if (!validStatuses.Contains(lead.Status))
        {
            return (false, "Invalid lead status provided.", null);
        }

        int count = await _context.Leads.CountAsync();
        lead.LeadCode = $"LEAD-{1001 + count}";
        lead.CreatedDate = DateTime.UtcNow;

        if (string.IsNullOrEmpty(lead.AssignedTo))
        {
            lead.AssignedTo = currentUserId;
        }

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Create",
            "Lead",
            lead.LeadId.ToString(),
            null,
            $"Created Lead {lead.LeadName} ({lead.LeadCode}) with value ${lead.ExpectedValue}",
            ipAddress);

        return (true, null, lead);
    }

    public async Task<(bool Success, string? Error)> UpdateLeadAsync(Lead lead, string currentUserId, string currentUserEmail, string userRole, string? ipAddress)
    {
        var existing = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == lead.LeadId);
        if (existing == null)
        {
            return (false, "Lead not found.");
        }

        if (userRole == AppRoles.SalesExecutive && existing.AssignedTo != currentUserId)
        {
            return (false, "You are not authorized to edit this lead.");
        }

        // Workflow rule: if already Converted, prevent modifying back to New without explicit conversion logic
        if (existing.Status == "Converted" && lead.Status != "Converted")
        {
            return (false, "A Converted lead cannot be reverted to an open status.");
        }

        string oldValue = $"Status: {existing.Status}, Priority: {existing.Priority}, Value: {existing.ExpectedValue}";

        existing.LeadName = lead.LeadName;
        existing.Email = lead.Email;
        existing.Phone = lead.Phone;
        existing.CompanyName = lead.CompanyName;
        existing.Source = lead.Source;
        existing.Status = lead.Status;
        existing.Priority = lead.Priority;
        existing.ExpectedValue = lead.ExpectedValue;
        existing.Notes = lead.Notes;

        if (userRole != AppRoles.SalesExecutive && !string.IsNullOrEmpty(lead.AssignedTo))
        {
            existing.AssignedTo = lead.AssignedTo;
        }

        await _context.SaveChangesAsync();

        string newValue = $"Status: {existing.Status}, Priority: {existing.Priority}, Value: {existing.ExpectedValue}";

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Update",
            "Lead",
            existing.LeadId.ToString(),
            oldValue,
            newValue,
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteLeadAsync(int id, string currentUserId, string currentUserEmail, string userRole, string? ipAddress)
    {
        var existing = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == id);
        if (existing == null)
        {
            return (false, "Lead not found.");
        }

        if (userRole == AppRoles.SalesExecutive)
        {
            return (false, "Sales executives do not have permission to delete leads.");
        }

        _context.Leads.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Delete",
            "Lead",
            id.ToString(),
            $"Lead {existing.LeadName} ({existing.LeadCode})",
            null,
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? Error, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(LeadConvertViewModel model, string currentUserId, string currentUserEmail, string userRole, string? ipAddress)
    {
        var lead = await _context.Leads.FirstOrDefaultAsync(l => l.LeadId == model.LeadId);
        if (lead == null)
        {
            return (false, "Lead not found.", null, null);
        }

        if (lead.Status == "Converted")
        {
            return (false, "This lead has already been converted.", lead.ConvertedCustomerId, lead.ConvertedOpportunityId);
        }

        // 1. Create or link Customer
        var customer = new Customer
        {
            CustomerName = model.CompanyName,
            CompanyName = model.CompanyName,
            Email = model.Email,
            Phone = model.Phone,
            Address = model.Address,
            City = model.City,
            State = model.State,
            Status = "Active",
            OwnerId = lead.AssignedTo ?? currentUserId
        };

        var (custSuccess, custError, createdCustomer) = await _customerService.CreateCustomerAsync(customer, currentUserId, currentUserEmail, ipAddress);
        if (!custSuccess || createdCustomer == null)
        {
            return (false, $"Customer creation failed: {custError}", null, null);
        }

        int? createdOppId = null;

        // 2. Create Opportunity if requested
        if (model.CreateOpportunity)
        {
            if (model.OpportunityAmount <= 0)
            {
                return (false, "Opportunity Amount must be greater than 0.", null, null);
            }

            if (model.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            {
                return (false, "Expected Close Date cannot be in the past.", null, null);
            }

            var opp = new Opportunity
            {
                OpportunityName = string.IsNullOrWhiteSpace(model.OpportunityName) ? $"{lead.CompanyName} - Initial Deal" : model.OpportunityName,
                CustomerId = createdCustomer.CustomerId,
                LeadId = lead.LeadId,
                Amount = model.OpportunityAmount,
                Stage = model.OpportunityStage,
                Probability = model.OpportunityProbability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = "Open",
                CreatedDate = DateTime.UtcNow,
                AssignedTo = lead.AssignedTo ?? currentUserId,
                Notes = $"Converted from Lead {lead.LeadCode}."
            };

            _context.Opportunities.Add(opp);
            await _context.SaveChangesAsync();
            createdOppId = opp.OpportunityId;

            await _auditService.LogAsync(
                currentUserId,
                currentUserEmail,
                "Create",
                "Opportunity",
                opp.OpportunityId.ToString(),
                null,
                $"Created Opportunity '{opp.OpportunityName}' via Lead Conversion from {lead.LeadCode}",
                ipAddress);
        }

        // 3. Update Lead Status to Converted
        lead.Status = "Converted";
        lead.ConvertedCustomerId = createdCustomer.CustomerId;
        lead.ConvertedOpportunityId = createdOppId;
        await _context.SaveChangesAsync();

        // 4. Audit Lead Conversion
        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Lead Conversion",
            "Lead",
            lead.LeadId.ToString(),
            "Qualified",
            $"Lead {lead.LeadCode} converted to Customer {createdCustomer.CustomerCode} (ID: {createdCustomer.CustomerId}){(createdOppId.HasValue ? $" and Opportunity (ID: {createdOppId})" : "")}",
            ipAddress);

        return (true, null, createdCustomer.CustomerId, createdOppId);
    }
}
