using PMHUB.Application.DTOs;

namespace PMHUB.Infrastructure.Repositories
{
    public interface IDashboardRepository
    {
        Task<DashboardOverviewDto> GetDashboardOverviewAsync(DashboardQueryDto query, bool isAdminScope, Guid? userId = null);
        Task<DashboardExtendedAdminDto> GetExtendedAdminDashboardAsync(DashboardQueryDto query);
        Task<DashboardAdminBiDto> GetAdminBiDashboardAsync(DashboardQueryDto query);
        Task<DashboardPersonalPerformanceDto> GetPersonalPerformanceDashboardAsync(Guid userId, DashboardQueryDto query);
    }
}
