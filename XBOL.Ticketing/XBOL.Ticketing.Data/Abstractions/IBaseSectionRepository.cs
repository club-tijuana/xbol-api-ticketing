using System.Linq.Expressions;
using XBOL.Ticketing.Core.Model;

namespace XBOL.Ticketing.Data.Abstractions;

public interface IBaseSectionRepository
{
    IQueryable<BaseSection> Get(
        Expression<Func<BaseSection, bool>>? filter = null,
        Func<IQueryable<BaseSection>, IOrderedQueryable<BaseSection>>? orderBy = null,
        int? pageSize = null,
        int? currentPage = null,
        params string[] includedProperties);
}
