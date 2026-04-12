using Environment = NatsManager.Domain.Modules.Environments.Environment;

namespace NatsManager.Application.Modules.Environments.Ports;

public interface IEnvironmentRepository
{
    Task<Environment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Environment?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Environment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Environment> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default);
    Task AddAsync(Environment environment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Environment environment, CancellationToken cancellationToken = default);
    Task DeleteAsync(Environment environment, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Environment>> GetEnabledAsync(CancellationToken cancellationToken = default);
}
