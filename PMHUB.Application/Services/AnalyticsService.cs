using PMHUB.Application.DTOs;
using PMHUB.Application.IRepositories;
using PMHUB.Application.IServices;

namespace PMHUB.Application.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IAnalyticsRepository _analyticsRepository;

        public AnalyticsService(IAnalyticsRepository analyticsRepository)
        {
            _analyticsRepository = analyticsRepository;
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
    }
}
