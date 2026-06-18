using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/hours-allocation")]
    [Authorize(Policy = "AdminOnly")]
    public class HoursAllocationController : ControllerBase
    {
        private readonly IHoursAllocationDashboardService _hoursAllocationDashboardService;

        public HoursAllocationController(IHoursAllocationDashboardService hoursAllocationDashboardService)
        {
            _hoursAllocationDashboardService = hoursAllocationDashboardService;
        }

        [HttpGet("filters")]
        public async Task<ActionResult<ApiResponse<HoursAllocationFiltersDto>>> GetFilters()
        {
            var result = await _hoursAllocationDashboardService.GetFiltersAsync();
            return Ok(ApiResponse<HoursAllocationFiltersDto>.Ok(result));
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<HoursAllocationDashboardDto>>> GetDashboard(
            [FromQuery] HoursAllocationDashboardQueryDto query)
        {
            var result = await _hoursAllocationDashboardService.GetDashboardAsync(query);
            var message = result.Summary.Allocations == 0
                ? "No hours allocation data was found for the requested filters."
                : null;

            return Ok(ApiResponse<HoursAllocationDashboardDto>.Ok(result, message));
        }

        [HttpGet("projects/{projectId:guid}/dashboard")]
        public async Task<ActionResult<ApiResponse<HoursAllocationDashboardDto>>> GetProjectDashboard(
            Guid projectId,
            [FromQuery] HoursAllocationDashboardQueryDto query)
        {
            query.ProjectId = projectId;

            var result = await _hoursAllocationDashboardService.GetDashboardAsync(query);
            var message = result.Summary.Allocations == 0
                ? "No hours allocation data was found for this project."
                : null;

            return Ok(ApiResponse<HoursAllocationDashboardDto>.Ok(result, message));
        }

        [HttpPost("reminders")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<HoursAllocationReminderResultDto>>> SendReminders(
            [FromBody] HoursAllocationReminderRequestDto request)
        {
            var result = await _hoursAllocationDashboardService.SendRemindersAsync(request);
            return Ok(ApiResponse<HoursAllocationReminderResultDto>.Ok(result));
        }
    }
}
