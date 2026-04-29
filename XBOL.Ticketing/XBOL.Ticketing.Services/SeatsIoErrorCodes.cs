using SeatsioDotNet;

namespace XBOL.Ticketing.Services;

public static class SeatsIoErrorCodes
{
    public const string UserNotFound = "USER_NOT_FOUND";

    private static readonly HashSet<string> UpstreamFailureCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        UserNotFound,
    };

    // 403 (account disabled) and 5xx return an empty response body per seats.io docs;
    // those reach the handler with Errors.Count == 0 and are mapped to 502 by the fallback.
    public static bool IsUpstreamFailure(SeatsioException ex) =>
        ex.Errors is { Count: > 0 } &&
        ex.Errors.Any(e => e?.Code is { } code && UpstreamFailureCodes.Contains(code));

    public static bool IsHoldTokenStaleOrMissing(SeatsioException ex) =>
        ex.Errors is { Count: > 0 } && ex.Errors.Any(e =>
        {
            string code = e?.Code ?? string.Empty;
            return code.StartsWith("HOLDTOKEN", StringComparison.OrdinalIgnoreCase)
                || code.StartsWith("HOLD_TOKEN", StringComparison.OrdinalIgnoreCase);
        });

    public static bool IsResourceNotFound(SeatsioException ex) =>
        !IsUpstreamFailure(ex)
        && !IsHoldTokenStaleOrMissing(ex)
        && ex.Errors is { Count: > 0 }
        && ex.Errors.Any(e => (e?.Code ?? string.Empty).Contains("NOT_FOUND", StringComparison.OrdinalIgnoreCase));
}
