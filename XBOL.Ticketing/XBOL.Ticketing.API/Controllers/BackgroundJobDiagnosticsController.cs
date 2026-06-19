using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Odasoft.XBOL.Commons.BackgroundJobs;
using System.Security.Cryptography;
using System.Text;
using XBOL.Ticketing.API.Options;

namespace XBOL.Ticketing.API.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/internal/background-jobs/diagnostics")]
public sealed class BackgroundJobDiagnosticsController(
    IBackgroundJobClient backgroundJobClient,
    IOptions<BackgroundJobDiagnosticsOptions> options,
    ILogger<BackgroundJobDiagnosticsController> logger) : ControllerBase
{
    public const string DiagnosticsKeyHeaderName = "X-XBOL-Diagnostics-Key";

    private const string Producer = "ticketing-api";

    [HttpPost("ping")]
    public ActionResult<BackgroundJobDiagnosticEnqueueResponse> RunPing(
        [FromBody] BackgroundJobDiagnosticPingRequest request)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        if (!IsLogSafeCorrelationId(request.CorrelationId))
        {
            return BadRequest(new { message = "CorrelationId must be 1-128 log-safe ASCII characters." });
        }

        var model = new BackgroundJobDiagnosticPing
        {
            CorrelationId = request.CorrelationId,
            Producer = Producer,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        return Enqueue(
            model.CorrelationId,
            "IBackgroundJobDiagnostics.RunPingAsync",
            () => backgroundJobClient.Enqueue<IBackgroundJobDiagnostics>(
                job => job.RunPingAsync(model)));
    }

    [HttpPost("email")]
    public ActionResult<BackgroundJobDiagnosticEnqueueResponse> SendEmailProbe(
        [FromBody] BackgroundJobDiagnosticEmailRequest request)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        if (!IsLogSafeCorrelationId(request.CorrelationId))
        {
            return BadRequest(new { message = "CorrelationId must be 1-128 log-safe ASCII characters." });
        }

        if (string.IsNullOrWhiteSpace(request.ToAddress) ||
            string.IsNullOrWhiteSpace(request.ToName) ||
            string.IsNullOrWhiteSpace(request.Subject))
        {
            return BadRequest(new { message = "ToAddress, ToName, and Subject are required." });
        }

        var model = new BackgroundJobDiagnosticEmail
        {
            CorrelationId = request.CorrelationId,
            Producer = Producer,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ToAddress = request.ToAddress,
            ToName = request.ToName,
            Subject = request.Subject
        };

        return Enqueue(
            model.CorrelationId,
            "IBackgroundJobDiagnostics.SendEmailProbeAsync",
            () => backgroundJobClient.Enqueue<IBackgroundJobDiagnostics>(
                job => job.SendEmailProbeAsync(model)));
    }

    private ActionResult<BackgroundJobDiagnosticEnqueueResponse> Enqueue(
        string correlationId,
        string method,
        Func<string> enqueue)
    {
        logger.LogInformation(
            "Received background-job diagnostic request. CorrelationId={CorrelationId} Method={Method}",
            correlationId,
            method);
        logger.LogInformation(
            "Attempting background-job diagnostic enqueue. CorrelationId={CorrelationId} Method={Method}",
            correlationId,
            method);

        try
        {
            var jobId = enqueue();
            logger.LogInformation(
                "Background-job diagnostic enqueue succeeded. CorrelationId={CorrelationId} Method={Method} JobId={JobId}",
                correlationId,
                method,
                jobId);
            return Ok(new BackgroundJobDiagnosticEnqueueResponse(correlationId, jobId, method));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Background-job diagnostic enqueue failed. CorrelationId={CorrelationId} Method={Method} ExceptionType={ExceptionType}",
                correlationId,
                method,
                ex.GetType().Name);
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "Background job diagnostic enqueue failed.", correlationId, method });
        }
    }

    private bool IsAuthorized()
    {
        if (!Request.Headers.TryGetValue(DiagnosticsKeyHeaderName, out var value))
        {
            return false;
        }

        return ConstantTimeEquals(value.ToString(), options.Value.SharedSecret);
    }

    private static bool ConstantTimeEquals(string submitted, string expected)
    {
        if (string.IsNullOrEmpty(submitted) || string.IsNullOrEmpty(expected))
        {
            return false;
        }

        var submittedBytes = Encoding.UTF8.GetBytes(submitted);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        return submittedBytes.Length == expectedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(submittedBytes, expectedBytes);
    }

    private static bool IsLogSafeCorrelationId(string? correlationId)
    {
        return correlationId is not null &&
            correlationId.Length is > 0 and <= 128 &&
            correlationId.All(c =>
                c is >= 'a' and <= 'z' ||
                c is >= 'A' and <= 'Z' ||
                c is >= '0' and <= '9' ||
                c is '-' or '_' or '.' or ':');
    }
}

public sealed record BackgroundJobDiagnosticPingRequest(string CorrelationId);

public sealed record BackgroundJobDiagnosticEmailRequest(
    string CorrelationId,
    string ToAddress,
    string ToName,
    string Subject);

public sealed record BackgroundJobDiagnosticEnqueueResponse(
    string CorrelationId,
    string JobId,
    string Method);
