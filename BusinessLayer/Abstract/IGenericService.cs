using System.Linq.Expressions;

namespace BusinessLayer.Abstract
{
    public interface IGenericService<T> where T : class
    {
        Task<bool> AddAsync(T t);
        Task<bool> AddRangeAsync(IEnumerable<T> entities);
        Task<bool> DeleteAsync(int id);
        Task<bool> DeleteRangeAsync(IEnumerable<T> entities);
        Task<bool> UpdateAsync(T t);
        Task<bool> UpdateRangeAsync(IEnumerable<T> entities);
        IQueryable<T> GetQueryable();

        Task<bool> AnyAsync(Expression<Func<T, bool>>? filter = null);

        Task<List<T>> GetListAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? take = null,
            bool tracking = true,
            params Expression<Func<T, object>>[] includes);

        Task<T> GetByIDAsync(
            int id,
            params Expression<Func<T, object>>[] includes);

        Task<T> FirstOrDefaultAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            bool tracking = true,
            params Expression<Func<T, object>>[] includes);
    }
}
