using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using System.Linq.Expressions;

namespace PMHUB.Infrastructure.Repositories.Implementation
{
    public interface IInternAllocationRepository : IRepository<InternAllocation>
    {
        Task<IEnumerable<InternAllocation>> FindWithIncludesAsync(Expression<Func<InternAllocation, bool>> predicate);
    }
}
