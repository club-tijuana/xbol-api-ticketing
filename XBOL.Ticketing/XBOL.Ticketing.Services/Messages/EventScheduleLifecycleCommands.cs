namespace XBOL.Ticketing.Services.Messages;

public enum SeatsIoEventDeletionMode
{
    Close,
    SoftDelete
}

public record CreateSeatsIoEventCommand(long EventScheduleId, Guid UserId);

public record UpdateSeatsIoEventCommand(long EventScheduleId);

public record DeleteSeatsIoEventCommand(
    long EventScheduleId,
    Guid UserId,
    SeatsIoEventDeletionMode Mode);

public record CreateSeatsIoSeasonCommand(long BundleId, Guid UserId);

public record AddEventsToSeasonCommand(long BundleId, long[] EventScheduleIds);

public record DeleteSeatsIoSeasonCommand(long BundleId, Guid UserId);

public record UpdateSeatsIoSeasonCommand(long BundleId);
