// Layer: Domain
// Responsibility: Provides the base exception type for domain rule violations.
namespace Greenlab.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
