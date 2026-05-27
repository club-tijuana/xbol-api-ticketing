using System.Linq.Expressions;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Abstractions;

public interface IBundlePassEventTicketRepository
{
    Task<BundlePassEventTicket?> GetByIdAsync(long id);
    Task InsertAsync(BundlePassEventTicket entity);
    Task CommitAsync();
    void HardDelete(BundlePassEventTicket entity);
    IQueryable<BundlePassEventTicket> Get(
        Expression<Func<BundlePassEventTicket, bool>>? filter = null,
        Func<IQueryable<BundlePassEventTicket>, IOrderedQueryable<BundlePassEventTicket>>? orderBy = null,
        int? pageSize = null,
        int? currentPage = null,
        params string[] includedProperties);
}
