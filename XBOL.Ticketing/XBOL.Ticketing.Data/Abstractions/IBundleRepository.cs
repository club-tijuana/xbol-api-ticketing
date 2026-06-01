using System.Linq.Expressions;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Abstractions;

public interface IBundleRepository
{
    Task<Core.Model.Bundle?> GetByIdAsync(long id);
    Task<Core.Model.Bundle?> GetByIdWithVenueMapAndSchedulesAsync(long id);
    Task InsertAsync(Core.Model.Bundle entity);
    Task CommitAsync();
    Task UpdateAsync(Core.Model.Bundle entity);
    Task HardDeleteAsync(Core.Model.Bundle entity);
    Task<List<EventCategory>> GetCategoriesByIdsAsync(IReadOnlyCollection<long> categoryIds);
    IQueryable<Core.Model.Bundle> Get(
        Expression<Func<Core.Model.Bundle, bool>>? filter = null,
        Func<IQueryable<Core.Model.Bundle>, IOrderedQueryable<Core.Model.Bundle>>? orderBy = null,
        int? pageSize = null,
        int? currentPage = null,
        params string[] includedProperties);
}
