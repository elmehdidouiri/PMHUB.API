using PMHUB.Application.DTOs;

namespace PMHUB.Infrastructure.Repositories
{
    public interface IDashboardRepository
    {
        Task<DashboardOverviewDto> GetDashboardOverviewAsync(DashboardQueryDto query, bool isAdminScope, Guid? userId = null);
        Task<DashboardExtendedAdminDto> GetExtendedAdminDashboardAsync(DashboardQueryDto query);
        Task<DashboardAdminBiDto> GetAdminBiDashboardAsync(DashboardQueryDto query);
        Task<DashboardGroupedDistributionDto> GetGroupedDistributionAsync(DashboardQueryDto query);
        Task<DashboardGroupedDistributionCountsDto> GetGroupedDistributionCountsAsync(DashboardQueryDto query);
        Task<DashboardPersonalPerformanceDto> GetPersonalPerformanceDashboardAsync(Guid userId, DashboardQueryDto query);
    }
}
