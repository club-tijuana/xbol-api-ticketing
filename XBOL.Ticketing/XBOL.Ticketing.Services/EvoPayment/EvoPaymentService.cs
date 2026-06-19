using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.EvoPayment;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Repositories.Order;
using XBOL.Ticketing.Services.Booking;
using XBOL.Ticketing.Services.Email;
using XBOL.Ticketing.Services.Odasoft.XBOL.Business.Services;
using ModelBundle = XBOL.Ticketing.Core.Model.Bundle;
using ModelClient = XBOL.Ticketing.Core.Model.Client;
using ModelOrder = XBOL.Ticketing.Core.Model.Order;
using ModelOrderItem = XBOL.Ticketing.Core.Model.OrderItem;
using ModelTicket = XBOL.Ticketing.Core.Model.Ticket;

namespace XBOL.Ticketing.Services.EvoPayment
{
    public class EvoPaymentService : IEvoPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly EvoSettings _settings;
        private readonly OrderRepository _orderRepository;
        private readonly GatewayOptions _opt;
        private readonly ILogger<EvoPaymentService> _logger;
        private readonly XBOLDbContext _dbContext;
        private readonly SequenceTrackerService _sequenceTrackerService;
        private readonly ISeatsIoBookingClient _seatsIoBookingClient;
        private readonly BookingConfirmationEmailQueue _confirmationEmailQueue;

        public EvoPaymentService(
            HttpClient httpClient,
            IOptions<EvoSettings> settings,
            OrderRepository orderRepository,
            IOptions<GatewayOptions> opt,
            ILogger<EvoPaymentService> logger,
            XBOLDbContext dbContext,
            SequenceTrackerService sequenceTrackerService,
            ISeatsIoBookingClient seatsIoBookingClient,
            BookingConfirmationEmailQueue confirmationEmailQueue)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _orderRepository = orderRepository;
            _logger = logger;
            _opt = opt.Value;
            _dbContext = dbContext;
            _sequenceTrackerService = sequenceTrackerService;
            _seatsIoBookingClient = seatsIoBookingClient;
            _confirmationEmailQueue = confirmationEmailQueue;
        }

        public async Task<SessionResponse> CreateSessionAsync()
        {
            var request = new
            {
                apiOperation = "CREATE_SESSION"
            };

            var response = await _httpClient.PostAsJsonAsync("session", request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "EVO HTTP error. StatusCode: {StatusCode}, Body: {Body}",
                    response.StatusCode, body);
                throw new Exception("Payment provider returned an unexpected error.");
            }

            var result = await response.Content
                .ReadFromJsonAsync<SessionResponse>()
                ?? throw new InvalidOperationException("EVO returned empty session response.");

            if (result != null)
            {
                result.OrderRefId = Guid.NewGuid().ToString("N");
                result.TransactionRefId = Guid.NewGuid().ToString("N");
            }

            return result;
        }

        public async Task<SessionResponse> GetSessionAsync(string sessionId)
        {
            var response = await _httpClient.GetAsync($"session/{sessionId}");
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<SessionResponse>())!;
        }

        public async Task<SessionResponse> UpdateSessionAsync(string sessionId, UpdateSessionRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync($"session/{sessionId}", request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "EVO HTTP error. StatusCode: {StatusCode}, Body: {Body}",
                    response.StatusCode, body);
                throw new Exception("Payment provider returned an unexpected error.");
            }

            return (await response.Content.ReadFromJsonAsync<SessionResponse>())!;
        }

        public async Task<SessionResponse> PayAsync(PayRequest request)
        {
            var order = await _orderRepository.GetByIdAsync(request.OrderId);
            if (order == null)
            {
                throw new InvalidOperationException("Order not found.");
            }

            var evoRequest = new
            {
                apiOperation = "PAY",
                order = new
                {
                    amount = order.Total,
                    currency = "MXN"
                },
                session = new { id = request.SessionId }
            };

            var path = $"order/{request.OrderRefId:N}/transaction/{request.TransactionRefId:N}";
            var response = await _httpClient.PutAsJsonAsync(path, evoRequest);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "EVO HTTP error. StatusCode: {StatusCode}, Body: {Body}",
                    response.StatusCode, body);
                throw new Exception("Payment provider returned an unexpected error.");
            }

            var body2 = await response.Content.ReadAsStringAsync();
            var result = await response.Content
                .ReadFromJsonAsync<SessionResponse>()
                ?? throw new InvalidOperationException("EVO returned empty pay response.");

            switch (result.Result)
            {
                case "SUCCESS":
                    var payment = order.Payments
                        .FirstOrDefault(p => p.TransactionReference == request.TransactionRefId);
                    if (payment == null)
                    {
                        throw new InvalidOperationException(
                            $"Payment with transaction reference {request.TransactionRefId} not found for order {request.OrderId}.");
                    }

                    order.Status = Core.Commons.Enums.OrderStatus.Paid;
                    order.PaidAt = DateTimeOffset.UtcNow;
                    payment.AppliedAt = DateTimeOffset.UtcNow;
                    await _orderRepository.CommitAsync();
                    break;

                case "FAILURE":
                    _logger.LogWarning(
                        "EVO payment failed for OrderId {OrderId}, OrderRefId {OrderRefId}, TransactionRefId {TransactionRefId}, Result {Result}, GatewayCode {GatewayCode}",
                        request.OrderId, request.OrderRefId, request.TransactionRefId,
                        result.Result, result.EvoResponse?.GatewayCode);
                    throw new Exception(
                        "Payment could not be processed. Please verify your payment information or try again.");

                case "PENDING":
                    break;

                default:
                    throw new Exception($"Unexpected EVO payment result: {result.Result}");
            }

            return result;
        }

        public async Task<CheckoutSessionResponse> CreateCheckoutSessionAsync(
            CreateCheckoutSessionRequest request,
            CancellationToken ct = default)
        {
            var orderRefId = Guid.NewGuid().ToString("N");

            var (sessionId, successIndicator) = await CallInitiateCheckoutAsync(
                orderRefId, request.Amount, request.Currency,
                request.ReturnUrl, request.Description ?? "XBOL Ticketing", ct);

            var gatewayBaseUrl = $"{_httpClient.BaseAddress!.Scheme}://{_httpClient.BaseAddress.Host}";

            return new CheckoutSessionResponse
            {
                MerchantId = _settings.MerchantId,
                SessionId = sessionId,
                SuccessIndicator = successIndicator,
                ApiVersion = _settings.Version,
                GatewayBaseUrl = gatewayBaseUrl,
                OrderRefId = orderRefId,
                Amount = request.Amount.ToString("F2", CultureInfo.InvariantCulture),
                Currency = request.Currency
            };
        }

        public async Task<RetrieveOrderResponse> RetrieveOrderAsync(
            string orderRefId,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(orderRefId))
            {
                throw new ArgumentException("orderRefId is required.", nameof(orderRefId));
            }

            _logger.LogInformation(
                "Querying order in EVO (Retrieve Order). orderRefId={OrderRefId}", orderRefId);

            var httpResponse = await _httpClient.GetAsync($"order/{orderRefId}", ct);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorBody = await httpResponse.Content.ReadAsStringAsync(ct);
                _logger.LogError(
                    "EVO Retrieve Order failed. StatusCode={StatusCode} Body={Body}",
                    httpResponse.StatusCode, SanitizeBody(errorBody));
                throw new Exception("The payment provider returned an error while querying the order.");
            }

            var rawBody = await httpResponse.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            var result = GetStringProp(root, "result") ?? "UNKNOWN";

            string? status = null, gatewayCode = null, currency = null;
            decimal? totalCaptured = null, totalAuthorized = null;

            status = GetStringProp(root, "status");
            currency = GetStringProp(root, "currency");
            if (root.TryGetProperty("totalCapturedAmount", out var tc) && tc.TryGetDecimal(out var tcVal))
            {
                totalCaptured = tcVal;
            }

            if (root.TryGetProperty("totalAuthorizedAmount", out var ta) && ta.TryGetDecimal(out var taVal))
            {
                totalAuthorized = taVal;
            }

            if (root.TryGetProperty("order", out var orderEl))
            {
                status ??= GetStringProp(orderEl, "status");
                currency ??= GetStringProp(orderEl, "currency");
                if (totalCaptured == null && orderEl.TryGetProperty("totalCapturedAmount", out var tc2) && tc2.TryGetDecimal(out var tcVal2))
                {
                    totalCaptured = tcVal2;
                }

                if (totalAuthorized == null && orderEl.TryGetProperty("totalAuthorizedAmount", out var ta2) && ta2.TryGetDecimal(out var taVal2))
                {
                    totalAuthorized = taVal2;
                }
            }

            if (root.TryGetProperty("response", out var resp))
            {
                gatewayCode = GetStringProp(resp, "gatewayCode");
            }

            string? cardMasked = null, cardBrand = null;
            if (root.TryGetProperty("sourceOfFunds", out var sof)
                && sof.TryGetProperty("provided", out var provided)
                && provided.TryGetProperty("card", out var card))
            {
                cardMasked = GetStringProp(card, "number");
                cardBrand = GetStringProp(card, "brand") ?? GetStringProp(card, "scheme");
            }

            _logger.LogInformation(
                "Retrieve Order completado. orderRefId={OrderRefId} result={Result} status={Status} gatewayCode={GatewayCode} totalCaptured={TotalCaptured}",
                orderRefId, result, status, gatewayCode, totalCaptured);

            return new RetrieveOrderResponse
            {
                OrderRefId = orderRefId,
                Result = result,
                Status = status,
                GatewayCode = gatewayCode,
                TotalCapturedAmount = totalCaptured,
                TotalAuthorizedAmount = totalAuthorized,
                Currency = currency,
                CardNumberMasked = cardMasked,
                CardBrand = cardBrand
            };
        }

        public async Task<InitiateCheckoutResponse> InitiateCheckoutAsync(
            InitiateCheckoutRequest request,
            CancellationToken ct = default)
        {
            EventSchedule? schedule = null;
            ModelBundle? bundle = null;
            List<EventSeat>? eventSeats = null;
            List<BundleSeat>? bundleSeats = null;
            HashSet<string>? foundSeatKeys = null;
            List<long> eventSchedulesIds = new List<long>();

            if (request.Seats.Count == 0)
            {
                throw new ArgumentException("At least one seat is required.", nameof(request));
            }

            var duplicateSeatKeys = request.Seats
                .GroupBy(s => s.SeatKey, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateSeatKeys.Count > 0)
            {
                throw new ArgumentException($"Duplicate SeatKeys: {string.Join(", ", duplicateSeatKeys)}");
            }

            if (string.IsNullOrWhiteSpace(request.ClientContact.PhoneNumber))
            {
                throw new ArgumentException("Buyer phone is required.", nameof(request));
            }

            if (!Uri.TryCreate(request.ReturnUrl, UriKind.Absolute, out _))
            {
                throw new ArgumentException("ReturnUrl must be a valid absolute URI.", nameof(request));
            }

            if (request.EventScheduleId != null)
            {
                schedule = await _dbContext.EventSchedules
                .FindAsync([request.EventScheduleId], ct);

                if (schedule is null)
                {
                    throw new KeyNotFoundException(
                        $"EventSchedule {request.EventScheduleId} not found.");
                }

                if (schedule.Status != ScheduleStatus.OnSale)
                {
                    throw new InvalidOperationException(
                        $"The event is not available for sale (status: {schedule.Status}).");
                }

                if (string.IsNullOrWhiteSpace(schedule.ExternalEventKey))
                {
                    throw new InvalidOperationException(
                        $"EventSchedule {request.EventScheduleId} does not have a published event in Seats.io.");
                }
            }
            else if (request.BundleId != null)
            {
                bundle = await _dbContext.Bundles
                .Include(b => b.BundleEventSchedules)
                    .ThenInclude(bes => bes.EventSchedule)
                .FirstOrDefaultAsync(b => b.Id == request.BundleId, ct);

                if (bundle is null)
                {
                    throw new KeyNotFoundException(
                        $"Bundle {request.BundleId} not found.");
                }

                if (
                    bundle.Status != EventStatus.Published ||
                    bundle.BundleEventSchedules.All(s => s.EventSchedule.Status != ScheduleStatus.OnSale)
                )
                {
                    throw new InvalidOperationException(
                        $"The bundle is not available for sale (status: {bundle.Status}).");
                }

                if (string.IsNullOrWhiteSpace(bundle.ExternalKey))
                {
                    throw new InvalidOperationException(
                        $"Bundle {request.BundleId} does not have a published season in Seats.io.");
                }
            }

            var requestedItemIds = request.Seats
                .Select(s => s.PriceListItemId)
                .Distinct()
                .ToList();

            long referenceId = 0;
            SaleType saleType = SaleType.Event;

            if (schedule != null)
            {
                referenceId = schedule.EventId;
            }
            else if (bundle != null)
            {
                referenceId = bundle.Id;
                saleType = SaleType.Bundle;
            }

            var validPriceItems = await _dbContext.PriceListItems
                .Include(i => i.PriceList)
                    .ThenInclude(pl => pl.PriceReference)
                .Where(i => requestedItemIds.Contains(i.Id)
                            && i.PriceList.Status == VersionStatus.Active
                            && i.PriceList.PriceReference.ReferenceType == saleType
                            && i.PriceList.PriceReference.ReferenceId == referenceId)
                .ToDictionaryAsync(i => i.Id, ct);

            var invalidItemIds = requestedItemIds.Where(id => !validPriceItems.ContainsKey(id)).ToList();
            if (invalidItemIds.Count > 0)
            {
                throw new KeyNotFoundException(
                    $"Invalid PriceListItem(s) or not belonging to this {(schedule != null ? "event" : "bundle")}: {string.Join(", ", invalidItemIds)}");
            }

            var total = request.Seats.Sum(s => validPriceItems[s.PriceListItemId].FinalPrice);
            var amountStr = total.ToString("F2", CultureInfo.InvariantCulture);

            var requestedSeatKeys = request.Seats.Select(s => s.SeatKey).ToList();

            if (schedule != null)
            {
                eventSeats = await _dbContext.EventSeats
                .Include(es => es.EventSection)
                .Where(es => requestedSeatKeys.Contains(es.ExternalSeatObjectKey)
                             && es.EventSection.EventScheduleId == request.EventScheduleId)
                .ToListAsync(ct);

                foundSeatKeys = eventSeats.Select(es => es.ExternalSeatObjectKey).ToHashSet(StringComparer.Ordinal);
            }
            else if (bundle != null)
            {
                bundleSeats = await _dbContext.BundleSeats
                    .Include(bs => bs.BundleSection)
                    .Where(bs => requestedSeatKeys.Contains(bs.ExternalSeatObjectKey)
                        && bs.BundleSection.BundleId == request.BundleId)
                    .ToListAsync(ct);

                foundSeatKeys = bundleSeats.Select(es => es.ExternalSeatObjectKey).ToHashSet(StringComparer.Ordinal);
            }

            var missingSeatKeys = requestedSeatKeys.Where(k => !foundSeatKeys.Contains(k)).ToList();
            if (missingSeatKeys.Count > 0)
            {
                throw new KeyNotFoundException(
                    $"SeatKeys not found for this {(schedule != null ? "event" : "bundle")}: {string.Join(", ", missingSeatKeys)}");
            }

            if (schedule != null)
            {
                eventSchedulesIds.Add(schedule.Id);
            }
            else if (bundle != null)
            {
                eventSchedulesIds = await _dbContext.BundleEventSchedules
                    .Where(bes => bes.BundleId == bundle.Id)
                    .Select(bes => bes.EventScheduleId)
                    .ToListAsync();
            }

            var inventoryBatchId = await _dbContext.InventoryBatches
                .Where(b => eventSchedulesIds.Contains(b.EventScheduleId)
                            && b.Status == InventoryBatchStatus.Active)
                .OrderBy(b => b.Id)
                .Select(b => (long?)b.Id)
                .FirstOrDefaultAsync(ct);

            if (schedule != null)
            {
                _logger.LogInformation(
                    "Initiating checkout. EventScheduleId={EventScheduleId} Seats={SeatCount} Total={Total} Currency={Currency}",
                    request.EventScheduleId, request.Seats.Count, amountStr, request.Currency);
            }
            else if (bundle != null)
            {
                _logger.LogInformation(
                    "Initiating checkout. BundleId={BundleId} Seats={SeatCount} Total={Total} Currency={Currency}",
                    bundle.Id, request.Seats.Count, amountStr, request.Currency);
            }

            var orderRefId = Guid.NewGuid().ToString("N");
            var (sessionId, successIndicator) = await CallInitiateCheckoutAsync(
                orderRefId, total, request.Currency, request.ReturnUrl,
                $"XBOL — {request.Seats.Count} ticket(s)", ct);


            var bookingSeats = request.Seats
                .Select(s => new BookingSeatRequest
                {
                    SeatKey = s.SeatKey,
                    SeatPrice = validPriceItems[s.PriceListItemId].FinalPrice,
                    PriceListItemId = s.PriceListItemId
                })
                .ToList();

            IReadOnlyList<string> bookedSeatKeys;
            string? seatsIokey = schedule != null ? schedule.ExternalEventKey : bundle?.ExternalKey;
            try
            {
                bookedSeatKeys = await _seatsIoBookingClient.BookSeatsAsync(
                    seatsIokey, bookingSeats, request.HoldToken, ct);
            }
            catch (Exception ex)
            {
                if (schedule != null)
                {
                    _logger.LogError(
                        ex,
                        "Seats.io BookSeatsAsync failed. EventScheduleId={EventScheduleId} HoldToken={HoldToken}",
                        request.EventScheduleId,
                        request.HoldToken);
                }
                else if (bundle != null)
                {
                    _logger.LogError(
                        ex,
                        "Seats.io BookSeatsAsync failed. BundleId={BundleId} HoldToken={HoldToken}",
                        bundle.Id,
                        request.HoldToken);
                }

                throw new InvalidOperationException(
                    "Could not confirm seat reservation. Please try again.", ex);
            }


            var now = DateTimeOffset.UtcNow;
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                var client = await ResolveClientAsync(request.ClientContact, now, ct);
                var reference = await _sequenceTrackerService.GenerateLocalizerAsync("ORD");

                var order = new ModelOrder
                {
                    Client = client,
                    UserId = null,
                    Reference = reference,
                    SubTotal = total,
                    TotalFees = 0,
                    TotalTaxes = 0,
                    Total = total,
                    Status = OrderStatus.Pending,
                    SaleChannel = SaleChannel.Online,
                    OrderType = schedule != null ? OrderType.Ticket : OrderType.Bundle,
                    RelatedOrderId = request.RelatedOrderId,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = Guid.Empty,
                    UpdatedBy = Guid.Empty
                };

                var payment = new Payment
                {
                    Order = order,
                    Amount = total,
                    PaymentType = PaymentType.Card,
                    Provider = "EVOPayments",
                    ProviderReference = orderRefId,
                    ProviderSessionReference = successIndicator,
                    PaymentStatus = PaymentStatus.Pending,
                    TransactionReference = Guid.NewGuid(),
                    AppliedAt = now,
                    CreatedAt = now,
                    CreatedBy = Guid.Empty,
                    UpdatedBy = Guid.Empty
                };

                List<BundlePass> bundlePasses = new List<BundlePass>();
                if (schedule != null)
                {
                    var eventSeatByKey = eventSeats.ToDictionary(es => es.ExternalSeatObjectKey, StringComparer.Ordinal);
                    foreach (var seatReq in request.Seats)
                    {
                        var eventSeat = eventSeatByKey[seatReq.SeatKey];
                        var pricePaid = validPriceItems[seatReq.PriceListItemId].FinalPrice;

                        order.Tickets.Add(new ModelTicket
                        {
                            EventScheduleId = (long)(request?.EventScheduleId.Value),
                            EventSectionId = eventSeat.EventSectionId,
                            EventSeatId = eventSeat.Id,
                            InventoryBatchId = inventoryBatchId,
                            OriginalClient = client,
                            CurrentClient = client,
                            OriginalOrder = order,
                            TicketCode = eventSeat.ExternalSeatObjectKey,
                            TicketType = ItemType.Ticket.ToString(),
                            PrivateToken = null,
                            SectionLabelSnapshot = eventSeat.EventSection.DisplayName,
                            SeatLabelSnapshot = eventSeat.ExternalSeatObjectKey,
                            IsDigital = true,
                            PricePaid = pricePaid,
                            Status = TicketStatus.PendingPayment,
                            CreatedAt = now,
                            UpdatedAt = now,
                            CreatedBy = Guid.Empty,
                            UpdatedBy = Guid.Empty
                        });
                    }
                }
                else if (bundle != null)
                {
                    var requestSeatByKey = request.Seats.ToDictionary(
                        x => x.SeatKey,
                        StringComparer.Ordinal);

                    var seatKeys = request.Seats
                        .Select(x => x.SeatKey)
                        .ToList();

                    // CREATE BUNDLE PASS
                    var bundlePassSeats = await _dbContext.BundleSeats
                        .Include(bs => bs.BundleSection)
                            .ThenInclude(bs => bs.Bundle)
                        .Where(bs =>
                            bs.BundleSection.BundleId == bundle.Id &&
                            seatKeys.Contains(bs.ExternalSeatObjectKey)
                        )
                        .ToListAsync();

                    foreach (var seat in bundlePassSeats)
                    {
                        var seatReq = requestSeatByKey[seat.ExternalSeatObjectKey];
                        var pricePaid = validPriceItems[seatReq.PriceListItemId].FinalPrice;

                        var bundlePass = new BundlePass
                        {
                            ClientId = client.Id,
                            UserId = null,
                            BundleId = bundle.Id,
                            BundleSeatId = seat.Id,
                            Price = pricePaid,
                            PurchasedAt = now, // TODO: Should it be nullable?
                            BundlePassType = BundlePassType.Full,
                            TrackingCode = seat.ExternalSeatObjectKey,
                            PrivateToken = "",
                            Status = BundlePassStatus.Active, // TODO: Should we add a Pending status?
                            CreatedAt = now,
                            CreatedBy = Guid.Empty,
                            UpdatedAt = now,
                            UpdatedBy = Guid.Empty
                        };

                        bundlePasses.Add(bundlePass);
                    }

                    _dbContext.BundlePasses.AddRange(bundlePasses);
                    // CREATE BUNDLE PASS

                    // CREATE BUNDLE TICKETS
                    var scheduleIds = await _dbContext.BundleEventSchedules
                        .Where(x => x.BundleId == bundle.Id)
                        .Select(x => x.EventScheduleId)
                        .ToListAsync(ct);

                    var baseSeatIds = bundleSeats
                        .Select(x => x.BaseSeatId)
                        .ToList();

                    eventSeats = await _dbContext.EventSeats
                        .Include(es => es.EventSection)
                        .Where(es =>
                            scheduleIds.Contains(es.EventSection.EventScheduleId) &&
                            baseSeatIds.Contains(es.BaseSeatId))
                        .ToListAsync(ct);

                    var eventSeatMap = eventSeats.ToDictionary(
                        es => (ScheduleId: es.EventSection.EventScheduleId, BaseSeatId: es.BaseSeatId)
                    );

                    foreach (var bundleSeat in bundleSeats)
                    {
                        var seatReq = requestSeatByKey[bundleSeat.ExternalSeatObjectKey];
                        var pricePaid = validPriceItems[seatReq.PriceListItemId].FinalPrice;

                        foreach (var scheduleId in scheduleIds)
                        {
                            var key = (scheduleId, bundleSeat.BaseSeatId);

                            if (!eventSeatMap.TryGetValue(key, out var eventSeat))
                            {
                                throw new InvalidOperationException(
                                    $"EventSeat not found for Schedule={scheduleId}, BaseSeat={bundleSeat.BaseSeatId}");
                            }

                            order.Tickets.Add(new ModelTicket
                            {
                                EventScheduleId = scheduleId,
                                EventSectionId = eventSeat.EventSectionId,
                                EventSeatId = eventSeat.Id,

                                InventoryBatchId = inventoryBatchId,
                                OriginalClient = client,
                                CurrentClient = client,
                                OriginalOrder = order,

                                TicketCode = eventSeat.ExternalSeatObjectKey,
                                TicketType = ItemType.BundlePass.ToString(),
                                PrivateToken = null,

                                SectionLabelSnapshot = eventSeat.EventSection.DisplayName,
                                SeatLabelSnapshot = eventSeat.ExternalSeatObjectKey,

                                IsDigital = true,
                                PricePaid = pricePaid,
                                Status = TicketStatus.PendingPayment,

                                CreatedAt = now,
                                UpdatedAt = now,
                                CreatedBy = Guid.Empty,
                                UpdatedBy = Guid.Empty
                            });
                        }
                    }
                    // CREATE BUNDLE TICKETS
                }

                _dbContext.Orders.Add(order);
                _dbContext.Payments.Add(payment);
                await _dbContext.SaveChangesAsync(ct);

                if (schedule != null)
                {
                    foreach (var ticket in order.Tickets)
                    {
                        order.Items.Add(new ModelOrderItem
                        {
                            ItemType = ItemType.Ticket,
                            ItemReferenceId = ticket.Id,
                            IsCourtesy = false,
                            Price = ticket.PricePaid
                        });
                    }
                }
                else if (bundle != null)
                {
                    foreach (var pass in bundlePasses)
                    {
                        order.Items.Add(new ModelOrderItem
                        {
                            ItemType = ItemType.BundlePass,
                            ItemReferenceId = pass.Id,
                            IsCourtesy = false,
                            Price = pass.Price
                        });
                    }
                }

                // CREATE BUNDLE PASS EVENT TICKET
                if (bundle != null)
                {
                    var passByCode = bundlePasses.ToDictionary(bp => bp.TrackingCode);
                    var joins = new List<BundlePassEventTicket>();

                    foreach (var ticket in order.Tickets)
                    {
                        if (!passByCode.TryGetValue(ticket.TicketCode, out var pass))
                        {
                            continue;
                        }

                        var join = new BundlePassEventTicket
                        {
                            BundlePassId = pass.Id,
                            TicketId = ticket.Id
                        };

                        _dbContext.BundlePassEventTickets.Add(join);
                    }
                }
                // CREATE BUNDLE PASS EVENT TICKET

                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation(
                    "Checkout initiated. LocalOrderId={OrderId} Reference={Reference} OrderRefId={OrderRefId} Amount={Amount} Tickets={TicketCount}",
                    order.Id, reference, orderRefId, amountStr, order.Tickets.Count);

                var gatewayBaseUrl = $"{_httpClient.BaseAddress!.Scheme}://{_httpClient.BaseAddress.Host}";

                return new InitiateCheckoutResponse
                {
                    LocalOrderId = order.Id,
                    SessionId = sessionId,
                    SuccessIndicator = successIndicator,
                    OrderRefId = orderRefId,
                    Amount = amountStr,
                    Currency = request.Currency,
                    MerchantId = _settings.MerchantId,
                    ApiVersion = _settings.Version,
                    GatewayBaseUrl = gatewayBaseUrl
                };
            }
            catch
            {
                await transaction.RollbackAsync(ct);


                try
                {
                    await _seatsIoBookingClient.ReleaseBookedSeatsAsync(
                        schedule.ExternalEventKey!, bookedSeatKeys, ct);
                    _logger.LogInformation(
                        "Compensatory Seats.io rollback completed. EventKey={EventKey}",
                        schedule.ExternalEventKey);
                }
                catch (Exception releaseEx)
                {
                    _logger.LogError(releaseEx,
                        "Compensatory Seats.io rollback FAILED. EventKey={EventKey} Seats={Seats}",
                        schedule.ExternalEventKey, string.Join(", ", bookedSeatKeys));
                }

                throw;
            }
        }

        public async Task<ConfirmCheckoutResponse> ConfirmCheckoutAsync(
            ConfirmCheckoutRequest request,
            CancellationToken ct = default)
        {
            var order = await _dbContext.Orders
                .Include(o => o.Client)
                .Include(o => o.Tickets)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == request.LocalOrderId, ct);

            if (order is null)
            {
                throw new KeyNotFoundException($"Order {request.LocalOrderId} not found.");
            }

            var payment = await _dbContext.Payments
                .FirstOrDefaultAsync(
                    p => p.OrderId == order.Id && p.ProviderReference == request.OrderRefId, ct);

            if (payment is null)
            {
                throw new KeyNotFoundException(
                    $"Payment not found for OrderId={order.Id} with ProviderReference={request.OrderRefId}.");
            }

            if (order.Status == OrderStatus.Paid
                && order.Tickets.All(t => t.Status == TicketStatus.Issued))
            {
                _logger.LogInformation(
                    "Idempotent confirm-checkout: Order {OrderId} was already confirmed.", order.Id);
                return new ConfirmCheckoutResponse
                {
                    OrderId = order.Id,
                    OrderStatus = order.Status.ToString(),
                    PaymentStatus = PaymentStatus.Captured.ToString(),
                    TicketsIssued = order.Tickets.Count(t => t.Status == TicketStatus.Issued),
                    Reference = order.Reference
                };
            }


            if (!string.IsNullOrWhiteSpace(request.ResultIndicator)
                && !string.Equals(request.ResultIndicator, payment.ProviderSessionReference, StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "resultIndicator does not match ProviderSessionReference. OrderId={OrderId} ResultIndicator={RI} Expected={Expected}. Continuing with Retrieve Order.",
                    order.Id, request.ResultIndicator, payment.ProviderSessionReference);
            }


            var evoResult = await RetrieveOrderAsync(request.OrderRefId, ct);


            var isSuccess =
                string.Equals(evoResult.Result, "SUCCESS", StringComparison.OrdinalIgnoreCase)
                && string.Equals(evoResult.Status, "CAPTURED", StringComparison.OrdinalIgnoreCase);

            var now = DateTimeOffset.UtcNow;

            if (isSuccess)
            {

                await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
                try
                {
                    payment.PaymentStatus = PaymentStatus.Captured;
                    payment.AppliedAt = now;

                    order.Status = OrderStatus.Paid;
                    order.PaidAt = now;
                    order.UpdatedAt = now;
                    order.UpdatedBy = Guid.Empty;

                    foreach (var ticket in order.Tickets.Where(t => t.Status != TicketStatus.Issued))
                    {
                        ticket.Status = TicketStatus.Issued;
                        ticket.PrivateToken = Guid.NewGuid().ToString("N");
                        ticket.UpdatedAt = now;
                        ticket.UpdatedBy = Guid.Empty;
                    }

                    await _dbContext.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    _logger.LogInformation(
                        "Starting confirmation email enqueue for evo order {OrderId} ({OrderReference}). OrderType={OrderType} SaleChannel={SaleChannel} EvoOrderRefId={EvoOrderRefId}",
                        order.Id,
                        order.Reference,
                        order.OrderType,
                        order.SaleChannel,
                        request.OrderRefId);
                    var emailResults = await _confirmationEmailQueue.EnqueueAsync(order.Id, order.Client, ct);
                    _logger.LogInformation(
                        "Completed confirmation email enqueue for evo order {OrderId} ({OrderReference}). Results={EmailEnqueueResults}",
                        order.Id,
                        order.Reference,
                        BookingConfirmationEmailEnqueueResultFormatter.Format(emailResults));

                    var issued = order.Tickets.Count(t => t.Status == TicketStatus.Issued);
                    _logger.LogInformation(
                        "Checkout confirmed. OrderId={OrderId} TicketsIssued={Count} Amount={Amount}",
                        order.Id, issued, evoResult.TotalCapturedAmount);

                    return new ConfirmCheckoutResponse
                    {
                        OrderId = order.Id,
                        OrderStatus = order.Status.ToString(),
                        PaymentStatus = PaymentStatus.Captured.ToString(),
                        TicketsIssued = issued,
                        Reference = order.Reference
                    };
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            }
            else
            {

                var newPaymentStatus = DetermineFailedPaymentStatus(evoResult);
                var newTicketStatus = newPaymentStatus == PaymentStatus.Expired
                    ? TicketStatus.Expired
                    : TicketStatus.Cancelled;

                await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
                try
                {
                    payment.PaymentStatus = newPaymentStatus;

                    order.Status = OrderStatus.Cancelled;
                    order.UpdatedAt = now;
                    order.UpdatedBy = Guid.Empty;

                    foreach (var ticket in order.Tickets.Where(t => t.Status == TicketStatus.PendingPayment))
                    {
                        ticket.Status = newTicketStatus;
                        ticket.UpdatedAt = now;
                        ticket.UpdatedBy = Guid.Empty;

                    }

                    await _dbContext.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                }
                catch
                {
                    await transaction.RollbackAsync(ct);
                    throw;
                }

                var seatsBySchedule = order.Tickets
                    .GroupBy(ticket => ticket.EventScheduleId)
                    .ToDictionary(
                        group => group.Key,
                        group => (IReadOnlyCollection<string>)group.Select(ticket => ticket.TicketCode).ToList());

                if (seatsBySchedule.Count > 0)
                {
                    var eventKeys = await _dbContext.EventSchedules
                        .AsNoTracking()
                        .Where(schedule => seatsBySchedule.Keys.Contains(schedule.Id))
                        .Select(schedule => new
                        {
                            schedule.Id,
                            schedule.ExternalEventKey
                        })
                        .ToDictionaryAsync(schedule => schedule.Id, schedule => schedule.ExternalEventKey, ct);

                    foreach (var (eventScheduleId, seatKeys) in seatsBySchedule)
                    {
                        if (!eventKeys.TryGetValue(eventScheduleId, out var eventKey) ||
                            string.IsNullOrWhiteSpace(eventKey))
                        {
                            continue;
                        }

                        try
                        {
                            await _seatsIoBookingClient.ReleaseBookedSeatsAsync(eventKey, seatKeys, ct);
                            _logger.LogInformation(
                                "Seats.io released after payment failure. OrderId={OrderId} EventScheduleId={EventScheduleId} EventKey={EventKey}",
                                order.Id, eventScheduleId, eventKey);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Could not release Seats.io after payment failure. OrderId={OrderId} EventScheduleId={EventScheduleId} EventKey={EventKey}",
                                order.Id, eventScheduleId, eventKey);
                        }
                    }
                }

                _logger.LogInformation(
                    "Checkout failed. OrderId={OrderId} EvoResult={Result} EvoStatus={Status} GatewayCode={GatewayCode}",
                    order.Id, evoResult.Result, evoResult.Status, evoResult.GatewayCode);

                return new ConfirmCheckoutResponse
                {
                    OrderId = order.Id,
                    OrderStatus = order.Status.ToString(),
                    PaymentStatus = newPaymentStatus.ToString(),
                    TicketsIssued = 0,
                    Reference = order.Reference
                };
            }
        }

        private static PaymentStatus DetermineFailedPaymentStatus(RetrieveOrderResponse evoResult)
        {
            if (string.Equals(evoResult.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            {
                return PaymentStatus.Cancelled;
            }

            if (string.Equals(evoResult.Status, "EXPIRED", StringComparison.OrdinalIgnoreCase))
            {
                return PaymentStatus.Expired;
            }

            return PaymentStatus.Failed;
        }

        private async Task<(string SessionId, string SuccessIndicator)> CallInitiateCheckoutAsync(
            string orderRefId,
            decimal amount,
            string currency,
            string returnUrl,
            string description,
            CancellationToken ct)
        {
            var body = new
            {
                apiOperation = "INITIATE_CHECKOUT",
                interaction = new
                {
                    operation = "PURCHASE",
                    merchant = new { name = "XBOL Ticketing" },
                    returnUrl
                },
                order = new
                {
                    id = orderRefId,
                    amount = amount.ToString("F2", CultureInfo.InvariantCulture),
                    currency,
                    description
                }
            };

            _logger.LogInformation(
                "Calling INITIATE_CHECKOUT. orderRefId={OrderRefId} amount={Amount} returnUrl={ReturnUrl}",
                orderRefId, amount, returnUrl);

            var httpResponse = await _httpClient.PostAsJsonAsync("session", body, ct);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var errorBody = await httpResponse.Content.ReadAsStringAsync(ct);
                _logger.LogError(
                    "EVO INITIATE_CHECKOUT failed. StatusCode={StatusCode} Body={Body}",
                    httpResponse.StatusCode, SanitizeBody(errorBody));
                throw new Exception("The payment provider returned an error while creating the checkout session.");
            }

            var rawBody = await httpResponse.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(rawBody);
            var root = doc.RootElement;

            var result = root.TryGetProperty("result", out var r) ? r.GetString() : null;
            if (!string.Equals(result, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "EVO INITIATE_CHECKOUT result is not SUCCESS. result={Result} Body={Body}",
                    result, SanitizeBody(rawBody));
                throw new Exception("The payment provider rejected the creation of the checkout session.");
            }

            var sessionId = root.TryGetProperty("session", out var sess) && sess.TryGetProperty("id", out var sid)
                ? sid.GetString()
                : null;

            var successIndicator = root.TryGetProperty("successIndicator", out var si)
                ? si.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new InvalidOperationException(
                    "EVO did not return session.id in the INITIATE_CHECKOUT response.");
            }

            if (string.IsNullOrWhiteSpace(successIndicator))
            {
                throw new InvalidOperationException(
                    "EVO did not return successIndicator in the INITIATE_CHECKOUT response.");
            }

            _logger.LogInformation(
                "INITIATE_CHECKOUT successful. orderRefId={OrderRefId} sessionId={SessionId} successIndicator={SI}",
                orderRefId, Redact(sessionId), Redact(successIndicator));

            return (sessionId, successIndicator);
        }

        private async Task<ModelClient> ResolveClientAsync(
            ClientInfoRequest contact,
            DateTimeOffset now,
            CancellationToken ct)
        {
            ModelClient? client = null;

            if (contact.Id.HasValue)
            {
                client = await _dbContext.Clients.FindAsync([contact.Id.Value], ct);
                if (client is null)
                {
                    throw new KeyNotFoundException($"Client {contact.Id.Value} not found.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(contact.Email))
            {
                var email = contact.Email.Trim();
                client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.Email == email, ct);
            }

            if (client is null)
            {
                client = new ModelClient
                {
                    ClientType = ClientType.Individual,
                    Email = contact.Email?.Trim(),
                    PhoneRegionCodeId = contact.PhoneRegionCodeId,
                    PhoneNumber = NormalizePhoneNumber(contact.PhoneNumber),
                    FullName = ResolveFullName(contact),
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = Guid.Empty,
                    UpdatedBy = Guid.Empty
                };
                _dbContext.Clients.Add(client);
                return client;
            }

            if (!string.IsNullOrWhiteSpace(contact.Email))
            {
                client.Email = contact.Email.Trim();
            }

            client.PhoneRegionCodeId = contact.PhoneRegionCodeId;
            client.PhoneNumber = NormalizePhoneNumber(contact.PhoneNumber);
            client.FullName = ResolveFullName(contact, client.FullName);
            client.UpdatedAt = now;
            client.UpdatedBy = Guid.Empty;
            return client;
        }

        private static string? ResolveFullName(ClientInfoRequest contact, string? fallback = null)
        {
            if (!string.IsNullOrWhiteSpace(contact.FullName))
            {
                return contact.FullName.Trim();
            }

            var composed = $"{contact.FirstName} {contact.LastName}".Trim();
            return string.IsNullOrWhiteSpace(composed) ? fallback : composed;
        }

        private static string NormalizePhoneNumber(string phoneNumber)
            => new string(phoneNumber.Where(char.IsAsciiDigit).ToArray());

        private static string? GetStringProp(JsonElement element, string name)
            => element.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString()
                : null;

        private static string Redact(string? value)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= 8)
            {
                return "***";
            }

            return $"{value[..6]}…{value[^4..]}";
        }

        private static string SanitizeBody(string body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return "(empty)";
            }

            var trimmed = body.Length > 800 ? body[..800] + "…(truncated)" : body;
            return trimmed.Replace(Environment.NewLine, " ").Replace("\n", " ");
        }
    }
}
