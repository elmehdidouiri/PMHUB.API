using PMHUB.Application.DTOs;

namespace PMHUB.Infrastructure.Repositories
{
    public interface IDashboardRepository
    {
        Task<DashboardOverviewDto> GetDashboardOverviewAsync(DashboardQueryDto query, bool isAdminScope, Guid? userId = null);
    }
}
