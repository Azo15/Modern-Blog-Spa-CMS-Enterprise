using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DataAccesLayer.Repository
{
    public class GenericRepository<T> : IGenericDal<T> where T : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<List<T>> TGetListAsync(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            int? take = null,
            bool tracking = true,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = tracking ? _dbSet : _dbSet.AsNoTracking();

            // Filtre
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // Include işlemleri
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            // Sıralama
            if (orderBy != null)
            {
                query = orderBy(query);
            }

            // Take (kaç adet veri alınacak)
            if (take.HasValue)
            {
                query = query.Take(take.Value);
            }

            return await query.ToListAsync();
        }


        // ID'ye göre getir + Include destekli
        public async Task<T> TGetByIDAsync(
            int id,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }

            return await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
        }

        public async Task TInsert(T entity)
        {

            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task TInsertRange(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        public async Task TUpdate(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task TUpdateRange(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task TDelete(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task TDeleteRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task<T> TFirstOrDefaultAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, bool tracking = true, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            if (!tracking)
            {
                query = query.AsNoTracking();
            }

            if (includes != null)
            {
                foreach (var include in includes)
                {
                    query = query.Include(include);
                }
            }
            // Sıralama
            if (orderBy != null)
            {
                query = orderBy(query);
            }
            return await query?
                .Where(predicate)
                .FirstOrDefaultAsync();
        }

        public async Task<T> TLastOrDefaultAsync(
            Expression<Func<T, bool>>? predicate = null,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            bool tracking = true,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;

            if (!tracking)
                query = query.AsNoTracking();

            if (includes != null)
                foreach (var include in includes)
                    query = query.Include(include);

            if (predicate != null)
                query = query.Where(predicate);

            if (orderBy != null)
                query = orderBy(query);

            return await query.LastOrDefaultAsync();
        }

        public async Task<bool> TAnyAsync(Expression<Func<T, bool>>? filter = null)
        {
            if (filter == null)
            {
                return await _dbSet.AnyAsync();
            }

            return await _dbSet.AnyAsync(filter);
        }

        public IQueryable<T> TGetQueryable()
        {
            return _dbSet.AsQueryable();
        }
    }
}
