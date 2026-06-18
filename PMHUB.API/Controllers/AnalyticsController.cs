using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    [Authorize(Policy = "AdminOnly")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<AnalyticsDashboardDto>>> GetDashboard([FromQuery] AnalyticsQueryDto query)
        {
            var result = await _analyticsService.GetDashboardAsync(query);
            return Ok(ApiResponse<AnalyticsDashboardDto>.Ok(result));
        }

        [HttpGet("filters")]
        public async Task<ActionResult<ApiResponse<AnalyticsFiltersDto>>> GetFilters()
        {
            var result = await _analyticsService.GetFiltersAsync();
            return Ok(ApiResponse<AnalyticsFiltersDto>.Ok(result));
        }

        [HttpGet("summary")]
        public async Task<ActionResult<ApiResponse<AnalyticsSummaryDto>>> GetSummary([FromQuery] AnalyticsQueryDto query)
        {
            var result = await _analyticsService.GetSummaryAsync(query);
            return Ok(ApiResponse<AnalyticsSummaryDto>.Ok(result));
        }

        [HttpGet("kpis")]
        public async Task<ActionResult<ApiResponse<AnalyticsKpisDto>>> GetKpis([FromQuery] AnalyticsQueryDto query)
        {
            var result = await _analyticsService.GetKpisAsync(query);
            return Ok(ApiResponse<AnalyticsKpisDto>.Ok(result));
        }

        [HttpGet("hours")]
        public async Task<ActionResult<ApiResponse<AnalyticsHoursDto>>> GetHours([FromQuery] AnalyticsQueryDto query)
        {
            var result = await _analyticsService.GetHoursAsync(query);
            return Ok(ApiResponse<AnalyticsHoursDto>.Ok(result));
        }

        [HttpGet("export")]
        public ActionResult<ApiResponse> Export()
        {
            return StatusCode(
                StatusCodes.Status501NotImplemented,
                ApiResponse.Fail("Analytics export is reserved for a later step."));
        }
    }
}
