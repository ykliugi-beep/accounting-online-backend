using ERPAccounting.Common.Exceptions;
using ERPAccounting.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ERPAccounting.API.Filters;

/// <summary>
/// Globalni exception filter koji hvata concurrency exception-e i vraća standardizovani 409 Conflict response.
/// </summary>
public class ConcurrencyExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ConcurrencyExceptionFilter> _logger;

    public ConcurrencyExceptionFilter(ILogger<ConcurrencyExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            // Domain-level concurrency exception
            case ConflictException conflictEx:
                HandleConflictException(context, conflictEx);
                break;

            // EF Core concurrency exception
            case DbUpdateConcurrencyException dbConcurrencyEx:
                HandleDbConcurrencyException(context, dbConcurrencyEx);
                break;
        }
    }

    private void HandleConflictException(ExceptionContext context, ConflictException exception)
    {
        _logger.LogWarning(
            exception,
            "Concurrency conflict detected: Entity={Entity}, ExpectedETag={ExpectedETag}, CurrentETag={CurrentETag}",
            exception.Entity,
            exception.ExpectedETag,
            exception.CurrentETag);

        var problemDetails = new ProblemDetailsDto
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            Title = "Concurrency Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = exception.Detail,
            Instance = context.HttpContext.Request.Path,
            TraceId = context.HttpContext.TraceIdentifier,
            Errors = new Dictionary<string, string[]>
            {
                ["concurrency"] = new[]
                {
                    $"Entity '{exception.Entity}' has been modified by another user.",
                    $"Expected ETag: {exception.ExpectedETag}",
                    $"Current ETag: {exception.CurrentETag}",
                    "Please refresh the entity and try again."
                }
            },
            Extensions = new Dictionary<string, object?>
            {
                ["entityType"] = exception.Entity,
                ["expectedETag"] = exception.ExpectedETag,
                ["currentETag"] = exception.CurrentETag,
                ["errorCode"] = "CONCURRENCY_CONFLICT"
            }
        };

        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status409Conflict
        };

        // Dodaj Current ETag u header kako bi klijent mogao odmah refresh-ovati
        if (!string.IsNullOrWhiteSpace(exception.CurrentETag))
        {
            context.HttpContext.Response.Headers["ETag"] = $"\"{exception.CurrentETag}\"";
        }

        context.ExceptionHandled = true;
    }

    private void HandleDbConcurrencyException(ExceptionContext context, DbUpdateConcurrencyException exception)
    {
        _logger.LogWarning(
            exception,
            "Database concurrency exception detected");

        var problemDetails = new ProblemDetailsDto
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            Title = "Concurrency Conflict",
            Status = StatusCodes.Status409Conflict,
            Detail = "The record has been modified by another user since it was retrieved.",
            Instance = context.HttpContext.Request.Path,
            TraceId = context.HttpContext.TraceIdentifier,
            Errors = new Dictionary<string, string[]>
            {
                ["concurrency"] = new[]
                {
                    "A concurrency conflict occurred while saving changes.",
                    "The record may have been modified or deleted by another user.",
                    "Please refresh the entity and try again."
                }
            },
            Extensions = new Dictionary<string, object?>
            {
                ["errorCode"] = "DATABASE_CONCURRENCY_CONFLICT"
            }
        };

        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status409Conflict
        };

        context.ExceptionHandled = true;
    }
}
