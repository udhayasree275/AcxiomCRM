using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IOpportunityService
{
    Task<(List<Opportunity> Items, int TotalCount, decimal TotalAmount, decimal WeightedAmount)> GetOpportunitiesAsync(
        string? userId, string userRole, string? search, string? stage, string? status, string? ownerId, int page, int pageSize);
    Task<Opportunity?> GetByIdAsync(int id, string? userId, string userRole);
    Task<(bool Success, string? Error, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity, string currentUserId, string currentUserEmail, string? ipAddress);
    Task<(bool Success, string? Error)> UpdateOpportunityAsync(Opportunity opportunity, string currentUserId, string currentUserEmail, string userRole, string? ipAddress);
    Task<(bool Success, string? Error)> UpdateStageAsync(int id, string newStage, string currentUserId, string currentUserEmail, string userRole, string? ipAddress);
    Task<(bool Success, string? Error)> DeleteOpportunityAsync(int id, string currentUserId, string currentUserEmail, string userRole, string? ipAddress);
}

public class OpportunityService : IOpportunityService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public OpportunityService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<(List<Opportunity> Items, int TotalCount, decimal TotalAmount, decimal WeightedAmount)> GetOpportunitiesAsync(
        string? userId, string userRole, string? search, string? stage, string? status, string? ownerId, int page, int pageSize)
    {
        var query = _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedUser)
            .AsQueryable();

        // Scope by Role
        if (userRole == AppRoles.SalesExecutive)
        {
            query = query.Where(o => o.AssignedTo == userId);
        }
        else if (!string.IsNullOrWhiteSpace(ownerId))
        {
            query = query.Where(o => o.AssignedTo == ownerId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(o =>
                o.OpportunityName.Contains(search) ||
                (o.Customer != null && o.Customer.CustomerName.Contains(search)) ||
                (o.Lead != null && o.Lead.LeadName.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(stage))
        {
            query = query.Where(o => o.Stage == stage);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        int totalCount = await query.CountAsync();
        var allMatching = await query.ToListAsync();
        decimal totalAmount = allMatching.Where(o => o.Status != "Lost").Sum(o => o.Amount);
        decimal weightedAmount = allMatching.Where(o => o.Status != "Lost").Sum(o => o.WeightedAmount);

        var items = allMatching
            .OrderByDescending(o => o.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (items, totalCount, totalAmount, weightedAmount);
    }

    public async Task<Opportunity?> GetByIdAsync(int id, string? userId, string userRole)
    {
        var opp = await _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedUser)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

        if (opp == null) return null;

        if (userRole == AppRoles.SalesExecutive && opp.AssignedTo != userId)
        {
            return null;
        }

        return opp;
    }

    public async Task<(bool Success, string? Error, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity, string currentUserId, string currentUserEmail, string? ipAddress)
    {
        // Business Validation 1: Amount must be greater than 0
        if (opportunity.Amount <= 0)
        {
            return (false, "Opportunity Amount must be greater than 0.", null);
        }

        // Business Validation 2: Probability 0 to 100
        if (opportunity.Probability < 0 || opportunity.Probability > 100)
        {
            return (false, "Probability must be between 0 and 100.", null);
        }

        // Business Validation 3: Expected Close Date cannot be in the past for active opportunities
        if (opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            return (false, "Expected Close Date cannot be in the past.", null);
        }

        // Customer must exist
        var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == opportunity.CustomerId);
        if (!customerExists)
        {
            return (false, "Selected Customer does not exist.", null);
        }

        opportunity.CreatedDate = DateTime.UtcNow;
        if (string.IsNullOrEmpty(opportunity.AssignedTo))
        {
            opportunity.AssignedTo = currentUserId;
        }

        // Set status from Stage
        if (opportunity.Stage == "Won") opportunity.Status = "Won";
        else if (opportunity.Stage == "Lost") opportunity.Status = "Lost";
        else opportunity.Status = "Open";

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Create",
            "Opportunity",
            opportunity.OpportunityId.ToString(),
            null,
            $"Created Opportunity '{opportunity.OpportunityName}' for ${opportunity.Amount} (Stage: {opportunity.Stage})",
            ipAddress);

        return (true, null, opportunity);
    }

    public async Task<(bool Success, string? Error)> UpdateOpportunityAsync(Opportunity opportunity, string currentUserId, string currentUserEmail, string userRole, string? ipAddress)
    {
        var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == opportunity.OpportunityId);
        if (existing == null)
        {
            return (false, "Opportunity not found.");
        }

        if (userRole == AppRoles.SalesExecutive && existing.AssignedTo != currentUserId)
        {
            return (false, "You are not authorized to edit this opportunity.");
        }

        // Business Validations
        if (opportunity.Amount <= 0)
        {
            return (false, "Opportunity Amount must be greater than 0.");
        }

        if (opportunity.Probability < 0 || opportunity.Probability > 100)
        {
            return (false, "Probability must be between 0 and 100.");
        }

        if (opportunity.Stage != "Won" && opportunity.Stage != "Lost" && opportunity.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
        {
            return (false, "Expected Close Date cannot be in the past for an active opportunity.");
        }

        string oldValue = $"Name: {existing.OpportunityName}, Amount: {existing.Amount}, Stage: {existing.Stage}, Prob: {existing.Probability}";

        existing.OpportunityName = opportunity.OpportunityName;
        existing.CustomerId = opportunity.CustomerId;
        existing.LeadId = opportunity.LeadId;
        existing.Amount = opportunity.Amount;
        existing.Stage = opportunity.Stage;
        existing.Probability = opportunity.Probability;
        existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
        existing.Notes = opportunity.Notes;

        if (opportunity.Stage == "Won") existing.Status = "Won";
        else if (opportunity.Stage == "Lost") existing.Status = "Lost";
        else existing.Status = "Open";

        if (userRole != AppRoles.SalesExecutive && !string.IsNullOrEmpty(opportunity.AssignedTo))
        {
            existing.AssignedTo = opportunity.AssignedTo;
        }

        await _context.SaveChangesAsync();

        string newValue = $"Name: {existing.OpportunityName}, Amount: {existing.Amount}, Stage: {existing.Stage}, Prob: {existing.Probability}";

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Update",
            "Opportunity",
            existing.OpportunityId.ToString(),
            oldValue,
            newValue,
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateStageAsync(int id, string newStage, string currentUserId, string currentUserEmail, string userRole, string? ipAddress)
    {
        var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == id);
        if (existing == null) return (false, "Opportunity not found.");

        if (userRole == AppRoles.SalesExecutive && existing.AssignedTo != currentUserId)
        {
            return (false, "Unauthorized.");
        }

        string oldStage = existing.Stage;
        existing.Stage = newStage;

        if (newStage == "Won")
        {
            existing.Status = "Won";
            existing.Probability = 100;
        }
        else if (newStage == "Lost")
        {
            existing.Status = "Lost";
            existing.Probability = 0;
        }
        else
        {
            existing.Status = "Open";
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Update",
            "Opportunity",
            id.ToString(),
            $"Stage: {oldStage}",
            $"Stage: {newStage}",
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteOpportunityAsync(int id, string currentUserId, string currentUserEmail, string userRole, string? ipAddress)
    {
        var existing = await _context.Opportunities.FirstOrDefaultAsync(o => o.OpportunityId == id);
        if (existing == null) return (false, "Opportunity not found.");

        if (userRole == AppRoles.SalesExecutive)
        {
            return (false, "Sales executives do not have permission to delete opportunities.");
        }

        _context.Opportunities.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Delete",
            "Opportunity",
            id.ToString(),
            $"Opportunity {existing.OpportunityName} (${existing.Amount})",
            null,
            ipAddress);

        return (true, null);
    }
}
