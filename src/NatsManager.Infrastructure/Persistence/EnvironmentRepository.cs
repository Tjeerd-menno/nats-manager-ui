using Microsoft.EntityFrameworkCore;
using NatsManager.Application.Modules.Environments.Ports;
using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Infrastructure.Persistence;

public sealed class EnvironmentRepository(AppDbContext dbContext) : IEnvironmentRepository
{
    public async Task<Environment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Environments.FindAsync([id], cancellationToken);
    }

    public async Task<Environment?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await dbContext.Environments
            .FirstOrDefaultAsync(e => e.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Environment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Environments
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Environment> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Environments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e =>
                EF.Functions.Like(e.Name, $"%{term}%") ||
                EF.Functions.Like(e.Description, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = sortBy?.ToLowerInvariant() switch
        {
            "name" => sortDescending ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name),
            "status" => sortDescending ? query.OrderByDescending(e => e.ConnectionStatus) : query.OrderBy(e => e.ConnectionStatus),
            "createdat" => sortDescending ? query.OrderByDescending(e => e.CreatedAt) : query.OrderBy(e => e.CreatedAt),
            "updatedat" => sortDescending ? query.OrderByDescending(e => e.UpdatedAt) : query.OrderBy(e => e.UpdatedAt),
            _ => query.OrderBy(e => e.Name)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(Environment environment, CancellationToken cancellationToken = default)
    {
        await dbContext.Environments.AddAsync(environment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Environment environment, CancellationToken cancellationToken = default)
    {
        dbContext.Environments.Update(environment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Environment environment, CancellationToken cancellationToken = default)
    {
        dbContext.Environments.Remove(environment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsWithNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        return await dbContext.Environments
            .AnyAsync(e => e.Name == name && (excludeId == null || e.Id != excludeId), cancellationToken);
    }

    public async Task<IReadOnlyList<Environment>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Environments
            .Where(e => e.IsEnabled)
            .OrderBy(e => e.Name)
            .ToListAsync(cancellationToken);
    }
}
