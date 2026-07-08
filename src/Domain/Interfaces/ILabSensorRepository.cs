// Layer: Domain
// Responsibility: Defines persistence operations for lab sensors.
using Greenlab.Domain.Entities;

namespace Greenlab.Domain.Interfaces;

/// <summary>
/// Provides persistence operations for lab sensors.
/// </summary>
public interface ILabSensorRepository : IRepository<LabSensor>
{
}
