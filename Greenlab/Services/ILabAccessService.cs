namespace Greenlab.Services;

public interface ILabAccessService
{
    Task<bool> CanStaffAccessLabAsync(int userId, int labId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetStaffAllowedLabIdsAsync(int userId, DateTime utcNow, CancellationToken cancellationToken = default);
}
