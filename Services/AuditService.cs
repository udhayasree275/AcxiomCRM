using AcxiomCRM.Data;
using AcxiomCRM.Models;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IAuditService
{
    Task LogAsync(string? userId, string? userEmail, string action, string entityName, string? recordId, string? oldValue, string? newValue, string? ipAddress);
    Task<List<AuditLog>> GetLogsAsync(string? searchTerm, string? entityFilter, string? actionFilter, string? userFilter, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 20);
    Task<int> GetTotalCountAsync(string? searchTerm, string? entityFilter, string? actionFilter, string? userFilter, DateTime? fromDate, DateTime? toDate);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string? userId, string? userEmail, string action, string entityName, string? recordId, string? oldValue, string? newValue, string? ipAddress)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId,
                UserEmail = userEmail,
                Action = action,
                EntityName = entityName,
                RecordId = recordId,
                OldValue = oldValue,
                NewValue = newValue,
                CreatedDate = DateTime.UtcNow,
                IpAddress = ipAddress
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Fail-safe: Audit logging should never crash the main application workflow
        }
    }

    public async Task<List<AuditLog>> GetLogsAsync(string? searchTerm, string? entityFilter, string? actionFilter, string? userFilter, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 20)
    {
        var query = BuildQuery(searchTerm, entityFilter, actionFilter, userFilter, fromDate, toDate);
        return await query
            .OrderByDescending(l => l.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? searchTerm, string? entityFilter, string? actionFilter, string? userFilter, DateTime? fromDate, DateTime? toDate)
    {
        var query = BuildQuery(searchTerm, entityFilter, actionFilter, userFilter, fromDate, toDate);
        return await query.CountAsync();
    }

    private IQueryable<AuditLog> BuildQuery(string? searchTerm, string? entityFilter, string? actionFilter, string? userFilter, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Trim();
            query = query.Where(l =>
                (l.Action != null && l.Action.Contains(searchTerm)) ||
                (l.EntityName != null && l.EntityName.Contains(searchTerm)) ||
                (l.UserEmail != null && l.UserEmail.Contains(searchTerm)) ||
                (l.RecordId != null && l.RecordId.Contains(searchTerm)) ||
                (l.NewValue != null && l.NewValue.Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(entityFilter))
        {
            query = query.Where(l => l.EntityName == entityFilter);
        }

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(l => l.Action == actionFilter);
        }

        if (!string.IsNullOrWhiteSpace(userFilter))
        {
            query = query.Where(l => l.UserId == userFilter || l.UserEmail == userFilter);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.CreatedDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(l => l.CreatedDate <= endOfDay);
        }

        return query;
    }
}
