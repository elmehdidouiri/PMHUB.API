using PMHUB.Application.DTOs;
using PMHUB.Domain.Enums;

namespace PMHUB.Application.Services
{
    public interface IProjectService
    {
        Task<ProjectDto> CreateAsync(CreateFullProjectDto dto);
        Task<IEnumerable<ProjectSummaryDto>> GetAllAsync();
        Task<ProjectDto?> GetByIdAsync(Guid id);
        Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto dto);
        Task DeleteAsync(Guid id);

         Task<IEnumerable<ProjectSummaryDto>> GetByDepartmentAsync(Guid departmentId);
        Task<IEnumerable<ProjectSummaryDto>> GetByBusinessUnitAsync(Guid businessUnitId);
        Task<IEnumerable<ProjectSummaryDto>> GetByPlantAsync(Guid plantId);
        Task<IEnumerable<ProjectSummaryDto>> GetByStatusAsync(ProjectStatus status);
        Task<IEnumerable<ProjectSummaryDto>> GetByPhaseAsync(ProjectPhase phase);

         Task<ProjectDto> AddSubProjectAsync(Guid parentId, CreateSubProjectDto dto);

         Task AddMemberAsync(Guid projectId, Guid userId);
        Task RemoveMemberAsync(Guid projectId, Guid userId);
    }
}