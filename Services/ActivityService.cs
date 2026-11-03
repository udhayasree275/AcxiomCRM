using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IActivityService
{
    Task<List<Activity>> GetActivitiesAsync(string? userId, string userRole, string? type, string? status, string? assignedTo, string? search);
    Task<Activity?> GetByIdAsync(int id, string? userId, string userRole);
    Task<(bool Success, string? Error, Activity? Activity)> LogActivityAsync(Activity activity, string currentUserId, string currentUserEmail, string? ipAddress);
    Task<(bool Success, string? Error)> UpdateActivityAsync(Activity activity, string currentUserId, string currentUserEmail, string userRole, string? ipAddress);
    Task<(bool Success, string? Error)> DeleteActivityAsync(int id, string currentUserId, string currentUserEmail, string userRole, string? ipAddress);
}

public class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public ActivityService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<Activity>> GetActivitiesAsync(string? userId, string userRole, string? type, string? status, string? assignedTo, string? search)
    {
        var query = _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedUser)
            .AsQueryable();

        if (userRole == AppRoles.SalesExecutive)
        {
            query = query.Where(a => a.AssignedTo == userId);
        }
        else if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            query = query.Where(a => a.AssignedTo == assignedTo);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(a => a.ActivityType == type);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(a =>
                a.Subject.Contains(search) ||
                (a.Description != null && a.Description.Contains(search)) ||
                (a.Customer != null && a.Customer.CustomerName.Contains(search)) ||
                (a.Lead != null && a.Lead.LeadName.Contains(search)));
        }

        return await query.OrderByDescending(a => a.ActivityDate).ToListAsync();
    }

    public async Task<Activity?> GetByIdAsync(int id, string? userId, string userRole)
    {
        var act = await _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedUser)
            .FirstOrDefaultAsync(a => a.ActivityId == id);

        if (act == null) return null;

        if (userRole == AppRoles.SalesExecutive && act.AssignedTo != userId)
        {
            return null;
        }

        return act;
    }

    public async Task<(bool Success, string? Error, Activity? Activity)> LogActivityAsync(Activity activity, string currentUserId, string currentUserEmail, string? ipAddress)
    {
        activity.CreatedDate = DateTime.UtcNow;
        if (string.IsNullOrEmpty(activity.AssignedTo))
        {
            activity.AssignedTo = currentUserId;
        }

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Create",
            "Activity",
            activity.ActivityId.ToString(),
            null,
            $"Logged {activity.ActivityType}: '{activity.Subject}'",
            ipAddress);

        return (true, null, activity);
    }

    public async Task<(bool Success, string? Error)> UpdateActivityAsync(Activity activity, string currentUserId, string currentUserEmail, string userRole, string? ipAddress)
    {
        var existing = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == activity.ActivityId);
        if (existing == null) return (false, "Activity not found.");

        if (userRole == AppRoles.SalesExecutive && existing.AssignedTo != currentUserId)
        {
            return (false, "Unauthorized.");
        }

        existing.ActivityType = activity.ActivityType;
        existing.Subject = activity.Subject;
        existing.Description = activity.Description;
        existing.ActivityDate = activity.ActivityDate;
        existing.CustomerId = activity.CustomerId;
        existing.LeadId = activity.LeadId;
        existing.Status = activity.Status;

        if (userRole != AppRoles.SalesExecutive && !string.IsNullOrEmpty(activity.AssignedTo))
        {
            existing.AssignedTo = activity.AssignedTo;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Update",
            "Activity",
            existing.ActivityId.ToString(),
            null,
            $"Updated {existing.ActivityType}: '{existing.Subject}'",
            ipAddress);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteActivityAsync(int id, string currentUserId, string currentUserEmail, string userRole, string? ipAddress)
    {
        var existing = await _context.Activities.FirstOrDefaultAsync(a => a.ActivityId == id);
        if (existing == null) return (false, "Activity not found.");

        if (userRole == AppRoles.SalesExecutive)
        {
            return (false, "Unauthorized.");
        }

        _context.Activities.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserEmail,
            "Delete",
            "Activity",
            id.ToString(),
            $"Activity '{existing.Subject}'",
            null,
            ipAddress);

        return (true, null);
    }
}
