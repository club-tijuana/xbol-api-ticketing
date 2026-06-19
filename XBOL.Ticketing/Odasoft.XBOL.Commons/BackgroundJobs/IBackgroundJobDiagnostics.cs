namespace Odasoft.XBOL.Commons.BackgroundJobs;

public interface IBackgroundJobDiagnostics
{
    Task RunPingAsync(BackgroundJobDiagnosticPing model);

    Task SendEmailProbeAsync(BackgroundJobDiagnosticEmail model);
}
