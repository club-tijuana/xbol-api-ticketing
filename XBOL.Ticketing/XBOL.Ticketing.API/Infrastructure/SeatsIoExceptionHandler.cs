using Microsoft.AspNetCore.Diagnostics;
using SeatsioDotNet;
using XBOL.Ticketing.Services;

namespace XBOL.Ticketing.API.Infrastructure
{
    public sealed class SeatsIoExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<SeatsIoExceptionHandler> _logger;

        public SeatsIoExceptionHandler(
            IProblemDetailsService problemDetailsService,
            ILogger<SeatsIoExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (exception is not SeatsioException seatsioException)
            {
                return false;
            }

            SeatsIoProblemDetails problem = seatsioException switch
            {
                RateLimitExceededException ex => BuildSeatsIoProblem(
                    ex,
                    StatusCodes.Status429TooManyRequests,
                    "Too many requests"),

                _ when SeatsIoErrorCodes.IsHoldTokenStaleOrMissing(seatsioException) => BuildSeatsIoProblem(
                    seatsioException,
                    StatusCodes.Status409Conflict,
                    "Hold token expired or unknown"),

                _ when SeatsIoErrorCodes.IsUpstreamFailure(seatsioException) => BuildSeatsIoProblem(
                    seatsioException,
                    StatusCodes.Status502BadGateway,
                    "Upstream Seats.io failure"),

                _ when SeatsIoErrorCodes.IsResourceNotFound(seatsioException) => BuildSeatsIoProblem(
                    seatsioException,
                    StatusCodes.Status404NotFound,
                    "Resource not found"),

                _ => BuildSeatsIoProblem(
                    seatsioException,
                    StatusCodes.Status400BadRequest,
                    "Seats.io request rejected"),
            };

            var logLevel = problem.Status >= 500 ? LogLevel.Error : LogLevel.Warning;
            _logger.Log(logLevel, exception,
                "Translating {ExceptionType} to status {Status}.",
                exception.GetType().Name, problem.Status);

            httpContext.Response.StatusCode = problem.Status!.Value;

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception,
            });
        }

        private static SeatsIoProblemDetails BuildSeatsIoProblem(
            SeatsioException ex, int status, string title) => new()
            {
                Status = status,
                Title = title,
                Detail = ex.Message,
                RequestId = ex.RequestId,
                Errors = ex.Errors?
                .Where(e => e is not null)
                .Select(e => new SeatsIoProblemError
                {
                    Code = e.Code ?? string.Empty,
                    Message = e.Message ?? string.Empty,
                })
                .ToList() ?? [],
            };
    }
}
