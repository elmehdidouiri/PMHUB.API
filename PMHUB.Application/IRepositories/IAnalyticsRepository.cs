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
        Task<CapacityPriceDashboardDto> GetCapacityPriceDashboardAsync(CapacityPriceQueryDto query);
        Task<ProjectCapacityPriceDashboardDto> GetProjectCapacityPriceDashboardAsync(AnalyticsQueryDto query);
        Task<InternCapacityPriceDashboardDto> GetInternCapacityPriceDashboardAsync(InternCapacityPriceQueryDto query);
        Task<MemberTahDashboardDto> GetMemberTahDashboardAsync(MemberTahQueryDto query);
        Task<BookingTargetComparisonDashboardDto> GetBookingTargetComparisonAsync(BookingTargetComparisonQueryDto query);
    }
}
