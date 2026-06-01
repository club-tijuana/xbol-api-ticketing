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
        public DbSet<BlobAsset> BlobAssets => Set<BlobAsset>();
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
        public DbSet<Bundle> Bundles => Set<Bundle>();
        public DbSet<BundleEventSchedule> BundleEventSchedules => Set<BundleEventSchedule>();
        public DbSet<BundleSection> BundleSections => Set<BundleSection>();
        public DbSet<BundleSeat> BundleSeats => Set<BundleSeat>();
        public DbSet<BundlePass> BundlePasses => Set<BundlePass>();
        public DbSet<BundlePassEventTicket> BundlePassEventTickets => Set<BundlePassEventTicket>();
        public DbSet<BundleTag> BundleTags => Set<BundleTag>();
        public DbSet<Gate> Gates => Set<Gate>();
        public DbSet<GateAccessRule> GateAccessRules => Set<GateAccessRule>();
        public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderFee> OrderFees => Set<OrderFee>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<OrderTax> OrderTaxs => Set<OrderTax>();
        public DbSet<Organizer> Organizers => Set<Organizer>();
        public DbSet<Payment> Payments => Set<Payment>();
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
        public DbSet<Media> Media => Set<Media>();
        public DbSet<Venue> Venues => Set<Venue>();
        public DbSet<VenueMap> VenueMaps => Set<VenueMap>();
        public DbSet<AdditionalCharge> AdditionalCharges => Set<AdditionalCharge>();
        public DbSet<Price> Prices => Set<Price>();
        public DbSet<PriceList> PriceLists => Set<PriceList>();
        public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
        public DbSet<PriceListItemFee> PriceListItemFees => Set<PriceListItemFee>();
        public DbSet<PriceReference> PriceReferences => Set<PriceReference>();
        public DbSet<PriceSegment> PriceSegments => Set<PriceSegment>();
        public DbSet<PriceType> PriceTypes => Set<PriceType>();

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
                optionsBuilder.UseNpgsql(@"Host=localhost;Port=5432;Database=XBOL;Username=postgres;Password=12345");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseModel).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property(nameof(BaseModel.Id))
                        .ValueGeneratedOnAdd();
                }
            }

            modelBuilder.RemovePluralizingTableNameConvention();

            modelBuilder.ApplyConfiguration(new BundleConfiguration());
            modelBuilder.ApplyConfiguration(new BundleEventScheduleConfiguration());
            modelBuilder.ApplyConfiguration(new BundlePassConfiguration());
            modelBuilder.ApplyConfiguration(new ClientConfiguration());
            modelBuilder.ApplyConfiguration(new EventConfiguration());
            modelBuilder.ApplyConfiguration(new EventScheduleConfiguration());
            modelBuilder.ApplyConfiguration(new OrganizerConfiguration());
            modelBuilder.ApplyConfiguration(new PriceListConfiguration());
            modelBuilder.ApplyConfiguration(new PriceReferenceConfiguration());
            modelBuilder.ApplyConfiguration(new PriceSegmentConfiguration());
            modelBuilder.ApplyConfiguration(new PriceTypeConfiguration());
            modelBuilder.ApplyConfiguration(new TicketConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new BlobAssetConfiguration());
            modelBuilder.ApplyConfiguration(new MediaConfiguration());
        }
    }
}
