using Microsoft.Extensions.DependencyInjection;
using XBOL.Ticketing.Data.Repositories;
using XBOL.Ticketing.Data.Repositories.Accreditation;
using XBOL.Ticketing.Data.Repositories.Client;
using XBOL.Ticketing.Data.Repositories.Event;
using XBOL.Ticketing.Data.Repositories.Identity;
using XBOL.Ticketing.Data.Repositories.Order;
using XBOL.Ticketing.Data.Repositories.Season;
using XBOL.Ticketing.Data.Repositories.Ticket;
using XBOL.Ticketing.Data.Repositories.Venue;

namespace XBOL.Ticketing.Data.Extensions
{
    public static class RepositoryConfiguration
    {
        public static IServiceCollection ConfigureRepositories(this IServiceCollection services)
        {
            services.AddScoped<AccreditationRepository>();
            services.AddScoped<AccreditationTypeRepository>();
            services.AddScoped<ClientCreditAccountRepository>();
            services.AddScoped<ClientCreditTransactionRepository>();
            services.AddScoped<ClientRepository>();
            services.AddScoped<EventMediaRepository>();
            services.AddScoped<EventRepository>();
            services.AddScoped<EventScheduleRepository>();
            services.AddScoped<EventSeatRepository>();
            services.AddScoped<EventSectionRepository>();
            services.AddScoped<EventTagRepository>();
            services.AddScoped<OrganizerMemberRepository>();
            services.AddScoped<OrganizerRepository>();
            services.AddScoped<RoleRepository>();
            services.AddScoped<UserRepository>();
            services.AddScoped<OrderFeeRepository>();
            services.AddScoped<OrderItemRepository>();
            services.AddScoped<OrderRepository>();
            services.AddScoped<OrderTaxRepository>();
            services.AddScoped<PaymentRepository>();
            services.AddScoped<PromoCodeRedemptionRepository>();
            services.AddScoped<PromoCodeRepository>();
            services.AddScoped<SeatHoldRepository>();
            services.AddScoped<SeasonPassEventTicketRepository>();
            services.AddScoped<SeasonPassRepository>();
            services.AddScoped<SeasonRepository>();
            services.AddScoped<SeasonTagRepository>();
            services.AddScoped<TicketRepository>();
            services.AddScoped<TicketScanLogRepository>();
            services.AddScoped<TicketTransferRepository>();
            services.AddScoped<BaseRowRepository>();
            services.AddScoped<BaseSeatRepository>();
            services.AddScoped<BaseSectionRepository>();
            services.AddScoped<BaseZoneRepository>();
            services.AddScoped<GateAccessRuleRepository>();
            services.AddScoped<GateRepository>();
            services.AddScoped<VenueMapRepository>();
            services.AddScoped<VenueRepository>();
            services.AddScoped<AuditLogRepository>();
            services.AddScoped<DeviceRepository>();
            services.AddScoped<DistributorRepository>();
            services.AddScoped<InventoryBatchRepository>();
            services.AddScoped<PriceRuleRepository>();
            services.AddScoped<TagRepository>();
            services.AddScoped<TagTypeRepository>();

            return services;
        }
    }
}