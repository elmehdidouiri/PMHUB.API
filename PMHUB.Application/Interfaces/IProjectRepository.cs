 using PMHUB.Domain.Entities;
 

namespace PMHUB.Infrastructure.Repositories
{
    public interface IProjectRepository : IRepository<Project>
    {
        Task<Project?> GetByNameAsync(string name);
        Task<Project?> GetFullProjectByIdAsync(Guid id);
        Task<Project?> GetByIdWithIncludesAsync(Guid id);
        Task<IEnumerable<Project>> GetAllWithIncludesAsync();
    }

}