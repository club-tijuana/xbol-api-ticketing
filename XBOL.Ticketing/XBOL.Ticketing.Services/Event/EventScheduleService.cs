using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Results;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Repositories.Event;

using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Event
{
    public class EventScheduleService(
        EventScheduleRepository repository,
        XBOLDbContext dbContext,
        IEventScheduleLifecycleService lifecycleService)
        : BaseService<EventScheduleRepository, EventSchedule>(repository)
    {
        public async Task<EventScheduleDTO?> GetScheduleByIdAsync(long id)
        {
            var schedule = await dbContext.EventSchedules
                .Include(s => s.Sections)
                .FirstOrDefaultAsync(s => s.Id == id);

            return schedule is null ? null : ToDto(schedule);
        }

        public async Task<EventScheduleResponse> CreateEventScheduleAsync(
            EventScheduleRequest request,
            Guid userId)
        {
            var now = DateTimeOffset.UtcNow;
            var scheduleEvent = await dbContext.Events
                .FirstOrDefaultAsync(x => x.Id == request.EventId)
                ?? throw new KeyNotFoundException($"Event {request.EventId} not found.");

            var newSchedule = new EventSchedule
            {
                EventId = scheduleEvent.Id,
                PreSaleStartDate = request.PreSaleStartDate?.ToUniversalTime(),
                PreSaleEndDate = request.PreSaleEndDate?.ToUniversalTime(),
                OnSaleDate = request.OnSaleDate.ToUniversalTime(),
                OffSaleDate = request.OffSaleDate.ToUniversalTime(),
                PublishedDate = request.PublishedDate?.ToUniversalTime(),
                GateOpenDate = request.GateOpenDate?.ToUniversalTime(),
                StartDateTime = request.StartDateTime.ToUniversalTime(),
                EndDateTime = request.EndDateTime.ToUniversalTime(),
                GameCategory = request.GameCategory,
                Status = ScheduleStatus.Draft,
                HoldExpirationInMinutes = request.HoldExpirationInMinutes,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            List<EventSection> sections = await dbContext.BaseSections
                .Where(x => x.BaseZone.VenueMapId == scheduleEvent.VenueMapId)
                .Select(x => new EventSection
                {
                    BaseSectionId = x.Id,
                    TotalSeats = 0,
                    AvailableSeats = 0,
                    DisplayName = $"{x.BaseZone.Name} {x.Name}",
                })
                .ToListAsync();

            foreach (EventSection section in sections)
            {
                section.EventSeats = await dbContext.BaseSeats
                    .Where(s => s.BaseRow.BaseSectionId == section.BaseSectionId)
                    .Select(s => new EventSeat
                    {
                        BaseSeatId = s.Id,
                        ForSale = true, // For now every seat is for sale.
                        ExternalSeatObjectKey = $"{s.BaseRow.BaseSection.Name}-{s.BaseRow.RowLabel}-{s.SeatNumber}", // Generate SeatsIo object key
                    })
                    .ToListAsync();
            }

            newSchedule.Sections = sections;

            await Repository.InsertAsync(newSchedule);
            await Repository.CommitAsync();

            return new EventScheduleResponse { Id = newSchedule.Id };
        }

        public async Task<bool> UpdateScheduleAsync(
            long id,
            EventScheduleRequest request,
            Guid userId)
        {
            var existingSchedule = await Repository.GetByIdAsync(id);
            if (existingSchedule is null)
            {
                return false;
            }

            existingSchedule.PreSaleStartDate = request.PreSaleStartDate?.ToUniversalTime();
            existingSchedule.PreSaleEndDate = request.PreSaleEndDate?.ToUniversalTime();
            existingSchedule.OnSaleDate = request.OnSaleDate.ToUniversalTime();
            existingSchedule.OffSaleDate = request.OffSaleDate.ToUniversalTime();
            existingSchedule.PublishedDate = request.PublishedDate?.ToUniversalTime();
            existingSchedule.GateOpenDate = request.GateOpenDate?.ToUniversalTime();
            existingSchedule.StartDateTime = request.StartDateTime.ToUniversalTime();
            existingSchedule.EndDateTime = request.EndDateTime.ToUniversalTime();
            existingSchedule.GameCategory = request.GameCategory;
            existingSchedule.HoldExpirationInMinutes = request.HoldExpirationInMinutes;
            existingSchedule.UpdatedAt = DateTimeOffset.UtcNow;
            existingSchedule.UpdatedBy = userId;

            await Repository.UpdateAsync(existingSchedule);
            await lifecycleService.SyncMetadataAsync(id);

            return true;
        }

        public async Task PublishScheduleAsync(long id, Guid userId)
        {
            await lifecycleService.PublishAsync(id, userId);
        }

        public async Task CancelScheduleAsync(long id, Guid userId)
        {
            await lifecycleService.CancelAsync(id, userId);
        }

        public async Task<bool> DeleteScheduleAsync(long id, Guid userId)
        {
            var existingSchedule = await Repository.GetByIdAsync(id);
            if (existingSchedule is null)
            {
                return false;
            }

            await lifecycleService.DeleteAsync(id, userId);
            return true;
        }

        private static EventScheduleDTO ToDto(EventSchedule schedule)
        {
            return new EventScheduleDTO
            {
                Id = schedule.Id,
                StartDateTime = schedule.StartDateTime,
                EndDateTime = schedule.EndDateTime,
                PublishedDate = schedule.PublishedDate,
                PreSaleStartDate = schedule.PreSaleStartDate,
                PreSaleEndDate = schedule.PreSaleEndDate,
                OnSaleDate = schedule.OnSaleDate,
                OffSaleDate = schedule.OffSaleDate,
                GateOpenDate = schedule.GateOpenDate,
                ExternalEventKey = schedule.ExternalEventKey,
                Status = schedule.Status,
                TotalSeats = schedule.Sections.Sum(s => s.TotalSeats),
                AvailableSeats = schedule.Sections.Sum(s => s.AvailableSeats)
            };
        }
    }
}
