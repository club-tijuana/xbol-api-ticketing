using Microsoft.Extensions.DependencyInjection;
using XBOL.Ticketing.DynamicPricing;
using XBOL.Ticketing.Services.Accreditation;
using XBOL.Ticketing.Services.Category;
using XBOL.Ticketing.Services.Client;
using XBOL.Ticketing.Services.Event;
using XBOL.Ticketing.Services.Identity;
using XBOL.Ticketing.Services.Order;
using XBOL.Ticketing.Services.RulesEngine;
using XBOL.Ticketing.Services.Season;
using XBOL.Ticketing.Services.Ticket;
using XBOL.Ticketing.Services.Venue;

namespace XBOL.Ticketing.Services.Extensions
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection ConfigureServices(this IServiceCollection services)
        {
            services.AddScoped<AccreditationService>();
            services.AddScoped<AccreditationTypeService>();
            services.AddScoped<CategoryService>();
            services.AddScoped<ClientCreditAccountService>();
            services.AddScoped<ClientCreditTransactionService>();
            services.AddScoped<ClientService>();
            services.AddScoped<EventMediaService>();
            services.AddScoped<EventScheduleService>();
            services.AddScoped<EventSeatService>();
            services.AddScoped<EventSectionService>();
            services.AddScoped<EventService>();
            services.AddScoped<EventTagService>();
            services.AddScoped<OrganizerMemberService>();
            services.AddScoped<OrganizerService>();
            services.AddScoped<RoleService>();
            services.AddScoped<UserService>();
            services.AddScoped<OrderFeeService>();
            services.AddScoped<OrderItemService>();
            services.AddScoped<OrderService>();
            services.AddScoped<OrderTaxService>();
            services.AddScoped<PaymentService>();
            services.AddScoped<PromoCodeRedemptionService>();
            services.AddScoped<PromoCodeService>();
            services.AddScoped<SeatHoldService>();
            services.AddScoped<SeasonPassEventTicketService>();
            services.AddScoped<SeasonPassService>();
            services.AddScoped<SeasonService>();
            services.AddScoped<SeasonTagService>();
            services.AddScoped<TicketScanLogService>();
            services.AddScoped<TicketService>();
            services.AddScoped<TicketTransferService>();
            services.AddScoped<BaseRowService>();
            services.AddScoped<BaseSeatService>();
            services.AddScoped<BaseSectionService>();
            services.AddScoped<BaseZoneService>();
            services.AddScoped<GateAccessRuleService>();
            services.AddScoped<GateService>();
            services.AddScoped<VenueMapService>();
            services.AddScoped<VenueService>();
            services.AddScoped<AuditLogService>();
            services.AddScoped<DeviceService>();
            services.AddScoped<DistributorService>();
            services.AddScoped<InventoryBatchService>();
            services.AddScoped<PriceRuleService>();
            services.AddScoped<TagService>();
            services.AddScoped<TagTypeService>();
            services.AddScoped<SeatsIoService>();

            services.AddScoped<RulesEngineService>();
            services.AddTransient<IEngine, Engine>();

            return services;
        }
    }
}