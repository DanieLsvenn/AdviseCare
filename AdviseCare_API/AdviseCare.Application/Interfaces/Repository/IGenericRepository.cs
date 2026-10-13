using System.Linq.Expressions;

namespace AdviseCare.Application.Interfaces.Repository
{
    /// <summary>
    /// Generic repository over a single EF Core entity type <typeparamref name="T"/>.
    ///
    /// Two ways to persist changes:
    ///   1. "Immediate" methods (CreateAsync/UpdateAsync/RemoveAsync) commit right away -
    ///      use these for simple, single-entity operations.
    ///   2. "Prepare" methods (PrepareCreate/PrepareUpdate/PrepareRemove) stage a change
    ///      without saving - call SaveAsync() once after staging everything, so a
    ///      business operation that touches more than one entity (e.g. across several
    ///      repositories) commits as a single transaction instead of partially applying.
    ///
    /// GetById*/GetByIdInclude* assume a single-column primary key. For composite-key
    /// entities, use SearchAsync/SearchAsyncInclude with an explicit predicate instead.
    /// </summary>
    public interface IGenericRepository<T> where T : class
    {
        // --- Reads ---
        Task<T?> GetByIdAsync<TKey>(TKey id) where TKey : notnull;
        Task<T?> GetByIdAsyncInclude<TKey>(TKey id, params Expression<Func<T, object>>[] includes) where TKey : notnull;

        Task<List<T>> GetAllAsync();
        Task<List<T>> GetAllAsyncInclude(params Expression<Func<T, object>>[] includes);
        Task<List<T>> GetAllAsyncOrderBy(Expression<Func<T, object>> orderBy, bool ascending = true);
        Task<List<T>> GetAllAsyncIncludeOrderBy(
            Expression<Func<T, object>> orderBy,
            bool ascending = true,
            params Expression<Func<T, object>>[] includes);

        Task<List<T>> SearchAsync(Expression<Func<T, bool>> predicate);
        Task<List<T>> SearchAsyncInclude(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        Task<List<T>> SearchAsyncOrderBy(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, object>> orderBy,
            bool ascending = true);
        Task<List<T>> SearchAsyncIncludeOrderBy(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, object>> orderBy,
            bool ascending = true,
            params Expression<Func<T, object>>[] includes);

        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

        /// <summary>
        /// Escape hatch for query composition the rest of this interface can't express.
        /// Use sparingly - prefer the typed methods above wherever they're sufficient.
        /// </summary>
        IQueryable<T> GetQueryable();

        // --- Immediate writes (commit right away) ---
        Task<T> CreateAsync(T entity);
        Task<T> UpdateAsync(T entity);
        Task<bool> RemoveAsync(T entity);

        // --- Staged writes (call SaveAsync once after staging all of them) ---
        void PrepareCreate(T entity);
        void PrepareUpdate(T entity);
        void PrepareRemove(T entity);
        Task<int> SaveAsync();
    }
}
