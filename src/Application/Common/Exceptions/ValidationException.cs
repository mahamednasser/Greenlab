// Layer: Application
// Responsibility: Represents validation errors collected by the application pipeline.
namespace Greenlab.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures occurred.")
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }
}
