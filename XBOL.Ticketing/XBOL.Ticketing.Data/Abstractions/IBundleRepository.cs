using System.Linq.Expressions;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Abstractions;

public interface IBundleRepository
{
    Task<Core.Model.Bundle?> GetByIdAsync(long id);
    Task InsertAsync(Core.Model.Bundle entity);
    Task CommitAsync();
    Task UpdateAsync(Core.Model.Bundle entity);
    Task HardDeleteAsync(Core.Model.Bundle entity);
    IQueryable<Core.Model.Bundle> Get(
        Expression<Func<Core.Model.Bundle, bool>>? filter = null,
        Func<IQueryable<Core.Model.Bundle>, IOrderedQueryable<Core.Model.Bundle>>? orderBy = null,
        int? pageSize = null,
        int? currentPage = null,
        params string[] includedProperties);
}
