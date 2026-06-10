using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Data.Repositories.Event
{
    public class EventSeatRepository(XBOLDbContext dbContext) : BaseRepository<EventSeat>(dbContext)
    {
        public async Task UpdatePricesFromList(List<(long Id, decimal PriceOverride)> eventSeats)
        {
            ArgumentNullException.ThrowIfNull(eventSeats);
            var rows = eventSeats.ConvertAll(x => new EventSeat { Id = x.Id });

            if (rows.Count == 0)
            {
                return;
            }

            await using var tx = await DbContext.Database.BeginTransactionAsync();

            var config = new BulkConfig
            {
                UpdateByProperties = [nameof(EventSeat.Id)],
                //PropertiesToInclude = [nameof(EventSeat.PriceOverride)],

                BatchSize = 5000,
                SetOutputIdentity = false,
                TrackingEntities = false
            };

            await DbContext.BulkUpdateAsync(rows, config);

            await tx.CommitAsync();
        }

        public async Task<EventSeat?> GetByExternalSeatObjectKey(string key)
        {
            return await DbSet.FirstOrDefaultAsync(x => x.ExternalSeatObjectKey == key);
        }
    }
}
