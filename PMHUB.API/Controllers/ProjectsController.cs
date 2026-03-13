using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _service;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(IProjectService service, ILogger<ProjectsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // POST api/projects
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> Create([FromBody] CreateFullProjectDto dto)
        {
            try
            {
                _logger.LogInformation("Création d'un projet : {@Dto}", dto);
                var result = await _service.CreateAsync(dto);
                _logger.LogInformation("Projet créé avec succès : {ProjectId}", result.Id);
                return CreatedAtAction(nameof(GetById),
                    new { id = result.Id },
                    ApiResponse<ProjectDto>.Ok(result, "Projet créé avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du projet");
                throw;
            }
        }

        // GET api/projects
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectSummaryDto>>>> GetAll()
        {
            try
            {
                _logger.LogInformation("Récupération de tous les projets");
                var result = await _service.GetAllAsync();
                return Ok(ApiResponse<IEnumerable<ProjectSummaryDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de tous les projets");
                throw;
            }
        }

        // GET api/projects/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> GetById(Guid id)
        {
            try
            {
                _logger.LogInformation("Récupération du projet Id {ProjectId}", id);
                var result = await _service.GetByIdAsync(id);
                return Ok(ApiResponse<ProjectDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du projet Id {ProjectId}", id);
                throw;
            }
        }

        // PUT api/projects/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> Update(Guid id, [FromBody] UpdateProjectDto dto)
        {
            try
            {
                _logger.LogInformation("Mise à jour du projet Id {ProjectId} : {@Dto}", id, dto);
                var result = await _service.UpdateAsync(id, dto);
                _logger.LogInformation("Projet Id {ProjectId} mis à jour avec succès", id);
                return Ok(ApiResponse<ProjectDto>.Ok(result, "Projet mis à jour avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du projet Id {ProjectId}", id);
                throw;
            }
        }

        // DELETE api/projects/{id}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            try
            {
                _logger.LogInformation("Suppression du projet Id {ProjectId}", id);
                await _service.DeleteAsync(id);
                _logger.LogInformation("Projet Id {ProjectId} supprimé avec succès", id);
                return Ok(ApiResponse.Ok("Projet supprimé avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du projet Id {ProjectId}", id);
                throw;
            }
        }

        // PATCH api/projects/{id}
        [HttpPatch("{id:guid}")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> Patch(Guid id, [FromBody] PatchProjectDto dto)
        {
            try
            {
                _logger.LogInformation("Patch du projet Id {ProjectId} : {@Dto}", id, dto);
                var project = await _service.PatchAsync(id, dto);
                _logger.LogInformation("Projet Id {ProjectId} patché avec succès", id);
                return Ok(ApiResponse<ProjectDto>.Ok(project, "Projet mis à jour avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du patch du projet Id {ProjectId}", id);
                throw;
            }
        }

        // GET api/projects/department/{departmentId}
        [HttpGet("department/{departmentId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectSummaryDto>>>> GetByDepartment(Guid departmentId)
        {
            try
            {
                _logger.LogInformation("Récupération des projets pour le département Id {DepartmentId}", departmentId);
                var result = await _service.GetByDepartmentAsync(departmentId);
                return Ok(ApiResponse<IEnumerable<ProjectSummaryDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des projets pour le département Id {DepartmentId}", departmentId);
                throw;
            }
        }

        // GET api/projects/businessunit/{businessUnitId}
        [HttpGet("businessunit/{businessUnitId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectSummaryDto>>>> GetByBusinessUnit(Guid businessUnitId)
        {
            try
            {
                _logger.LogInformation("Récupération des projets pour BusinessUnit Id {BusinessUnitId}", businessUnitId);
                var result = await _service.GetByBusinessUnitAsync(businessUnitId);
                return Ok(ApiResponse<IEnumerable<ProjectSummaryDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des projets pour BusinessUnit Id {BusinessUnitId}", businessUnitId);
                throw;
            }
        }

        // GET api/projects/plant/{plantId}
        [HttpGet("plant/{plantId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectSummaryDto>>>> GetByPlant(Guid plantId)
        {
            try
            {
                _logger.LogInformation("Récupération des projets pour Plant Id {PlantId}", plantId);
                var result = await _service.GetByPlantAsync(plantId);
                return Ok(ApiResponse<IEnumerable<ProjectSummaryDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des projets pour Plant Id {PlantId}", plantId);
                throw;
            }
        }

        // GET api/projects/status/{status}
        [HttpGet("status/{status}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectSummaryDto>>>> GetByStatus(ProjectStatus status)
        {
            try
            {
                _logger.LogInformation("Récupération des projets avec Status {Status}", status);
                var result = await _service.GetByStatusAsync(status);
                return Ok(ApiResponse<IEnumerable<ProjectSummaryDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des projets avec Status {Status}", status);
                throw;
            }
        }

        // GET api/projects/phase/{phase}
        [HttpGet("phase/{phase}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectSummaryDto>>>> GetByPhase(ProjectPhase phase)
        {
            try
            {
                _logger.LogInformation("Récupération des projets avec Phase {Phase}", phase);
                var result = await _service.GetByPhaseAsync(phase);
                return Ok(ApiResponse<IEnumerable<ProjectSummaryDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des projets avec Phase {Phase}", phase);
                throw;
            }
        }

        // POST api/projects/{id}/subprojects
        [HttpPost("{id:guid}/subprojects")]
        public async Task<ActionResult<ApiResponse<ProjectDto>>> AddSubProject(Guid id, [FromBody] CreateSubProjectDto dto)
        {
            try
            {
                _logger.LogInformation("Ajout d'un sous-projet pour Projet Id {ProjectId} : {@Dto}", id, dto);
                var result = await _service.AddSubProjectAsync(id, dto);
                _logger.LogInformation("Sous-projet créé avec succès : {SubProjectId}", result.Id);
                return CreatedAtAction(nameof(GetById),
                    new { id = result.Id },
                    ApiResponse<ProjectDto>.Ok(result, "Sous-projet créé avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout d'un sous-projet pour Projet Id {ProjectId}", id);
                throw;
            }
        }

        // POST api/projects/{id}/members
        [HttpPost("{id:guid}/members")]
        public async Task<ActionResult<ApiResponse>> AddMember(Guid id, [FromBody] AddProjectMemberDto dto)
        {
            try
            {
                _logger.LogInformation("Ajout du membre UserId {UserId} au projet Id {ProjectId}", dto.UserId, id);
                await _service.AddMemberAsync(id, dto.UserId, dto.RoleId);
                _logger.LogInformation("Membre ajouté avec succès au projet Id {ProjectId}", id);
                return Ok(ApiResponse.Ok("Membre ajouté au projet avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout du membre UserId {UserId} au projet Id {ProjectId}", dto.UserId, id);
                throw;
            }
        }

        // DELETE api/projects/{id}/members/{userId}
        [HttpDelete("{id:guid}/members/{userId:guid}")]
        public async Task<ActionResult<ApiResponse>> RemoveMember(Guid id, Guid userId)
        {
            try
            {
                _logger.LogInformation("Suppression du membre UserId {UserId} du projet Id {ProjectId}", userId, id);
                await _service.RemoveMemberAsync(id, userId);
                _logger.LogInformation("Membre supprimé avec succès du projet Id {ProjectId}", id);
                return Ok(ApiResponse.Ok("Membre retiré du projet avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du membre UserId {UserId} du projet Id {ProjectId}", userId, id);
                throw;
            }
        }

        // GET api/projects/paged
        [HttpGet("paged")]
        public async Task<ActionResult<ApiResponse<PaginatedResultDto<ProjectSummaryDto>>>> GetPaged([FromQuery] PaginationQueryDto query)
        {
            try
            {
                _logger.LogInformation("Récupération paginée des projets : {@Query}", query);
                var result = await _service.GetPagedAsync(query);
                return Ok(ApiResponse<PaginatedResultDto<ProjectSummaryDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des projets : {@Query}", query);
                throw;
            }
        }
    }
}