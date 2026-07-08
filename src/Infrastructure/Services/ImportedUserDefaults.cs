// Layer: Infrastructure
// Responsibility: Exposes imported user defaults from configuration.
using Greenlab.Application.Common.Interfaces;
using Greenlab.Infrastructure.Services.Options;
using Microsoft.Extensions.Options;

namespace Greenlab.Infrastructure.Services;

public class ImportedUserDefaults : IImportedUserDefaults
{
    private readonly ImportedUserOptions _options;

    public ImportedUserDefaults(IOptions<ImportedUserOptions> options)
    {
        _options = options.Value;
    }

    public string DefaultPassword => _options.DefaultPassword;
}
