using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;

namespace XBOL.Ticketing.Services
{
    public class PriceService
    {
        private readonly XBOLDbContext _dbContext;

        public PriceService(XBOLDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<SeatsIoPriceDTO>?> GetSeatsIoPricesAsync(SaleType referenceType, long referenceId)
        {
            var priceListQuery = await (from pr in _dbContext.PriceReferences
                                        join pl in _dbContext.PriceLists on pr.Id equals pl.PriceReferenceId
                                        where pr.ReferenceType == referenceType
                                              && pr.ReferenceId == referenceId
                                              && pr.IsActive == true
                                              && pl.Status == VersionStatus.Active
                                        orderby pl.VersionNumber descending
                                        select pl).FirstOrDefaultAsync();

            if (priceListQuery == null)
            {
                return null;
            }

            var validItemsQuery = _dbContext.PriceListItems
                .Where(pli => pli.PriceListId == priceListQuery.Id && pli.FinalPrice > 0);

            var categoryPrices = await (from pli in validItemsQuery
                                        join pt in _dbContext.PriceTypes on pli.PriceTypeId equals pt.Id
                                        join bz in _dbContext.BaseZones on pli.BaseZoneId equals bz.Id
                                        where pli.BaseSectionId == null && pli.BaseRowId == null && pli.BaseSeatId == null
                                        select new
                                        {
                                            Item = pli,
                                            Type = pt,
                                            ExternalZoneKey = bz.ExternalZoneKey
                                        }).ToListAsync();

            var seatPrices = await (from pli in validItemsQuery
                                    join pt in _dbContext.PriceTypes on pli.PriceTypeId equals pt.Id
                                    join bst in _dbContext.BaseSeats on pli.BaseSeatId equals bst.Id
                                    join br in _dbContext.BaseRows on bst.BaseRowId equals br.Id
                                    join bs in _dbContext.BaseSections on br.BaseSectionId equals bs.Id
                                    select new
                                    {
                                        Item = pli,
                                        Type = pt,
                                        SectionName = bs.Name,
                                        RowLabel = br.RowLabel,
                                        SeatNumber = bst.SeatNumber,
                                        Priority = 1
                                    }).ToListAsync();

            var rowPrices = await (from pli in validItemsQuery
                                   join pt in _dbContext.PriceTypes on pli.PriceTypeId equals pt.Id
                                   join br in _dbContext.BaseRows on pli.BaseRowId equals br.Id
                                   join bs in _dbContext.BaseSections on br.BaseSectionId equals bs.Id
                                   join bst in _dbContext.BaseSeats on br.Id equals bst.BaseRowId
                                   where pli.BaseSeatId == null
                                   select new
                                   {
                                       Item = pli,
                                       Type = pt,
                                       SectionName = bs.Name,
                                       RowLabel = br.RowLabel,
                                       SeatNumber = bst.SeatNumber,
                                       Priority = 2
                                   }).ToListAsync();

            var sectionPrices = await (from pli in validItemsQuery
                                       join pt in _dbContext.PriceTypes on pli.PriceTypeId equals pt.Id
                                       join bs in _dbContext.BaseSections on pli.BaseSectionId equals bs.Id
                                       join br in _dbContext.BaseRows on bs.Id equals br.BaseSectionId
                                       join bst in _dbContext.BaseSeats on br.Id equals bst.BaseRowId // Join expansivo doble
                                       where pli.BaseRowId == null && pli.BaseSeatId == null
                                       select new
                                       {
                                           Item = pli,
                                           Type = pt,
                                           SectionName = bs.Name,
                                           RowLabel = br.RowLabel,
                                           SeatNumber = bst.SeatNumber,
                                           Priority = 3
                                       }).ToListAsync();

            var allObjectOverrides = seatPrices.Concat(rowPrices).Concat(sectionPrices);

            var uniqueObjectOverrides = allObjectOverrides
                .GroupBy(x => new { x.SectionName, x.RowLabel, x.SeatNumber })
                .SelectMany(g =>
                {
                    var topPriority = g.Min(x => x.Priority);
                    return g.Where(x => x.Priority == topPriority);
                })
                .ToList();

            var validItemIds = categoryPrices.Select(x => x.Item.Id)
                                .Concat(uniqueObjectOverrides.Select(x => x.Item.Id))
                                .Distinct()
                                .ToList();

            var feesDict = await _dbContext.PriceListItemFees
                .Where(f => validItemIds.Contains(f.PriceListItemId))
                .GroupBy(f => f.PriceListItemId)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(f => (decimal?)f.FeeAmount) ?? 0);

            var rawSeatsIoPrices = new List<SeatsIoPriceDTO>();

            foreach (var group in categoryPrices.GroupBy(x => x.ExternalZoneKey))
            {
                var baseItem = group.FirstOrDefault(x => x.Type.IsBasePrice == true) ?? group.First();
                var dto = new SeatsIoPriceDTO { Category = group.Key };

                if (group.Count() > 1)
                {
                    dto.TicketTypes = group.Select(x => CreateTicketTypeDTO(x.Item, x.Type)).ToArray();
                }
                else
                {
                    AssignSinglePrice(dto, baseItem.Item, feesDict.GetValueOrDefault(baseItem.Item.Id, 0));
                }
                rawSeatsIoPrices.Add(dto);
            }

            foreach (var group in uniqueObjectOverrides.GroupBy(x => new { x.SectionName, x.RowLabel, x.SeatNumber }))
            {
                var baseItem = group.FirstOrDefault(x => x.Type.IsBasePrice == true) ?? group.First();

                var keyParts = new[] { group.Key.SectionName, group.Key.RowLabel, group.Key.SeatNumber }
                                    .Where(s => !string.IsNullOrWhiteSpace(s));

                var dto = new SeatsIoPriceDTO { Objects = new[] { string.Join("-", keyParts) } };

                if (group.Count() > 1)
                {
                    dto.TicketTypes = group.Select(x => CreateTicketTypeDTO(x.Item, x.Type)).ToArray();
                }
                else
                {
                    AssignSinglePrice(dto, baseItem.Item, feesDict.GetValueOrDefault(baseItem.Item.Id, 0));
                }
                rawSeatsIoPrices.Add(dto);
            }

            var finalPrices = new List<SeatsIoPriceDTO>();
            finalPrices.AddRange(rawSeatsIoPrices.Where(x => x.Category != null));

            var groupedObjectPrices = rawSeatsIoPrices.Where(x => x.Objects != null)
                .GroupBy(x => new
                {
                    x.Price,
                    TicketTypesKey = x.TicketTypes == null ? "" : string.Join("|", x.TicketTypes.OrderBy(t => t.TicketType).Select(t => $"{t.TicketType}:{t.Price}"))
                })
                .Select(g => new SeatsIoPriceDTO
                {
                    Category = null,
                    PriceListItemId = g.First().PriceListItemId,
                    Price = g.Key.Price,
                    OriginalPrice = g.First().OriginalPrice,
                    Fee = g.First().Fee,
                    TicketTypes = g.First().TicketTypes,
                    Objects = g.SelectMany(x => x.Objects!).Distinct().ToArray()
                });

            finalPrices.AddRange(groupedObjectPrices);
            return finalPrices;
        }

        // Métodos auxiliares para no repetir código en el if/else
        private TicketTypeDTO CreateTicketTypeDTO(PriceListItem item, PriceType type) => new()
        {
            PriceListItemId = item.Id,
            TicketType = type.Name,
            Price = item.FinalPrice,
            Label = type.Label,
            Description = type.Description,
            Primary = type.Primary,
            Unavailable = false
        };

        private void AssignSinglePrice(SeatsIoPriceDTO dto, PriceListItem item, decimal fee)
        {
            dto.PriceListItemId = item.Id;
            dto.Price = item.FinalPrice;
            dto.OriginalPrice = item.BasePrice;
            dto.Fee = fee;
        }
    }
}
