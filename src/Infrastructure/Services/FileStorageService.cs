// Layer: Infrastructure
// Responsibility: Stores and reads uploaded schedule files from local disk.
using Greenlab.Application.Common.Interfaces;
using Greenlab.Infrastructure.Services.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Greenlab.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IHostEnvironment _environment;
    private readonly StorageOptions _storageOptions;

    public FileStorageService(IHostEnvironment environment, IOptions<StorageOptions> storageOptions)
    {
        _environment = environment;
        _storageOptions = storageOptions.Value;
    }

    public async Task<string> SaveScheduleImageAsync(string fileName, byte[] content, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
            extension = ".bin";

        var directory = Path.Combine(_environment.ContentRootPath, _storageOptions.ScheduleUploadsPath);
        Directory.CreateDirectory(directory);
        var safeName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(directory, safeName);
        await File.WriteAllBytesAsync(fullPath, content, cancellationToken).ConfigureAwait(false);
        return Path.Combine(_storageOptions.ScheduleUploadsPath, safeName).Replace('\\', '/');
    }

    public Task<byte[]> ReadFileAsync(string storedPath, CancellationToken cancellationToken = default)
    {
        return File.ReadAllBytesAsync(GetFullPath(storedPath), cancellationToken);
    }

    public bool Exists(string storedPath)
    {
        return File.Exists(GetFullPath(storedPath));
    }

    private string GetFullPath(string storedPath)
    {
        return Path.Combine(_environment.ContentRootPath, storedPath.Replace('/', Path.DirectorySeparatorChar));
    }
}
