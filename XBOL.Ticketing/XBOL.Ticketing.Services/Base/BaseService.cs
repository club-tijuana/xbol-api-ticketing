using System.Linq.Expressions;
using XBOL.Ticketing.Core.Model;
using XBOL.Ticketing.Data.Repositories.Base;

namespace XBOL.Ticketing.Services.Base
{
    public class BaseService<R, M>(R repository)
        where M : BaseModel
        where R : BaseRepository<M>
    {
        protected R Repository { get; set; } = repository;

        public void Commit()
        {
            Repository.Commit();
        }

        public async Task CommitAsync()
        {
            await Repository.CommitAsync();
        }

        public virtual void Create(M entity)
        {
            Repository.Insert(entity);
            Repository.Commit();
        }

        public virtual async Task CreateAsync(M entity)
        {
            await Repository.InsertAsync(entity);
            await Repository.CommitAsync();
        }

        public List<M> GetList(
            Expression<Func<M, bool>>? filter = null,
            Func<IQueryable<M>, IOrderedQueryable<M>>? orderBy = null,
            int? pageSize = null,
            int? currentPage = null
        )
        {
            return Repository.Get(filter, orderBy, pageSize, currentPage).ToList();
        }

        public List<M> GetAll()
        {
            return Repository.Get().ToList();
        }

        public M? GetById(long id)
        {
            return Repository.GetByIdSync(id);
        }

        public async Task<M?> GetByIdAsync(long id)
        {
            return await Repository.GetByIdAsync(id);
        }

        public virtual void HardDelete(M entity)
        {
            Repository.HardDelete(entity);
        }

        public virtual async Task HardDeleteAsync(M entity)
        {
            await Repository.HardDeleteAsync(entity);
        }

        public void Update(M entity)
        {
            Repository.UpdateSync(entity);
        }

        public virtual async Task UpdateAsync(M entity)
        {
            await Repository.UpdateAsync(entity);
        }
    }
}
