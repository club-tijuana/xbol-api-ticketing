using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace XBOL.Ticketing.API.Filters;

public class ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var problem = context.Exception switch
        {
            KeyNotFoundException ex => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Detail = ex.Message
            },
            InvalidOperationException ex => new ProblemDetails
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Business rule violation",
                Detail = ex.Message
            },
            DbUpdateException ex => FromDbUpdateException(ex),
            _ => null
        };

        if (problem is null)
        {
            return;
        }

        logger.LogWarning(context.Exception,
            "Mapping {ExceptionType} to status {Status}.",
            context.Exception.GetType().Name, problem.Status);

        context.Result = new ObjectResult(problem) { StatusCode = problem.Status };
        context.ExceptionHandled = true;
    }

    private static ProblemDetails FromDbUpdateException(DbUpdateException ex)
    {
        var detail = ex.InnerException?.Message ?? ex.Message;

        // Npgsql error codes: 23505 = unique constraint, 23503 = FK constraint
        var isConstraintViolation = detail.Contains("23505") || detail.Contains("23503");

        return new ProblemDetails
        {
            Status = isConstraintViolation
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status422UnprocessableEntity,
            Title = isConstraintViolation ? "Conflict" : "Database error",
            Detail = isConstraintViolation
                ? "The operation would violate a database constraint."
                : detail
        };
    }
}
