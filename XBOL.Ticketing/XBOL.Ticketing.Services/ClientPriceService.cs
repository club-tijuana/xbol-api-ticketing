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
                referenceType = SaleType.Bundle;
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
                                    .Include(pli => pli.FeeList)
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
                    if (sectionPrices.TryGetValue(section.Id, out var sPli))
                    {
                        matchedPriceItem = sPli;
                    }
                    else if (zonePrices.TryGetValue(section.BaseZoneId, out var zPli))
                    {
                        matchedPriceItem = zPli;
                    }

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

        public async Task<List<ZonePriceResponse>> GetZonePricesAsync(SaleType saleType, long referenceId)
        {
            var priceReference = await _dbContext.PriceReferences
                                            .AsNoTracking()
                                            .Where(x => x.ReferenceType == saleType
                                                && x.ReferenceId == referenceId
                                                && x.IsActive)
                                            .SingleOrDefaultAsync();

            if (priceReference == null)
            {
                return new List<ZonePriceResponse>();
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

            var activeZoneIds = zonePrices.Keys.ToList();

            if (!activeZoneIds.Any())
            {
                return new List<ZonePriceResponse>();
            }

            var dbZones = await _dbContext.BaseZones
                                    .AsNoTracking()
                                    .Where(s => activeZoneIds.Contains(s.Id))
                                    .ToListAsync();

            var groupedZonesPrices = dbZones
                .Select(zone =>
                {
                    PriceListItem? matchedPriceItem = null;
                    if (zonePrices.TryGetValue(zone.Id, out var zPli))
                    {
                        matchedPriceItem = zPli;
                    }

                    return new
                    {
                        ZoneName = zone.Name,
                        Price = matchedPriceItem?.FinalPrice
                    };
                })
                .Where(x => x.Price.HasValue)
                .GroupBy(x => x.Price!.Value)
                .Select(group => new ZonePriceResponse
                {
                    Price = group.Key,
                    Objects = group.Select(x => x.ZoneName).Distinct().OrderBy(name => name).ToList(),
                    Currency = "MXN", // TODO: Add currency support for totals
                })
                .OrderByDescending(dto => dto.Price) // Optional: order highest price to lowest
                .ToList();

            return groupedZonesPrices;
        }

        private async Task<SeatAvailabilityResponse> GenerateSeatAvailabilityAsync(
            List<PriceListItem> priceItems,
            ReservationFiltersRequest filters)
        {
            var zonePrices = priceItems
                .Where(p =>
                    p.BaseZoneId != null &&
                    p.BaseSectionId == null &&
                    p.Price.PriceType.IsBasePrice)
                .GroupBy(p => p.BaseZoneId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var sectionPrices = priceItems
                .Where(p =>
                    p.BaseSectionId != null &&
                    p.BaseRowId == null &&
                    p.BaseSeatId == null &&
                    p.Price.PriceType.IsBasePrice)
                .GroupBy(p => p.BaseSectionId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var rowPrices = priceItems
                .Where(p =>
                    p.BaseRowId != null &&
                    p.BaseSeatId == null &&
                    p.Price.PriceType.IsBasePrice)
                .GroupBy(p => p.BaseRowId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var seatPrices = priceItems
                .Where(p =>
                    p.BaseSeatId != null &&
                    p.Price.PriceType.IsBasePrice)
                .GroupBy(p => p.BaseSeatId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            decimal min = filters.MinimumPrice ?? 0;
            decimal? max = filters.MaximumPrice;

            var activeZoneIds = zonePrices.Keys.ToList();

            var zoneQuery = _dbContext.Set<BaseZone>()
                .Where(z => activeZoneIds.Contains(z.Id));

            if (filters.ZoneId.HasValue)
            {
                zoneQuery = zoneQuery.Where(z => z.Id == filters.ZoneId.Value);
            }

            var dbZones = await zoneQuery.ToListAsync();
            var zones = new List<ZoneResponse>();

            foreach (var zone in dbZones)
            {
                if (!zonePrices.TryGetValue(zone.Id, out var zonePli))
                {
                    continue;
                }

                decimal price = zonePli.FinalPrice;

                if (price >= min && (max == null || price <= max.Value))
                {
                    zones.Add(new ZoneResponse
                    {
                        Id = zone.Id,
                        Name = zone.Name,
                        DisplayName = zone.Name,
                        Price = price,
                        PriceListItemId = zonePli.Id,
                        Fees = zonePli.FeeList.Select(f => new FeeResponse
                        {
                            FeeName = f.FeeName,
                            FeeAmount = f.FeeAmount,
                            ChargeCategory = string.IsNullOrEmpty(f.ChargeCategory) ? "Fee" : f.ChargeCategory
                        }).ToList()
                    });
                }
            }

            var activeSectionIds = sectionPrices.Keys.ToList();
            var activeRowIds = rowPrices.Keys.ToList();
            var activeSeatIds = seatPrices.Keys.ToList();

            var seatQuery = _dbContext.BaseSeats
                .Include(bs => bs.BaseRow)
                    .ThenInclude(br => br.BaseSection)
                .AsNoTracking()
                .Where(s =>
                    activeSeatIds.Contains(s.Id) ||
                    activeRowIds.Contains(s.BaseRowId) ||
                    activeSectionIds.Contains(s.BaseRow.BaseSectionId));

            if (filters.ZoneId.HasValue)
            {
                seatQuery = seatQuery.Where(s =>
                    s.BaseRow.BaseSection.BaseZoneId == filters.ZoneId.Value);
            }

            var dbSeats = await seatQuery.ToListAsync();
            var seatOverrides = new List<SeatResponse>();

            foreach (var seat in dbSeats)
            {
                PriceListItem? matchedPriceItem = null;

                if (seatPrices.TryGetValue(seat.Id, out var seatPli))
                {
                    matchedPriceItem = seatPli;
                }
                else if (rowPrices.TryGetValue(seat.BaseRowId, out var rowPli))
                {
                    matchedPriceItem = rowPli;
                }
                else if (sectionPrices.TryGetValue(seat.BaseRow.BaseSectionId, out var sectionPli))
                {
                    matchedPriceItem = sectionPli;
                }

                if (matchedPriceItem == null)
                {
                    continue;
                }

                decimal price = matchedPriceItem.FinalPrice;

                if (price >= min && (max == null || price <= max.Value))
                {
                    seatOverrides.Add(new SeatResponse
                    {
                        Id = seat.Id,
                        ExternalSeatObjectKey =
                            $"{seat.BaseRow.BaseSection.Name}-{seat.BaseRow.RowLabel}-{seat.SeatNumber}",
                        PriceOverride = price,
                        PriceListItemId = matchedPriceItem.Id,
                        Fees = matchedPriceItem.FeeList.Select(f => new FeeResponse
                        {
                            FeeName = f.FeeName,
                            FeeAmount = f.FeeAmount,
                            ChargeCategory = string.IsNullOrEmpty(f.ChargeCategory) ? "Fee" : f.ChargeCategory
                        }).ToList()
                    });
                }
            }

            return new SeatAvailabilityResponse
            {
                Zones = zones,
                SeatOverrides = seatOverrides
            };
        }
    }
}
