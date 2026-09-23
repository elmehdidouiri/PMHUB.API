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

        [HttpGet("capacity-price")]
        public async Task<ActionResult<ApiResponse<CapacityPriceDashboardDto>>> GetCapacityPriceDashboard([FromQuery] CapacityPriceQueryDto query)
        {
            var result = await _analyticsService.GetCapacityPriceDashboardAsync(query);
            return Ok(ApiResponse<CapacityPriceDashboardDto>.Ok(result));
        }

        [HttpGet("project-capacity-price")]
        public async Task<ActionResult<ApiResponse<ProjectCapacityPriceDashboardDto>>> GetProjectCapacityPriceDashboard([FromQuery] AnalyticsQueryDto query)
        {
            var result = await _analyticsService.GetProjectCapacityPriceDashboardAsync(query);
            return Ok(ApiResponse<ProjectCapacityPriceDashboardDto>.Ok(result));
        }

        [HttpGet("intern-capacity-price")]
        public async Task<ActionResult<ApiResponse<InternCapacityPriceDashboardDto>>> GetInternCapacityPriceDashboard([FromQuery] InternCapacityPriceQueryDto query)
        {
            var result = await _analyticsService.GetInternCapacityPriceDashboardAsync(query);
            return Ok(ApiResponse<InternCapacityPriceDashboardDto>.Ok(result));
        }

        [HttpGet("booking-target-comparison")]
        public async Task<ActionResult<ApiResponse<BookingTargetComparisonDashboardDto>>> GetBookingTargetComparison([FromQuery] BookingTargetComparisonQueryDto query)
        {
            var result = await _analyticsService.GetBookingTargetComparisonAsync(query);
            return Ok(ApiResponse<BookingTargetComparisonDashboardDto>.Ok(result));
        }

        [HttpGet("member-tah")]
        public async Task<ActionResult<ApiResponse<MemberTahDashboardDto>>> GetMemberTahDashboard([FromQuery] MemberTahQueryDto query)
        {
            var result = await _analyticsService.GetMemberTahDashboardAsync(query);
            var message = result.Summary.EmployeeCount + result.Summary.SubcontractorCount == 0
                ? "No member TAH data was found for the requested filters."
                : null;

            return Ok(ApiResponse<MemberTahDashboardDto>.Ok(result, message));
        }

        [HttpGet("member-tah/export")]
        public async Task<IActionResult> ExportMemberTahDashboard([FromQuery] MemberTahQueryDto query)
        {
            var relativePath = await _analyticsService.ExportMemberTahDashboardAsync(query);
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.TrimStart('/'));

            if (!System.IO.File.Exists(fullPath))
                return NotFound(ApiResponse.Fail("Le fichier d'export n'a pas pu être généré."));

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                Path.GetFileName(fullPath));
        }
    }
}
