// Layer: Application
// Responsibility: Defines the contract for retrieving lab occupancy counts from an external source.
using Greenlab.Domain.Entities;

namespace Greenlab.Application.Common.Interfaces;

/// <summary>
/// Gets occupancy counts for labs.
/// </summary>
public interface ICrowdCountClient
{
    /// <summary>
    /// Gets the current occupancy count for a lab.
    /// </summary>
    Task<int> GetOccupancyCountAsync(Lab lab, CancellationToken cancellationToken = default);
}
