using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/hour-entries")]
    [Authorize]
    public class HourEntriesController : ControllerBase
    {
        private readonly IHourEntryService _hourEntryService;
        private readonly ILogger<HourEntriesController> _logger;


        public HourEntriesController(IHourEntryService hourEntryService, ILogger<HourEntriesController> logger)
        {
            _hourEntryService = hourEntryService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateAllocation([FromBody] CreateHourEntryDto dto)
        {
            var userId = GetAuthenticatedUserId();
            _logger.LogInformation("Début CreateAllocation pour User {UserId} sur Projet {ProjectId}", userId, dto.ProjectId);

            var result = await _hourEntryService.CreateAsync(dto, userId);

            _logger.LogInformation("Fin CreateAllocation pour User {UserId} — HourEntry Id: {Id}", userId, result.Id);
            return Ok(ApiResponse<HourEntryDto>.Ok(result, "Allocation créée avec succès"));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAllocation(Guid id, [FromBody] UpdateHourEntryDto dto)
        {
            var userId = GetAuthenticatedUserId();
            _logger.LogInformation("Début UpdateAllocation pour User {UserId} — HourEntry Id: {Id}", userId, id);

            var result = await _hourEntryService.UpdateAsync(id, dto, userId);

            _logger.LogInformation("Fin UpdateAllocation pour User {UserId} — HourEntry Id: {Id}", userId, result.Id);
            return Ok(ApiResponse<HourEntryDto>.Ok(result, "Allocation mise à jour avec succès"));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAllocation(Guid id)
        {
            var userId = GetAuthenticatedUserId();
            _logger.LogInformation("Début DeleteAllocation pour User {UserId} — HourEntry Id: {Id}", userId, id);

            await _hourEntryService.DeleteAsync(id, userId);

            _logger.LogInformation("Fin DeleteAllocation pour User {UserId} — HourEntry Id: {Id}", userId, id);
            return Ok(ApiResponse.Ok("Allocation supprimée avec succès"));
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyEntries()
        {
            var userId = GetAuthenticatedUserId();
            _logger.LogInformation("Récupération de toutes les entrées pour User {UserId}", userId);

            var result = await _hourEntryService.GetMyEntriesAsync(userId);

            _logger.LogInformation("{Count} entrées récupérées pour User {UserId}", result?.Count() ?? 0, userId);
            return Ok(ApiResponse<IEnumerable<HourEntrySummaryDto>>.Ok(result ?? Enumerable.Empty<HourEntrySummaryDto>()));
        }

        [HttpGet("my/date")]
        public async Task<IActionResult> GetMyEntriesByDate([FromQuery] DateTime date)
        {
            var userId = GetAuthenticatedUserId();
            _logger.LogInformation("Récupération des entrées pour User {UserId} à la date {Date}", userId, date);

            var result = await _hourEntryService.GetMyEntriesByDateAsync(userId, date);

            _logger.LogInformation("{Count} entrées récupérées pour User {UserId} à la date {Date}", result?.Count() ?? 0, userId, date);
            return Ok(ApiResponse<IEnumerable<HourEntrySummaryDto>>.Ok(result ?? Enumerable.Empty<HourEntrySummaryDto>()));
        }

        [HttpGet("my/month")]
        public async Task<IActionResult> GetMyEntriesByMonth([FromQuery] int year, [FromQuery] int month)
        {
            if (month < 1 || month > 12)
            throw new BadRequestException("Month must be between 1 and 12.");

            var userId = GetAuthenticatedUserId();
            _logger.LogInformation("Récupération des entrées pour User {UserId} — Mois {Month}/{Year}", userId, month, year);

            var result = await _hourEntryService.GetMyEntriesByMonthAsync(userId, year, month);

            _logger.LogInformation("{Count} entrées récupérées pour User {UserId} — Mois {Month}/{Year}", result?.Count() ?? 0, userId, month, year);
            return Ok(ApiResponse<IEnumerable<HourEntrySummaryDto>>.Ok(result ?? Enumerable.Empty<HourEntrySummaryDto>()));
        }

        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetByProject(Guid projectId)
        {
            _logger.LogInformation("Récupération des entrées pour Projet {ProjectId}", projectId);

            var result = await _hourEntryService.GetByProjectAsync(projectId);

            _logger.LogInformation("{Count} entrées récupérées pour Projet {ProjectId}", result?.Count() ?? 0, projectId);
            return Ok(ApiResponse<IEnumerable<HourEntrySummaryDto>>.Ok(result ?? Enumerable.Empty<HourEntrySummaryDto>()));
        }

        [HttpGet("dashboard/monthly")]
        public async Task<IActionResult> GetMonthlyDashboard([FromQuery] int year, [FromQuery] int month)
        {
            if (month < 1 || month > 12)
            throw new BadRequestException("Month must be between 1 and 12.");

            var userId = GetAuthenticatedUserId();
            _logger.LogInformation("Récupération du dashboard mensuel pour User {UserId} — Mois {Month}/{Year}", userId, month, year);

            var result = await _hourEntryService.GetMonthlyDashboardAsync(userId, year, month);

            _logger.LogInformation("Dashboard mensuel généré pour User {UserId} — TotalHeures {LoggedHours}", userId, result.LoggedHours);
            return Ok(ApiResponse<MonthlyHoursDashboardDto>.Ok(result));
        }

        [HttpGet("dashboard/ytd")]
        public async Task<IActionResult> GetYtdDashboard([FromQuery] int companyYear)
        {
            var userId = GetAuthenticatedUserId();
            _logger.LogInformation("Récupération du dashboard YTD pour User {UserId} — CompanyYear {Year}", userId, companyYear);

            var result = await _hourEntryService.GetYtdDashboardAsync(userId, companyYear);

            _logger.LogInformation("Dashboard YTD généré pour User {UserId} — TotalHeures {YtdHours}", userId, result.YtdHours);
            return Ok(ApiResponse<YtdDashboardDto>.Ok(result));
        }

        [HttpGet("my/projects")]
        public async Task<IActionResult> GetMyProjects()
        {
            var userId = GetAuthenticatedUserId();
            _logger.LogInformation("Récupération des projets pour User {UserId}", userId);

            var result = await _hourEntryService.GetMyProjectsAsync(userId);

            _logger.LogInformation("{Count} projets récupérés pour User {UserId}", result?.Count() ?? 0, userId);
            return Ok(ApiResponse<IEnumerable<ProjectSummaryDto>>.Ok(result ?? Enumerable.Empty<ProjectSummaryDto>()));
        }

        [HttpGet("my/supervised-interns")]
        public async Task<IActionResult> GetMySupervisedInterns()
        {
            var userId = GetAuthenticatedUserId();
            _logger.LogInformation("Récupération des stagiaires supervisés pour User {UserId}", userId);

            var result = await _hourEntryService.GetSupervisedInternsAsync(userId);

            _logger.LogInformation("{Count} stagiaires récupérés pour User {UserId}", result?.Count() ?? 0, userId);
            return Ok(ApiResponse<IEnumerable<ProjectInternAllocationDto>>.Ok(result ?? Enumerable.Empty<ProjectInternAllocationDto>()));
        }

        [HttpGet("premium/pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingPremium()
        {
            _logger.LogInformation("Récupération des heures premium en attente");

            var result = await _hourEntryService.GetPendingPremiumAsync();

            _logger.LogInformation("{Count} entrées premium en attente récupérées", result?.Count() ?? 0);
            return Ok(ApiResponse<IEnumerable<HourEntryDto>>.Ok(result ?? Enumerable.Empty<HourEntryDto>()));
        }

        [HttpPut("premium/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApprovePremium([FromBody] ApprovePremiumDto dto)
        {
            _logger.LogInformation("Application de la décision premium pour HourEntry {HourEntryId} — Décision: {IsApproved}", dto.HourEntryId, dto.IsApproved);

            await _hourEntryService.ApprovePremiumAsync(dto);

            _logger.LogInformation("Décision premium appliquée pour HourEntry {HourEntryId}", dto.HourEntryId);
            return Ok(ApiResponse.Ok("Décision premium appliquée avec succès"));
        }

        private Guid GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Utilisateur non authentifié lors de l'appel API {Path}", HttpContext.Request.Path);
            throw new UnauthorizedException("User is not authenticated.");
            }

            return userId;
        }
    }
}
