namespace XBOL.Ticketing.Services.Bundle;

public interface IBundlePassTicketMaterializationService
{
    Task<int> MaterializeIssuedTicketsAsync(
        long bundleId,
        IReadOnlyCollection<long> eventScheduleIds,
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
