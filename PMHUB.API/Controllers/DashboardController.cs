using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("admin")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<DashboardOverviewDto>>> GetAdminDashboard([FromQuery] DashboardQueryDto query)
        {
            var requesterId = TryGetAuthenticatedUserId();
            var result = await _dashboardService.GetAdminDashboardAsync(query, requesterId);
            var message = result.Summary.TotalProjects == 0
                ? "No data was found for the requested filters."
                : null;

            return Ok(ApiResponse<DashboardOverviewDto>.Ok(result, message));
        }

        [HttpGet("admin/extended")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<DashboardExtendedAdminDto>>> GetExtendedAdminDashboard([FromQuery] DashboardQueryDto query)
        {
            var requesterId = TryGetAuthenticatedUserId();
            var result = await _dashboardService.GetExtendedAdminDashboardAsync(query, requesterId);
            var message = result.Summary.TotalProjects == 0
                ? "No data was found for the requested filters."
                : null;

            return Ok(ApiResponse<DashboardExtendedAdminDto>.Ok(result, message));
        }

        [HttpGet("admin/bi")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<DashboardAdminBiDto>>> GetAdminBiDashboard([FromQuery] DashboardQueryDto query)
        {
            var requesterId = TryGetAuthenticatedUserId();
            var result = await _dashboardService.GetAdminBiDashboardAsync(query, requesterId);
            var message = result.Summary.TotalProjects == 0
                ? "No data was found for the requested filters."
                : null;

            return Ok(ApiResponse<DashboardAdminBiDto>.Ok(result, message));
        }

        [HttpGet("admin/grouped-distribution")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<DashboardGroupedDistributionDto>>> GetGroupedDistribution([FromQuery] DashboardQueryDto query)
        {
            var requesterId = TryGetAuthenticatedUserId();
            var result = await _dashboardService.GetGroupedDistributionAsync(query, requesterId);
            var totalProjects = result.Status.SelectMany(x => x.Projects).Select(x => x.Id).Distinct().Count();
            var message = totalProjects == 0
                ? "No data was found for the requested filters."
                : null;

            return Ok(ApiResponse<DashboardGroupedDistributionDto>.Ok(result, message));
        }

        [HttpGet("admin/grouped-distribution/counts")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<DashboardGroupedDistributionCountsDto>>> GetGroupedDistributionCounts([FromQuery] DashboardQueryDto query)
        {
            var requesterId = TryGetAuthenticatedUserId();
            var result = await _dashboardService.GetGroupedDistributionCountsAsync(query, requesterId);
            var message = result.Summary.TotalProjects == 0
                ? "No data was found for the requested filters."
                : null;

            return Ok(ApiResponse<DashboardGroupedDistributionCountsDto>.Ok(result, message));
        }

        [HttpGet("me")]
        [Authorize(Policy = "UserOnly")]
        public async Task<ActionResult<ApiResponse<DashboardOverviewDto>>> GetMyDashboard([FromQuery] DashboardQueryDto query)
        {
            var userId = GetAuthenticatedUserId();
            var result = await _dashboardService.GetMyDashboardAsync(userId, query);
            var message = result.Summary.TotalProjects == 0
                ? "No data was found for the requested filters."
                : null;

            return Ok(ApiResponse<DashboardOverviewDto>.Ok(result, message));
        }

        [HttpGet("me/performance")]
        [Authorize(Policy = "UserOnly")]
        public async Task<ActionResult<ApiResponse<DashboardPersonalPerformanceDto>>> GetMyPerformanceDashboard([FromQuery] DashboardQueryDto query)
        {
            var userId = GetAuthenticatedUserId();
            var result = await _dashboardService.GetMyPerformanceDashboardAsync(userId, query);
            var message = result.Summary.TotalLoggedHours == 0 && result.Summary.AssignedProjects == 0
                ? "No personal performance data was found for the requested filters."
                : null;

            return Ok(ApiResponse<DashboardPersonalPerformanceDto>.Ok(result, message));
        }

        [HttpGet("admin/user/{userId:guid}/performance")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<DashboardPersonalPerformanceDto>>> GetUserPerformanceDashboard(Guid userId, [FromQuery] DashboardQueryDto query)
        {
            var result = await _dashboardService.GetUserPerformanceDashboardAsync(userId, query);
            var message = result.Summary.TotalLoggedHours == 0 && result.Summary.AssignedProjects == 0
                ? "No personal performance data was found for this user with the requested filters."
                : null;

            return Ok(ApiResponse<DashboardPersonalPerformanceDto>.Ok(result, message));
        }

        private Guid GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException("Authenticated user could not be resolved.");
            }

            return userId;
        }

        private Guid? TryGetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            return userId;
        }
    }
}
