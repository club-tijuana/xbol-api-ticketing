using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Odasoft.XBOL.Commons.BackgroundJobs;
using XBOL.Ticketing.API.Controllers;
using XBOL.Ticketing.API.Options;

namespace XBOL.Ticketing.Tests.Controllers;

public sealed class BackgroundJobDiagnosticsControllerTests
{
    private const string DiagnosticsKey = "diagnostics-secret";
    private const string CorrelationId = "ticketing-hangfire-probe-20260619-001";

    [Fact]
    public void RunPing_WhenDiagnosticsKeyMissing_ReturnsUnauthorizedAndDoesNotEnqueue()
    {
        var backgroundJobs = Substitute.For<IBackgroundJobClient>();
        var controller = CreateController(backgroundJobs);

        var result = controller.RunPing(new BackgroundJobDiagnosticPingRequest(CorrelationId));

        result.Result.Should().BeOfType<UnauthorizedResult>();
        backgroundJobs.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    [Fact]
    public void RunPing_WhenAuthorized_EnqueuesPingJobAndReturnsJobId()
    {
        var createdJobs = new List<Job>();
        var backgroundJobs = Substitute.For<IBackgroundJobClient>();
        backgroundJobs
            .Create(Arg.Do<Job>(createdJobs.Add), Arg.Any<EnqueuedState>())
            .Returns("job-123");
        var controller = CreateController(backgroundJobs);
        SetDiagnosticsKey(controller);

        var result = controller.RunPing(new BackgroundJobDiagnosticPingRequest(CorrelationId));

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<BackgroundJobDiagnosticEnqueueResponse>().Subject;
        response.CorrelationId.Should().Be(CorrelationId);
        response.JobId.Should().Be("job-123");
        response.Method.Should().Be("IBackgroundJobDiagnostics.RunPingAsync");

        var job = createdJobs.Should().ContainSingle().Subject;
        job.Type.Should().Be(typeof(IBackgroundJobDiagnostics));
        job.Method.Name.Should().Be(nameof(IBackgroundJobDiagnostics.RunPingAsync));
        var payload = job.Args.Should().ContainSingle().Subject
            .Should().BeOfType<BackgroundJobDiagnosticPing>().Subject;
        payload.CorrelationId.Should().Be(CorrelationId);
        payload.Producer.Should().Be("ticketing-api");
    }

    [Fact]
    public void SendEmailProbe_WhenAuthorized_EnqueuesEmailProbeAndReturnsJobId()
    {
        var createdJobs = new List<Job>();
        var backgroundJobs = Substitute.For<IBackgroundJobClient>();
        backgroundJobs
            .Create(Arg.Do<Job>(createdJobs.Add), Arg.Any<EnqueuedState>())
            .Returns("job-456");
        var controller = CreateController(backgroundJobs);
        SetDiagnosticsKey(controller);

        var result = controller.SendEmailProbe(new BackgroundJobDiagnosticEmailRequest(
            CorrelationId,
            "probe@example.test",
            "Probe User",
            "XBOL diagnostic email"));

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<BackgroundJobDiagnosticEnqueueResponse>().Subject;
        response.CorrelationId.Should().Be(CorrelationId);
        response.JobId.Should().Be("job-456");
        response.Method.Should().Be("IBackgroundJobDiagnostics.SendEmailProbeAsync");

        var job = createdJobs.Should().ContainSingle().Subject;
        job.Type.Should().Be(typeof(IBackgroundJobDiagnostics));
        job.Method.Name.Should().Be(nameof(IBackgroundJobDiagnostics.SendEmailProbeAsync));
        var payload = job.Args.Should().ContainSingle().Subject
            .Should().BeOfType<BackgroundJobDiagnosticEmail>().Subject;
        payload.CorrelationId.Should().Be(CorrelationId);
        payload.ToAddress.Should().Be("probe@example.test");
    }

    [Fact]
    public void RunPing_WhenEnqueueFails_ReturnsServiceUnavailableAndLogsExceptionType()
    {
        var backgroundJobs = Substitute.For<IBackgroundJobClient>();
        backgroundJobs
            .Create(Arg.Any<Job>(), Arg.Any<EnqueuedState>())
            .Returns(_ => throw new InvalidOperationException("storage unavailable"));
        var logger = Substitute.For<ILogger<BackgroundJobDiagnosticsController>>();
        logger.IsEnabled(LogLevel.Error).Returns(true);
        var controller = CreateController(backgroundJobs, logger);
        SetDiagnosticsKey(controller);

        var result = controller.RunPing(new BackgroundJobDiagnosticPingRequest(CorrelationId));

        var serviceUnavailable = result.Result.Should().BeOfType<ObjectResult>().Subject;
        serviceUnavailable.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        RenderedLogMessages(logger, LogLevel.Error).Should().Contain(message =>
            message.Contains(CorrelationId, StringComparison.Ordinal) &&
            message.Contains(nameof(InvalidOperationException), StringComparison.Ordinal));
    }

    [Fact]
    public void RunPing_WhenCorrelationIdIsNotLogSafe_ReturnsBadRequestAndDoesNotEnqueue()
    {
        var backgroundJobs = Substitute.For<IBackgroundJobClient>();
        var controller = CreateController(backgroundJobs);
        SetDiagnosticsKey(controller);

        var result = controller.RunPing(new BackgroundJobDiagnosticPingRequest("bad correlation"));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        backgroundJobs.DidNotReceiveWithAnyArgs().Create(default!, default!);
    }

    private static BackgroundJobDiagnosticsController CreateController(
        IBackgroundJobClient backgroundJobs,
        ILogger<BackgroundJobDiagnosticsController>? logger = null)
    {
        return new BackgroundJobDiagnosticsController(
            backgroundJobs,
            Options.Create(new BackgroundJobDiagnosticsOptions { SharedSecret = DiagnosticsKey }),
            logger ?? Substitute.For<ILogger<BackgroundJobDiagnosticsController>>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static void SetDiagnosticsKey(ControllerBase controller)
    {
        controller.Request.Headers[BackgroundJobDiagnosticsController.DiagnosticsKeyHeaderName] = DiagnosticsKey;
    }

    private static List<string> RenderedLogMessages(
        ILogger logger,
        LogLevel logLevel)
    {
        return logger.ReceivedCalls()
            .Where(call =>
                call.GetMethodInfo().Name == nameof(ILogger.Log) &&
                call.GetArguments()[0] is LogLevel level &&
                level == logLevel)
            .Select(call => call.GetArguments()[2]?.ToString() ?? "")
            .ToList();
    }
}
