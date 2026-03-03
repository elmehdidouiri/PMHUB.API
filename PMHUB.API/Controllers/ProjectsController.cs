using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.Services;
using PMHUB.Domain.Enums;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _service;

        public ProjectsController(IProjectService service)
        {
            _service = service;
        }

        // POST api/projects
        [HttpPost]
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

        // GET api/projects/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(ApiResponse<ProjectDto>.Ok(result));
        }

        // PUT api/projects/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> Update(
            Guid id, [FromBody] UpdateProjectDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<ProjectDto>.Ok(result, "Projet mis à jour avec succès."));
        }

        // DELETE api/projects/{id}
        [HttpDelete("{id:guid}")]
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
        public async Task<ActionResult<ApiResponse<ProjectDto>>> AddSubProject(
            Guid id, [FromBody] CreateSubProjectDto dto)
        {
            var result = await _service.AddSubProjectAsync(id, dto);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Id },
                ApiResponse<ProjectDto>.Ok(result, "Sous-projet créé avec succès."));
        }

        // POST api/projects/{id}/members/{userId}
        [HttpPost("{id:guid}/members/{userId:guid}")]
        public async Task<ActionResult<ApiResponse>> AddMember(Guid id, Guid userId)
        {
            await _service.AddMemberAsync(id, userId);
            return Ok(ApiResponse.Ok("Membre ajouté au projet avec succès."));
        }

        // DELETE api/projects/{id}/members/{userId}
        [HttpDelete("{id:guid}/members/{userId:guid}")]
        public async Task<ActionResult<ApiResponse>> RemoveMember(Guid id, Guid userId)
        {
            await _service.RemoveMemberAsync(id, userId);
            return Ok(ApiResponse.Ok("Membre retiré du projet avec succès."));
        }
    }
}