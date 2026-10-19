using ExpenseTracker.Application.Exceptions;
using ExpenseTracker.Domain.Rules;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace ExpenseTracker.Api.ErrorHandling;

// The one place that turns exceptions into HTTP responses (D-13).
// Anything not handled here falls through to the default 500 ProblemDetails, without internal details.
public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails? problem = exception switch
        {
            DomainValidationException ex => new ValidationProblemDetails(ex.Errors)
            {
                Status = StatusCodes.Status400BadRequest
            },
            NotFoundException ex => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not found",
                Detail = ex.Message
            },
            _ when IsDatabaseUnavailable(exception) => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Service unavailable",
                Detail = "The database is unavailable. Try again later."
            },
            _ => null
        };

        if (problem is null)
            return false;

        if (problem.Status == StatusCodes.Status503ServiceUnavailable)
            logger.LogWarning("Database unavailable: {ExceptionType}", exception.GetType().Name);

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    // Connection failures, not errors reported by PostgreSQL itself (e.g. a broken constraint).
    private static bool IsDatabaseUnavailable(Exception? exception)
    {
        for (; exception is not null; exception = exception.InnerException)
        {
            if (exception is NpgsqlException and not PostgresException)
                return true;
        }

        return false;
    }
}
