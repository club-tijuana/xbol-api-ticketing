using Microsoft.AspNetCore.Identity;
using SeatsioDotNet.Events;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Commons.Views;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Abstractions;
using XBOL.Ticketing.Data.Repositories.Event;
using XBOL.Ticketing.Data.Repositories.Order;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Event
{
    public class EventService(EventRepository repository,
                              UserManager<User> userManager,
                              OrderRepository orderRepository,
                              EventSeatRepository eventSeatRepository,
                              EventScheduleRepository eventScheduleRepository,
                              SeatsIoService _seatsIoService,
                              IEventScheduleLifecycleService scheduleLifecycleService,
                              IBundleEventScheduleRepository bundleEventScheduleRepository)
        : BaseService<EventRepository, Core.Model.Event>(repository)
    {
        internal async Task<IList<DynamicPricingEvent>> GetDynamicPricingData(long eventId) => await Repository.GetDynamicPricingData(eventId);

        // TODO: move to OrderService
        [Obsolete]
        public async Task BookEventSeatsAsync(EventBookingRequest request)
        {
            ChangeObjectStatusResult result = await _seatsIoService.BookEventSeatsAsync(request);

            // TODO: Get seller from Identity, and seller email from request, create custom NotFoundException
            User? buyer = await userManager.FindByEmailAsync("admin@xbol.com") ?? throw new KeyNotFoundException();

            User? seller = buyer;

            EventSchedule schedule = eventScheduleRepository.Get(x => x.ExternalEventKey == request.EventKey).First();

            // TODO: Calculate total, taxes, and fees

            // Create Order
            var newOrder = new Core.Model.Order
            {
                UserId = buyer.Id,
                Reference = request.HoldToken,
                Status = OrderStatus.Pending,
                SubTotal = 0,
                TotalFees = 0,
                TotalTaxes = 0,
                Total = 0,
                OrderType = OrderType.Ticket,
                PayformType = PayformType.BoxOffice,

                CreatedAt = DateTimeOffset.Now,
                CreatedBy = seller.Id,
                UpdatedAt = DateTimeOffset.Now,
                UpdatedBy = seller.Id,
                Items = [.. request.Seats.Select(x => new OrderItem
                    {
                        ItemType = ItemType.Ticket,
                    // TODO: Check what is this field
                        ItemReferenceId = 0,
                        Price = 0
                    })]
            };

            // TODO: Confirm if Ticket creation should be done after payment confirmation
            foreach (var item in result.Objects)
            {
                // TODO: Get all seats info and handle in memory to avoid multiple calls to database
                EventSeat seat = await eventSeatRepository.GetByExternalSeatObjectKey(item.Key) ?? throw new KeyNotFoundException();

                newOrder.Tickets.Add(new Core.Model.Ticket
                {
                    EventSeatId = seat.Id,
                    Status = TicketStatus.Issued,
                    EventScheduleId = schedule.Id,
                    EventSectionId = seat.EventSectionId,
                });
            }

            await orderRepository.InsertAsync(newOrder);
            await orderRepository.CommitAsync();

            // Process Payment
        }

        public async Task<string?> GetEventKeyAsync(long eventScheduleId)
        {
            return await Repository.GetEventScheduleKeyAsync(eventScheduleId);
        }

        public async Task<string?> GetSeasonKeyAsync(long seasonId)
        {
            return await Repository.GetSeasonKeyAsync(seasonId);
        }

        public async Task<bool> CancelEventAsync(long id, Guid userId, CancellationToken cancellation = default)
        {
            var existingEvent = await Repository.GetByIdWithSchedulesAsync(id);
            if (existingEvent is null)
            {
                return false;
            }

            EventStatusTransitions.ValidateTransition(existingEvent.Status, EventStatus.Cancelled);

            foreach (var schedule in existingEvent.Schedules.OrderBy(s => s.Id))
            {
                var links = await bundleEventScheduleRepository.GetByEventScheduleIdAsync(schedule.Id);
                if (links.Any(link => link.Bundle?.BundleType == BundleType.SeasonPass))
                {
                    continue;
                }

                await scheduleLifecycleService.CancelAsync(schedule.Id, userId, cancellation);
            }

            existingEvent.Status = EventStatus.Cancelled;
            existingEvent.UpdatedAt = DateTimeOffset.UtcNow;
            existingEvent.UpdatedBy = userId;
            await Repository.UpdateAsync(existingEvent);

            return true;
        }
    }
}
