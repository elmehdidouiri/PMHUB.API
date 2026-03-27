using Microsoft.EntityFrameworkCore;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Persistence;
using PMHUB.Infrastructure.Repositories.Generique;
using PMHUB.Infrastructure.Repositories.Implementation;
using System.Linq.Expressions;

namespace PMHUB.Infrastructure.Repositories
{
    public class HourEntryRepository : Repository<HourEntry>, IHourEntryRepository
    {
        public HourEntryRepository(PMHubDbContext context) : base(context) { }
 
        public async Task<IEnumerable<HourEntry>> FindWithIncludesAsync(
            Expression<Func<HourEntry, bool>> predicate)
        {
            return await _context.HourEntries
                .AsNoTracking()
                .Include(h => h.Project)
                .Include(h => h.User)
                .Where(predicate)
                .ToListAsync();
        }

        public async Task<HourEntry?> GetByIdWithIncludesAsync(Guid id)
        {
            return await _context.HourEntries
                .AsNoTracking()
                .Include(h => h.Project)
                .Include(h => h.User)
                .FirstOrDefaultAsync(h => h.Id == id);
        }
 
        public async Task<IEnumerable<HourEntry>> GetByYearAsync(int year)
        {
            return await _context.HourEntries
                .AsNoTracking()
                .Include(h => h.Project)
                .Include(h => h.User)
                .Where(h => h.Date.Year == year)
                .ToListAsync();
        }

        public async Task<IEnumerable<HourEntry>> GetByMonthAsync(int year, int month)
        {
            return await _context.HourEntries
                .AsNoTracking()
                .Include(h => h.Project)
                .Include(h => h.User)
                .Where(h => h.Date.Year == year && h.Date.Month == month)
                .ToListAsync();
        }

        public async Task<IEnumerable<HourEntry>> GetByUserAsync(Guid userId, int? year = null, int? month = null)
        {
            var query = _context.HourEntries
                .AsNoTracking()
                .Include(h => h.Project)
                .Include(h => h.User)
                .Where(h => h.UserId == userId)
                .AsQueryable();

            if (year.HasValue)
                query = query.Where(h => h.Date.Year == year.Value);

            if (month.HasValue)
                query = query.Where(h => h.Date.Month == month.Value);

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<HourEntry>> GetByProjectAsync(Guid projectId, int? year = null, int? month = null)
        {
            var query = _context.HourEntries
                .AsNoTracking()
                .Include(h => h.Project)
                .Include(h => h.User)
                .Where(h => h.ProjectId == projectId)
                .AsQueryable();

            if (year.HasValue)
                query = query.Where(h => h.Date.Year == year.Value);

            if (month.HasValue)
                query = query.Where(h => h.Date.Month == month.Value);

            return await query.ToListAsync();
        }
    }
}