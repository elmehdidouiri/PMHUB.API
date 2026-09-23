using PMHUB.Application.DTOs;

namespace PMHUB.Application.IServices
{
    public interface IAnalyticsService
    {
        Task<AnalyticsDashboardDto> GetDashboardAsync(AnalyticsQueryDto query);
        Task<AnalyticsFiltersDto> GetFiltersAsync();
        Task<AnalyticsSummaryDto> GetSummaryAsync(AnalyticsQueryDto query);
        Task<AnalyticsKpisDto> GetKpisAsync(AnalyticsQueryDto query);
        Task<AnalyticsHoursDto> GetHoursAsync(AnalyticsQueryDto query);
        Task<CapacityPriceDashboardDto> GetCapacityPriceDashboardAsync(CapacityPriceQueryDto query);
        Task<ProjectCapacityPriceDashboardDto> GetProjectCapacityPriceDashboardAsync(AnalyticsQueryDto query);
        Task<InternCapacityPriceDashboardDto> GetInternCapacityPriceDashboardAsync(InternCapacityPriceQueryDto query);
        Task<MemberTahDashboardDto> GetMemberTahDashboardAsync(MemberTahQueryDto query);
        Task<string> ExportMemberTahDashboardAsync(MemberTahQueryDto query);
        Task<BookingTargetComparisonDashboardDto> GetBookingTargetComparisonAsync(BookingTargetComparisonQueryDto query);
    }
}
