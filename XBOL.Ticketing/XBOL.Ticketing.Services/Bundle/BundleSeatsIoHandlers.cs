using Microsoft.Extensions.Logging;
using SeatsioDotNet;
using Wolverine.Attributes;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Services.Messages;

namespace XBOL.Ticketing.Services.Bundle;

public class CreateSeatsIoSeasonHandler(
    IBundleRepository bundleRepository,
    ISeatsIoSeasonLifecycleClient seatsIo,
    ILogger<CreateSeatsIoSeasonHandler> logger)
{
    [Transactional]
    public async Task Handle(CreateSeatsIoSeasonCommand command)
    {
        var bundle = await bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(command.BundleId)
            ?? throw new KeyNotFoundException($"Bundle {command.BundleId} not found.");

        if (bundle.BundleType != BundleType.SeasonPass)
        {
            throw new InvalidOperationException(
                $"Bundle {bundle.Id} is {bundle.BundleType}; only SeasonPass bundles create Seats.io seasons.");
        }

        var chartKey = ResolveChartKey(bundle);
        var seasonKey = string.IsNullOrWhiteSpace(bundle.ExternalKey)
            ? $"season-{bundle.Id}"
            : bundle.ExternalKey;
        var links = bundle.BundleEventSchedules
            .OrderBy(link => link.SortOrder ?? int.MaxValue)
            .ThenBy(link => link.EventScheduleId)
            .ToList();
        if (links.Count == 0)
        {
            throw new InvalidOperationException(
                $"Bundle {bundle.Id} cannot be published because it has no linked event schedules.");
        }

        var eventKeys = links
            .Select(link => $"{seasonKey}-schedule-{link.EventScheduleId}")
            .ToArray();

        await seatsIo.CreateSeatsIoSeasonAsync(chartKey, seasonKey, eventKeys);
        logger.LogInformation(
            "Created Seats.io season {SeasonKey} for Bundle {BundleId}.",
            seasonKey,
            bundle.Id);

        try
        {
            var now = DateTimeOffset.UtcNow;
            bundle.ExternalKey = seasonKey;
            bundle.Status = EventStatus.Published;
            bundle.PublishedDate ??= now;

            for (var index = 0; index < links.Count; index++)
            {
                BundleSeatsIoSchedulePublisher.PublishSeasonSchedule(
                    links[index].EventSchedule,
                    eventKeys[index],
                    now);
            }

            bundle.UpdatedAt = now;
            await bundleRepository.UpdateAsync(bundle);
        }
        catch
        {
            await seatsIo.DeleteSeatsIoSeasonAsync(seasonKey);
            throw;
        }
    }

    private static string ResolveChartKey(XBOL.Ticketing.Core.Model.Bundle bundle)
    {
        return bundle.VenueMap?.ExternalMapKey
            ?? throw new InvalidOperationException(
                $"Bundle {bundle.Id} cannot be published because it has no VenueMap.ExternalMapKey.");
    }
}

public class AddEventsToSeasonHandler(
    IBundleRepository bundleRepository,
    ISeatsIoSeasonLifecycleClient seatsIo,
    ILogger<AddEventsToSeasonHandler> logger)
{
    [Transactional]
    public async Task Handle(AddEventsToSeasonCommand command)
    {
        var bundle = await bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(command.BundleId)
            ?? throw new KeyNotFoundException($"Bundle {command.BundleId} not found.");

        if (bundle.BundleType != BundleType.SeasonPass)
        {
            throw new InvalidOperationException(
                $"Bundle {bundle.Id} is {bundle.BundleType}; only SeasonPass bundles own Seats.io season events.");
        }

        var seasonKey = bundle.ExternalKey
            ?? throw new InvalidOperationException($"Bundle {bundle.Id} has no Seats.io season key.");

        var scheduleIdSet = command.EventScheduleIds.ToHashSet();
        var links = bundle.BundleEventSchedules
            .Where(link => scheduleIdSet.Contains(link.EventScheduleId))
            .OrderBy(link => link.SortOrder ?? int.MaxValue)
            .ThenBy(link => link.EventScheduleId)
            .ToList();

        if (links.Count != scheduleIdSet.Count)
        {
            var linkedIds = links.Select(link => link.EventScheduleId).ToHashSet();
            var missingIds = scheduleIdSet.Except(linkedIds);
            throw new KeyNotFoundException(
                $"Bundle {bundle.Id} does not contain EventSchedule(s): {string.Join(", ", missingIds)}.");
        }

        var eventKeys = links
            .Select(link => $"{seasonKey}-schedule-{link.EventScheduleId}")
            .ToArray();

        await seatsIo.CreateSeatsIoEventsInSeasonAsync(seasonKey, eventKeys);
        logger.LogInformation(
            "Created {EventCount} Seats.io event(s) in season {SeasonKey} for Bundle {BundleId}.",
            eventKeys.Length,
            seasonKey,
            bundle.Id);

        try
        {
            var now = DateTimeOffset.UtcNow;
            for (var index = 0; index < links.Count; index++)
            {
                BundleSeatsIoSchedulePublisher.PublishSeasonSchedule(
                    links[index].EventSchedule,
                    eventKeys[index],
                    now);
            }

            bundle.UpdatedAt = now;
            await bundleRepository.UpdateAsync(bundle);
        }
        catch
        {
            foreach (var eventKey in eventKeys)
            {
                await seatsIo.DeleteSeatsIoEventAsync(eventKey);
            }

            throw;
        }
    }
}

internal static class BundleSeatsIoSchedulePublisher
{
    public static void PublishSeasonSchedule(
        EventSchedule schedule,
        string eventKey,
        DateTimeOffset publishedAt)
    {
        if (schedule.Status != ScheduleStatus.OnSale)
        {
            ScheduleStatusTransitions.ValidateTransition(schedule.Status, ScheduleStatus.OnSale);
        }

        schedule.ExternalEventKey = eventKey;
        schedule.Status = ScheduleStatus.OnSale;
        schedule.PublishedDate ??= publishedAt;
    }
}

public class DeleteSeatsIoSeasonHandler(
    IBundleRepository bundleRepository,
    ISeatsIoSeasonLifecycleClient seatsIo,
    ILogger<DeleteSeatsIoSeasonHandler> logger)
{
    [Transactional]
    public async Task Handle(DeleteSeatsIoSeasonCommand command)
    {
        var bundle = await bundleRepository.GetByIdWithVenueMapAndSchedulesAsync(command.BundleId)
            ?? throw new KeyNotFoundException($"Bundle {command.BundleId} not found.");

        if (bundle.BundleType != BundleType.SeasonPass)
        {
            throw new InvalidOperationException(
                $"Bundle {bundle.Id} is {bundle.BundleType}; only SeasonPass bundles own Seats.io seasons.");
        }

        var seasonKey = bundle.ExternalKey;
        if (!string.IsNullOrWhiteSpace(seasonKey))
        {
            try
            {
                await seatsIo.DeleteSeatsIoSeasonAsync(seasonKey);
            }
            catch (SeatsioException ex) when (SeatsIoErrorCodes.IsResourceNotFound(ex))
            {
                logger.LogInformation(ex,
                    "Seats.io season {SeasonKey} for Bundle {BundleId} was already absent.",
                    seasonKey,
                    bundle.Id);
            }
        }

        var ownedEventKeyPrefix = $"{seasonKey}-schedule-";
        foreach (var link in bundle.BundleEventSchedules)
        {
            if (link.EventSchedule.ExternalEventKey?.StartsWith(
                    ownedEventKeyPrefix,
                    StringComparison.Ordinal) == true)
            {
                link.EventSchedule.ExternalEventKey = null;
            }
        }

        bundle.ExternalKey = null;
        bundle.Status = EventStatus.Cancelled;
        bundle.UpdatedAt = DateTimeOffset.UtcNow;
        bundle.UpdatedBy = command.UserId;

        await bundleRepository.UpdateAsync(bundle);
    }
}

public class UpdateSeatsIoSeasonHandler(
    IBundleRepository bundleRepository,
    ISeatsIoSeasonLifecycleClient seatsIo,
    ILogger<UpdateSeatsIoSeasonHandler> logger)
{
    [Transactional]
    public async Task Handle(UpdateSeatsIoSeasonCommand command)
    {
        var bundle = await bundleRepository.GetByIdAsync(command.BundleId)
            ?? throw new KeyNotFoundException($"Bundle {command.BundleId} not found.");

        if (bundle.BundleType != BundleType.SeasonPass)
        {
            throw new InvalidOperationException(
                $"Bundle {bundle.Id} is {bundle.BundleType}; only SeasonPass bundles own Seats.io season metadata.");
        }

        if (bundle.Status != EventStatus.Published || string.IsNullOrWhiteSpace(bundle.ExternalKey))
        {
            return;
        }

        try
        {
            await seatsIo.UpdateSeatsIoSeasonAsync(bundle.ExternalKey, bundle.Name);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to update Seats.io season {SeasonKey} metadata for Bundle {BundleId}; keeping local update.",
                bundle.ExternalKey,
                bundle.Id);
        }
    }
}
