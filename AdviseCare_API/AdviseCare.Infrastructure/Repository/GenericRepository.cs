using AdviseCare.Application.Interfaces.Repository;
using AdviseCare.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AdviseCare.Infrastructure.Repository
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly AdviseCareContext _context;

        public GenericRepository(AdviseCareContext context)
        {
            _context = context;
        }

        // --- Reads ---

        public async Task<T?> GetByIdAsync<TKey>(TKey id) where TKey : notnull
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public async Task<T?> GetByIdAsyncInclude<TKey>(TKey id, params Expression<Func<T, object>>[] includes) where TKey : notnull
        {
            var keyName = GetPrimaryKeyName();
            IQueryable<T> query = _context.Set<T>();
            foreach (var include in includes)
                query = query.Include(include);

            return await query.FirstOrDefaultAsync(e => EF.Property<TKey>(e, keyName).Equals(id));
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }

        public async Task<List<T>> GetAllAsyncInclude(params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var include in includes)
                query = query.Include(include);
            return await query.ToListAsync();
        }

        public async Task<List<T>> GetAllAsyncOrderBy(Expression<Func<T, object>> orderBy, bool ascending = true)
        {
            IQueryable<T> query = _context.Set<T>();
            query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
            return await query.ToListAsync();
        }

        public async Task<List<T>> GetAllAsyncIncludeOrderBy(
            Expression<Func<T, object>> orderBy,
            bool ascending = true,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var include in includes)
                query = query.Include(include);
            query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
            return await query.ToListAsync();
        }

        public async Task<List<T>> SearchAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().Where(predicate).ToListAsync();
        }

        public async Task<List<T>> SearchAsyncInclude(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var include in includes)
                query = query.Include(include);
            return await query.Where(predicate).ToListAsync();
        }

        public async Task<List<T>> SearchAsyncOrderBy(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, object>> orderBy,
            bool ascending = true)
        {
            IQueryable<T> query = _context.Set<T>().Where(predicate);
            query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
            return await query.ToListAsync();
        }

        public async Task<List<T>> SearchAsyncIncludeOrderBy(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, object>> orderBy,
            bool ascending = true,
            params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var include in includes)
                query = query.Include(include);
            query = query.Where(predicate);
            query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);
            return await query.ToListAsync();
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().FirstOrDefaultAsync(predicate);
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _context.Set<T>();
            foreach (var include in includes)
                query = query.Include(include);
            return await query.FirstOrDefaultAsync(predicate);
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().Where(predicate).AnyAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            IQueryable<T> query = _context.Set<T>();
            if (predicate != null)
                query = query.Where(predicate);
            return await query.CountAsync();
        }

        public IQueryable<T> GetQueryable()
        {
            return _context.Set<T>();
        }

        // --- Immediate writes ---

        public async Task<T> CreateAsync(T entity)
        {
            _context.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<T> UpdateAsync(T entity)
        {
            _context.ChangeTracker.Clear();
            var tracker = _context.Attach(entity);
            tracker.State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> RemoveAsync(T entity)
        {
            _context.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Staged writes ---

        public void PrepareCreate(T entity) => _context.Add(entity);

        public void PrepareUpdate(T entity)
        {
            var tracker = _context.Attach(entity);
            tracker.State = EntityState.Modified;
        }

        public void PrepareRemove(T entity) => _context.Remove(entity);

        public async Task<int> SaveAsync() => await _context.SaveChangesAsync();

        // --- Helpers ---

        /// <summary>
        /// Marks <paramref name="entity"/> as modified without disturbing tracking for
        /// any other entity on this DbContext. If a *different* instance with the same
        /// key is already tracked (e.g. it was loaded earlier in the same unit of work),
        /// copies the new values onto that tracked instance instead of attaching a
        /// duplicate, which would otherwise throw.
        /// </summary>
        private void AttachOrMergeModified(T entity)
        {
            var existing = _context.ChangeTracker.Entries<T>()
                .FirstOrDefault(e => !ReferenceEquals(e.Entity, entity));

            if (existing != null)
            {
                existing.CurrentValues.SetValues(entity);
            }
            else
            {
                _context.Entry(entity).State = EntityState.Modified;
            }
        }

        private string GetPrimaryKeyName()
        {
            var key = _context.Model.FindEntityType(typeof(T))?.FindPrimaryKey()
                ?? throw new InvalidOperationException($"No primary key found for entity type {typeof(T).Name}.");

            if (key.Properties.Count != 1)
                throw new InvalidOperationException(
                    $"GetByIdAsyncInclude only supports single-column primary keys; " +
                    $"{typeof(T).Name} has a composite key. Use SearchAsyncInclude instead.");

            return key.Properties[0].Name;
        }
    }
}
