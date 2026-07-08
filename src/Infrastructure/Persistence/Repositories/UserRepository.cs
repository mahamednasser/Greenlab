// Layer: Infrastructure
// Responsibility: Implements user persistence queries with EF Core.
using Greenlab.Domain.Entities;
using Greenlab.Domain.Enums;
using Greenlab.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Greenlab.Infrastructure.Persistence.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public Task<User?> GetByEmailAsync(string email, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = track ? DbContext.Users.AsQueryable() : DbContext.Users.AsNoTracking();
        return query.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public Task<User?> FindByEmailOrNameAsync(string email, string fullName, CancellationToken cancellationToken = default)
    {
        return DbContext.Users.FirstOrDefaultAsync(u => u.Email == email || u.FullName == fullName, cancellationToken);
    }

    public bool IsStaffRole(Role role)
    {
        return role is Role.Teacher or Role.TeacherAssistant;
    }
}
