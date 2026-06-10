using Microsoft.EntityFrameworkCore;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data;
using XBOL.Ticketing.Data.Repositories.Venue;
using XBOL.Ticketing.Services.Base;

namespace XBOL.Ticketing.Services.Venue
{
    public class VenueMapService(
        VenueMapRepository repository,
        SeatsIoService seatsIoService,
        XBOLDbContext dbContext) : BaseService<VenueMapRepository, VenueMap>(repository)
    {

        // TODO: Refactor and consider registration of items that are not seats, like general admission areas, VIP boxes, etc. Currently, the sync focuses on seats and their hierarchy (zones, sections, rows) but may need to be expanded to accommodate other types of venue areas in the future.
        public async Task SyncVenueMapAsync(long venueMapId, Guid userId)
        {
            var venueMap = await Repository.GetByIdAsync(venueMapId);

            if (venueMap is null)
            {
                return;
            }

            var report = await seatsIoService.GetChartReportByLabel(venueMap.ExternalMapKey);

            var seatsIoList = report.Values.SelectMany(x => x)
                .Where(item => item.ObjectType == "seat")
                .ToList();

            using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                var dbZones = await dbContext.BaseZones.IgnoreQueryFilters().Where(c => c.VenueMapId == venueMapId).ToListAsync();
                var dbSections = await dbContext.BaseSections.IgnoreQueryFilters().Include(s => s.BaseZone).Where(s => s.BaseZone.VenueMapId == venueMapId).ToListAsync();
                var dbRows = await dbContext.BaseRows.IgnoreQueryFilters().Include(r => r.BaseSection).ThenInclude(s => s.BaseZone).Where(r => r.BaseSection.BaseZone.VenueMapId == venueMapId).ToListAsync();
                var dbSeats = await dbContext.BaseSeats.IgnoreQueryFilters().Include(s => s.BaseRow).ThenInclude(r => r.BaseSection).ThenInclude(s => s.BaseZone).Where(s => s.BaseRow.BaseSection.BaseZone.VenueMapId == venueMapId).ToListAsync();

                // Categories upsert
                var seatsIoCategories = seatsIoList.GroupBy(s => s.CategoryKey)
                    .Select(g => new { Key = g.Key, Name = g.First().CategoryLabel }).ToList();

                foreach (var cat in seatsIoCategories)
                {
                    var existingZone = dbZones.FirstOrDefault(c => c.ExternalZoneKey == (long.TryParse(cat.Key, out long result) ? result : null));
                    if (existingZone == null)
                    {
                        dbContext.BaseZones.Add(new BaseZone
                        {
                            VenueMapId = venueMapId,
                            ExternalZoneKey = long.TryParse(cat.Key, out long result) ? result : null,
                            Name = cat.Name,
                            CreatedAt = DateTimeOffset.UtcNow,
                            CreatedBy = userId,
                            UpdatedAt = DateTimeOffset.UtcNow,
                            UpdatedBy = userId
                        });
                    }
                    else
                    {
                        existingZone.Name = cat.Name;
                        existingZone.DeletedAt = null;
                        existingZone.UpdatedAt = DateTimeOffset.UtcNow;
                        existingZone.UpdatedBy = userId;
                    }
                }
                await dbContext.SaveChangesAsync();
                dbZones = await dbContext.BaseZones.IgnoreQueryFilters().Where(c => c.VenueMapId == venueMapId).ToListAsync();

                // Sections upsert
                var seatsIoSections = seatsIoList
                    .Where(s => s.Labels.Section != null)
                    .GroupBy(s => new { CategoryKey = s.CategoryKey, SectionName = s.IDs.Section, SectionDisplay = s.Labels.Section })
                    .Select(g => g.Key).ToList();

                foreach (var sec in seatsIoSections)
                {
                    var parentZone = dbZones.First(c => c.ExternalZoneKey == long.Parse(sec.CategoryKey));
                    var existingSec = dbSections.FirstOrDefault(s => s.Name == sec.SectionName && s.BaseZoneId == parentZone.Id);

                    if (existingSec == null)
                    {
                        dbContext.BaseSections.Add(new BaseSection
                        {
                            BaseZoneId = parentZone.Id,
                            Name = sec.SectionName,
                            DisplayName = sec.SectionDisplay,
                            CreatedAt = DateTimeOffset.UtcNow,
                            CreatedBy = userId,
                            UpdatedAt = DateTimeOffset.UtcNow,
                            UpdatedBy = userId
                        });
                    }
                    else
                    {
                        existingSec.DisplayName = sec.SectionDisplay;
                        existingSec.DeletedAt = null;
                        existingSec.UpdatedAt = DateTimeOffset.UtcNow;
                        existingSec.UpdatedBy = userId;
                    }
                }
                await dbContext.SaveChangesAsync();
                dbSections = await dbContext.BaseSections.IgnoreQueryFilters().Include(s => s.BaseZone).Where(s => s.BaseZone.VenueMapId == venueMapId).ToListAsync();

                // Rows upsert
                var seatsIoRows = seatsIoList
                   .Where(s => s.Labels.Section != null && s.Labels.Parent != null)
                   .GroupBy(s => new { CategoryKey = s.CategoryKey, SectionName = s.IDs.Section, RowName = s.IDs.Parent, RowDisplay = s.Labels.Parent.Label })
                   .Select(g => g.Key).ToList();

                foreach (var row in seatsIoRows)
                {
                    var parentSection = dbSections.First(s => s.Name == row.SectionName && s.BaseZone.ExternalZoneKey == long.Parse(row.CategoryKey));
                    var existingRow = dbRows.FirstOrDefault(r => r.RowLabel == row.RowName && r.BaseSectionId == parentSection.Id);

                    if (existingRow == null)
                    {
                        dbContext.BaseRows.Add(new BaseRow
                        {
                            BaseSectionId = parentSection.Id,
                            RowLabel = row.RowName,
                            DisplayName = row.RowDisplay,
                            CreatedAt = DateTimeOffset.UtcNow,
                            CreatedBy = userId,
                            UpdatedAt = DateTimeOffset.UtcNow,
                            UpdatedBy = userId
                        });
                    }
                    else
                    {
                        existingRow.DisplayName = row.RowDisplay;
                        existingRow.DeletedAt = null;
                        existingRow.UpdatedAt = DateTimeOffset.UtcNow;
                        existingRow.UpdatedBy = userId;
                    }
                }
                await dbContext.SaveChangesAsync();
                dbRows = await dbContext.BaseRows.IgnoreQueryFilters().Include(r => r.BaseSection).ThenInclude(s => s.BaseZone).Where(r => r.BaseSection.BaseZone.VenueMapId == venueMapId).ToListAsync();

                // Seats upsert
                var seatsIoIds = seatsIoList.Select(s => s.IDs.Own).ToHashSet();

                // Soft-delete missing seats
                foreach (var seat in dbSeats.Where(s => !seatsIoIds.Contains(s.SeatNumber) && !s.DeletedAt.HasValue))
                {
                    seat.DeletedAt = DateTimeOffset.UtcNow;
                    seat.UpdatedAt = DateTimeOffset.UtcNow;
                    seat.UpdatedBy = userId;
                }

                // Insert or restore seats
                foreach (var item in seatsIoList)
                {
                    var matchedRow = dbRows.First(r =>
                        r.RowLabel == item.IDs.Parent &&
                        r.BaseSection.Name == item.IDs.Section &&
                        r.BaseSection.BaseZone.ExternalZoneKey == long.Parse(item.CategoryKey));

                    var existingSeat = dbSeats.FirstOrDefault(s => s.SeatNumber == item.IDs.Own);

                    if (existingSeat == null)
                    {
                        dbContext.BaseSeats.Add(new BaseSeat
                        {
                            BaseRowId = matchedRow.Id,
                            DisplayName = item.Labels.Own.Label,
                            SeatNumber = item.IDs.Own,
                            CreatedAt = DateTimeOffset.UtcNow,
                            CreatedBy = userId,
                            UpdatedAt = DateTimeOffset.UtcNow,
                            UpdatedBy = userId
                        });
                    }
                    else
                    {
                        existingSeat.BaseRowId = matchedRow.Id;
                        existingSeat.DisplayName = item.Labels.Own.Label;
                        existingSeat.DeletedAt = null;
                        existingSeat.UpdatedBy = userId;
                        existingSeat.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                }
                await dbContext.SaveChangesAsync();


                // Soft-delete missing items in a bottom-up manner to respect hierarchy
                var activeCategories = seatsIoCategories.Select(c => c.Key).ToHashSet();
                var activeSections = seatsIoSections.Select(s => new { s.CategoryKey, s.SectionName }).ToHashSet();
                var activeRows = seatsIoRows.Select(r => new { r.CategoryKey, r.SectionName, r.RowName }).ToHashSet();

                // Clean Rows
                foreach (var row in dbRows.Where(r => !r.DeletedAt.HasValue && !activeRows.Contains(new { CategoryKey = r.BaseSection.BaseZone.ExternalZoneKey.GetValueOrDefault().ToString(), SectionName = r.BaseSection.Name, RowName = r.RowLabel })))
                {
                    row.DeletedAt = DateTimeOffset.UtcNow;
                    row.UpdatedAt = DateTimeOffset.UtcNow;
                    row.UpdatedBy = userId;
                }

                // Clean Sections
                foreach (var sec in dbSections.Where(s => !s.DeletedAt.HasValue && !activeSections.Contains(new { CategoryKey = s.BaseZone.ExternalZoneKey.GetValueOrDefault().ToString(), SectionName = s.Name })))
                {
                    sec.DeletedAt = DateTimeOffset.UtcNow;
                    sec.UpdatedAt = DateTimeOffset.UtcNow;
                    sec.UpdatedBy = userId;
                }

                // Soft delete any duplicate sections that may have been created due to the upsert logic (same section name under the same category)
                var groupedSections = dbSections
                    .Where(s => !s.DeletedAt.HasValue)
                    .GroupBy(s => new
                    {
                        CategoryKey = s.BaseZone.ExternalZoneKey.GetValueOrDefault().ToString(),
                        SectionName = s.Name
                    })
                    .Where(g => g.Count() > 1);

                foreach (var group in groupedSections)
                {
                    var duplicatesToSoftDelete = group.Skip(1);

                    foreach (var sec in duplicatesToSoftDelete)
                    {
                        sec.DeletedAt = DateTimeOffset.UtcNow;
                        sec.UpdatedAt = DateTimeOffset.UtcNow;
                        sec.UpdatedBy = userId;
                    }
                }

                // Clean Categories
                foreach (var cat in dbZones.Where(c => !c.DeletedAt.HasValue && !activeCategories.Contains(c.ExternalZoneKey.GetValueOrDefault().ToString())))
                {
                    cat.DeletedAt = DateTimeOffset.UtcNow;
                    cat.UpdatedAt = DateTimeOffset.UtcNow;
                    cat.UpdatedBy = userId;
                }

                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Deeply nested tracking sync failed.", ex);
            }
        }
    }
}
