// Layer: Domain
// Responsibility: Represents a missing domain resource.
namespace Greenlab.Domain.Exceptions;

public class NotFoundException : DomainException
{
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with key '{key}' was not found.")
    {
        ResourceName = resourceName;
        Key = key;
    }

    public string ResourceName { get; }
    public object Key { get; }
}
