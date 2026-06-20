namespace Odasoft.XBOL.Commons.BackgroundJobs;

public sealed class BackgroundJobDiagnosticPing
{
    public required string CorrelationId { get; init; }

    public required string Producer { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
}

public sealed class BackgroundJobDiagnosticEmail
{
    public required string CorrelationId { get; init; }

    public required string Producer { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required string ToAddress { get; init; }

    public required string ToName { get; init; }

    public required string Subject { get; init; }
}
