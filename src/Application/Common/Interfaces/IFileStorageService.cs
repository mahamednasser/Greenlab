// Layer: Application
// Responsibility: Defines file storage operations required by use cases.
namespace Greenlab.Application.Common.Interfaces;

/// <summary>
/// Stores and reads application files.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves a schedule image and returns its relative stored path.
    /// </summary>
    Task<string> SaveScheduleImageAsync(string fileName, byte[] content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a previously stored file by relative path.
    /// </summary>
    Task<byte[]> ReadFileAsync(string storedPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a stored file exists.
    /// </summary>
    bool Exists(string storedPath);
}
