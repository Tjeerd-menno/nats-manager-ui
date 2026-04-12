using Microsoft.EntityFrameworkCore;
using NatsManager.Application.Modules.Auth.Ports;
using NatsManager.Domain.Modules.Auth;

namespace NatsManager.Infrastructure.Persistence;

public sealed class UserRepository(AppDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => await context.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Users.AsNoTracking().OrderBy(u => u.Username).ToListAsync(cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetRolesAsync(CancellationToken cancellationToken = default)
        => await context.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UserRoleAssignment>> GetUserRoleAssignmentsAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.UserRoleAssignments.AsNoTracking().Where(a => a.UserId == userId).ToListAsync(cancellationToken);

    public async Task AddRoleAssignmentAsync(UserRoleAssignment assignment, CancellationToken cancellationToken = default)
    {
        context.UserRoleAssignments.Add(assignment);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveRoleAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await context.UserRoleAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId, cancellationToken);
        if (assignment is not null)
        {
            context.UserRoleAssignments.Remove(assignment);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
