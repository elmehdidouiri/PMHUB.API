using PMHUB.Application.DTOs;

namespace PMHUB.Application.IServices
{
    public interface IDashboardService
    {
        Task<DashboardOverviewDto> GetAdminDashboardAsync(DashboardQueryDto query, Guid? requesterUserId = null);
        Task<DashboardOverviewDto> GetMyDashboardAsync(Guid userId, DashboardQueryDto query);
    }
}
