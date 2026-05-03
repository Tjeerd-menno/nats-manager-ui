using Microsoft.EntityFrameworkCore;
using NatsManager.Application.Modules.Audit.Ports;
using NatsManager.Domain.Modules.Audit;
using NatsManager.Domain.Modules.Common;

namespace NatsManager.Infrastructure.Persistence;

public sealed class AuditEventRepository(AppDbContext context) : IAuditEventRepository
{
    public async Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        context.AuditEvents.Add(auditEvent);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<AuditEvent> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Guid? actorId = null,
        ActionType? actionType = null,
        ResourceType? resourceType = null,
        Guid? environmentId = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        AuditSource? source = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.AuditEvents.AsNoTracking().AsQueryable();

        if (actorId.HasValue)
        {
            query = query.Where(e => e.ActorId == actorId.Value);
        }

        if (actionType.HasValue)
        {
            query = query.Where(e => e.ActionType == actionType.Value);
        }

        if (resourceType.HasValue)
        {
            query = query.Where(e => e.ResourceType == resourceType.Value);
        }

        if (environmentId.HasValue)
        {
            query = query.Where(e => e.EnvironmentId == environmentId.Value);
        }

        if (source.HasValue)
        {
            query = query.Where(e => e.Source == source.Value);
        }

        if (context.Database.IsSqlite())
        {
            return await GetPagedForSqliteAsync(query, page, pageSize, fromDate, toDate, cancellationToken);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.Timestamp <= toDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private static async Task<(IReadOnlyList<AuditEvent> Items, int TotalCount)> GetPagedForSqliteAsync(
        IQueryable<AuditEvent> query,
        int page,
        int pageSize,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken cancellationToken)
    {
        // SQLite stores DateTimeOffset values as text, but EF Core does not translate
        // ordering/range comparisons for DateTimeOffset. Apply non-temporal filters in SQL,
        // then perform timestamp filtering, ordering, and pagination in memory.
        var events = await query.ToListAsync(cancellationToken);
        var filteredEvents = events.AsEnumerable();

        if (fromDate.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.Timestamp <= toDate.Value);
        }

        var orderedEvents = filteredEvents
            .OrderByDescending(e => e.Timestamp)
            .ToList();

        var items = orderedEvents
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (items, orderedEvents.Count);
    }
}
