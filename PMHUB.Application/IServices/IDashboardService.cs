using PMHUB.Application.DTOs;

namespace PMHUB.Application.IServices
{
    public interface IDashboardService
    {
        Task<DashboardOverviewDto> GetAdminDashboardAsync(DashboardQueryDto query, Guid? requesterUserId = null);
        Task<DashboardExtendedAdminDto> GetExtendedAdminDashboardAsync(DashboardQueryDto query, Guid? requesterUserId = null);
        Task<DashboardAdminBiDto> GetAdminBiDashboardAsync(DashboardQueryDto query, Guid? requesterUserId = null);
        Task<DashboardExtendedAdminDto> GetAllProjectsExtendedDashboardAsync(DashboardQueryDto query, Guid? requesterUserId = null);
        Task<DashboardAdminBiDto> GetAllProjectsBiDashboardAsync(DashboardQueryDto query, Guid? requesterUserId = null);
        Task<DashboardExtendedAdminDto> GetMyExtendedDashboardAsync(Guid userId, DashboardQueryDto query);
        Task<DashboardAdminBiDto> GetMyBiDashboardAsync(Guid userId, DashboardQueryDto query);
        Task<DashboardGroupedDistributionDto> GetGroupedDistributionAsync(DashboardQueryDto query, Guid? requesterUserId = null);
        Task<DashboardGroupedDistributionCountsDto> GetGroupedDistributionCountsAsync(DashboardQueryDto query, Guid? requesterUserId = null);
        Task<DashboardOverviewDto> GetMyDashboardAsync(Guid userId, DashboardQueryDto query);
        Task<DashboardPersonalPerformanceDto> GetMyPerformanceDashboardAsync(Guid userId, DashboardQueryDto query);
        Task<DashboardPersonalPerformanceDto> GetUserPerformanceDashboardAsync(Guid userId, DashboardQueryDto query);
    }
}
