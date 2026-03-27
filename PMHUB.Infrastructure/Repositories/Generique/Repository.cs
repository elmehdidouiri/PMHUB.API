using Microsoft.EntityFrameworkCore;
using PMHUB.Infrastructure.Persistence;
using System.Linq.Expressions;

namespace PMHUB.Infrastructure.Repositories.Generique
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly PMHubDbContext _context;
        private readonly DbSet<T> _dbSet;

        public Repository(PMHubDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(Guid id) =>
            await _dbSet.FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync() =>
            await _dbSet.AsNoTracking().ToListAsync();

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
            await _dbSet.AsNoTracking().Where(predicate).ToListAsync();

      

        public async Task<decimal> SumAsync(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, decimal>> selector)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(predicate)
                .SumAsync(selector);
        }

        public async Task AddAsync(T entity) =>
            await _dbSet.AddAsync(entity);

        public void Update(T entity) =>
            _dbSet.Update(entity);

        public void Remove(T entity) =>
            _dbSet.Remove(entity);

        public async Task<int> SaveChangesAsync() =>
            await _context.SaveChangesAsync();
    }
}