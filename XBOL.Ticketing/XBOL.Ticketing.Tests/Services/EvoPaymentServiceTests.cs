using System.Net;
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
using XBOL.Ticketing.Core.DTO.EvoPayment;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Repositories.Order;
using XBOL.Ticketing.Data.Repositories;
using XBOL.Ticketing.Services;
using XBOL.Ticketing.Services.Booking;
using XBOL.Ticketing.Services.Email;
using XBOL.Ticketing.Services.EvoPayment;
using XBOL.Ticketing.Services.Odasoft.XBOL.Business.Services;
using TicketingEmailTemplateOptions = XBOL.Ticketing.Core.Commons.Options.EmailTemplateOptions;

namespace XBOL.Ticketing.Tests.Services;

public sealed class EvoPaymentServiceTests
{
    private const string SupportEmail = "soporte@pwrticket.mx";

    [Fact]
    public async Task InitiateCheckoutAsync_PersistsPendingHostedCheckoutOrderWithPaymentFields()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedHostedCheckoutCatalogAsync(context);

        var seatsIoBookingClient = Substitute.For<ISeatsIoBookingClient>();
        seatsIoBookingClient
            .BookSeatsAsync("schedule-100", Arg.Any<List<BookingSeatRequest>>(), "hold-token-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>(new List<string> { "A-1" }));

        var sut = CreateService(
            context,
            Substitute.For<IBackgroundJobClient>(),
            seatsIoBookingClient,
            """{"result":"SUCCESS","session":{"id":"SESSION000000000000000000000000001"},"successIndicator":"success-indicator-1"}""");

        var result = await sut.InitiateCheckoutAsync(new InitiateCheckoutRequest
        {
            EventScheduleId = 100,
            HoldToken = "hold-token-1",
            Seats =
            [
                new CheckoutSeatRequest
                {
                    SeatKey = "A-1",
                    PriceListItemId = 10
                }
            ],
            ClientContact = new ClientInfoRequest
            {
                Id = 1,
                PhoneRegionCodeId = 1,
                PhoneNumber = "5552220100",
                Email = "buyer@example.com",
                FullName = "Buyer Example"
            },
            ReturnUrl = "https://pwrticket.mx/client/booking/100/?source=evo",
            Currency = "MXN"
        });

        result.Amount.Should().Be("125.00");

        var order = await context.Orders
            .Include(o => o.Payments)
            .Include(o => o.Tickets)
            .Include(o => o.Items)
            .SingleAsync(o => o.Id == result.LocalOrderId);

        order.Status.Should().Be(OrderStatus.Pending);
        order.Tickets.Should().ContainSingle(t =>
            t.EventScheduleId == 100 &&
            t.Status == TicketStatus.PendingPayment);
        order.Items.Should().ContainSingle(i => i.ItemReferenceId == order.Tickets.Single().Id);

        var payment = order.Payments.Should().ContainSingle().Subject;
        payment.Currency.Should().Be(CurrencyType.MXN);
        payment.Amount.Should().Be(125m);
        payment.AmountMXN.Should().Be(125m);
        payment.ReceivedAmount.Should().BeNull();
        payment.ReceivedAmountMXN.Should().BeNull();
        payment.ExchangeRateId.Should().Be(0);
        payment.ExchangeRate.Should().Be(0);
        payment.PaymentStatus.Should().Be(PaymentStatus.Pending);
        payment.ProviderReference.Should().Be(result.OrderRefId);
        payment.ProviderSessionReference.Should().Be(result.SuccessIndicator);
        await seatsIoBookingClient.Received(1).BookSeatsAsync(
            "schedule-100",
            Arg.Is<List<BookingSeatRequest>>(seats =>
                seats.Count == 1 &&
                seats[0].SeatKey == "A-1" &&
                seats[0].PriceListItemId == 10),
            "hold-token-1",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiateCheckoutAsync_WhenSeatsIoBookingFails_MarksLocalCheckoutFailed()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedHostedCheckoutCatalogAsync(context);

        var seatsIoBookingClient = Substitute.For<ISeatsIoBookingClient>();
        seatsIoBookingClient
            .BookSeatsAsync("schedule-100", Arg.Any<List<BookingSeatRequest>>(), "hold-token-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<string>>(new InvalidOperationException("not held")));

        var sut = CreateService(
            context,
            Substitute.For<IBackgroundJobClient>(),
            seatsIoBookingClient,
            """{"result":"SUCCESS","session":{"id":"SESSION000000000000000000000000001"},"successIndicator":"success-indicator-1"}""");

        var act = () => sut.InitiateCheckoutAsync(new InitiateCheckoutRequest
        {
            EventScheduleId = 100,
            HoldToken = "hold-token-1",
            Seats =
            [
                new CheckoutSeatRequest
                {
                    SeatKey = "A-1",
                    PriceListItemId = 10
                }
            ],
            ClientContact = new ClientInfoRequest
            {
                Id = 1,
                PhoneRegionCodeId = 1,
                PhoneNumber = "5552220100",
                Email = "buyer@example.com",
                FullName = "Buyer Example"
            },
            ReturnUrl = "https://pwrticket.mx/client/booking/100/?source=evo",
            Currency = "MXN"
        });

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Could not confirm seat reservation. Please try again.");

        var order = await context.Orders
            .Include(o => o.Payments)
            .Include(o => o.Tickets)
            .SingleAsync();

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.Payments.Should().ContainSingle().Subject.PaymentStatus.Should().Be(PaymentStatus.Failed);
        order.Tickets.Should().ContainSingle().Subject.Status.Should().Be(TicketStatus.Expired);
    }

    [Fact]
    public async Task ConfirmCheckoutAsync_WhenPaymentIsCaptured_EnqueuesConfirmationEmails()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedPendingCheckoutOrderAsync(context);

        var createdJobs = new List<Job>();
        var backgroundJobs = Substitute.For<IBackgroundJobClient>();
        backgroundJobs
            .Create(Arg.Do<Job>(createdJobs.Add), Arg.Any<EnqueuedState>())
            .Returns(_ => $"job-{createdJobs.Count}");
        var sut = CreateService(context, backgroundJobs);

        var result = await sut.ConfirmCheckoutAsync(new ConfirmCheckoutRequest
        {
            LocalOrderId = 1,
            OrderRefId = "evo-order-1",
            ResultIndicator = "success-indicator-1"
        });

        result.OrderStatus.Should().Be(OrderStatus.Paid.ToString());
        result.PaymentStatus.Should().Be(PaymentStatus.Captured.ToString());

        var emailModels = ExtractOrderConfirmationModels(createdJobs);
        emailModels.Should().HaveCount(2);
        emailModels.Should().Contain(model =>
            model.ToAddress == "buyer@example.com" &&
            model.OrderDetails.OrderNumber == "ORD-PAY-1" &&
            !model.IsBundle);
        emailModels.Should().Contain(model =>
            model.ToAddress == SupportEmail &&
            model.OrderDetails.OrderNumber == "ORD-PAY-1" &&
            !model.IsBundle);
    }

    [Fact]
    public async Task ConfirmCheckoutAsync_WhenBundlePaymentIsCaptured_EnqueuesBundleConfirmationEmails()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedPendingBundleCheckoutOrderAsync(context);

        var createdJobs = new List<Job>();
        var backgroundJobs = Substitute.For<IBackgroundJobClient>();
        backgroundJobs
            .Create(Arg.Do<Job>(createdJobs.Add), Arg.Any<EnqueuedState>())
            .Returns(_ => $"job-{createdJobs.Count}");
        var sut = CreateService(context, backgroundJobs);

        var result = await sut.ConfirmCheckoutAsync(new ConfirmCheckoutRequest
        {
            LocalOrderId = 2,
            OrderRefId = "evo-order-bundle-1",
            ResultIndicator = "success-indicator-bundle-1"
        });

        result.OrderStatus.Should().Be(OrderStatus.Paid.ToString());
        result.PaymentStatus.Should().Be(PaymentStatus.Captured.ToString());

        var emailModels = ExtractOrderConfirmationModels(createdJobs);
        emailModels.Should().HaveCount(2);
        emailModels.Should().Contain(model =>
            model.ToAddress == "bundle-buyer@example.com" &&
            model.OrderDetails.OrderNumber == "ORD-BUNDLE-PAY-1" &&
            model.IsBundle);
        emailModels.Should().Contain(model =>
            model.ToAddress == SupportEmail &&
            model.OrderDetails.OrderNumber == "ORD-BUNDLE-PAY-1" &&
            model.IsBundle);
        emailModels.Should().OnlyContain(model =>
            model.EventImageUrl == "https://cdn.example.test/bundle-email-banner.png");
    }

    [Fact]
    public async Task ConfirmCheckoutAsync_WhenPaymentLinkBundlePaymentIsCaptured_CreatesPaymentAndIssuesPendingTickets()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedPendingBundlePaymentLinkOrderAsync(context);

        var createdJobs = new List<Job>();
        var backgroundJobs = Substitute.For<IBackgroundJobClient>();
        backgroundJobs
            .Create(Arg.Do<Job>(createdJobs.Add), Arg.Any<EnqueuedState>())
            .Returns(_ => $"job-{createdJobs.Count}");
        var sut = CreateService(context, backgroundJobs);

        var result = await sut.ConfirmCheckoutAsync(new ConfirmCheckoutRequest
        {
            LocalOrderId = 3,
            OrderRefId = "evo-order-bundle-payment-link-1",
            ResultIndicator = "success-indicator-bundle-payment-link-1"
        });

        result.OrderStatus.Should().Be(OrderStatus.Paid.ToString());
        result.PaymentStatus.Should().Be(PaymentStatus.Captured.ToString());
        result.TicketsIssued.Should().Be(1);

        var order = await context.Orders
            .Include(o => o.Payments)
            .Include(o => o.Tickets)
            .SingleAsync(o => o.Id == 3);

        order.Status.Should().Be(OrderStatus.Paid);
        order.Payments.Should().ContainSingle().Which.Should().Match<Payment>(payment =>
            payment.Provider == "EVOPayments" &&
            payment.ProviderReference == "evo-order-bundle-payment-link-1" &&
            payment.PaymentStatus == PaymentStatus.Captured &&
            payment.Amount == 500m &&
            payment.AmountMXN == 500m &&
            payment.Currency == CurrencyType.MXN);
        order.Tickets.Should().ContainSingle().Which.Should().Match<Ticket>(ticket =>
            ticket.Status == TicketStatus.Issued &&
            !string.IsNullOrWhiteSpace(ticket.PrivateToken));

        var emailModels = ExtractOrderConfirmationModels(createdJobs);
        emailModels.Should().HaveCount(2);
        emailModels.Should().OnlyContain(model => model.IsBundle);
    }

    [Fact]
    public async Task ConfirmCheckoutAsync_WhenPaymentIsCaptured_LogsCheckoutConfirmationWithEvoPaymentCategory()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedPendingCheckoutOrderAsync(context);

        var backgroundJobs = Substitute.For<IBackgroundJobClient>();
        backgroundJobs
            .Create(Arg.Any<Job>(), Arg.Any<EnqueuedState>())
            .Returns("buyer-job-1", "seller-job-1");
        var logger = Substitute.For<ILogger<EvoPaymentService>>();
        var sut = CreateService(context, backgroundJobs, logger: logger);

        await sut.ConfirmCheckoutAsync(new ConfirmCheckoutRequest
        {
            LocalOrderId = 1,
            OrderRefId = "evo-order-1",
            ResultIndicator = "success-indicator-1"
        });

        var infoLogs = RenderedLogMessages(logger, LogLevel.Information);
        infoLogs.Should().Contain(message =>
            message.Contains("Checkout confirmed", StringComparison.Ordinal) &&
            message.Contains("OrderId=1", StringComparison.Ordinal));
        infoLogs.Should().Contain(message =>
            message.Contains("Starting confirmation email enqueue", StringComparison.Ordinal) &&
            message.Contains("evo", StringComparison.Ordinal) &&
            message.Contains("1", StringComparison.Ordinal) &&
            message.Contains("evo-order-1", StringComparison.Ordinal));
        infoLogs.Should().Contain(message =>
            message.Contains("Completed confirmation email enqueue", StringComparison.Ordinal) &&
            message.Contains("1", StringComparison.Ordinal) &&
            message.Contains("buyer-job-1", StringComparison.Ordinal) &&
            message.Contains("seller-job-1", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConfirmCheckoutAsync_WhenOrderIsAlreadyConfirmed_DoesNotEnqueueConfirmationEmails()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedPendingCheckoutOrderAsync(context);
        var order = await context.Orders
            .Include(order => order.Tickets)
            .SingleAsync(order => order.Id == 1);
        order.Status = OrderStatus.Paid;
        foreach (var ticket in order.Tickets)
        {
            ticket.Status = TicketStatus.Issued;
        }
        await context.SaveChangesAsync();

        var createdJobs = new List<Job>();
        var backgroundJobs = Substitute.For<IBackgroundJobClient>();
        backgroundJobs
            .Create(Arg.Do<Job>(createdJobs.Add), Arg.Any<EnqueuedState>())
            .Returns(_ => $"job-{createdJobs.Count}");
        var sut = CreateService(context, backgroundJobs);

        var result = await sut.ConfirmCheckoutAsync(new ConfirmCheckoutRequest
        {
            LocalOrderId = 1,
            OrderRefId = "evo-order-1",
            ResultIndicator = "success-indicator-1"
        });

        result.OrderStatus.Should().Be(OrderStatus.Paid.ToString());
        result.TicketsIssued.Should().Be(1);
        createdJobs.Should().BeEmpty();
    }

    [Fact]
    public async Task ConfirmCheckoutAsync_WhenPaymentFails_ReleasesSeatsFromTicketSchedule()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<XBOLDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new XBOLDbContext(options);
        await context.Database.EnsureCreatedAsync();
        await SeedPendingCheckoutOrderAsync(context);

        var seatsIoBookingClient = Substitute.For<ISeatsIoBookingClient>();
        var sut = CreateService(
            context,
            Substitute.For<IBackgroundJobClient>(),
            seatsIoBookingClient,
            """{"result":"FAILURE","status":"CANCELLED","gatewayCode":"DECLINED","totalCapturedAmount":0}""");

        var result = await sut.ConfirmCheckoutAsync(new ConfirmCheckoutRequest
        {
            LocalOrderId = 1,
            OrderRefId = "evo-order-1",
            ResultIndicator = "success-indicator-1"
        });

        result.OrderStatus.Should().Be(OrderStatus.Cancelled.ToString());
        result.PaymentStatus.Should().Be(PaymentStatus.Cancelled.ToString());

        await seatsIoBookingClient.Received(1)
            .ReleaseBookedSeatsAsync("schedule-100", Arg.Is<IReadOnlyCollection<string>>(seats => seats.SequenceEqual(new[] { "A-1" })), Arg.Any<CancellationToken>());
    }

    private static EvoPaymentService CreateService(
        XBOLDbContext context,
        IBackgroundJobClient backgroundJobs,
        ISeatsIoBookingClient? seatsIoBookingClient = null,
        string retrieveOrderResponse = """{"result":"SUCCESS","status":"CAPTURED","totalCapturedAmount":125.00}""",
        ILogger<EvoPaymentService>? logger = null)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(retrieveOrderResponse))
        {
            BaseAddress = new Uri("https://gateway.example.test/api/")
        };

        return new EvoPaymentService(
            httpClient,
            Options.Create(new EvoSettings
            {
                APIPassword = "secret",
                MerchantId = "merchant",
                Version = "100"
            }),
            new OrderRepository(context),
            Options.Create(new GatewayOptions()),
            logger ?? Substitute.For<ILogger<EvoPaymentService>>(),
            context,
            new SequenceTrackerService(new SequenceTrackerRepository(context)),
            seatsIoBookingClient ?? Substitute.For<ISeatsIoBookingClient>(),
            new BookingConfirmationEmailQueue(
                backgroundJobs,
                new BookingEmailModelBuilder(context),
                Options.Create(new TicketingEmailTemplateOptions
                {
                    SupportEmail = SupportEmail
                }),
                Substitute.For<ILogger<BookingConfirmationEmailQueue>>()));
    }

    private static List<string> RenderedLogMessages(
        ILogger logger,
        LogLevel logLevel)
    {
        return logger.ReceivedCalls()
            .Where(call =>
                call.GetMethodInfo().Name == nameof(ILogger.Log) &&
                call.GetArguments()[0] is LogLevel level &&
                level == logLevel)
            .Select(call => call.GetArguments()[2]?.ToString() ?? "")
            .ToList();
    }

    private static List<OrderEmailModel> ExtractOrderConfirmationModels(IEnumerable<Job> jobs)
    {
        return jobs
            .Where(job =>
                job.Type == typeof(IEmailJob) &&
                job.Method.Name == nameof(IEmailJob.SendOrderConfirmationAsync) &&
                job.Args.Count == 1)
            .Select(job => job.Args[0])
            .OfType<OrderEmailModel>()
            .ToList();
    }

    private static async Task SeedPendingCheckoutOrderAsync(XBOLDbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        await EnsurePhoneRegionCodeAsync(context);

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
        var baseSeat = new BaseSeat
        {
            BaseRow = baseRow,
            SeatNumber = "1",
            SeatType = SeatType.Standard
        };
        var eventEntity = new Event
        {
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
            TotalSeats = 1,
            AvailableSeats = 0
        };
        var eventSeat = new EventSeat
        {
            EventSection = section,
            BaseSeat = baseSeat,
            ExternalSeatObjectKey = "A-1"
        };
        var client = new Client
        {
            ClientType = ClientType.Individual,
            Email = "buyer@example.com",
            FullName = "Buyer Example",
            PhoneRegionCodeId = 1,
            PhoneNumber = "5552220100",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        var order = new Order
        {
            Id = 1,
            Client = client,
            Reference = "ORD-PAY-1",
            SubTotal = 125m,
            TotalFees = 0m,
            TotalTaxes = 0m,
            Total = 125m,
            Status = OrderStatus.Pending,
            SaleChannel = SaleChannel.Online,
            OrderType = OrderType.Ticket,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        var ticket = new Ticket
        {
            Id = 1,
            EventSchedule = schedule,
            EventSection = section,
            EventSeat = eventSeat,
            OriginalClient = client,
            CurrentClient = client,
            OriginalOrder = order,
            TicketCode = "A-1",
            TicketType = ItemType.Ticket.ToString(),
            PrivateToken = null,
            SectionLabelSnapshot = "Lower 101",
            SeatLabelSnapshot = "A-1",
            IsDigital = true,
            PricePaid = 125m,
            Status = TicketStatus.PendingPayment,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        order.Tickets.Add(ticket);
        order.Items.Add(new OrderItem
        {
            ItemType = ItemType.Ticket,
            ItemReferenceId = 1,
            Price = 125m
        });
        order.Payments.Add(new Payment
        {
            Amount = 125m,
            PaymentType = PaymentType.Card,
            Provider = "EVOPayments",
            ProviderReference = "evo-order-1",
            TransactionReference = Guid.NewGuid(),
            AppliedAt = now,
            CreatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        });

        context.Orders.Add(order);
        await context.SaveChangesAsync();
    }

    private static async Task SeedHostedCheckoutCatalogAsync(XBOLDbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        await EnsurePhoneRegionCodeAsync(context);

        var venue = new Venue
        {
            Id = 100,
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
            Id = 100,
            Venue = venue,
            Name = "Main",
            ExternalMapKey = "chart-100",
            CreatedAt = now,
            UpdatedAt = now
        };
        var baseZone = new BaseZone
        {
            Id = 100,
            VenueMap = venueMap,
            Name = "Lower Bowl"
        };
        var baseSection = new BaseSection
        {
            Id = 100,
            BaseZone = baseZone,
            Name = "Lower 101",
            SectionType = SectionType.General
        };
        var baseRow = new BaseRow
        {
            Id = 100,
            BaseSection = baseSection,
            RowLabel = "A"
        };
        var baseSeat = new BaseSeat
        {
            Id = 100,
            BaseRow = baseRow,
            SeatNumber = "1",
            SeatType = SeatType.Standard
        };
        var eventEntity = new Event
        {
            Id = 100,
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
        var eventSection = new EventSection
        {
            Id = 100,
            EventSchedule = schedule,
            BaseSection = baseSection,
            DisplayName = "Lower 101",
            TotalSeats = 1,
            AvailableSeats = 1
        };
        var eventSeat = new EventSeat
        {
            Id = 100,
            EventSection = eventSection,
            BaseSeat = baseSeat,
            ExternalSeatObjectKey = "A-1"
        };
        var client = new Client
        {
            Id = 1,
            ClientType = ClientType.Individual,
            Email = "buyer@example.com",
            FullName = "Buyer Example",
            PhoneRegionCodeId = 1,
            PhoneNumber = "5552220100",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        var distributor = new Distributor
        {
            Id = 100,
            Name = "Online",
            Contact = "online@example.com"
        };
        var inventoryBatch = new InventoryBatch
        {
            Id = 100,
            EventSchedule = schedule,
            Distributor = distributor,
            Quantity = 1,
            CutoffDate = now.AddDays(7),
            Status = InventoryBatchStatus.Active
        };
        var priceReference = new PriceReference
        {
            Id = 100,
            ReferenceType = SaleType.Event,
            ReferenceId = eventEntity.Id,
            IsActive = true
        };
        var priceSegment = new PriceSegment
        {
            Id = 100,
            PriceReference = priceReference,
            BaseZone = baseZone,
            VenueMap = venueMap,
            PriceItemType = PriceItemType.Seat
        };
        var priceType = new PriceType
        {
            Id = 100,
            PriceSegment = priceSegment,
            Name = "General",
            IsBasePrice = true,
            Primary = true,
            IsActive = true
        };
        var price = new Price
        {
            Id = 100,
            PriceSegment = priceSegment,
            PriceType = priceType,
            PriceValue = 125m,
            IsActive = true
        };
        var priceList = new PriceList
        {
            Id = 100,
            PriceReference = priceReference,
            VersionNumber = 1,
            PublishedAt = now,
            PublishBy = Guid.Empty,
            Status = VersionStatus.Active
        };
        var priceListItem = new PriceListItem
        {
            Id = 10,
            PriceList = priceList,
            BaseSeat = baseSeat,
            Price = price,
            PriceType = priceType,
            BasePrice = 125m,
            FinalPrice = 125m
        };

        context.AddRange(
            venue,
            venueMap,
            baseZone,
            baseSection,
            baseRow,
            baseSeat,
            eventEntity,
            schedule,
            eventSection,
            eventSeat,
            client,
            distributor,
            inventoryBatch,
            priceReference,
            priceSegment,
            priceType,
            price,
            priceList,
            priceListItem);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPendingBundleCheckoutOrderAsync(XBOLDbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        await EnsurePhoneRegionCodeAsync(context);

        var client = new Client
        {
            ClientType = ClientType.Individual,
            Email = "bundle-buyer@example.com",
            FullName = "Bundle Buyer",
            PhoneRegionCodeId = 1,
            PhoneNumber = "5553330100",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        var bundle = new Bundle
        {
            Id = 2,
            Name = "Season Bundle",
            Status = EventStatus.Published,
            BundleType = BundleType.SeasonPass,
            BundlePricingType = BundlePricingType.Single,
            BannerImageUrl = "bundle-banner.png",
            LandingUrl = "https://example.test/bundle",
            StartDate = now.AddDays(7),
            EndDate = now.AddMonths(6),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        var pass = new BundlePass
        {
            Id = 2,
            Bundle = bundle,
            Client = client,
            TrackingCode = "A-1",
            PrivateToken = "private-token",
            BundlePassType = BundlePassType.Full,
            Status = BundlePassStatus.Active,
            IsDigital = true,
            Price = 500m,
            PurchasedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        var order = new Order
        {
            Id = 2,
            Client = client,
            Reference = "ORD-BUNDLE-PAY-1",
            SubTotal = 500m,
            TotalFees = 0m,
            TotalTaxes = 0m,
            Total = 500m,
            Status = OrderStatus.Pending,
            SaleChannel = SaleChannel.Online,
            OrderType = OrderType.Bundle,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        order.Items.Add(new OrderItem
        {
            ItemType = ItemType.BundlePass,
            ItemReferenceId = pass.Id,
            Price = 500m
        });
        order.Payments.Add(new Payment
        {
            Amount = 500m,
            PaymentType = PaymentType.Card,
            Provider = "EVOPayments",
            ProviderReference = "evo-order-bundle-1",
            TransactionReference = Guid.NewGuid(),
            AppliedAt = now,
            CreatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        });

        context.Orders.Add(order);
        context.BundlePasses.Add(pass);
        context.Media.Add(new Media
        {
            ReferenceId = bundle.Id,
            ReferenceType = SaleType.Bundle,
            MediaType = MediaType.Banner,
            Order = 0,
            BlobAsset = new BlobAsset
            {
                BucketName = "bucket",
                ObjectName = "bundle/email-banner.png",
                FileName = "email-banner.png",
                ContentType = "image/png",
                SizeBytes = 42,
                Url = "https://cdn.example.test/bundle-email-banner.png",
                Status = BlobAssetStatus.Available,
                CreatedAt = now,
                UpdatedAt = now
            },
            CreatedAt = now,
            UpdatedAt = now
        });
        await context.SaveChangesAsync();
    }

    private static async Task SeedPendingBundlePaymentLinkOrderAsync(XBOLDbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        await EnsurePhoneRegionCodeAsync(context);

        var venue = new Venue
        {
            Id = 300,
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
            Id = 300,
            Venue = venue,
            Name = "Main",
            ExternalMapKey = "chart-300",
            CreatedAt = now,
            UpdatedAt = now
        };
        var baseZone = new BaseZone
        {
            Id = 300,
            VenueMap = venueMap,
            Name = "Lower Bowl",
            CreatedAt = now,
            UpdatedAt = now
        };
        var baseSection = new BaseSection
        {
            Id = 300,
            BaseZone = baseZone,
            Name = "Lower 101",
            SectionType = SectionType.General,
            CreatedAt = now,
            UpdatedAt = now
        };
        var baseRow = new BaseRow
        {
            Id = 300,
            BaseSection = baseSection,
            RowLabel = "A",
            CreatedAt = now,
            UpdatedAt = now
        };
        var baseSeat = new BaseSeat
        {
            Id = 300,
            BaseRow = baseRow,
            SeatNumber = "1",
            SeatType = SeatType.Standard,
            CreatedAt = now,
            UpdatedAt = now
        };
        var eventEntity = new Event
        {
            Id = 300,
            VenueMap = venueMap,
            Name = "Bundle Game",
            Status = EventStatus.Published,
            BannerImageUrl = "event-banner.png",
            LandingUrl = "https://example.test/event",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        var schedule = new EventSchedule
        {
            Id = 300,
            Event = eventEntity,
            StartDateTime = now.AddDays(7),
            EndDateTime = now.AddDays(7).AddHours(2),
            OnSaleDate = now.AddDays(-1),
            OffSaleDate = now.AddDays(6),
            Status = ScheduleStatus.OnSale,
            ExternalEventKey = "schedule-300",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        var eventSection = new EventSection
        {
            Id = 300,
            EventSchedule = schedule,
            BaseSection = baseSection,
            DisplayName = "Lower 101",
            TotalSeats = 1,
            AvailableSeats = 1
        };
        var eventSeat = new EventSeat
        {
            Id = 300,
            EventSection = eventSection,
            BaseSeat = baseSeat,
            ExternalSeatObjectKey = "A-1"
        };
        var client = new Client
        {
            ClientType = ClientType.Individual,
            Email = "bundle-link-buyer@example.com",
            FullName = "Bundle Link Buyer",
            PhoneRegionCodeId = 1,
            PhoneNumber = "5553330101",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        var bundle = new Bundle
        {
            Id = 3,
            Name = "Season Bundle Link",
            Status = EventStatus.Published,
            BundleType = BundleType.SeasonPass,
            BundlePricingType = BundlePricingType.Single,
            BannerImageUrl = "bundle-banner.png",
            LandingUrl = "https://example.test/bundle",
            StartDate = now.AddDays(7),
            EndDate = now.AddMonths(6),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        var pass = new BundlePass
        {
            Id = 3,
            Bundle = bundle,
            Client = client,
            TrackingCode = "A-1",
            PrivateToken = "bundle-pass-private-token",
            BundlePassType = BundlePassType.Full,
            Status = BundlePassStatus.Active,
            IsDigital = true,
            Price = 500m,
            PurchasedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        var order = new Order
        {
            Id = 3,
            Client = client,
            Reference = "ORD-BUNDLE-LINK-1",
            SubTotal = 500m,
            TotalFees = 0m,
            TotalTaxes = 0m,
            Total = 500m,
            Status = OrderStatus.Pending,
            SaleChannel = SaleChannel.Online,
            OrderType = OrderType.Bundle,
            OrderTags = [OrderTag.PaymentLink],
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };
        var ticket = new Ticket
        {
            Id = 300,
            EventSchedule = schedule,
            EventSection = eventSection,
            EventSeat = eventSeat,
            OriginalClient = client,
            CurrentClient = client,
            OriginalOrder = order,
            TicketCode = "A-1",
            TicketType = ItemType.BundlePass.ToString(),
            PrivateToken = null,
            SectionLabelSnapshot = "Lower 101",
            SeatLabelSnapshot = "A-1",
            IsDigital = true,
            PricePaid = 0m,
            Status = TicketStatus.PendingPayment,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        };

        order.Tickets.Add(ticket);
        order.Items.Add(new OrderItem
        {
            ItemType = ItemType.BundlePass,
            ItemReferenceId = pass.Id,
            Price = 500m
        });

        context.AddRange(
            venue,
            venueMap,
            baseZone,
            baseSection,
            baseRow,
            baseSeat,
            eventEntity,
            schedule,
            eventSection,
            eventSeat,
            client,
            bundle,
            pass,
            order,
            new BundlePassEventTicket
            {
                BundlePass = pass,
                Ticket = ticket
            });
        await context.SaveChangesAsync();
    }

    private static async Task EnsurePhoneRegionCodeAsync(XBOLDbContext context)
    {
        if (await context.Set<PhoneRegionCode>().AnyAsync(region => region.Id == 1))
        {
            return;
        }

        context.Set<PhoneRegionCode>().Add(new PhoneRegionCode
        {
            Id = 1,
            RegionCode = "MX",
            DialCode = "+52",
            FlagEmoji = "MX"
        });
        await context.SaveChangesAsync();
    }

    private sealed class StubHttpMessageHandler(string content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            });
        }
    }
}
