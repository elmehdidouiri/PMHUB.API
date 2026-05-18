using PMHUB.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PMHUB.Infrastructure.Repositories.Implementation
{
    public interface IHourEntryRepository : IRepository<HourEntry>
    {
         Task<IEnumerable<HourEntry>> FindWithIncludesAsync(Expression<Func<HourEntry, bool>> predicate);
        Task<HourEntry?> GetByIdWithIncludesAsync(Guid id);

         Task<IEnumerable<HourEntry>> GetByYearAsync(int year);
        Task<IEnumerable<HourEntry>> GetByMonthAsync(int year, int month);
        Task<IEnumerable<HourEntry>> GetByUserAsync(Guid userId, int? year = null, int? month = null);
        Task<IEnumerable<HourEntry>> GetByProjectAsync(Guid projectId, int? year = null, int? month = null);
        Task<HashSet<Guid>> GetUserIdsWithEntriesSinceAsync(DateTime since);
        Task<DateTime?> GetLastBookingDateAsync(Guid userId);
        Task<decimal> SumUserHoursAsync(Guid userId, DateTime from, DateTime to);
    }
}
