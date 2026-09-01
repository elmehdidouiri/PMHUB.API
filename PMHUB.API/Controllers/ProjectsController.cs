using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Domain.Enums;
using PMHUB.Shared.Helpers;
using System.Security.Claims;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/projects")]
    [Authorize]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _service;
        private readonly ILogger<ProjectsController> _logger;
        private readonly IExcelExportService _excelService;


        public ProjectsController(IProjectService service, ILogger<ProjectsController> logger, IExcelExportService excelService)
        {
            _service = service;
            _logger = logger;
            _excelService = excelService;
        }

        // POST api/projects
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> Create(
            [FromBody] CreateFullProjectDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Id },
                ApiResponse<ProjectDto>.Ok(result, "Projet créé avec succès."));
        }

        // GET api/projects
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectSummaryDto>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<ProjectSummaryDto>>.Ok(result));
        }

        // Endpoint 1 : Les stats de l'utilisateur normal
        [HttpGet("stats/me")]
        public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetMyStats()
        {
            var userId = GetAuthenticatedUserId(); // On récupère l'ID de celui qui requête
            var result = await _service.GetUserDashboardStatsAsync(userId);
            return Ok(ApiResponse<DashboardStatsDto>.Ok(result));
        }

        // Endpoint 2 : Les stats globales pour l'admin
        [HttpGet("stats/admin")]
        [Authorize(Policy = "AdminOnly")]  
        public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetAdminStats()
        {
            var result = await _service.GetAdminDashboardStatsAsync();
            return Ok(ApiResponse<DashboardStatsDto>.Ok(result));
        }

        // GET api/projects/paged
        [HttpGet("paged")]
        public async Task<ActionResult<ApiResponse<PaginatedResultDto<ProjectSummaryDto>>>> GetPaged(
            [FromQuery] ProjectSearchDto query)
        {
            if (!query.All && !IsCurrentUserAdmin() && !IsCurrentUserProjectManager())
                query.UserId = GetAuthenticatedUserId();

            var result = await _service.GetPagedAsync(query);
            return Ok(ApiResponse<PaginatedResultDto<ProjectSummaryDto>>.Ok(result));
        }

        [HttpGet("export")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ExportProjects(
            [FromQuery] ProjectSearchDto query)
        {
            var relativePath = await _service.ExportProjectsAsync(query);
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.TrimStart('/'));

            if (!System.IO.File.Exists(fullPath))
                return NotFound(ApiResponse.Fail("Le fichier d'export n'a pas pu être généré."));

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Path.GetFileName(fullPath));
        }

        [HttpGet("export/booking-hours")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ExportProjectBookingHours(
            [FromQuery] ProjectSearchDto query)
        {
            var relativePath = await _service.ExportProjectBookingHoursAsync(query);
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.TrimStart('/'));

            if (!System.IO.File.Exists(fullPath))
                return NotFound(ApiResponse.Fail("Le fichier d'export n'a pas pu Ãªtre gÃ©nÃ©rÃ©."));

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Path.GetFileName(fullPath));
        }

        [HttpGet("export/booking-hours/preview")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectBookingExportDto>>>> PreviewProjectBookingHours(
            [FromQuery] ProjectSearchDto query)
        {
            var result = await _service.GetProjectBookingHoursPreviewAsync(query);
            return Ok(ApiResponse<IEnumerable<ProjectBookingExportDto>>.Ok(result));
        }

        // GET api/projects/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null)
                return NotFound(ApiResponse<ProjectDto>.Fail("Project was not found."));
            return Ok(ApiResponse<ProjectDto>.Ok(result));
        }

        // PUT api/projects/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> Update(
            Guid id, [FromBody] UpdateProjectDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<ProjectDto>.Ok(result, "Projet mis à jour avec succès."));
        }

        // PATCH api/projects/{id}
        [HttpPatch("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> Patch(
            Guid id, [FromBody] PatchProjectDto dto)
        {
            var result = await _service.PatchAsync(id, dto);
            return Ok(ApiResponse<ProjectDto>.Ok(result, "Projet mis à jour avec succès."));
        }

        // DELETE api/projects/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Projet supprimé avec succès."));
        }

        // GET api/projects/department/{departmentId}
        [HttpGet("department/{departmentId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectSummaryDto>>>> GetByDepartment(
            Guid departmentId)
        {
            var result = await _service.GetByDepartmentAsync(departmentId);
            return Ok(ApiResponse<IEnumerable<ProjectSummaryDto>>.Ok(result));
        }

        // GET api/projects/businessunit/{businessUnitId}
        [HttpGet("businessunit/{businessUnitId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectSummaryDto>>>> GetByBusinessUnit(
            Guid businessUnitId)
        {
            var result = await _service.GetByBusinessUnitAsync(businessUnitId);
            return Ok(ApiResponse<IEnumerable<ProjectSummaryDto>>.Ok(result));
        }

        // GET api/projects/plant/{plantId}
        [HttpGet("plant/{plantId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectSummaryDto>>>> GetByPlant(
            Guid plantId)
        {
            var result = await _service.GetByPlantAsync(plantId);
            return Ok(ApiResponse<IEnumerable<ProjectSummaryDto>>.Ok(result));
        }

        // GET api/projects/status/{status}
        [HttpGet("status/{status}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectSummaryDto>>>> GetByStatus(
            ProjectStatus status)
        {
            var result = await _service.GetByStatusAsync(status);
            return Ok(ApiResponse<IEnumerable<ProjectSummaryDto>>.Ok(result));
        }

        // GET api/projects/phase/{phase}
        [HttpGet("phase/{phase}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectSummaryDto>>>> GetByPhase(
            ProjectPhase phase)
        {
            var result = await _service.GetByPhaseAsync(phase);
            return Ok(ApiResponse<IEnumerable<ProjectSummaryDto>>.Ok(result));
        }

        // POST api/projects/{id}/subprojects
        [HttpPost("{id:guid}/subprojects")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> AddSubProject(
            Guid id, [FromBody] CreateSubProjectDto dto)
        {
            var result = await _service.AddSubProjectAsync(id, dto);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Id },
                ApiResponse<ProjectDto>.Ok(result, "Sous-projet créé avec succès."));
        }

        // POST api/projects/{id}/members
        [HttpPost("{id:guid}/members")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse>> AddMember(
            Guid id, [FromBody] AddProjectMemberDto dto)
        {
            await _service.AddMemberAsync(id, dto.UserId, dto.RoleId);
            return Ok(ApiResponse.Ok("Membre ajouté au projet avec succès."));
        }

        [HttpGet("{id:guid}/members")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectMemberDto>>>> GetMembers(Guid id)
        {
            var result = await _service.GetMembersAsync(id);
            return Ok(ApiResponse<IEnumerable<ProjectMemberDto>>.Ok(result));
        }

        // DELETE api/projects/{id}/members/{memberIdOrUserId}
        [HttpDelete("{id:guid}/members/{memberIdOrUserId:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse>> RemoveMember(Guid id, Guid memberIdOrUserId)
        {
            await _service.RemoveMemberAsync(id, memberIdOrUserId);
            return Ok(ApiResponse.Ok("Membre retiré du projet avec succès."));
        }

        [HttpGet("{id:guid}/deliverables")]
        public async Task<ActionResult<ApiResponse<IEnumerable<DeliverableBreakdownDto>>>> GetDeliverables(Guid id)
        {
            var result = await _service.GetDeliverablesAsync(id);
            return Ok(ApiResponse<IEnumerable<DeliverableBreakdownDto>>.Ok(result));
        }

        [HttpPost("{id:guid}/deliverables")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<DeliverableBreakdownDto>>> AddDeliverable(Guid id, [FromBody] CreateDeliverableBreakdownDto dto)
        {
            var result = await _service.AddDeliverableAsync(id, dto);
            return Ok(ApiResponse<DeliverableBreakdownDto>.Ok(result, "Deliverable créé avec succès."));
        }

        [HttpPut("{id:guid}/deliverables/{deliverableId:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<DeliverableBreakdownDto>>> UpdateDeliverable(Guid id, Guid deliverableId, [FromBody] UpdateDeliverableBreakdownDto dto)
        {
            var result = await _service.UpdateDeliverableAsync(id, deliverableId, dto);
            return Ok(ApiResponse<DeliverableBreakdownDto>.Ok(result, "Deliverable mis à jour avec succès."));
        }

        [HttpDelete("{id:guid}/deliverables/{deliverableId:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse>> DeleteDeliverable(Guid id, Guid deliverableId)
        {
            await _service.DeleteDeliverableAsync(id, deliverableId);
            return Ok(ApiResponse.Ok("Deliverable supprimé avec succès."));
        }

        [HttpPost("{id:guid}/deliverables/{deliverableId:guid}/tasks")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<DeliverableTaskDto>>> AddDeliverableTask(Guid id, Guid deliverableId, [FromBody] CreateDeliverableTaskDto dto)
        {
            var result = await _service.AddDeliverableTaskAsync(id, deliverableId, dto);
            return Ok(ApiResponse<DeliverableTaskDto>.Ok(result, "Task créée avec succès."));
        }

        [HttpPut("{id:guid}/deliverables/{deliverableId:guid}/tasks/{taskId:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<DeliverableTaskDto>>> UpdateDeliverableTask(Guid id, Guid deliverableId, Guid taskId, [FromBody] UpdateDeliverableTaskDto dto)
        {
            var result = await _service.UpdateDeliverableTaskAsync(id, deliverableId, taskId, dto);
            return Ok(ApiResponse<DeliverableTaskDto>.Ok(result, "Task mise à jour avec succès."));
        }

        [HttpDelete("{id:guid}/deliverables/{deliverableId:guid}/tasks/{taskId:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse>> DeleteDeliverableTask(Guid id, Guid deliverableId, Guid taskId)
        {
            await _service.DeleteDeliverableTaskAsync(id, deliverableId, taskId);
            return Ok(ApiResponse.Ok("Task supprimée avec succès."));
        }

        [HttpGet("{id:guid}/timeline")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectTimelineEntryDto>>>> GetTimeline(Guid id)
        {
            var result = await _service.GetTimelineAsync(id);
            return Ok(ApiResponse<IEnumerable<ProjectTimelineEntryDto>>.Ok(result));
        }

        [HttpPost("{id:guid}/timeline")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<ProjectTimelineEntryDto>>> AddTimelineEntry(Guid id, [FromBody] CreateProjectTimelineEntryDto dto)
        {
            var result = await _service.AddTimelineEntryAsync(id, dto);
            return Ok(ApiResponse<ProjectTimelineEntryDto>.Ok(result, "Entrée timeline créée avec succès."));
        }

        [HttpPut("{id:guid}/timeline/{entryId:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<ProjectTimelineEntryDto>>> UpdateTimelineEntry(Guid id, Guid entryId, [FromBody] UpdateProjectTimelineEntryDto dto)
        {
            var result = await _service.UpdateTimelineEntryAsync(id, entryId, dto);
            return Ok(ApiResponse<ProjectTimelineEntryDto>.Ok(result, "Entrée timeline mise à jour avec succès."));
        }

        [HttpDelete("{id:guid}/timeline/{entryId:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse>> DeleteTimelineEntry(Guid id, Guid entryId)
        {
            await _service.DeleteTimelineEntryAsync(id, entryId);
            return Ok(ApiResponse.Ok("Entrée timeline supprimée avec succès."));
        }

        [HttpGet("{id:guid}/roadblocks")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectRoadblockDto>>>> GetRoadblocks(Guid id)
        {
            var result = await _service.GetRoadblocksAsync(id);
            return Ok(ApiResponse<IEnumerable<ProjectRoadblockDto>>.Ok(result));
        }

        [HttpGet("{id:guid}/roadblocks/delayed")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectRoadblockDto>>>> GetDelayedRoadblocks(Guid id)
        {
            var result = await _service.GetDelayedRoadblocksAsync(id);
            return Ok(ApiResponse<IEnumerable<ProjectRoadblockDto>>.Ok(result));
        }

        [HttpPost("{id:guid}/roadblocks")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<ProjectRoadblockDto>>> AddRoadblock(Guid id, [FromBody] CreateProjectRoadblockDto dto)
        {
            var result = await _service.AddRoadblockAsync(id, dto);
            return Ok(ApiResponse<ProjectRoadblockDto>.Ok(result, "Roadblock créé avec succès."));
        }

        [HttpPut("{id:guid}/roadblocks/{roadblockId:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<ProjectRoadblockDto>>> UpdateRoadblock(Guid id, Guid roadblockId, [FromBody] UpdateProjectRoadblockDto dto)
        {
            var result = await _service.UpdateRoadblockAsync(id, roadblockId, dto);
            return Ok(ApiResponse<ProjectRoadblockDto>.Ok(result, "Roadblock mis à jour avec succès."));
        }

        [HttpDelete("{id:guid}/roadblocks/{roadblockId:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse>> DeleteRoadblock(Guid id, Guid roadblockId)
        {
            await _service.DeleteRoadblockAsync(id, roadblockId);
            return Ok(ApiResponse.Ok("Roadblock supprimé avec succès."));
        }

        [HttpGet("{id:guid}/interns")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectInternAllocationDto>>>> GetInternAllocations(Guid id)
        {
            var result = await _service.GetInternAllocationsAsync(id);
            return Ok(ApiResponse<IEnumerable<ProjectInternAllocationDto>>.Ok(result));
        }

        [HttpPost("{id:guid}/interns")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<ProjectInternAllocationDto>>> AddInternAllocation(Guid id, [FromBody] CreateProjectInternAllocationDto dto)
        {
            var result = await _service.AddInternAllocationAsync(id, dto);
            return Ok(ApiResponse<ProjectInternAllocationDto>.Ok(result, "Intern ajouté au projet avec succès."));
        }

        [HttpPut("{id:guid}/interns/{allocationId:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<ProjectInternAllocationDto>>> UpdateInternAllocation(Guid id, Guid allocationId, [FromBody] UpdateProjectInternAllocationDto dto)
        {
            var result = await _service.UpdateInternAllocationAsync(id, allocationId, dto);
            return Ok(ApiResponse<ProjectInternAllocationDto>.Ok(result, "Allocation intern mise à jour avec succès."));
        }

        [HttpDelete("{id:guid}/interns/{allocationId:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse>> DeleteInternAllocation(Guid id, Guid allocationId)
        {
            await _service.DeleteInternAllocationAsync(id, allocationId);
            return Ok(ApiResponse.Ok("Allocation intern supprimée avec succès."));
        }

        [HttpGet("{id:guid}/interns/{allocationId:guid}/hours")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InternHourEntryDto>>>> GetInternHourEntries(Guid id, Guid allocationId)
        {
            var result = await _service.GetInternHourEntriesAsync(id, allocationId);
            return Ok(ApiResponse<IEnumerable<InternHourEntryDto>>.Ok(result));
        }

        [HttpPost("{id:guid}/interns/{allocationId:guid}/hours")]
        public async Task<ActionResult<ApiResponse<InternHourEntryDto>>> AddInternHourEntry(Guid id, Guid allocationId, [FromBody] CreateInternHourEntryDto dto)
        {
            var result = await _service.AddInternHourEntryAsync(id, allocationId, dto, GetAuthenticatedUserId());
            return Ok(ApiResponse<InternHourEntryDto>.Ok(result, "Heures de l'intern enregistrées avec succès."));
        }

        [HttpPut("{id:guid}/interns/{allocationId:guid}/hours/{hourEntryId:guid}")]
        public async Task<ActionResult<ApiResponse<InternHourEntryDto>>> UpdateInternHourEntry(Guid id, Guid allocationId, Guid hourEntryId, [FromBody] UpdateInternHourEntryDto dto)
        {
            var result = await _service.UpdateInternHourEntryAsync(id, allocationId, hourEntryId, dto, GetAuthenticatedUserId());
            return Ok(ApiResponse<InternHourEntryDto>.Ok(result, "Heures de l'intern mises à jour avec succès."));
        }

        [HttpDelete("{id:guid}/interns/{allocationId:guid}/hours/{hourEntryId:guid}")]
        public async Task<ActionResult<ApiResponse>> DeleteInternHourEntry(Guid id, Guid allocationId, Guid hourEntryId)
        {
            await _service.DeleteInternHourEntryAsync(id, allocationId, hourEntryId, GetAuthenticatedUserId());
            return Ok(ApiResponse.Ok("Entrée d'heures intern supprimée avec succès."));
        }

        [HttpGet("export/monthly/{year}/{month}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ExportMonthlyLink(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var data = await _service.GetForExportAsync(startDate, endDate);
            var link = _excelService.GenerateMonthlyExcel(data, year, month);

            return Ok(new { url = link });
        }

        [HttpGet("export/yearly/{companyYear}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ExportYearlyLink(int companyYear)
        {
            var startDate = CompanyYearHelper.GetCompanyYearStart(companyYear);
            var endDate = CompanyYearHelper.GetCompanyYearEnd(companyYear);

            var data = await _service.GetForExportAsync(startDate, endDate);
            var link = _excelService.GenerateYearlyExcel(data, companyYear);

            return Ok(new { url = link });
        }

        private Guid GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException("User is not authenticated.");

            return userId;
        }

        private bool IsCurrentUserAdmin() =>
            string.Equals(User.FindFirst("isAdmin")?.Value, "true", StringComparison.OrdinalIgnoreCase);

        private bool IsCurrentUserProjectManager()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return string.Equals(role, "Project Manager", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "ProjectManager", StringComparison.OrdinalIgnoreCase);
        }
    }
}
