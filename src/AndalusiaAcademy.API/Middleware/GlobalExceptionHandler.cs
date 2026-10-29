using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AndalusiaAcademy.API.Middleware;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ValidationException validationException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ValidationProblemDetails(
                    validationException.Errors
                        .GroupBy(error => error.PropertyName)
                        .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Type = "https://www.rfc-editor.org/rfc/rfc9110#section-15.5.1",
                },
            });
        }

        logger.LogError(
            exception,
            "Unhandled exception {ExceptionType} while processing {Method} {Path}. CorrelationId: {CorrelationId}",
            exception.GetType().Name,
            httpContext.Request.Method,
            httpContext.Request.Path,
            httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Type = "https://www.rfc-editor.org/rfc/rfc9110#section-15.6.1",
            },
        });
    }
}
