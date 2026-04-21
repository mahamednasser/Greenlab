using Greenlab.Models;

namespace Greenlab.Services;

public interface ICrowdCountClient
{
    Task<int> GetOccupancyCountAsync(Lab lab, CancellationToken cancellationToken = default);
}
