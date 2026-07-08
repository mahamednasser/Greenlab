// Layer: Domain
// Responsibility: Defines persistence operations for manual control commands.
using Greenlab.Domain.Entities;

namespace Greenlab.Domain.Interfaces;

/// <summary>
/// Provides persistence operations for manual control commands.
/// </summary>
public interface IManualControlCommandRepository : IRepository<ManualControlCommand>
{
}
