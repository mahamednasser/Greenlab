// Layer: Domain
// Responsibility: Represents a domain operation denied by access rules.
namespace Greenlab.Domain.Exceptions;

public class ForbiddenAccessException : DomainException
{
    public ForbiddenAccessException(string message) : base(message)
    {
    }
}
