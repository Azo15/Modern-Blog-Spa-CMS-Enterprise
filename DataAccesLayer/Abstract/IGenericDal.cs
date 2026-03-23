using System.Linq.Expressions;

public interface IGenericDal<T> where T : class
{
    Task<List<T>> TGetListAsync(
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        int? take = null,
        bool tracking = true,
        params Expression<Func<T, object>>[] includes);

    Task<T> TGetByIDAsync(
        int id,
        params Expression<Func<T, object>>[] includes);

    Task<T> TFirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool tracking = true,
        params Expression<Func<T, object>>[] includes);

    Task<bool> TAnyAsync(Expression<Func<T, bool>>? filter = null);

    IQueryable<T> TGetQueryable();
    Task TInsert(T entity);
    Task TInsertRange(IEnumerable<T> entities);
    Task TUpdate(T entity);
    Task TUpdateRange(IEnumerable<T> entities);
    Task TDelete(T entity);
    Task TDeleteRange(IEnumerable<T> entities);
}
