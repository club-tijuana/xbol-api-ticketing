using FluentAssertions;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Odasoft.XBOL.Commons.Email;
using Odasoft.XBOL.Commons.Requests;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.Commons.Options;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Repositories;
using XBOL.Ticketing.Services.Booking;
using XBOL.Ticketing.Services.Email;
using XBOL.Ticketing.Services.Odasoft.XBOL.Business.Services;

namespace XBOL.Ticketing.Tests.Services;

public class BookingOrchestrationServiceTests
{
    [Fact]
    public async Task BookAsync_TicketRequest_BooksSeatsAndPersistsPaidOrderTicketsAndItems()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedStandaloneEventAsync(context);

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        bookingClient.BookSeatsAsync(
                "schedule-100",
                Arg.Any<List<BookingSeatRequest>>(),
                "hold-123",
                Arg.Any<CancellationToken>())
            .Returns(["A-1", "A-2"]);

        var backgroundJobs = Substitute.For<IBackgroundJobClient>();
        var sut = CreateService(context, bookingClient, backgroundJobs);
        var actorUserId = Guid.NewGuid();
        var request = new BookSeatsActionRequest
        {
            EventKey = "schedule-100",
            EventScheduleId = 100,
            HoldToken = "hold-123",
            TicketType = ItemType.Ticket,
            Localizer = "ORD-E-100-000001",
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 125m, PriceListItemId = 1 }, new BookingSeatRequest { SeatKey = "A-2", SeatPrice = 175m, PriceListItemId = 2 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "buyer@example.com",
                FirstName = "Rita",
                LastName = "Moreno",
                PhoneNumber = "(555) 222-0100"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var result = await sut.BookAsync(request, actorUserId);

        result.Reference.Should().Be("ORD-E-100-000001");
        result.BookedSeatKeys.Should().BeEquivalentTo(["A-1", "A-2"]);
        result.TicketIds.Should().HaveCount(2);

        await bookingClient.Received(1).BookSeatsAsync(
            "schedule-100",
            Arg.Is<List<BookingSeatRequest>>(seats =>
                seats.Count == 2 &&
                seats.Any(s => s.SeatKey == "A-1" && s.SeatPrice == 125m) &&
                seats.Any(s => s.SeatKey == "A-2" && s.SeatPrice == 175m)),
            "hold-123",
            Arg.Any<CancellationToken>());

        var order = await context.Orders
            .Include(o => o.Client)
            .Include(o => o.Items)
            .Include(o => o.Tickets)
            .SingleAsync();

        order.Id.Should().Be(result.OrderId);
        order.Reference.Should().Be("ORD-E-100-000001");
        order.Status.Should().Be(OrderStatus.Paid);
        order.OrderType.Should().Be(OrderType.Ticket);
        order.SaleChannel.Should().Be(SaleChannel.BoxOffice);
        order.SubTotal.Should().Be(300m);
        order.Total.Should().Be(300m);
        order.CreatedBy.Should().Be(actorUserId);
        order.Client.Email.Should().Be("buyer@example.com");
        order.Client.FullName.Should().Be("Rita Moreno");
        order.Client.PhoneNumber.Should().Be("5552220100");

        order.Tickets.Should().HaveCount(2);
        order.Tickets.Select(t => t.TicketCode).Should().BeEquivalentTo(["A-1", "A-2"]);
        order.Tickets.Should().OnlyContain(t =>
            t.Status == TicketStatus.Issued &&
            t.OriginalClientId == order.ClientId &&
            t.CurrentClientId == order.ClientId &&
            t.OriginalOrderId == order.Id &&
            t.CreatedBy == actorUserId);

        order.Items.Should().HaveCount(2);
        order.Items.Should().OnlyContain(i => i.ItemType == ItemType.Ticket);
        order.Items.Should().OnlyContain(i => !i.IsCourtesy);
        order.Items.Select(i => i.Price).Should().BeEquivalentTo([125m, 175m]);
        order.Items.Select(i => i.ItemReferenceId).Should().BeEquivalentTo(result.TicketIds);

        backgroundJobs.Received(2).Create(
            Arg.Is<Job>(job =>
                job.Type == typeof(IEmailJob) &&
                job.Method.Name == nameof(IEmailJob.SendOrderConfirmationAsync)),
            Arg.Any<EnqueuedState>());
    }

    [Fact]
    public async Task BookAsync_WhenInventoryBatchIsMissing_PersistsTicketWithoutInventoryBatch()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedStandaloneEventAsync(context, includeInventoryBatch: false);

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        bookingClient.BookSeatsAsync(
                "schedule-100",
                Arg.Any<List<BookingSeatRequest>>(),
                "hold-123",
                Arg.Any<CancellationToken>())
            .Returns(["A-1"]);

        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            EventKey = "schedule-100",
            EventScheduleId = 100,
            HoldToken = "hold-123",
            TicketType = ItemType.Ticket,
            Localizer = "ORD-E-100-000001",
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 125m, PriceListItemId = 1 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "buyer@example.com",
                FirstName = "Rita",
                LastName = "Moreno"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var result = await sut.BookAsync(request, Guid.NewGuid());

        result.TicketIds.Should().ContainSingle();
        await bookingClient.DidNotReceive().ReleaseBookedSeatsAsync(
            "schedule-100",
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Any<CancellationToken>());
        context.Orders.Should().ContainSingle();
        var ticket = await context.Tickets.SingleAsync();
        ticket.InventoryBatchId.Should().BeNull();
    }

    [Fact]
    public async Task BookAsync_SeasonPassBundleRequest_BooksSeasonAndCreatesBundlePassTicketsAndOrderItem()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        var sourceOrder = await SeedSourceOrderAsync(context, "ORD-B-OLD-000001");

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        AllowSeasonPassRemoteReadiness(bookingClient);
        bookingClient.BookSeatsAsync(
                "season-20",
                Arg.Any<List<BookingSeatRequest>>(),
                "hold-123",
                Arg.Any<CancellationToken>())
            .Returns(["A-1"]);

        var sut = CreateService(context, bookingClient);
        var actorUserId = Guid.NewGuid();
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            ReferenceOrderId = sourceOrder.Id,
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var result = await sut.BookAsync(request, actorUserId);

        result.Reference.Should().Be("ORD-B-20-000001");
        result.BookedSeatKeys.Should().BeEquivalentTo(["A-1"]);
        result.BundlePassIds.Should().HaveCount(1);
        result.TicketIds.Should().HaveCount(2);

        await bookingClient.Received(1).BookSeatsAsync(
            "season-20",
            Arg.Is<List<BookingSeatRequest>>(seats =>
                seats.Count == 1 &&
                seats.Any(s => s.SeatKey == "A-1" && s.SeatPrice == 500m)),
            "hold-123",
            Arg.Any<CancellationToken>());

        var order = await context.Orders
            .Include(o => o.Items)
            .Include(o => o.Tickets)
            .SingleAsync(o => o.Reference == "ORD-B-20-000001");
        var bundlePass = await context.BundlePasses.SingleAsync();
        var joins = await context.BundlePassEventTickets
            .Include(j => j.Ticket)
            .ToListAsync();

        order.OrderType.Should().Be(OrderType.Bundle);
        order.RelatedOrderId.Should().Be(sourceOrder.Id);
        order.SaleChannel.Should().Be(SaleChannel.BoxOffice);
        order.Total.Should().Be(500m);
        order.Items.Should().ContainSingle(i =>
            i.ItemType == ItemType.BundlePass &&
            i.ItemReferenceId == bundlePass.Id &&
            !i.IsCourtesy &&
            i.Price == 500m);

        bundlePass.BundleId.Should().Be(20);
        bundlePass.ClientId.Should().Be(order.ClientId);
        bundlePass.BundlePassType.Should().Be(BundlePassType.Full);
        bundlePass.Status.Should().Be(BundlePassStatus.Active);
        bundlePass.Price.Should().Be(500m);
        bundlePass.BundleSeatId.Should().NotBeNull();
        result.BundlePassIds.Should().BeEquivalentTo([bundlePass.Id]);

        joins.Should().HaveCount(2);
        joins.Should().OnlyContain(j => j.BundlePassId == bundlePass.Id);
        joins.Select(j => j.Ticket.EventScheduleId).Should().BeEquivalentTo([201L, 202L]);
        joins.Select(j => j.Ticket.TicketCode).Should().OnlyContain(code => code == "A-1");
        order.Tickets.Should().HaveCount(2);
    }

    [Fact]
    public async Task BookAsync_SeasonPassBundleRequest_RejectsMissingRemoteSeasonBeforeSeatsIoBooking()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        bookingClient.EventOrSeasonExistsAsync(
                Arg.Is<string>(key => key.StartsWith("season-20-schedule-", StringComparison.Ordinal)),
                Arg.Any<CancellationToken>())
            .Returns(true);
        bookingClient.EventOrSeasonExistsAsync(
                "season-20",
                Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Seats.io season*season-20*does not exist*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_SeasonPassBundleRequest_RejectsMissingRemoteSeatBeforeSeatsIoBooking()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        bookingClient.EventOrSeasonExistsAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        bookingClient.ValidateSeatsExistAsync(
                "season-20",
                Arg.Is<IReadOnlyCollection<string>>(seats => seats.SequenceEqual(new[] { "A-1" })),
                Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Seats.io season*does not contain one or more requested seats*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_SeasonPassBundleRequest_RejectsMissingRemoteSeasonEventBeforeSeatsIoBooking()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        bookingClient.EventOrSeasonExistsAsync(
                "season-20",
                Arg.Any<CancellationToken>())
            .Returns(true);
        bookingClient.EventOrSeasonExistsAsync(
                "season-20-schedule-201",
                Arg.Any<CancellationToken>())
            .Returns(true);
        bookingClient.EventOrSeasonExistsAsync(
                "season-20-schedule-202",
                Arg.Any<CancellationToken>())
            .Returns(false);

        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Seats.io season event*season-20-schedule-202*does not exist*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_SeasonPassBundleRequest_EnqueuesBundleConfirmationEmails()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        var sourceOrder = await SeedSourceOrderAsync(context, "ORD-B-OLD-000001");

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        AllowSeasonPassRemoteReadiness(bookingClient);
        bookingClient.BookSeatsAsync(
                "season-20",
                Arg.Any<List<BookingSeatRequest>>(),
                "hold-123",
                Arg.Any<CancellationToken>())
            .Returns(["A-1"]);

        var backgroundJobs = Substitute.For<IBackgroundJobClient>();
        var sut = CreateService(context, bookingClient, backgroundJobs);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            ReferenceOrderId = sourceOrder.Id,
            Seats =
            [
                new BookingSeatRequest
                {
                    SeatKey = "A-1",
                    SeatPrice = 500m,
                    PriceListItemId = 3
                }
            ],
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        await sut.BookAsync(request, Guid.NewGuid());

        backgroundJobs.Received(2).Create(
            Arg.Is<Job>(job => IsBundleOrderConfirmationJob(job)),
            Arg.Any<EnqueuedState>());
    }

    [Fact]
    public async Task BookAsync_RenewalBundleRequest_WithOwnedActiveSourcePass_BooksSeason()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        await MarkBundleAsRenewalAsync(context, 20, 19);
        var sourceOrder = await SeedRenewableSourceOrderAsync(
            context,
            19,
            "ORD-B-OLD-000001",
            "season@example.com");

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        AllowSeasonPassRemoteReadiness(bookingClient);
        bookingClient.BookSeatsAsync(
                "season-20",
                Arg.Any<List<BookingSeatRequest>>(),
                "hold-123",
                Arg.Any<CancellationToken>())
            .Returns(["A-1"]);

        var sut = CreateService(context, bookingClient);
        var actorUserId = Guid.NewGuid();
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            ReferenceOrderId = sourceOrder.Id,
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var result = await sut.BookAsync(request, actorUserId);

        result.Reference.Should().Be("ORD-B-20-000001");
        result.BookedSeatKeys.Should().BeEquivalentTo(["A-1"]);

        await bookingClient.Received(1).BookSeatsAsync(
            "season-20",
            Arg.Is<List<BookingSeatRequest>>(seats =>
                seats.Count == 1 &&
                seats.Any(s => s.SeatKey == "A-1" && s.SeatPrice == 500m)),
            "hold-123",
            Arg.Any<CancellationToken>());

        var order = await context.Orders
            .Include(o => o.Items)
            .SingleAsync(o => o.Reference == "ORD-B-20-000001");
        var renewalPass = await context.BundlePasses.SingleAsync(pass => pass.BundleId == 20);

        order.RelatedOrderId.Should().Be(sourceOrder.Id);
        order.ClientId.Should().Be(sourceOrder.ClientId);
        order.Items.Should().ContainSingle(i =>
            i.ItemType == ItemType.BundlePass &&
            i.ItemReferenceId == renewalPass.Id &&
            i.Price == 500m);
        renewalPass.ClientId.Should().Be(sourceOrder.ClientId);
        renewalPass.TrackingCode.Should().Be("A-1");
        renewalPass.Status.Should().Be(BundlePassStatus.Active);
        result.BundlePassIds.Should().BeEquivalentTo([renewalPass.Id]);
    }

    [Fact]
    public async Task BookAsync_RenewalBundleRequest_WithoutLocalizer_GeneratesReferenceAndPersistsOrder()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        await MarkBundleAsRenewalAsync(context, 20, 19);
        var sourceOrder = await SeedRenewableSourceOrderAsync(
            context,
            19,
            "ORD-B-OLD-000001",
            "season@example.com");

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        AllowSeasonPassRemoteReadiness(bookingClient);
        bookingClient.BookSeatsAsync(
                "season-20",
                Arg.Any<List<BookingSeatRequest>>(),
                "hold-123",
                Arg.Any<CancellationToken>())
            .Returns(["A-1"]);

        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            ReferenceOrderId = sourceOrder.Id,
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FullName = "Ada Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var result = await sut.BookAsync(request, Guid.NewGuid());

        result.Reference.Should().Be("ORD1");
        result.BookedSeatKeys.Should().BeEquivalentTo(["A-1"]);
        context.SequenceTrackers.Single().SequenceKey.Should().Be("ORD");
        (await context.Orders.SingleAsync(o => o.Reference == "ORD1")).RelatedOrderId.Should().Be(sourceOrder.Id);
    }

    [Fact]
    public async Task BookAsync_BundleRequest_RejectsSeatWithoutBundleSeatBeforeSeatsIoBooking()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        bookingClient.BookSeatsAsync(
                Arg.Any<string>(),
                Arg.Any<List<BookingSeatRequest>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(["A-404"]);

        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-404", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not configured for this bundle*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_BundleRequest_RejectsNotForSaleBundleSeatBeforeSeatsIoBooking()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        var bundleSeat = await context.BundleSeats.SingleAsync();
        bundleSeat.ForSale = false;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        bookingClient.BookSeatsAsync(
                Arg.Any<string>(),
                Arg.Any<List<BookingSeatRequest>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(["A-1"]);

        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            ReferenceOrderId = 123,
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not for sale*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_BundleRequest_RejectsBeforeOnSaleDateBeforeSeatsIoBooking()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        var bundle = await context.Bundles.SingleAsync(b => b.Id == 20);
        bundle.OnSaleDate = DateTimeOffset.UtcNow.AddDays(1);
        bundle.OffSaleDate = DateTimeOffset.UtcNow.AddDays(30);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            ReferenceOrderId = 123,
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not on sale*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_BundleRequest_RejectsAfterOffSaleDateBeforeSeatsIoBooking()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        var bundle = await context.Bundles.SingleAsync(b => b.Id == 20);
        bundle.OnSaleDate = DateTimeOffset.UtcNow.AddDays(-30);
        bundle.OffSaleDate = DateTimeOffset.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            ReferenceOrderId = 123,
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not on sale*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_BundleRequest_RejectsMissingSaleWindowBeforeSeatsIoBooking()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        var bundle = await context.Bundles.SingleAsync(b => b.Id == 20);
        bundle.OnSaleDate = null;
        bundle.OffSaleDate = null;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            ReferenceOrderId = 123,
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sale window is not configured*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_RenewalBundleRequest_RejectsBeforeRenewalWindowBeforeSeatsIoBooking()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        await MarkBundleAsRenewalAsync(context, 20, 19);
        var bundle = await context.Bundles.SingleAsync(b => b.Id == 20);
        bundle.RenewalStartDate = DateTimeOffset.UtcNow.AddDays(1);
        bundle.RenewalEndDate = DateTimeOffset.UtcNow.AddDays(30);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            ReferenceOrderId = 123,
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*renewal window is not open*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_RenewalBundleRequest_WithReferenceAfterRenewalWindowBooksSelectedSeats()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        await MarkBundleAsRenewalAsync(context, 20, 19);
        var sourceOrder = await SeedRenewableSourceOrderAsync(
            context,
            19,
            "ORD-B-OLD-000001",
            "season@example.com");
        await AddSecondSeasonPassSeatAsync(context, 20, 201, 202);
        var bundle = await context.Bundles.SingleAsync(b => b.Id == 20);
        bundle.RenewalStartDate = DateTimeOffset.UtcNow.AddDays(-30);
        bundle.RenewalEndDate = DateTimeOffset.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        AllowSeasonPassRemoteReadiness(bookingClient);
        bookingClient.BookSeatsAsync(
                "season-20",
                Arg.Any<List<BookingSeatRequest>>(),
                "hold-123",
                Arg.Any<CancellationToken>())
            .Returns(["A-2"]);
        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            ReferenceOrderId = sourceOrder.Id,
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-2", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var result = await sut.BookAsync(request, Guid.NewGuid());

        result.BookedSeatKeys.Should().BeEquivalentTo(["A-2"]);
        var order = await context.Orders.SingleAsync(order => order.Reference == "ORD-B-20-000001");
        order.RelatedOrderId.Should().Be(sourceOrder.Id);
        order.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task BookAsync_RenewalBundleRequest_WithReferenceAfterRenewalWindowRejectsAlreadyConsumedSourceOrder()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        await MarkBundleAsRenewalAsync(context, 20, 19);
        var sourceOrder = await SeedRenewableSourceOrderAsync(
            context,
            19,
            "ORD-B-OLD-000001",
            "season@example.com");
        await SeedRelatedRenewalOrderAsync(context, sourceOrder, 20, "A-1");
        var bundle = await context.Bundles.SingleAsync(b => b.Id == 20);
        bundle.RenewalStartDate = DateTimeOffset.UtcNow.AddDays(-30);
        bundle.RenewalEndDate = DateTimeOffset.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000002",
            ReferenceOrderId = sourceOrder.Id,
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*source order*already been renewed*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_RenewalBundleRequest_WithReferenceDuringRenewalWindowRejectsAlreadyConsumedSourceOrder()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        await MarkBundleAsRenewalAsync(context, 20, 19);
        var sourceOrder = await SeedRenewableSourceOrderAsync(
            context,
            19,
            "ORD-B-OLD-000001",
            "season@example.com");
        await AddSecondSeasonPassSeatAsync(context, 20, 201, 202);
        await SeedRelatedRenewalOrderAsync(context, sourceOrder, 20, "A-2");

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000002",
            ReferenceOrderId = sourceOrder.Id,
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*source order*already been renewed*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_RenewalBundleRequest_WithoutReferenceDuringRenewalWindowRejectsOpenPurchaseBeforeSeatsIoBooking()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        await MarkBundleAsRenewalAsync(context, 20, 19);

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*reserved for renewals*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_RenewalBundleRequest_WithoutReferenceAfterRenewalWindowBooksOpenPurchase()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        await MarkBundleAsRenewalAsync(context, 20, 19);
        var bundle = await context.Bundles.SingleAsync(b => b.Id == 20);
        bundle.RenewalStartDate = DateTimeOffset.UtcNow.AddDays(-30);
        bundle.RenewalEndDate = DateTimeOffset.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        AllowSeasonPassRemoteReadiness(bookingClient);
        bookingClient.BookSeatsAsync(
                "season-20",
                Arg.Any<List<BookingSeatRequest>>(),
                "hold-123",
                Arg.Any<CancellationToken>())
            .Returns(["A-1"]);

        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "new-buyer@example.com",
                FirstName = "New",
                LastName = "Buyer"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var result = await sut.BookAsync(request, Guid.NewGuid());

        result.Reference.Should().Be("ORD-B-20-000001");
        result.BookedSeatKeys.Should().BeEquivalentTo(["A-1"]);
        var order = await context.Orders.SingleAsync(order => order.Reference == "ORD-B-20-000001");
        order.RelatedOrderId.Should().BeNull();
    }

    [Fact]
    public async Task BookAsync_RenewalBundleRequest_RejectsReferenceOrderWithoutOwnedSourcePassBeforeSeatsIoBooking()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        var sourceOrder = await SeedSourceOrderAsync(context, "ORD-B-OLD-000001");
        await MarkBundleAsRenewalAsync(context, 20, 19);

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        bookingClient.BookSeatsAsync(
                Arg.Any<string>(),
                Arg.Any<List<BookingSeatRequest>>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(["A-1"]);

        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            ReferenceOrderId = sourceOrder.Id,
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = sourceOrder.Client.Email!,
                FirstName = "Existing",
                LastName = "Buyer"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*source order*requested bundle seats*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_RenewalBundleRequest_RejectsUnpaidSourceOrderBeforeSeatsIoBooking()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        await MarkBundleAsRenewalAsync(context, 20, 19);
        var sourceOrder = await SeedRenewableSourceOrderAsync(
            context,
            19,
            "ORD-B-OLD-000001",
            "season@example.com",
            OrderStatus.Cancelled,
            BundlePassStatus.Active);

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            ReferenceOrderId = sourceOrder.Id,
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*source order*requested bundle seats*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_RenewalBundleRequest_RejectsSuspendedSourcePassBeforeSeatsIoBooking()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        await MarkBundleAsRenewalAsync(context, 20, 19);
        var sourceOrder = await SeedRenewableSourceOrderAsync(
            context,
            19,
            "ORD-B-OLD-000001",
            "season@example.com",
            OrderStatus.Paid,
            BundlePassStatus.Suspended);

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            ReferenceOrderId = sourceOrder.Id,
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*source order*requested bundle seats*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_BundleRequest_RejectsMissingLinkedEventSeatBeforeSeatsIoBooking()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedSeasonPassBundleAsync(context);
        var missingSeat = await context.EventSeats
            .SingleAsync(seat => seat.EventSection.EventScheduleId == 202);
        context.EventSeats.Remove(missingSeat);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 20,
            EventKey = "season-20",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-20-000001",
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 500m, PriceListItemId = 3 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "season@example.com",
                FirstName = "Ada",
                LastName = "Lovelace"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*every linked event schedule*");
        await bookingClient.DidNotReceiveWithAnyArgs().BookSeatsAsync(
            default(string)!,
            default!,
            default!,
            default);
    }

    [Fact]
    public async Task BookAsync_BasicBundleRequest_BooksSelectedLinkedScheduleOnly()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedBasicBundleAsync(context);

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        bookingClient.BookSeatsAsync(
                Arg.Is<string[]>(keys => keys.SequenceEqual(new[] { "basic-21-schedule-201" })),
                Arg.Any<List<BookingSeatRequest>>(),
                "hold-123",
                Arg.Any<CancellationToken>())
            .Returns(["A-1"]);

        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 21,
            EventKey = "basic-21",
            EventScheduleId = 201,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-21-000001",
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 100m, PriceListItemId = 4 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "basic@example.com",
                FirstName = "Grace",
                LastName = "Hopper"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var result = await sut.BookAsync(request, Guid.NewGuid());

        result.BookedSeatKeys.Should().BeEquivalentTo(["A-1"]);
        result.BundlePassIds.Should().ContainSingle();
        result.TicketIds.Should().ContainSingle();
        await bookingClient.Received(1).BookSeatsAsync(
            Arg.Is<string[]>(keys => keys.SequenceEqual(new[] { "basic-21-schedule-201" })),
            Arg.Any<List<BookingSeatRequest>>(),
            "hold-123",
            Arg.Any<CancellationToken>());
        await bookingClient.DidNotReceive().BookSeatsAsync(
            Arg.Is<string[]>(keys => keys.Contains("basic-21-schedule-202")),
            Arg.Any<List<BookingSeatRequest>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());

        var order = await context.Orders
            .Include(o => o.Items)
            .Include(o => o.Tickets)
            .SingleAsync();
        var bundlePass = await context.BundlePasses.SingleAsync();
        var join = await context.BundlePassEventTickets
            .Include(j => j.Ticket)
            .SingleAsync();

        order.OrderType.Should().Be(OrderType.Bundle);
        order.Items.Should().ContainSingle(i =>
            i.ItemType == ItemType.BundlePass &&
            !i.IsCourtesy &&
            i.ItemReferenceId == bundlePass.Id);
        join.BundlePassId.Should().Be(bundlePass.Id);
        join.Ticket.EventScheduleId.Should().Be(201);
    }

    [Fact]
    public async Task BookAsync_BasicBundleRequest_WhenBatchSeatsIoBookingFails_PersistsNoOrder()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedBasicBundleAsync(context);

        var bookingClient = Substitute.For<ISeatsIoBookingClient>();
        bookingClient.BookSeatsAsync(
                Arg.Is<string[]>(keys => keys.SequenceEqual(new[] { "basic-21-schedule-201", "basic-21-schedule-202" })),
                Arg.Any<List<BookingSeatRequest>>(),
                "hold-123",
                Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyList<string>>>(_ => throw new InvalidOperationException("Seats.io failed"));

        var sut = CreateService(context, bookingClient);
        var request = new BookSeatsActionRequest
        {
            BundleId = 21,
            EventKey = "basic-21",
            EventScheduleId = 0,
            HoldToken = "hold-123",
            TicketType = ItemType.BundlePass,
            Localizer = "ORD-B-21-000001",
            Seats = new List<BookingSeatRequest> { new BookingSeatRequest { SeatKey = "A-1", SeatPrice = 100m, PriceListItemId = 4 } },
            ClientContact = new ClientInfoRequest
            {
                Email = "basic@example.com",
                FirstName = "Grace",
                LastName = "Hopper"
            },
            PaymentInfoRequest = PaidInCash(),
            ChangeInfoRequest = new ChangeInfoRequest()
        };

        var act = () => sut.BookAsync(request, Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Seats.io failed");
        await bookingClient.DidNotReceiveWithAnyArgs().ReleaseBookedSeatsAsync(
            default!,
            default!,
            default);
        context.Orders.Should().BeEmpty();
        context.Tickets.Should().BeEmpty();
        context.BundlePasses.Should().BeEmpty();
    }

    private static BookingOrchestrationService CreateService(
        XBOLDbContext context,
        ISeatsIoBookingClient bookingClient,
        IBackgroundJobClient? backgroundJobClient = null)
    {
        return new BookingOrchestrationService(
            context,
            bookingClient,
            new SequenceTrackerService(new SequenceTrackerRepository(context)),
            backgroundJobClient ?? Substitute.For<IBackgroundJobClient>(),
            new BookingEmailModelBuilder(context),
            Substitute.For<ILogger<BookingOrchestrationService>>(),
            Options.Create(new DefaultExchangeRateOptions { Value = 20m }),
            Options.Create(new PaymentLinkOptions { Url = "https://example.test/pay/{paymentLinkCode}" }),
            new ExchangeRateRepository(context));
    }

    private static void AllowSeasonPassRemoteReadiness(ISeatsIoBookingClient bookingClient)
    {
        bookingClient.EventOrSeasonExistsAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
        bookingClient.ValidateSeatsExistAsync(
                Arg.Any<string>(),
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private static PaymentInfoRequest PaidInCash() => new()
    {
        CashAmount = 10_000m
    };

    private static bool IsBundleOrderConfirmationJob(Job job)
    {
        if (job.Type != typeof(IEmailJob) ||
            job.Method.Name != nameof(IEmailJob.SendOrderConfirmationAsync) ||
            job.Args.Count != 1 ||
            job.Args[0] is not OrderEmailModel model)
        {
            return false;
        }

        return model.GetType().GetProperty("IsBundle")?.GetValue(model) is true;
    }

    private static async Task SeedStandaloneEventAsync(
        XBOLDbContext context,
        bool includeInventoryBatch = true)
    {
        var now = DateTimeOffset.UtcNow;
        var venue = new Venue
        {
            Name = "Arena",
            AddressLine = "1 Main St",
            City = "Tijuana",
            State = "BC",
            Country = "MX",
            Category = VenueCategory.Arena,
            ShortDescription = "Arena",
            LongDescription = "Arena",
            LogoImageUrl = "logo.png",
            BannerImageUrl = "banner.png",
            LandingUrl = "https://example.test",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var venueMap = new VenueMap
        {
            Id = 10,
            Venue = venue,
            Name = "Main",
            ExternalMapKey = "chart-10",
            CreatedAt = now,
            UpdatedAt = now
        };
        var baseZone = new BaseZone
        {
            VenueMap = venueMap,
            Name = "Lower Bowl"
        };
        var baseSection = new BaseSection
        {
            BaseZone = baseZone,
            Name = "Lower 101",
            SectionType = SectionType.General
        };
        var baseRow = new BaseRow
        {
            BaseSection = baseSection,
            RowLabel = "A"
        };
        var baseSeat1 = new BaseSeat
        {
            BaseRow = baseRow,
            SeatNumber = "1",
            SeatType = SeatType.Standard
        };
        var baseSeat2 = new BaseSeat
        {
            BaseRow = baseRow,
            SeatNumber = "2",
            SeatType = SeatType.Standard
        };
        var eventEntity = new Event
        {
            Id = 50,
            VenueMap = venueMap,
            Name = "Home Opener",
            Status = EventStatus.Published,
            CreatedAt = now,
            UpdatedAt = now
        };
        var schedule = new EventSchedule
        {
            Id = 100,
            Event = eventEntity,
            StartDateTime = now.AddDays(7),
            EndDateTime = now.AddDays(7).AddHours(3),
            OnSaleDate = now.AddDays(-1),
            OffSaleDate = now.AddDays(7),
            Status = ScheduleStatus.OnSale,
            ExternalEventKey = "schedule-100"
        };
        var section = new EventSection
        {
            EventSchedule = schedule,
            BaseSection = baseSection,
            DisplayName = "Lower 101",
            TotalSeats = 2,
            AvailableSeats = 2
        };
        if (includeInventoryBatch)
        {
            schedule.InventoryBatches.Add(new InventoryBatch
            {
                Distributor = new Distributor
                {
                    Name = "Box Office",
                    Contact = "box-office@example.com"
                },
                Quantity = 2,
                CutoffDate = now.AddDays(7),
                Status = InventoryBatchStatus.Active
            });
        }

        context.EventSeats.AddRange(
            new EventSeat
            {
                EventSection = section,
                BaseSeat = baseSeat1,
                ExternalSeatObjectKey = "A-1"
            },
            new EventSeat
            {
                EventSection = section,
                BaseSeat = baseSeat2,
                ExternalSeatObjectKey = "A-2"
            });
        SeedPriceListItem(context, SaleType.Event, schedule.Id, venueMap.Id, baseZone, baseSection, baseRow, baseSeat1);

        await context.SaveChangesAsync();
    }

    private static void SeedPriceListItem(
        XBOLDbContext context,
        SaleType referenceType,
        long referenceId,
        long venueMapId,
        BaseZone baseZone,
        BaseSection baseSection,
        BaseRow baseRow,
        BaseSeat baseSeat)
    {
        var priceReference = new PriceReference
        {
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            IsActive = true
        };
        var priceSegment = new PriceSegment
        {
            PriceReference = priceReference,
            BaseZone = baseZone,
            BaseSection = baseSection,
            BaseRow = baseRow,
            BaseSeat = baseSeat,
            PriceItemType = PriceItemType.Seat,
            VenueMapId = venueMapId,
            PriceTypes =
            [
                new PriceType
                {
                    Id = 1,
                    Name = "General",
                    IsBasePrice = true
                }
            ]
        };
        var priceType = priceSegment.PriceTypes.Single();
        var price = new Price
        {
            Id = 1,
            PriceSegment = priceSegment,
            PriceType = priceType,
            PriceValue = 125m
        };
        var priceList = new PriceList
        {
            Id = 1,
            PriceReference = priceReference,
            Status = VersionStatus.Active
        };

        priceList.Items.Add(CreatePriceListItem(
            id: 1,
            baseZone,
            baseSection,
            baseRow,
            baseSeat,
            price,
            priceType,
            basePrice: 125m));
        priceList.Items.Add(CreatePriceListItem(
            id: 2,
            baseZone,
            baseSection,
            baseRow,
            baseSeat,
            price,
            priceType,
            basePrice: 175m));
        priceList.Items.Add(CreatePriceListItem(
            id: 3,
            baseZone,
            baseSection,
            baseRow,
            baseSeat,
            price,
            priceType,
            basePrice: 500m));
        priceList.Items.Add(CreatePriceListItem(
            id: 4,
            baseZone,
            baseSection,
            baseRow,
            baseSeat,
            price,
            priceType,
            basePrice: 100m));

        context.PriceLists.Add(priceList);
    }

    private static PriceListItem CreatePriceListItem(
        long id,
        BaseZone baseZone,
        BaseSection baseSection,
        BaseRow baseRow,
        BaseSeat baseSeat,
        Price price,
        PriceType priceType,
        decimal basePrice)
    {
        return new PriceListItem
        {
            Id = id,
            BaseZone = baseZone,
            BaseSection = baseSection,
            BaseRow = baseRow,
            BaseSeat = baseSeat,
            Price = price,
            PriceType = priceType,
            BasePrice = basePrice,
            FinalPrice = basePrice
        };
    }

    private static async Task SeedSeasonPassBundleAsync(XBOLDbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var venue = BuildVenue(now);
        var venueMap = new VenueMap
        {
            Id = 20,
            Venue = venue,
            Name = "Season Map",
            ExternalMapKey = "chart-20",
            CreatedAt = now,
            UpdatedAt = now
        };
        var baseZone = new BaseZone
        {
            VenueMap = venueMap,
            Name = "Lower Bowl"
        };
        var baseSection = new BaseSection
        {
            BaseZone = baseZone,
            Name = "Lower 101",
            SectionType = SectionType.General
        };
        var baseRow = new BaseRow
        {
            BaseSection = baseSection,
            RowLabel = "A"
        };
        var baseSeat = new BaseSeat
        {
            BaseRow = baseRow,
            SeatNumber = "1",
            SeatType = SeatType.Standard
        };
        var bundle = new Bundle
        {
            Id = 20,
            VenueMap = venueMap,
            Name = "Season 2026",
            Status = EventStatus.Published,
            BundleType = BundleType.SeasonPass,
            BundlePricingType = BundlePricingType.Single,
            ExternalKey = "season-20",
            OnSaleDate = now.AddDays(-1),
            OffSaleDate = now.AddDays(30),
            CreatedAt = now,
            UpdatedAt = now,
            BundleSections =
            [
                new BundleSection
                {
                    BaseSection = baseSection,
                    DisplayName = "Lower 101",
                    TotalSeats = 1,
                    AvailableSeats = 1,
                    BundleSeats =
                    [
                        new BundleSeat
                        {
                            BaseSeat = baseSeat,
                            ExternalSeatObjectKey = "A-1",
                            ForSale = true
                        }
                    ]
                }
            ]
        };
        var eventEntity = new Event
        {
            Id = 30,
            VenueMap = venueMap,
            Name = "Season Events",
            Status = EventStatus.Published,
            CreatedAt = now,
            UpdatedAt = now
        };
        var firstSchedule = BuildLinkedSchedule(201, "season-20-schedule-201", eventEntity, baseSection, baseSeat, now);
        var secondSchedule = BuildLinkedSchedule(202, "season-20-schedule-202", eventEntity, baseSection, baseSeat, now);

        bundle.BundleEventSchedules.Add(new BundleEventSchedule
        {
            Bundle = bundle,
            EventSchedule = firstSchedule,
            SortOrder = 1
        });
        bundle.BundleEventSchedules.Add(new BundleEventSchedule
        {
            Bundle = bundle,
            EventSchedule = secondSchedule,
            SortOrder = 2
        });

        context.Bundles.Add(bundle);
        SeedPriceListItem(context, SaleType.SeasonPass, bundle.Id, venueMap.Id, baseZone, baseSection, baseRow, baseSeat);
        await context.SaveChangesAsync();
    }

    private static async Task<Order> SeedSourceOrderAsync(XBOLDbContext context, string reference)
    {
        var now = DateTimeOffset.UtcNow;
        var client = new Client
        {
            ClientType = ClientType.Individual,
            Email = $"{reference.ToLowerInvariant()}@example.com",
            FullName = "Existing Buyer",
            PhoneNumber = "5550000100",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var order = new Order
        {
            Client = client,
            Reference = reference,
            SubTotal = 500m,
            TotalFees = 0,
            TotalTaxes = 0,
            Total = 500m,
            Status = OrderStatus.Paid,
            OrderType = OrderType.Bundle,
            SaleChannel = SaleChannel.BoxOffice,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order;
    }

    private static async Task<Order> SeedRenewableSourceOrderAsync(
        XBOLDbContext context,
        long bundleId,
        string reference,
        string email,
        OrderStatus orderStatus = OrderStatus.Paid,
        BundlePassStatus passStatus = BundlePassStatus.Active)
    {
        var now = DateTimeOffset.UtcNow;
        var client = new Client
        {
            ClientType = ClientType.Individual,
            Email = email,
            FullName = "Existing Buyer",
            PhoneNumber = "5550000100",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var pass = new BundlePass
        {
            BundleId = bundleId,
            Client = client,
            TrackingCode = "A-1",
            PrivateToken = Guid.NewGuid().ToString("N"),
            BundlePassType = BundlePassType.Full,
            Status = passStatus,
            IsDigital = true,
            PurchasedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.BundlePasses.Add(pass);
        await context.SaveChangesAsync();

        var order = new Order
        {
            Client = client,
            Reference = reference,
            SubTotal = pass.Price,
            TotalFees = 0,
            TotalTaxes = 0,
            Total = pass.Price,
            Status = orderStatus,
            OrderType = OrderType.Bundle,
            SaleChannel = SaleChannel.BoxOffice,
            CreatedAt = now,
            UpdatedAt = now,
            Items =
            [
                new OrderItem
                {
                    ItemType = ItemType.BundlePass,
                    ItemReferenceId = pass.Id,
                    Price = pass.Price
                }
            ]
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order;
    }

    private static async Task SeedBasicBundleAsync(XBOLDbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var venue = BuildVenue(now);
        var venueMap = new VenueMap
        {
            Id = 21,
            Venue = venue,
            Name = "Basic Map",
            ExternalMapKey = "chart-21",
            CreatedAt = now,
            UpdatedAt = now
        };
        var baseZone = new BaseZone
        {
            VenueMap = venueMap,
            Name = "Lower Bowl"
        };
        var baseSection = new BaseSection
        {
            BaseZone = baseZone,
            Name = "Lower 101",
            SectionType = SectionType.General
        };
        var baseRow = new BaseRow
        {
            BaseSection = baseSection,
            RowLabel = "A"
        };
        var baseSeat = new BaseSeat
        {
            BaseRow = baseRow,
            SeatNumber = "1",
            SeatType = SeatType.Standard
        };
        var bundle = new Bundle
        {
            Id = 21,
            VenueMap = venueMap,
            Name = "Basic Bundle",
            Status = EventStatus.Published,
            BundleType = BundleType.Basic,
            BundlePricingType = BundlePricingType.Composite,
            OnSaleDate = now.AddDays(-1),
            OffSaleDate = now.AddDays(30),
            CreatedAt = now,
            UpdatedAt = now,
            BundleSections =
            [
                new BundleSection
                {
                    BaseSection = baseSection,
                    DisplayName = "Lower 101",
                    TotalSeats = 1,
                    AvailableSeats = 1,
                    BundleSeats =
                    [
                        new BundleSeat
                        {
                            BaseSeat = baseSeat,
                            ExternalSeatObjectKey = "A-1",
                            ForSale = true
                        }
                    ]
                }
            ]
        };
        var eventEntity = new Event
        {
            Id = 31,
            VenueMap = venueMap,
            Name = "Basic Events",
            Status = EventStatus.Published,
            CreatedAt = now,
            UpdatedAt = now
        };
        var firstSchedule = BuildLinkedSchedule(201, "basic-21-schedule-201", eventEntity, baseSection, baseSeat, now);
        var secondSchedule = BuildLinkedSchedule(202, "basic-21-schedule-202", eventEntity, baseSection, baseSeat, now);

        bundle.BundleEventSchedules.Add(new BundleEventSchedule
        {
            Bundle = bundle,
            EventSchedule = firstSchedule,
            SortOrder = 1
        });
        bundle.BundleEventSchedules.Add(new BundleEventSchedule
        {
            Bundle = bundle,
            EventSchedule = secondSchedule,
            SortOrder = 2
        });

        context.Bundles.Add(bundle);
        SeedPriceListItem(context, SaleType.Bundle, bundle.Id, venueMap.Id, baseZone, baseSection, baseRow, baseSeat);
        await context.SaveChangesAsync();
    }

    private static async Task MarkBundleAsRenewalAsync(
        XBOLDbContext context,
        long renewalBundleId,
        long sourceBundleId)
    {
        var renewalBundle = await context.Bundles.SingleAsync(b => b.Id == renewalBundleId);
        context.Bundles.Add(new Bundle
        {
            Id = sourceBundleId,
            VenueMapId = renewalBundle.VenueMapId,
            Name = "Previous Season",
            Status = EventStatus.Published,
            BundleType = BundleType.SeasonPass,
            BundlePricingType = BundlePricingType.Single,
            ExternalKey = $"season-{sourceBundleId}",
            OnSaleDate = DateTimeOffset.UtcNow.AddDays(-365),
            OffSaleDate = DateTimeOffset.UtcNow.AddDays(-30),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        renewalBundle.PreviousBundleId = sourceBundleId;
        renewalBundle.RenewalStartDate = DateTimeOffset.UtcNow.AddDays(-1);
        renewalBundle.RenewalEndDate = DateTimeOffset.UtcNow.AddDays(30);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static async Task AddSecondSeasonPassSeatAsync(
        XBOLDbContext context,
        long bundleId,
        params long[] scheduleIds)
    {
        var bundleSection = await context.BundleSections
            .Include(section => section.BundleSeats)
            .ThenInclude(seat => seat.BaseSeat)
            .ThenInclude(seat => seat.BaseRow)
            .SingleAsync(section => section.BundleId == bundleId);
        var baseRow = bundleSection.BundleSeats.Single().BaseSeat.BaseRow;
        var baseSeat = new BaseSeat
        {
            BaseRow = baseRow,
            SeatNumber = "2",
            SeatType = SeatType.Standard
        };

        bundleSection.TotalSeats += 1;
        bundleSection.AvailableSeats += 1;
        bundleSection.BundleSeats.Add(new BundleSeat
        {
            BaseSeat = baseSeat,
            ExternalSeatObjectKey = "A-2",
            ForSale = true
        });

        var eventSections = await context.EventSections
            .Include(section => section.EventSeats)
            .Where(section => scheduleIds.Contains(section.EventScheduleId))
            .ToListAsync();

        foreach (var eventSection in eventSections)
        {
            eventSection.TotalSeats += 1;
            eventSection.AvailableSeats += 1;
            eventSection.EventSeats.Add(new EventSeat
            {
                EventSection = eventSection,
                BaseSeat = baseSeat,
                ExternalSeatObjectKey = "A-2"
            });
        }

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static async Task SeedRelatedRenewalOrderAsync(
        XBOLDbContext context,
        Order sourceOrder,
        long bundleId,
        string seatKey)
    {
        var now = DateTimeOffset.UtcNow;
        var pass = new BundlePass
        {
            BundleId = bundleId,
            ClientId = sourceOrder.ClientId,
            TrackingCode = seatKey,
            PrivateToken = Guid.NewGuid().ToString("N"),
            BundlePassType = BundlePassType.Full,
            Status = BundlePassStatus.Active,
            IsDigital = true,
            PurchasedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        context.BundlePasses.Add(pass);
        await context.SaveChangesAsync();

        context.Orders.Add(new Order
        {
            ClientId = sourceOrder.ClientId,
            Reference = "ORD-B-RENEWED-000001",
            SubTotal = 500m,
            TotalFees = 0,
            TotalTaxes = 0,
            Total = 500m,
            Status = OrderStatus.Paid,
            OrderType = OrderType.Bundle,
            SaleChannel = SaleChannel.BoxOffice,
            RelatedOrderId = sourceOrder.Id,
            CreatedAt = now,
            UpdatedAt = now,
            Items =
            [
                new OrderItem
                {
                    ItemType = ItemType.BundlePass,
                    ItemReferenceId = pass.Id,
                    Price = pass.Price
                }
            ]
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static EventSchedule BuildLinkedSchedule(
        long scheduleId,
        string eventKey,
        Event eventEntity,
        BaseSection baseSection,
        BaseSeat baseSeat,
        DateTimeOffset now)
    {
        var schedule = new EventSchedule
        {
            Id = scheduleId,
            Event = eventEntity,
            StartDateTime = now.AddDays(scheduleId - 200),
            EndDateTime = now.AddDays(scheduleId - 200).AddHours(3),
            OnSaleDate = now.AddDays(-1),
            OffSaleDate = now.AddDays(30),
            Status = ScheduleStatus.OnSale,
            ExternalEventKey = eventKey,
            InventoryBatches =
            [
                new InventoryBatch
                {
                    Distributor = new Distributor
                    {
                        Name = $"Distributor {scheduleId}",
                        Contact = $"dist-{scheduleId}@example.com"
                    },
                    Quantity = 1,
                    CutoffDate = now.AddDays(30),
                    Status = InventoryBatchStatus.Active
                }
            ]
        };
        var section = new EventSection
        {
            EventSchedule = schedule,
            BaseSection = baseSection,
            DisplayName = "Lower 101",
            TotalSeats = 1,
            AvailableSeats = 1
        };
        section.EventSeats.Add(new EventSeat
        {
            EventSection = section,
            BaseSeat = baseSeat,
            ExternalSeatObjectKey = "A-1"
        });
        schedule.Sections.Add(section);
        return schedule;
    }

    private static Venue BuildVenue(DateTimeOffset now)
    {
        return new Venue
        {
            Name = "Arena",
            AddressLine = "1 Main St",
            City = "Tijuana",
            State = "BC",
            Country = "MX",
            Category = VenueCategory.Arena,
            ShortDescription = "Arena",
            LongDescription = "Arena",
            LogoImageUrl = "logo.png",
            BannerImageUrl = "banner.png",
            LandingUrl = "https://example.test",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
