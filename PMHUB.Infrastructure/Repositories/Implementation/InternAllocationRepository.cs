using Microsoft.EntityFrameworkCore;
using PMHUB.Application.IRepositories;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Persistence;
using PMHUB.Infrastructure.Repositories.Generique;
using System.Linq.Expressions;

namespace PMHUB.Infrastructure.Repositories.Implementation
{
    public class InternAllocationRepository : Repository<InternAllocation>, IInternAllocationRepository
    {
        public InternAllocationRepository(PMHubDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<InternAllocation>> FindWithIncludesAsync(Expression<Func<InternAllocation, bool>> predicate)
        {
            return await _dbSet
                .Include(ia => ia.Intern)
                .Include(ia => ia.Project)
                .Where(predicate)
                .ToListAsync();
        }
    }
}
