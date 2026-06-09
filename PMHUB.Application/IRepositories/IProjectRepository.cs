using PMHUB.Application.DTOs;
using PMHUB.Domain.Entities;
using System.Linq.Expressions;



namespace PMHUB.Infrastructure.Repositories
{
    public interface IProjectRepository : IRepository<Project>
    {
        Task<Project?> GetByNameAsync(string name);
        Task<Project?> GetFullProjectByIdAsync(Guid id);
        Task<Project?> GetByIdWithIncludesAsync(Guid id);
        Task<Project?> GetByIdForUpdateAsync(Guid id);
        Task<DeliverableTask?> GetDeliverableTaskWithIncludesAsync(Guid taskId);
        Task<IEnumerable<Project>> GetAllWithIncludesAsync();
        Task<IEnumerable<Project>> GetAllSummariesAsync();
        Task<IEnumerable<Project>> FindSummariesAsync(
        Expression<Func<Project, bool>> predicate);
        Task<IEnumerable<Project>> FindWithIncludesAsync(
        Expression<Func<Project, bool>> predicate);
        Task<(IEnumerable<Project> Items, int TotalCount)> GetPagedAsync(ProjectSearchDto query);
        Task<IEnumerable<Project>> GetFilteredAsync(ProjectSearchDto query);
        Task<IEnumerable<Project>> GetFilteredForExportAsync(ProjectSearchDto query);
    }

}
