using System.Linq.Expressions;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Abstractions;

public interface IBundlePassRepository
{
    Task<BundlePass?> GetByIdAsync(long id);
    Task InsertAsync(BundlePass entity);
    Task CommitAsync();
    Task UpdateAsync(BundlePass entity);
    Task HardDeleteAsync(BundlePass entity);
    IQueryable<BundlePass> Get(
        Expression<Func<BundlePass, bool>>? filter = null,
        Func<IQueryable<BundlePass>, IOrderedQueryable<BundlePass>>? orderBy = null,
        int? pageSize = null,
        int? currentPage = null,
        params string[] includedProperties);
}
