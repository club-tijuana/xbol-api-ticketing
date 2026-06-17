using Microsoft.Extensions.Logging;
using SeatsioDotNet;
using Wolverine.Attributes;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Services.Messages;

namespace XBOL.Ticketing.Services.Event;

public class CreateSeatsIoEventHandler(
    IEventScheduleRepository eventScheduleRepository,
    ISeatsIoEventLifecycleClient seatsIo,
    ILogger<CreateSeatsIoEventHandler> logger)
{
    [Transactional]
    public async Task Handle(CreateSeatsIoEventCommand command)
    {
        var schedule = await eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(command.EventScheduleId)
            ?? throw new KeyNotFoundException($"EventSchedule {command.EventScheduleId} not found.");

        if (!string.IsNullOrWhiteSpace(schedule.ExternalEventKey))
        {
            schedule.Status = ScheduleStatus.OnSale;
            schedule.PublishedDate ??= DateTimeOffset.UtcNow;
            await eventScheduleRepository.UpdateAsync(schedule);
            return;
        }

        var chartKey = ResolveChartKey(schedule);
        var eventKey = $"schedule-{schedule.Id}";
        var eventDate = ToSeatsIoDate(schedule.StartDateTime);

        try
        {
            await seatsIo.CreateSeatsIoEventAsync(chartKey, eventKey, schedule.Event.Name, eventDate);
            logger.LogInformation(
                "Created Seats.io event {EventKey} for EventSchedule {EventScheduleId}.",
                eventKey,
                schedule.Id);
        }
        catch (SeatsioException ex) when (SeatsIoErrorCodes.IsEventKeyAlreadyExists(ex))
        {
            logger.LogInformation(ex,
                "Seats.io event {EventKey} for EventSchedule {EventScheduleId} already exists; linking to existing event.",
                eventKey,
                schedule.Id);
        }

        schedule.ExternalEventKey = eventKey;
        schedule.Status = ScheduleStatus.OnSale;
        schedule.PublishedDate ??= DateTimeOffset.UtcNow;

        try
        {
            await eventScheduleRepository.UpdateAsync(schedule);
        }
        catch
        {
            await seatsIo.DeleteSeatsIoEventAsync(eventKey);
            throw;
        }
    }

    private static string ResolveChartKey(EventSchedule schedule)
    {
        return schedule.Event.VenueMap?.ExternalMapKey
            ?? throw new InvalidOperationException(
                $"EventSchedule {schedule.Id} cannot be published because its Event has no VenueMap.ExternalMapKey.");
    }

    private static DateOnly ToSeatsIoDate(DateTimeOffset value)
    {
        return DateOnly.FromDateTime(value.UtcDateTime);
    }
}

public class UpdateSeatsIoEventHandler(
    IEventScheduleRepository eventScheduleRepository,
    ISeatsIoEventLifecycleClient seatsIo,
    ILogger<UpdateSeatsIoEventHandler> logger)
{
    [Transactional]
    public async Task Handle(UpdateSeatsIoEventCommand command)
    {
        var schedule = await eventScheduleRepository.GetByIdWithEventAndVenueMapAsync(command.EventScheduleId)
            ?? throw new KeyNotFoundException($"EventSchedule {command.EventScheduleId} not found.");

        if (string.IsNullOrWhiteSpace(schedule.ExternalEventKey))
        {
            return;
        }

        try
        {
            await seatsIo.UpdateSeatsIoEventAsync(
                schedule.ExternalEventKey,
                schedule.Event.Name,
                DateOnly.FromDateTime(schedule.StartDateTime.UtcDateTime));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to update Seats.io event {EventKey} for EventSchedule {EventScheduleId}.",
                schedule.ExternalEventKey,
                schedule.Id);
        }
    }
}

public class DeleteSeatsIoEventHandler(
    IEventScheduleRepository eventScheduleRepository,
    ISeatsIoEventLifecycleClient seatsIo,
    ILogger<DeleteSeatsIoEventHandler> logger)
{
    [Transactional]
    public async Task Handle(DeleteSeatsIoEventCommand command)
    {
        var schedule = await eventScheduleRepository.GetByIdIncludingDeletedAsync(command.EventScheduleId)
            ?? throw new KeyNotFoundException($"EventSchedule {command.EventScheduleId} not found.");

        var eventKey = schedule.ExternalEventKey;
        if (command.Mode == SeatsIoEventDeletionMode.Close && schedule.Status != ScheduleStatus.Closed)
        {
            ScheduleStatusTransitions.ValidateTransition(schedule.Status, ScheduleStatus.Closed);
        }

        if (!string.IsNullOrWhiteSpace(eventKey))
        {
            try
            {
                await seatsIo.DeleteSeatsIoEventAsync(eventKey);
            }
            catch (SeatsioException ex) when (SeatsIoErrorCodes.IsResourceNotFound(ex))
            {
                logger.LogInformation(ex,
                    "Seats.io event {EventKey} for EventSchedule {EventScheduleId} was already absent.",
                    eventKey,
                    schedule.Id);
            }
        }

        schedule.ExternalEventKey = null;

        if (command.Mode == SeatsIoEventDeletionMode.SoftDelete)
        {
            schedule.DeletedAt ??= DateTimeOffset.UtcNow;
        }
        else if (schedule.Status != ScheduleStatus.Closed)
        {
            schedule.Status = ScheduleStatus.Closed;
        }

        await eventScheduleRepository.UpdateAsync(schedule);
    }
}
