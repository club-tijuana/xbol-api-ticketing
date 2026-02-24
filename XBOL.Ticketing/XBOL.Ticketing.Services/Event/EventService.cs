using Microsoft.AspNetCore.Identity;
using SeatsioDotNet.Events;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Commons.Views;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.Model;
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
                              SeatsIoService _seatsIoService)
        : BaseService<EventRepository, Core.Model.Event>(repository)
    {
        internal async Task<IList<DynamicPricingEvent>> GetDynamicPricingData(long eventId) => await Repository.GetDynamicPricingData(eventId);

        // TODO: move to OrderService
        public async Task BookSeatsAsync(BookingRequest request)
        {
            ChangeObjectStatusResult result = await _seatsIoService.BookSeatsAsync(request);

            // TODO: Get seller from Identity, and seller email from request, create custom NotFoundException
            User? buyer = await userManager.FindByEmailAsync("admin@xbol.com") ?? throw new KeyNotFoundException();

            User? seller = buyer;

            EventSchedule schedule = eventScheduleRepository.Get(x => x.ExternalEventKey == request.EventId).First();

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

        public async Task<string?> GetEventKeyAsync(long eventId)
        {
            return await Repository.GetEventKeyAsync(eventId);
        }

        public async Task<EventListItem?> GetEventByIdAsync(long id) =>
            await Repository.GetEventByIdAsync(id);

        public async Task<PagedResponse<EventListItem>> GetEventListAsync(
            List<string>? venues = null,
            List<EventCategory>? categories = null,
            DateTimeOffset? startDate = null,
            DateTimeOffset? endDate = null,
            string? search = null,
            string sortBy = "dateTime",
            bool descending = false,
            int page = 1,
            int pageSize = 20
        )
        {
            var (items, totalCount) = await Repository.GetEventListAsync(
                venues,
                categories,
                startDate,
                endDate,
                search,
                sortBy,
                descending,
                page,
                pageSize
            );

            return new PagedResponse<EventListItem>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }
    }
}
