using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/admin/target-settings")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminTargetSettingsController : ControllerBase
    {
        private readonly ITargetSettingsService _service;

        public AdminTargetSettingsController(ITargetSettingsService service)
        {
            _service = service;
        }

        [HttpGet("company")]
        public async Task<ActionResult<ApiResponse<CompanyTargetSettingsDto>>> GetCompanyTargets()
        {
            var result = await _service.GetCompanyTargetsAsync();
            return Ok(ApiResponse<CompanyTargetSettingsDto>.Ok(result));
        }

        [HttpPut("company")]
        public async Task<ActionResult<ApiResponse<CompanyTargetSettingsDto>>> UpdateCompanyTargets(
            [FromBody] UpdateCompanyTargetSettingsDto dto)
        {
            var result = await _service.UpdateCompanyTargetsAsync(dto);
            return Ok(ApiResponse<CompanyTargetSettingsDto>.Ok(result, "Targets globaux mis a jour avec succes."));
        }

        [HttpGet("kpis")]
        public async Task<ActionResult<ApiResponse<IEnumerable<KpiTargetSettingDto>>>> GetKpiTargets(
            [FromQuery] bool includeInactive = false)
        {
            var result = await _service.GetKpiTargetsAsync(includeInactive);
            return Ok(ApiResponse<IEnumerable<KpiTargetSettingDto>>.Ok(result));
        }

        [HttpGet("kpis/{id:guid}")]
        public async Task<ActionResult<ApiResponse<KpiTargetSettingDto>>> GetKpiTargetById(Guid id)
        {
            var result = await _service.GetKpiTargetByIdAsync(id);
            if (result is null)
                return NotFound(ApiResponse<KpiTargetSettingDto>.Fail("KPI target was not found."));

            return Ok(ApiResponse<KpiTargetSettingDto>.Ok(result));
        }

        [HttpPost("kpis")]
        public async Task<ActionResult<ApiResponse<KpiTargetSettingDto>>> CreateKpiTarget(
            [FromBody] CreateKpiTargetSettingDto dto)
        {
            var result = await _service.CreateKpiTargetAsync(dto);
            return CreatedAtAction(nameof(GetKpiTargetById),
                new { id = result.Id },
                ApiResponse<KpiTargetSettingDto>.Ok(result, "KPI target cree avec succes."));
        }

        [HttpPut("kpis/{id:guid}")]
        public async Task<ActionResult<ApiResponse<KpiTargetSettingDto>>> UpdateKpiTarget(
            Guid id,
            [FromBody] UpdateKpiTargetSettingDto dto)
        {
            var result = await _service.UpdateKpiTargetAsync(id, dto);
            return Ok(ApiResponse<KpiTargetSettingDto>.Ok(result, "KPI target mis a jour avec succes."));
        }

        [HttpDelete("kpis/{id:guid}")]
        public async Task<ActionResult<ApiResponse>> DeleteKpiTarget(Guid id)
        {
            await _service.DeleteKpiTargetAsync(id);
            return Ok(ApiResponse.Ok("KPI target supprime avec succes."));
        }
    }
}
