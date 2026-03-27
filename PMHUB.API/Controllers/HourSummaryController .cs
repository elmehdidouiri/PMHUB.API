using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.IServices;
using PMHUB.Application.DTOs;

namespace PMHUB.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HourSummaryController : ControllerBase
    {
        private readonly IHourSummaryService _hourSummaryService;

        public HourSummaryController(IHourSummaryService hourSummaryService)
        {
            _hourSummaryService = hourSummaryService;
        }

        // GET: api/HourSummary/monthly?year=2026
        [HttpGet("monthly")]
        public async Task<ActionResult<IEnumerable<MonthlyHoursDto>>> GetMonthlySummary([FromQuery] int year)
        {
            var result = await _hourSummaryService.GetMonthlySummary(year);
            return Ok(result);
        }

        // GET: api/HourSummary/project?year=2026
        [HttpGet("project")]
        public async Task<ActionResult<IEnumerable<ProjectHoursDto>>> GetProjectSummary([FromQuery] int year)
        {
            var result = await _hourSummaryService.GetProjectSummary(year);
            return Ok(result);
        }

        // GET: api/HourSummary/user?year=2026
        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<UserHoursDto>>> GetUserSummary([FromQuery] int year)
        {
            var result = await _hourSummaryService.GetUserSummary(year);
            return Ok(result);
        }

        // GET: api/HourSummary/top-projects?year=2026&topCount=5
        [HttpGet("top-projects")]
        public async Task<ActionResult<IEnumerable<ProjectHoursDto>>> GetTopProjects(
            [FromQuery] int year,
            [FromQuery] int topCount = 5)
        {
            var result = await _hourSummaryService.GetTopProjects(year, topCount);
            return Ok(result);
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
            return Ok(totalHours);
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
            return Ok(breakdown);
        }
    }
}   