using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EmployeeManagement.Exceptions;

/// <summary>
/// Converts exceptions into RFC 7807 problem responses so controllers
/// don't need their own try/catch blocks.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // The client disconnected and the request token cancelled the work in flight.
        // That's normal, not an error: nobody is left to receive a response.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request cancelled by client: {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

            httpContext.Response.StatusCode = 499; // non-standard "client closed request"
            return true;
        }

        ProblemDetails problem;

        if (exception is AppException appException)
        {
            problem = new ProblemDetails
            {
                Status = appException.StatusCode,
                Title = appException.Title,
                Detail = appException.Message
            };
        }
        else if (exception is DbUpdateException { InnerException: PostgresException pg }
                 && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // Two concurrent requests can both pass a service-level "already exists"
            // check; the unique index is the real guard, so report it as a conflict.
            problem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = DescribeUniqueViolation(pg.ConstraintName)
            };
        }
        else
        {
            // Unexpected: log everything, but don't leak details to the client.
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);

            problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            };
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }

    private static string DescribeUniqueViolation(string? constraintName) => constraintName switch
    {
        "IX_Employees_Email" => "Email already exists !",
        "IX_Skills_Name" => "Skill already exists.",
        "PK_EmployeeSkills" => "This skill is already assigned to the employee.",
        "PK_EmployeeProjects" => "Employee is already assigned to this project !",
        _ => "A record with the same unique value already exists."
    };
}
