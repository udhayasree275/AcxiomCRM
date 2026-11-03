using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IFollowUpService
{
    Task<List<FollowUp>> GetFollowUpsAsync(string? userId, string userRole, string? status, string? type, string? assignedTo, string? search, string filterTab = "all");
    Task<FollowUp?> GetByIdAsync(int id, string? userId, string userRole);
    Task<(bool Success, string? Error, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp, string currentUserId, string currentUserEmail, string? ipAddress);
    Task<(bool Success, string? Error)> UpdateFollowUpAsync(FollowUp followUp, string currentUserId, string currentUserEmail, string userRole, string? ipAddress);
    Task<(bool Success, string? Error)> CompleteFollowUpAsync(int id, string? remarks, string currentUserId, string currentUserEmail, string userRole, string? ipAddress);
    Task<(bool Success, string? Error)> RescheduleFollowUpAsync(int id, DateTime newDate, string? remarks, string currentUserId, string currentUserEmail, string userRole, string? ipAddress);
    Task<(bool Success, string? Error)> DeleteFollowUpAsync(int id, string currentUserId, string currentUserEmail, string userRole, string? ipAddress);
}

public class FollowUpService : IFollowUpService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public FollowUpService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<FollowUp>> GetFollowUpsAsync(string? userId, string userRole, string? status, string? type, string? assignedTo, string? search, string filterTab = "all")
    {
        var query = _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.AssignedUser)
            .AsQueryable();

        if (userRole == AppRoles.SalesExecutive)
        {
            query = query.Where(f => f.AssignedTo == userId);
        }
        else if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            query = query.Where(f => f.AssignedTo == assignedTo);
        }

        var today = DateTime.UtcNow.Date;

        if (filterTab == "upcoming")
        {
            query = query.Where(f => f.Status == "Planned" && f.FollowUpDate >= today);
        }
        else if (filterTab == "overdue")
        {
            query = query.Where(f => (f.Status == "Planned" || f.Status == "Missed") && f.FollowUpDate < today);
        }
        else if (filterTab == "completed")
        {
            query = query.Where(f => f.Status == "Completed");
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(f => f.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(f => f.FollowUpType == type);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(f =>
                f.Subject.Contains(search) ||
                f.Remarks.Contains(search) ||
                (f.Customer != null && f.Customer.CustomerName.Contains(search)) ||
                (f.Lead != null && f.Lead.LeadName.Contains(search)));
        }

        return await query.OrderBy(f => f.FollowUpDate).ToListAsync();
    }

    public async Task<FollowUp?> GetByIdAsync(int id, string? userId, string userRole)
    {
        var fu = await _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.AssignedUser)
            .FirstOrDefaultAsync(f => f.FollowUpId == id);

        if (fu == null) return null;

        if (userRole == AppRoles.SalesExecutive && fu.AssignedTo != userId)
        {
            return null;
        }

        return fu;
    }

    public async Task<(bool Success, string? Error, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp, string currentUserId, string currentUserEmail, string? ipAddress)
    {
        // Business Validation: Follow-up Date cannot be earlier than today for a new/planned follow-up!
        if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            return (false, "Follow-up date cannot be earlier than today.", null);
        }

        followUp.CreatedDate = DateTime.UtcNow;
        if (string.IsNullOrEmpty(followUp.AssignedTo))
        {
            followUp.AssignedTo = currentUserId;
        }

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Create",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            null,
            $"Scheduled Follow-up '{followUp.Subject}' ({followUp.FollowUpType}) for {followUp.FollowUpDate:yyyy-MM-dd}",
            ipAddress);

        return (true, null, followUp);
    }

    public async Task<(bool Success, string? Error)> UpdateFollowUpAsync(FollowUp followUp, string currentUserId, string currentUserEmail, string userRole, string? ipAddress)
    {
        var existing = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUp.FollowUpId);
        if (existing == null) return (false, "Follow-up not found.");

        if (userRole == AppRoles.SalesExecutive && existing.AssignedTo != currentUserId)
        {
            return (false, "Unauthorized.");
        }

        if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
        {
            return (false, "Follow-up date cannot be earlier than today for a planned activity.");
        }

        existing.CustomerId = followUp.CustomerId;
        existing.LeadId = followUp.LeadId;
        existing.FollowUpDate = followUp.FollowUpDate;
        existing.FollowUpType = followUp.FollowUpType;
        existing.Subject = followUp.Subject;
        existing.Remarks = followUp.Remarks;
        existing.Status = followUp.Status;

        if (userRole != AppRoles.SalesExecutive && !string.IsNullOrEmpty(followUp.AssignedTo))
        {
            existing.AssignedTo = followUp.AssignedTo;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Update",
            "FollowUp",
            existing.FollowUpId.ToString(),
            null,
            $"Updated Follow-up '{existing.Subject}' (Status: {existing.Status})",
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CompleteFollowUpAsync(int id, string? remarks, string currentUserId, string currentUserEmail, string userRole, string? ipAddress)
    {
        var existing = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (existing == null) return (false, "Follow-up not found.");

        if (userRole == AppRoles.SalesExecutive && existing.AssignedTo != currentUserId)
        {
            return (false, "Unauthorized.");
        }

        existing.Status = "Completed";
        existing.CompletedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            existing.Remarks = existing.Remarks + " | Outcome: " + remarks;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Complete",
            "FollowUp",
            id.ToString(),
            "Planned",
            "Completed",
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RescheduleFollowUpAsync(int id, DateTime newDate, string? remarks, string currentUserId, string currentUserEmail, string userRole, string? ipAddress)
    {
        var existing = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (existing == null) return (false, "Follow-up not found.");

        if (userRole == AppRoles.SalesExecutive && existing.AssignedTo != currentUserId)
        {
            return (false, "Unauthorized.");
        }

        if (newDate.Date < DateTime.UtcNow.Date)
        {
            return (false, "New follow-up date cannot be earlier than today.");
        }

        DateTime oldDate = existing.FollowUpDate;
        existing.FollowUpDate = newDate;
        existing.Status = "Planned";
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            existing.Remarks = existing.Remarks + $" | Rescheduled to {newDate:yyyy-MM-dd}: {remarks}";
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Reschedule",
            "FollowUp",
            id.ToString(),
            $"Date: {oldDate:yyyy-MM-dd}",
            $"Rescheduled to: {newDate:yyyy-MM-dd}",
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteFollowUpAsync(int id, string currentUserId, string currentUserEmail, string userRole, string? ipAddress)
    {
        var existing = await _context.FollowUps.FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (existing == null) return (false, "Follow-up not found.");

        _context.FollowUps.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Delete",
            "FollowUp",
            id.ToString(),
            $"Follow-up '{existing.Subject}'",
            null,
            ipAddress);

        return (true, null);
    }
}
