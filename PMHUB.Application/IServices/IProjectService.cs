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

        Task<DashboardStatsDto> GetUserDashboardStatsAsync(Guid userId);
        Task<DashboardStatsDto> GetAdminDashboardStatsAsync();


         Task AddMemberAsync(Guid projectId, Guid userId, Guid roleId);
        Task<IEnumerable<ProjectMemberDto>> GetMembersAsync(Guid projectId);

        Task<IEnumerable<ProjectSummaryDto>> GetByDepartmentAsync(Guid departmentId);
        Task<IEnumerable<ProjectSummaryDto>> GetByBusinessUnitAsync(Guid businessUnitId);
        Task<IEnumerable<ProjectSummaryDto>> GetByPlantAsync(Guid plantId);
        Task<IEnumerable<ProjectSummaryDto>> GetByStatusAsync(ProjectStatus status);
        Task<IEnumerable<ProjectSummaryDto>> GetByPhaseAsync(ProjectPhase phase);
        Task<ProjectDto> AddSubProjectAsync(Guid parentId, CreateSubProjectDto dto);
        Task RemoveMemberAsync(Guid projectId, Guid userId);
        Task<ProjectDto> PatchAsync(Guid id, PatchProjectDto dto);
        Task<PaginatedResultDto<ProjectSummaryDto>> GetPagedAsync(ProjectSearchDto query);
        Task<string> ExportProjectsAsync(ProjectSearchDto query);
        Task<IEnumerable<ProjectExportDto>> GetForExportAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<DeliverableBreakdownDto>> GetDeliverablesAsync(Guid projectId);
        Task<DeliverableBreakdownDto> AddDeliverableAsync(Guid projectId, CreateDeliverableBreakdownDto dto);
        Task<DeliverableBreakdownDto> UpdateDeliverableAsync(Guid projectId, Guid deliverableId, UpdateDeliverableBreakdownDto dto);
        Task DeleteDeliverableAsync(Guid projectId, Guid deliverableId);
        Task<DeliverableTaskDto> AddDeliverableTaskAsync(Guid projectId, Guid deliverableId, CreateDeliverableTaskDto dto);
        Task<DeliverableTaskDto> UpdateDeliverableTaskAsync(Guid projectId, Guid deliverableId, Guid taskId, UpdateDeliverableTaskDto dto);
        Task DeleteDeliverableTaskAsync(Guid projectId, Guid deliverableId, Guid taskId);
        Task<IEnumerable<ProjectTimelineEntryDto>> GetTimelineAsync(Guid projectId);
        Task<ProjectTimelineEntryDto> AddTimelineEntryAsync(Guid projectId, CreateProjectTimelineEntryDto dto);
        Task<ProjectTimelineEntryDto> UpdateTimelineEntryAsync(Guid projectId, Guid entryId, UpdateProjectTimelineEntryDto dto);
        Task DeleteTimelineEntryAsync(Guid projectId, Guid entryId);
        Task<IEnumerable<ProjectRoadblockDto>> GetRoadblocksAsync(Guid projectId);
        Task<IEnumerable<ProjectRoadblockDto>> GetDelayedRoadblocksAsync(Guid projectId);
        Task<ProjectRoadblockDto> AddRoadblockAsync(Guid projectId, CreateProjectRoadblockDto dto);
        Task<ProjectRoadblockDto> UpdateRoadblockAsync(Guid projectId, Guid roadblockId, UpdateProjectRoadblockDto dto);
        Task DeleteRoadblockAsync(Guid projectId, Guid roadblockId);
        Task<IEnumerable<ProjectInternAllocationDto>> GetInternAllocationsAsync(Guid projectId);
        Task<ProjectInternAllocationDto> AddInternAllocationAsync(Guid projectId, CreateProjectInternAllocationDto dto);
        Task<ProjectInternAllocationDto> UpdateInternAllocationAsync(Guid projectId, Guid allocationId, UpdateProjectInternAllocationDto dto);
        Task DeleteInternAllocationAsync(Guid projectId, Guid allocationId);
        Task<IEnumerable<InternHourEntryDto>> GetInternHourEntriesAsync(Guid projectId, Guid allocationId);
        Task<InternHourEntryDto> AddInternHourEntryAsync(Guid projectId, Guid allocationId, CreateInternHourEntryDto dto, Guid bookedByUserId);
        Task<InternHourEntryDto> UpdateInternHourEntryAsync(Guid projectId, Guid allocationId, Guid hourEntryId, UpdateInternHourEntryDto dto, Guid bookedByUserId);
        Task DeleteInternHourEntryAsync(Guid projectId, Guid allocationId, Guid hourEntryId, Guid bookedByUserId);
    }
}
