using PMHUB.Application.DTOs;

namespace PMHUB.Application.IRepositories
{
    public interface IAnalyticsRepository
    {
        Task<AnalyticsDashboardDto> GetDashboardAsync(AnalyticsQueryDto query);
        Task<AnalyticsSummaryDto> GetSummaryAsync(AnalyticsQueryDto query);
        Task<AnalyticsKpisDto> GetKpisAsync(AnalyticsQueryDto query);
        Task<AnalyticsHoursDto> GetHoursAsync(AnalyticsQueryDto query);
        Task<AnalyticsFiltersDto> GetFiltersAsync();
    }
}
