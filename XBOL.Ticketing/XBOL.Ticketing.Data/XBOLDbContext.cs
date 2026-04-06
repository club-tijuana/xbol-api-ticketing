using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Configurations;
using XBOL.Ticketing.Data.Extensions;

namespace XBOL.Ticketing.Data
{
    public class XBOLDbContext : IdentityDbContext<User, Role, Guid>
    {
        public DbSet<Accreditation> Accreditations => Set<Accreditation>();
        public DbSet<AccreditationType> AccreditationTypes => Set<AccreditationType>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<BaseRow> BaseRows => Set<BaseRow>();
        public DbSet<BaseSeat> BaseSeats => Set<BaseSeat>();
        public DbSet<BaseSection> BaseSections => Set<BaseSection>();
        public DbSet<BaseZone> BaseZones => Set<BaseZone>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<ClientCreditAccount> ClientCreditAccounts => Set<ClientCreditAccount>();
        public DbSet<ClientCreditTransaction> ClientCreditTransactions => Set<ClientCreditTransaction>();
        public DbSet<Device> Devices => Set<Device>();
        public DbSet<Distributor> Distributors => Set<Distributor>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventSchedule> EventSchedules => Set<EventSchedule>();
        public DbSet<EventMedia> EventMedias => Set<EventMedia>();
        public DbSet<EventSeat> EventSeats => Set<EventSeat>();
        public DbSet<EventSection> EventSections => Set<EventSection>();
        public DbSet<EventTag> EventTags => Set<EventTag>();
        public DbSet<EventCategory> EventCategories => Set<EventCategory>();

        public DbSet<Gate> Gates => Set<Gate>();
        public DbSet<GateAccessRule> GateAccessRules => Set<GateAccessRule>();
        public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderFee> OrderFees => Set<OrderFee>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<OrderTax> OrderTaxs => Set<OrderTax>();
        public DbSet<Organizer> Organizers => Set<Organizer>();
        public DbSet<OrganizerMember> OrganizerMembers => Set<OrganizerMember>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<PriceRule> PriceRules => Set<PriceRule>();
        public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
        public DbSet<PromoCodeRedemption> PromoCodeRedemptions => Set<PromoCodeRedemption>();

        public DbSet<Season> Seasons => Set<Season>();
        public DbSet<SeasonPass> SeasonPasses => Set<SeasonPass>();
        public DbSet<SeasonPassEventTicket> SeasonPassEventTickets => Set<SeasonPassEventTicket>();
        public DbSet<SeasonTag> SeasonTags => Set<SeasonTag>();
        public DbSet<SeatHold> SeatHolds => Set<SeatHold>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<TagType> TagTypes => Set<TagType>();
        public DbSet<Ticket> Tickets => Set<Ticket>();
        public DbSet<TicketScanLog> TicketScanLogs => Set<TicketScanLog>();
        public DbSet<TicketTransfer> TicketTransfers => Set<TicketTransfer>();
        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<VenueMap> VenueMaps => Set<VenueMap>();

        public XBOLDbContext() : base()
        {
        }

        public XBOLDbContext(DbContextOptions<XBOLDbContext> options)
           : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(
                    @"Host=localhost;Port=5432;Database=XBOL;Username=postgres;Password=12345");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.RemovePluralizingTableNameConvention();

            modelBuilder.ApplyConfiguration(new EventConfiguration());
            modelBuilder.ApplyConfiguration(new ClientConfiguration());
            modelBuilder.ApplyConfiguration(new OrganizerConfiguration());
            modelBuilder.ApplyConfiguration(new TicketConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
        }
    }
}
