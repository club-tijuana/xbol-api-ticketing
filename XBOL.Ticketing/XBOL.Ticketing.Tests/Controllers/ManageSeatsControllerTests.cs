using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using XBOL.Ticketing.API.Controllers;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Services.Booking;

namespace XBOL.Ticketing.Tests.Controllers;

public class ManageSeatsControllerTests
{
    [Fact]
    public async Task BookSeatsActionAsync_DelegatesToBookingOrchestrationAndReturnsResult()
    {
        var bookingService = Substitute.For<IBookingOrchestrationService>();
        var request = new BookSeatsActionRequest
        {
            EventKey = "schedule-100",
            EventScheduleId = 100,
            HoldToken = "hold-123",
            TicketType = ItemType.Ticket,
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 125m, PriceListItemId = 1 } },
            ClientContact = new ClientInfoRequest(),
            PaymentInfoRequest = new PaymentInfoRequest(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };
        var response = new BookingResultResponse
        {
            OrderId = 12,
            Reference = "ORD-E-100-000001",
            BookedSeatKeys = ["A-1"],
            TicketIds = [34],
            ClientId = 56,
            Total = 125m
        };
        bookingService.BookAsync(request, Guid.Empty, Arg.Any<CancellationToken>())
            .Returns(response);

        var controller = new ManageSeatsController(null!, bookingService);

        var result = await controller.BookSeatsActionAsync(request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(response);
        await bookingService.Received(1).BookAsync(
            request,
            Guid.Empty,
            Arg.Any<CancellationToken>());
    }
}
