using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.IServices;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using System.Security.Claims;

namespace PMHUB.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HourSummaryController : ControllerBase
    {
        private readonly IHourSummaryService _hourSummaryService;

        public HourSummaryController(IHourSummaryService hourSummaryService)
        {
            _hourSummaryService = hourSummaryService;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException("The authenticated user identifier is missing or invalid.");
            }

            return userId;
        }

        // GET: api/HourSummary/monthly?year=2026
        [HttpGet("monthly")]
        public async Task<ActionResult<IEnumerable<MonthlyHoursDto>>> GetMonthlySummary([FromQuery] int year)
        {
            var result = await _hourSummaryService.GetMonthlySummary(year);
            return Ok(ApiResponse<IEnumerable<MonthlyHoursDto>>.Ok(result));
        }

        // GET: api/HourSummary/me/monthly?year=2026
        [HttpGet("me/monthly")]
        public async Task<ActionResult<IEnumerable<MonthlyHoursDto>>> GetMyMonthlySummary([FromQuery] int year)
        {
            var result = await _hourSummaryService.GetMonthlySummary(year, GetCurrentUserId());
            return Ok(ApiResponse<IEnumerable<MonthlyHoursDto>>.Ok(result));
        }

        // GET: api/HourSummary/project?year=2026
        [HttpGet("project")]
        public async Task<ActionResult<IEnumerable<ProjectHoursDto>>> GetProjectSummary([FromQuery] int year)
        {
            var result = await _hourSummaryService.GetProjectSummary(year);
            return Ok(ApiResponse<IEnumerable<ProjectHoursDto>>.Ok(result));
        }

        // GET: api/HourSummary/me/project?year=2026
        [HttpGet("me/project")]
        public async Task<ActionResult<IEnumerable<ProjectHoursDto>>> GetMyProjectSummary([FromQuery] int year)
        {
            var result = await _hourSummaryService.GetProjectSummary(year, GetCurrentUserId());
            return Ok(ApiResponse<IEnumerable<ProjectHoursDto>>.Ok(result));
        }

        // GET: api/HourSummary/user?year=2026
        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<UserHoursDto>>> GetUserSummary([FromQuery] int year)
        {
            var result = await _hourSummaryService.GetUserSummary(year);
            return Ok(ApiResponse<IEnumerable<UserHoursDto>>.Ok(result));
        }

        // GET: api/HourSummary/top-projects?year=2026&topCount=5
        [HttpGet("top-projects")]
        public async Task<ActionResult<IEnumerable<ProjectHoursDto>>> GetTopProjects(
            [FromQuery] int year,
            [FromQuery] int topCount = 5)
        {
            var result = await _hourSummaryService.GetTopProjects(year, topCount);
            return Ok(ApiResponse<IEnumerable<ProjectHoursDto>>.Ok(result));
        }

        // GET: api/HourSummary/me/top-projects?year=2026&topCount=5
        [HttpGet("me/top-projects")]
        public async Task<ActionResult<IEnumerable<ProjectHoursDto>>> GetMyTopProjects(
            [FromQuery] int year,
            [FromQuery] int topCount = 5)
        {
            var result = await _hourSummaryService.GetTopProjects(year, topCount, GetCurrentUserId());
            return Ok(ApiResponse<IEnumerable<ProjectHoursDto>>.Ok(result));
        }

        // GET: api/HourSummary/total-hours?year=2026&month=3&userId=&projectId=
        [HttpGet("total-hours")]
        public async Task<ActionResult<decimal>> GetTotalHours(
            [FromQuery] int year,
            [FromQuery] int month,
            [FromQuery] Guid? userId = null,
            [FromQuery] Guid? projectId = null)
        {
            var totalHours = await _hourSummaryService.GetTotalHoursAsync(year, month, userId, projectId);
            return Ok(ApiResponse<decimal>.Ok(totalHours));
        }

        // GET: api/HourSummary/me/total-hours?year=2026&month=3&projectId=
        [HttpGet("me/total-hours")]
        public async Task<ActionResult<decimal>> GetMyTotalHours(
            [FromQuery] int year,
            [FromQuery] int month,
            [FromQuery] Guid? projectId = null)
        {
            var totalHours = await _hourSummaryService.GetTotalHoursAsync(year, month, GetCurrentUserId(), projectId);
            return Ok(ApiResponse<decimal>.Ok(totalHours));
        }

        // GET: api/HourSummary/breakdown?year=2026&month=3&userId=&projectId=
        [HttpGet("breakdown")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetBreakdown(
            [FromQuery] int year,
            [FromQuery] int month,
            [FromQuery] Guid? userId = null,
            [FromQuery] Guid? projectId = null)
        {
            var breakdown = await _hourSummaryService.GetBreakdownAsync(year, month, userId, projectId);
            return Ok(ApiResponse<Dictionary<string, decimal>>.Ok(breakdown));
        }

        // GET: api/HourSummary/me/breakdown?year=2026&month=3&projectId=
        [HttpGet("me/breakdown")]
        public async Task<ActionResult<Dictionary<string, decimal>>> GetMyBreakdown(
            [FromQuery] int year,
            [FromQuery] int month,
            [FromQuery] Guid? projectId = null)
        {
            var breakdown = await _hourSummaryService.GetBreakdownAsync(year, month, GetCurrentUserId(), projectId);
            return Ok(ApiResponse<Dictionary<string, decimal>>.Ok(breakdown));
        }
    }
}   
