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
    }

    private static EvoPaymentService CreateService(
        XBOLDbContext context,
        IBackgroundJobClient backgroundJobs)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(
            """{"result":"SUCCESS","status":"CAPTURED","totalCapturedAmount":125.00}"""))
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
            Substitute.For<ILogger<SeatsIoService>>(),
            context,
            new SequenceTrackerService(new SequenceTrackerRepository(context)),
            Substitute.For<ISeatsIoBookingClient>(),
            new BookingConfirmationEmailQueue(
                backgroundJobs,
                new BookingEmailModelBuilder(context),
                Options.Create(new TicketingEmailTemplateOptions
                {
                    SupportEmail = SupportEmail
                }),
                Substitute.For<ILogger<BookingConfirmationEmailQueue>>()));
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
            EventScheduleId = schedule.Id,
            HoldToken = "hold-123",
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
            ProviderSessionReference = "success-indicator-1",
            PaymentStatus = PaymentStatus.Pending,
            TransactionReference = Guid.NewGuid(),
            AppliedAt = null,
            CreatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        });

        context.Orders.Add(order);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPendingBundleCheckoutOrderAsync(XBOLDbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var client = new Client
        {
            ClientType = ClientType.Individual,
            Email = "bundle-buyer@example.com",
            FullName = "Bundle Buyer",
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
            ProviderSessionReference = "success-indicator-bundle-1",
            PaymentStatus = PaymentStatus.Pending,
            TransactionReference = Guid.NewGuid(),
            AppliedAt = null,
            CreatedAt = now,
            CreatedBy = Guid.Empty,
            UpdatedBy = Guid.Empty
        });

        context.Orders.Add(order);
        context.BundlePasses.Add(pass);
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
