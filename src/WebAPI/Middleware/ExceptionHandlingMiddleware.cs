// Layer: WebAPI
// Responsibility: Converts application and domain exceptions into consistent HTTP responses.
using System.Net;
using System.Text.Json;
using Greenlab.Application.Common.Exceptions;
using Greenlab.Domain.Exceptions;

namespace Greenlab.WebAPI.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        object payload = new { error = "An unexpected error occurred." };

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = HttpStatusCode.BadRequest;
                payload = new { error = validationException.Message, validationException.Errors };
                break;
            case NotFoundException notFoundException:
                statusCode = HttpStatusCode.NotFound;
                payload = new { error = notFoundException.Message };
                break;
            case ForbiddenAccessException forbiddenException:
                statusCode = HttpStatusCode.Forbidden;
                payload = new { error = forbiddenException.Message };
                break;
            case DomainException domainException:
                statusCode = HttpStatusCode.BadRequest;
                payload = new { error = domainException.Message };
                break;
            case InvalidOperationException invalidOperationException:
                statusCode = HttpStatusCode.BadRequest;
                payload = new { error = invalidOperationException.Message };
                break;
        }

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception");
        else
            _logger.LogWarning(exception, "Handled exception");

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload)).ConfigureAwait(false);
    }
}
