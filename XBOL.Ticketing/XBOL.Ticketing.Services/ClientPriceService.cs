using JasperFx.Core.Filters;
using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Commons.Enums;
using XBOL.Ticketing.Core.DTO.Requests;
using XBOL.Ticketing.Core.DTO.Responses;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;

namespace XBOL.Ticketing.Services
{
    [Obsolete("This service is being deprecated in favor of standarize the price with the PriceService. For now we are only using it for backward compatibility.")]
    public class ClientPriceService
    {
        private readonly XBOLDbContext _dbContext;

        public ClientPriceService(XBOLDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<SeatAvailabilityResponse> GetSeatAvailabilityAsync(ReservationFiltersRequest filters)
        {
            long? referenceId;
            SaleType? referenceType;

            if (filters.SeasonId.HasValue)
            {
                referenceId = filters.SeasonId.Value;
                referenceType = SaleType.SeasonPass;
            }
            else if (filters.ScheduleId.HasValue)
            {
                var eventSchedule = await _dbContext.EventSchedules
                                            .AsNoTracking()
                                            .Where(x => x.Id == filters.ScheduleId.Value)
                                            .SingleOrDefaultAsync();

                if (eventSchedule == null)
                {
                    return new SeatAvailabilityResponse();
                }

                referenceId = eventSchedule.EventId;
                referenceType = SaleType.Event;
            }
            else
            {
                return new SeatAvailabilityResponse();
            }
            var priceReference = await _dbContext.PriceReferences
                                        .AsNoTracking()
                                        .Where(pr => pr.ReferenceId == referenceId.Value
                                            && pr.ReferenceType == referenceType.Value
                                            && pr.IsActive)
                                        .SingleOrDefaultAsync();

            if (priceReference == null)
            {
                return new SeatAvailabilityResponse();
            }

            var priceItems = await _dbContext.PriceListItems
                                    .Include(pli => pli.Price)
                                        .ThenInclude(p => p.PriceType)
                                    .AsNoTracking()
                                    .Where(pli => pli.PriceList.PriceReferenceId == priceReference.Id
                                        && pli.PriceList.Status == VersionStatus.Active)
                                    .ToListAsync();

            if (priceItems.Count == 0)
            {
                return new SeatAvailabilityResponse();
            }

            return await GenerateSeatAvailabilityAsync(priceItems, filters);
        }

        public async Task<List<SectionPriceResponse>> GetSectionPricesAsync(SaleType saleType, long referenceId)
        {
            var priceReference = await _dbContext.PriceReferences
                                            .AsNoTracking()
                                            .Where(x => x.ReferenceType == saleType
                                                && x.ReferenceId == referenceId
                                                && x.IsActive)
                                            .SingleOrDefaultAsync();

            if (priceReference == null)
            {
                return new List<SectionPriceResponse>();
            }

            var priceItems = await _dbContext.PriceListItems
                                    .Include(pli => pli.Price)
                                        .ThenInclude(p => p.PriceType)
                                    .AsNoTracking()
                                    .Where(pli => pli.PriceList.PriceReferenceId == priceReference.Id
                                        && pli.PriceList.Status == VersionStatus.Active
                                        && (pli.BaseZoneId != null || pli.BaseSectionId != null)
                                        && pli.BaseRowId == null
                                        && pli.BaseSeatId == null)
                                    .ToListAsync();

            var zonePrices = priceItems.Where(p => p.BaseZoneId != null && p.BaseSectionId == null && p.Price.PriceType.IsBasePrice).ToDictionary(p => p.BaseZoneId!.Value);
            var sectionPrices = priceItems.Where(p => p.BaseSectionId != null && p.Price.PriceType.IsBasePrice).ToDictionary(p => p.BaseSectionId!.Value);

            var activeZoneIds = zonePrices.Keys.ToList();
            var activeSectionIds = sectionPrices.Keys.ToList();

            if (!activeZoneIds.Any() && !activeSectionIds.Any())
            {
                return new List<SectionPriceResponse>();
            }

            var dbSections = await _dbContext.BaseSections
                                    .AsNoTracking()
                                    .Where(s => activeZoneIds.Contains(s.BaseZoneId)
                                        || activeSectionIds.Contains(s.Id))
                                    .ToListAsync();

            var groupedSectionPrices = dbSections
                .Select(section =>
                {
                    PriceListItem? matchedPriceItem = null;
                    if (sectionPrices.TryGetValue(section.Id, out var sPli)) matchedPriceItem = sPli;
                    else if (zonePrices.TryGetValue(section.BaseZoneId, out var zPli)) matchedPriceItem = zPli;

                    return new
                    {
                        SectionName = section.Name,
                        Price = matchedPriceItem?.FinalPrice
                    };
                })
                .Where(x => x.Price.HasValue) 
                .GroupBy(x => x.Price!.Value)
                .Select(group => new SectionPriceResponse
                {
                    Price = group.Key,
                    Objects = group.Select(x => x.SectionName).Distinct().OrderBy(name => name).ToList(),
                    Currency = "MXN", // TODO: Add currency support for totals
                })
                .OrderByDescending(dto => dto.Price) // Optional: order highest price to lowest
                .ToList();

            return groupedSectionPrices;
        }

        private async Task<SeatAvailabilityResponse> GenerateSeatAvailabilityAsync(List<PriceListItem> priceItems, ReservationFiltersRequest filters)
        {
            // We should only bring for now the base prices, since the current structure can't handle multiple price types
            var zonePrices = priceItems.Where(p => p.BaseZoneId != null && p.BaseSectionId == null && p.Price.PriceType.IsBasePrice).ToDictionary(p => p.BaseZoneId!.Value);
            var sectionPrices = priceItems.Where(p => p.BaseSectionId != null && p.BaseRowId == null && p.Price.PriceType.IsBasePrice).ToDictionary(p => p.BaseSectionId!.Value);
            var rowPrices = priceItems.Where(p => p.BaseRowId != null && p.BaseSeatId == null && p.Price.PriceType.IsBasePrice).ToDictionary(p => p.BaseRowId!.Value);
            var seatPrices = priceItems.Where(p => p.BaseSeatId != null && p.Price.PriceType.IsBasePrice).ToDictionary(p => p.BaseSeatId!.Value);

            decimal min = filters.MinimumPrice ?? 0;
            decimal? max = filters.MaximumPrice;

            var activeZoneIds = zonePrices.Keys.ToList();
            var activeSectionIds = sectionPrices.Keys.ToList();

            var sectionQuery = _dbContext.Set<BaseSection>()
                                .Where(s => activeZoneIds.Contains(s.BaseZoneId)
                                    || activeSectionIds.Contains(s.Id));

            if (filters.ZoneId.HasValue)
            {
                sectionQuery = sectionQuery.Where(s => s.BaseZoneId == filters.ZoneId.Value);
            }
            if (filters.SectionId.HasValue)
            {
                sectionQuery = sectionQuery.Where(s => s.Id == filters.SectionId.Value);
            }

            var dbSections = await sectionQuery.ToListAsync();
            var sections = new List<SectionResponse>();

            foreach (var section in dbSections)
            {
                PriceListItem? matchedPriceItem = null;
                if (sectionPrices.TryGetValue(section.Id, out var sPli))
                {
                    matchedPriceItem = sPli;
                }
                else if (zonePrices.TryGetValue(section.BaseZoneId, out var zPli))
                {
                    matchedPriceItem = zPli;
                }

                if (matchedPriceItem != null)
                {
                    decimal price = matchedPriceItem.FinalPrice;
                    if (price >= min && (max == null || price <= max.Value))
                    {
                        sections.Add(new SectionResponse
                        {
                            Id = section.Id,
                            Name = section.Name,
                            DisplayName = section.Name,
                            Price = price,
                            PriceListItemId = matchedPriceItem.Id
                        });
                    }
                }
            }

            var activeRowIds = rowPrices.Keys.ToList();
            var activeSeatIds = seatPrices.Keys.ToList();

            var seatQuery = _dbContext.BaseSeats
                            .Include(bs => bs.BaseRow)
                                .ThenInclude(br => br.BaseSection)
                            .AsNoTracking()
                            .Where(s => activeRowIds.Contains(s.BaseRowId)
                                || activeSeatIds.Contains(s.Id));

            if (filters.ZoneId.HasValue)
            {
                seatQuery = seatQuery.Where(s => s.BaseRow.BaseSection.BaseZoneId == filters.ZoneId.Value);
            }
            if (filters.SectionId.HasValue)
            {
                seatQuery = seatQuery.Where(s => s.BaseRow.BaseSectionId == filters.SectionId.Value);
            }

            var dbSeats = await seatQuery.ToListAsync();
            var seatOverrides = new List<SeatResponse>();

            foreach (var seat in dbSeats)
            {
                PriceListItem? matchedPriceItem = null;
                if (seatPrices.TryGetValue(seat.Id, out var stPli))
                {
                    matchedPriceItem = stPli;
                }
                else if (rowPrices.TryGetValue(seat.BaseRowId, out var rPli))
                {
                    matchedPriceItem = rPli;
                }

                if (matchedPriceItem != null)
                {
                    decimal price = matchedPriceItem.FinalPrice;
                    if (price >= min && (max == null || price <= max.Value))
                    {
                        seatOverrides.Add(new SeatResponse
                        {
                            Id = seat.Id,
                            ExternalSeatObjectKey = $"{seat.BaseRow.BaseSection.Name}-{seat.BaseRow.RowLabel}-{seat.SeatNumber}",
                            PriceOverride = price,
                            PriceListItemId = matchedPriceItem.Id
                        });
                    }
                }
            }

            return new SeatAvailabilityResponse
            {
                Sections = sections,
                SeatOverrides = seatOverrides
            };
        }
    }
}
