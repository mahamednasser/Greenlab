// Layer: Application
// Responsibility: Defines defaults used when creating users from imported schedules.
namespace Greenlab.Application.Common.Interfaces;

/// <summary>
/// Supplies defaults for users created from imported schedule data.
/// </summary>
public interface IImportedUserDefaults
{
    /// <summary>
    /// Gets the initial password assigned to imported users.
    /// </summary>
    string DefaultPassword { get; }
}
