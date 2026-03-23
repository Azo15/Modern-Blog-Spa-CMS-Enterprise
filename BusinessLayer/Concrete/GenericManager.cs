using BusinessLayer.Abstract;
using System.Linq.Expressions;

namespace BusinessLayer.Concrete
{
    public class GenericManager<T> : IGenericService<T> where T : class
    {
        private readonly IGenericDal<T> _genericDal;

        public GenericManager(IGenericDal<T> genericDal)
        {
            _genericDal = genericDal;
        }

        public async Task<bool> AddAsync(T t)
        {
            await _genericDal.TInsert(t);
            return true;
        }

        public async Task<bool> AddRangeAsync(IEnumerable<T> entities)
        {
            await _genericDal.TInsertRange(entities);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _genericDal.TGetByIDAsync(id);
            if (entity == null)
                return false;
            await _genericDal.TDelete(entity);
            return true;
        }

        public async Task<bool> DeleteRangeAsync(IEnumerable<T> entities)
        {
            await _genericDal.TDeleteRange(entities);
            return true;
        }

        public async Task<bool> UpdateAsync(T t)
        {
            await _genericDal.TUpdate(t);
            return true;
        }

        public async Task<bool> UpdateRangeAsync(IEnumerable<T> entities)
        {
            await _genericDal.TUpdateRange(entities);
            return true;
        }

        public async Task<T> GetByIDAsync(int id, params Expression<Func<T, object>>[] includes)
        {
            return await _genericDal.TGetByIDAsync(id, includes);
        }

        public async Task<List<T>> GetListAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? take = null,
            bool tracking = true,
            params Expression<Func<T, object>>[] includes)
        {
            return await _genericDal.TGetListAsync(filter, orderBy, take, tracking, includes);
        }

        public async Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool tracking = true, params Expression<Func<T, object>>[] includes)
        {
            return await _genericDal.TFirstOrDefaultAsync(predicate, orderBy, tracking, includes);
        }

        public IQueryable<T> GetQueryable()
        {
            return _genericDal.TGetQueryable();
        }

        public async Task<bool> AnyAsync(Expression<Func<T, bool>>? filter = null)
        {
            return await _genericDal.TAnyAsync(filter);
        }
    }
}
