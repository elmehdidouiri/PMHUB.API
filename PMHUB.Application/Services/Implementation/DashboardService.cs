using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.IServices;
using PMHUB.Infrastructure.Repositories;

namespace PMHUB.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IDashboardRepository dashboardRepository,
            ILogger<DashboardService> logger)
        {
            _dashboardRepository = dashboardRepository;
            _logger = logger;
        }

        public async Task<DashboardOverviewDto> GetAdminDashboardAsync(DashboardQueryDto query, Guid? requesterUserId = null)
        {
            NormalizeQuery(query);

            var sw = Stopwatch.StartNew();
            var result = await _dashboardRepository.GetDashboardOverviewAsync(query, isAdminScope: true);
            sw.Stop();

            _logger.LogInformation(
                "Dashboard admin generated in {DurationMs} ms | requester={Requester} | filters={@Filters}",
                sw.ElapsedMilliseconds,
                requesterUserId,
                query);

            return result;
        }

        public async Task<DashboardOverviewDto> GetMyDashboardAsync(Guid userId, DashboardQueryDto query)
        {
            NormalizeQuery(query);

            var sw = Stopwatch.StartNew();
            var result = await _dashboardRepository.GetDashboardOverviewAsync(query, isAdminScope: false, userId);
            sw.Stop();

            _logger.LogInformation(
                "Dashboard user generated in {DurationMs} ms | userId={UserId} | filters={@Filters}",
                sw.ElapsedMilliseconds,
                userId,
                query);

            return result;
        }

        private static void NormalizeQuery(DashboardQueryDto query)
        {
            if (query.TopN <= 0)
                query.TopN = 5;
        }
    }
}
