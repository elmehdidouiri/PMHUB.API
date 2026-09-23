using PMHUB.Application.DTOs;
using PMHUB.Application.IRepositories;
using PMHUB.Application.IServices;

namespace PMHUB.Application.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsRepository _analyticsRepository;
        private readonly IExcelExportService _excelExportService;

        public AnalyticsService(IAnalyticsRepository analyticsRepository, IExcelExportService excelExportService)
        {
            _analyticsRepository = analyticsRepository;
            _excelExportService = excelExportService;
        }

        public Task<AnalyticsDashboardDto> GetDashboardAsync(AnalyticsQueryDto query)
        {
            return _analyticsRepository.GetDashboardAsync(query);
        }

        public Task<AnalyticsFiltersDto> GetFiltersAsync()
        {
            return _analyticsRepository.GetFiltersAsync();
        }

        public Task<AnalyticsSummaryDto> GetSummaryAsync(AnalyticsQueryDto query)
        {
            return _analyticsRepository.GetSummaryAsync(query);
        }

        public Task<AnalyticsKpisDto> GetKpisAsync(AnalyticsQueryDto query)
        {
            return _analyticsRepository.GetKpisAsync(query);
        }

        public Task<AnalyticsHoursDto> GetHoursAsync(AnalyticsQueryDto query)
        {
            return _analyticsRepository.GetHoursAsync(query);
        }

        public Task<CapacityPriceDashboardDto> GetCapacityPriceDashboardAsync(CapacityPriceQueryDto query)
        {
            return _analyticsRepository.GetCapacityPriceDashboardAsync(query);
        }

        public Task<ProjectCapacityPriceDashboardDto> GetProjectCapacityPriceDashboardAsync(AnalyticsQueryDto query)
        {
            return _analyticsRepository.GetProjectCapacityPriceDashboardAsync(query);
        }

        public Task<InternCapacityPriceDashboardDto> GetInternCapacityPriceDashboardAsync(InternCapacityPriceQueryDto query)
        {
            return _analyticsRepository.GetInternCapacityPriceDashboardAsync(query);
        }

        public Task<MemberTahDashboardDto> GetMemberTahDashboardAsync(MemberTahQueryDto query)
        {
            return _analyticsRepository.GetMemberTahDashboardAsync(query);
        }

        public async Task<string> ExportMemberTahDashboardAsync(MemberTahQueryDto query)
        {
            var data = await _analyticsRepository.GetMemberTahDashboardAsync(query);
            return _excelExportService.GenerateMemberTahExcel(data);
        }

        public Task<BookingTargetComparisonDashboardDto> GetBookingTargetComparisonAsync(BookingTargetComparisonQueryDto query)
        {
            return _analyticsRepository.GetBookingTargetComparisonAsync(query);
        }
    }
}
