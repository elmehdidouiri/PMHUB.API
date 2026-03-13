using PMHUB.Application.DTOs;
using PMHUB.Domain.Enums;

namespace PMHUB.Application.IServices
{
    public interface IProjectService
    {
        Task<ProjectDto> CreateAsync(CreateFullProjectDto dto);
        Task<IEnumerable<ProjectSummaryDto>> GetAllAsync();
        Task<ProjectDto?> GetByIdAsync(Guid id);
        Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto dto);
        Task DeleteAsync(Guid id);

         Task AddMemberAsync(Guid projectId, Guid userId, Guid roleId);

        Task<IEnumerable<ProjectSummaryDto>> GetByDepartmentAsync(Guid departmentId);
        Task<IEnumerable<ProjectSummaryDto>> GetByBusinessUnitAsync(Guid businessUnitId);
        Task<IEnumerable<ProjectSummaryDto>> GetByPlantAsync(Guid plantId);
        Task<IEnumerable<ProjectSummaryDto>> GetByStatusAsync(ProjectStatus status);
        Task<IEnumerable<ProjectSummaryDto>> GetByPhaseAsync(ProjectPhase phase);
        Task<ProjectDto> AddSubProjectAsync(Guid parentId, CreateSubProjectDto dto);
        Task RemoveMemberAsync(Guid projectId, Guid userId);
        Task<ProjectDto> PatchAsync(Guid id, PatchProjectDto dto);
        Task<PaginatedResultDto<ProjectSummaryDto>> GetPagedAsync(PaginationQueryDto query);
    }
}