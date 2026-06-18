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

        public async Task<DashboardExtendedAdminDto> GetExtendedAdminDashboardAsync(DashboardQueryDto query, Guid? requesterUserId = null)
        {
            NormalizeQuery(query);

            var sw = Stopwatch.StartNew();
            var result = await _dashboardRepository.GetExtendedAdminDashboardAsync(query);
            sw.Stop();

            _logger.LogInformation(
                "Extended dashboard admin generated in {DurationMs} ms | requester={Requester} | filters={@Filters}",
                sw.ElapsedMilliseconds,
                requesterUserId,
                query);

            return result;
        }

        public async Task<DashboardAdminBiDto> GetAdminBiDashboardAsync(DashboardQueryDto query, Guid? requesterUserId = null)
        {
            NormalizeQuery(query);

            var sw = Stopwatch.StartNew();
            var result = await _dashboardRepository.GetAdminBiDashboardAsync(query);
            sw.Stop();

            _logger.LogInformation(
                "Admin BI dashboard generated in {DurationMs} ms | requester={Requester} | filters={@Filters}",
                sw.ElapsedMilliseconds,
                requesterUserId,
                query);

            return result;
        }

        public async Task<DashboardExtendedAdminDto> GetAllProjectsExtendedDashboardAsync(DashboardQueryDto query, Guid? requesterUserId = null)
        {
            NormalizeQuery(query);

            var sw = Stopwatch.StartNew();
            var result = await _dashboardRepository.GetExtendedAdminDashboardAsync(query, isAdminScope: true);
            sw.Stop();

            _logger.LogInformation(
                "Extended dashboard all projects generated in {DurationMs} ms | requester={Requester} | filters={@Filters}",
                sw.ElapsedMilliseconds,
                requesterUserId,
                query);

            return result;
        }

        public async Task<DashboardAdminBiDto> GetAllProjectsBiDashboardAsync(DashboardQueryDto query, Guid? requesterUserId = null)
        {
            NormalizeQuery(query);

            var sw = Stopwatch.StartNew();
            var result = await _dashboardRepository.GetAdminBiDashboardAsync(query, isAdminScope: true);
            sw.Stop();

            _logger.LogInformation(
                "BI dashboard all projects generated in {DurationMs} ms | requester={Requester} | filters={@Filters}",
                sw.ElapsedMilliseconds,
                requesterUserId,
                query);

            return result;
        }

        public async Task<DashboardExtendedAdminDto> GetMyExtendedDashboardAsync(Guid userId, DashboardQueryDto query)
        {
            NormalizeQuery(query);

            var sw = Stopwatch.StartNew();
            var result = await _dashboardRepository.GetExtendedAdminDashboardAsync(query, isAdminScope: false, userId);
            sw.Stop();

            _logger.LogInformation(
                "Extended dashboard user generated in {DurationMs} ms | userId={UserId} | filters={@Filters}",
                sw.ElapsedMilliseconds,
                userId,
                query);

            return result;
        }

        public async Task<DashboardAdminBiDto> GetMyBiDashboardAsync(Guid userId, DashboardQueryDto query)
        {
            NormalizeQuery(query);

            var sw = Stopwatch.StartNew();
            var result = await _dashboardRepository.GetAdminBiDashboardAsync(query, isAdminScope: false, userId);
            sw.Stop();

            _logger.LogInformation(
                "BI dashboard user generated in {DurationMs} ms | userId={UserId} | filters={@Filters}",
                sw.ElapsedMilliseconds,
                userId,
                query);

            return result;
        }

        public async Task<DashboardGroupedDistributionDto> GetGroupedDistributionAsync(DashboardQueryDto query, Guid? requesterUserId = null)
        {
            NormalizeQuery(query);

            var sw = Stopwatch.StartNew();
            var result = await _dashboardRepository.GetGroupedDistributionAsync(query);
            sw.Stop();

            _logger.LogInformation(
                "Grouped distribution dashboard generated in {DurationMs} ms | requester={Requester} | filters={@Filters}",
                sw.ElapsedMilliseconds,
                requesterUserId,
                query);

            return result;
        }

        public async Task<DashboardGroupedDistributionCountsDto> GetGroupedDistributionCountsAsync(DashboardQueryDto query, Guid? requesterUserId = null)
        {
            NormalizeQuery(query);

            var sw = Stopwatch.StartNew();
            var result = await _dashboardRepository.GetGroupedDistributionCountsAsync(query);
            sw.Stop();

            _logger.LogInformation(
                "Grouped distribution counts dashboard generated in {DurationMs} ms | requester={Requester} | filters={@Filters}",
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

            // Clear sensitive admin-only statistics for normal users
            result.Charts.UsersByRole = new List<UsersByRoleDto>();
            result.Charts.ProjectTeamMembersByRole = new List<UsersByRoleDto>();
            
            result.Summary.TotalUsers = 0;
            result.Summary.ActiveUsers = 0;
            result.Summary.ApprovedUsers = 0;

            _logger.LogInformation(
                "Dashboard user generated in {DurationMs} ms | userId={UserId} | filters={@Filters}",
                sw.ElapsedMilliseconds,
                userId,
                query);

            return result;
        }

        public async Task<DashboardPersonalPerformanceDto> GetMyPerformanceDashboardAsync(Guid userId, DashboardQueryDto query)
        {
            NormalizeQuery(query);

            var sw = Stopwatch.StartNew();
            var result = await _dashboardRepository.GetPersonalPerformanceDashboardAsync(userId, query);
            sw.Stop();

            _logger.LogInformation(
                "Personal performance dashboard generated in {DurationMs} ms | userId={UserId} | filters={@Filters}",
                sw.ElapsedMilliseconds,
                userId,
                query);

            return result;
        }

        public async Task<DashboardPersonalPerformanceDto> GetUserPerformanceDashboardAsync(Guid userId, DashboardQueryDto query)
        {
            NormalizeQuery(query);
            return await _dashboardRepository.GetPersonalPerformanceDashboardAsync(userId, query);
        }

        private static void NormalizeQuery(DashboardQueryDto query)
        {
            if (query.TopN <= 0)
                query.TopN = 5;
        }
    }
}
