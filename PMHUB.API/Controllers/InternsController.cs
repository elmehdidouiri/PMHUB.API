using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Shared.Helpers;
using System.Security.Claims;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/interns")]
    [Authorize]
    public class InternsController : ControllerBase
    {
        private readonly IInternService _service;
        private readonly IInternStatisticsService _statisticsService;

        public InternsController(IInternService service, IInternStatisticsService statisticsService)
        {
            _service = service;
            _statisticsService = statisticsService;
        }

        // ───────────────────────────────────────────────────────────
        // GESTION DE BASE (CRUD)
        // ───────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<InternDto>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<InternDto>>.Ok(result));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<InternDto>>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(ApiResponse<InternDto>.Ok(result));
        }

        [HttpGet("supervisor/{supervisorId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<InternDto>>>> GetBySupervisor(Guid supervisorId)
        {
            var result = await _service.GetBySupervisorIdAsync(supervisorId);
            return Ok(ApiResponse<IEnumerable<InternDto>>.Ok(result));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<InternDto>>> Create([FromBody] CreateInternDto dto)
        {
            var result = await _service.CreateAsync(dto, GetAuthenticatedUserId());
            return Ok(ApiResponse<InternDto>.Ok(result, "Intern créé avec succès."));
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<InternDto>>> Update(Guid id, [FromBody] UpdateInternDto dto)
        {
            dto.Id = id;
            var result = await _service.UpdateAsync(dto, GetAuthenticatedUserId());
            return Ok(ApiResponse<InternDto>.Ok(result, "Intern mis à jour avec succès."));
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            await _service.DeleteAsync(id, GetAuthenticatedUserId());
            return Ok(ApiResponse.Ok("Intern supprimé avec succès."));
        }

        // ───────────────────────────────────────────────────────────
        // STATISTIQUES ET VISUALISATIONS
        // ───────────────────────────────────────────────────────────

     
     
        [HttpGet("{id:guid}/statistics")]
        public async Task<ActionResult<ApiResponse<InternStatisticsDto>>> GetInternStatistics(Guid id)
        {
            var result = await _statisticsService.GetInternStatisticsAsync(id);
            return Ok(ApiResponse<InternStatisticsDto>.Ok(result));
        }

          [HttpGet("{id:guid}/work-visualization")]
        public async Task<ActionResult<ApiResponse<InternWorkVisualizationDto>>> GetInternWorkVisualization(
            Guid id,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var result = await _statisticsService.GetInternWorkVisualizationAsync(id, startDate, endDate);
            return Ok(ApiResponse<InternWorkVisualizationDto>.Ok(result));
        }

          [HttpGet("{id:guid}/period-statistics")]
        public async Task<ActionResult<ApiResponse<InternPeriodStatisticsDto>>> GetInternPeriodStatistics(
            Guid id,
            [FromQuery] int? year = null,
            [FromQuery] int? month = null)
        {
            if (month.HasValue && (month < 1 || month > 12))
                return BadRequest(ApiResponse.Fail("Month must be between 1 and 12."));

            var resolvedYear = CompanyYearHelper.ResolveCompanyYear(year);
            var result = await _statisticsService.GetInternPeriodStatisticsAsync(id, resolvedYear, month);
            return Ok(ApiResponse<InternPeriodStatisticsDto>.Ok(result));
        }

         /// Dashboard agrégé pour tous les stagiaires
         [HttpGet("dashboard/all")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<InternsDashboardDto>>> GetInternsDashboard()
        {
            var result = await _statisticsService.GetInternsDashboardAsync();
            return Ok(ApiResponse<InternsDashboardDto>.Ok(result));
        }

         /// Statistiques pour les stagiaires d'un superviseur
         [HttpGet("dashboard/supervisor/{supervisorId:guid}")]
        public async Task<ActionResult<ApiResponse<InternsDashboardDto>>> GetSupervisorInternsDashboard(Guid supervisorId)
        {
            var currentUserId = GetAuthenticatedUserId();
            var isAdmin = User.IsInRole("Admin");
 
            if (!isAdmin && supervisorId != currentUserId)
                return Forbid();

            var result = await _statisticsService.GetSupervisorInternsStatisticsAsync(supervisorId);
            return Ok(ApiResponse<InternsDashboardDto>.Ok(result));
        }

  
 
        /// Supprimer tous les enregistrements d'un stagiaire
 
        [HttpDelete("{id:guid}/all-data")]
        public async Task<ActionResult<ApiResponse>> DeleteInternAllData(Guid id)
        {
            await _statisticsService.DeleteInternAllDataAsync(id, GetAuthenticatedUserId());
            return Ok(ApiResponse.Ok("Toutes les données du stagiaire ont été supprimées avec succès."));
        }
  
        /// Supprimer toutes les données de tous les stagiaires (Admin uniquement)
 
        [HttpDelete("all-data")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse>> DeleteAllInternsData()
        {
            await _statisticsService.DeleteAllInternDataAsync(GetAuthenticatedUserId());
            return Ok(ApiResponse.Ok("Toutes les données de tous les stagiaires ont été supprimées avec succès."));
        }
 
         /// Récupère l'ID de l'utilisateur authentifié depuis les claims JWT
         private Guid GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException("Authenticated user could not be resolved.");
            }

            return userId;
        }

     
    }
}
