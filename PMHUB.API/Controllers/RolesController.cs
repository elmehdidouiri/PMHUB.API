using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _service;
        private readonly ILogger<RolesController> _logger;

        public RolesController(IRoleService service, ILogger<RolesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // ── GET api/roles
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetAll()
        {
            try
            {
                _logger.LogInformation("Récupération de tous les rôles");
                var roles = await _service.GetAllRolesAsync();
                _logger.LogInformation("{Count} rôles récupérés", roles?.Count() ?? 0);
                return Ok(ApiResponse<IEnumerable<RoleDto>>.Ok(roles));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de tous les rôles");
                throw;
            }
        }

        // ── GET api/roles/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<RoleDto>>> GetById(Guid id)
        {
            try
            {
                _logger.LogInformation("Récupération du rôle Id {RoleId}", id);
                var role = await _service.GetRoleByIdAsync(id);
                if (role == null)
                {
                    _logger.LogWarning("Rôle Id {RoleId} non trouvé", id);
                    throw new NotFoundException("Role not found");
                }

                return Ok(ApiResponse<RoleDto>.Ok(role));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du rôle Id {RoleId}", id);
                throw;
            }
        }

        // ── POST api/roles
        [HttpPost]
        public async Task<ActionResult<ApiResponse<RoleDto>>> Create([FromBody] CreateRoleDto dto)
        {
            try
            {
                _logger.LogInformation("Création d'un rôle : {@Dto}", dto);
                var role = await _service.CreateRoleAsync(dto);
                _logger.LogInformation("Rôle créé avec succès Id {RoleId}", role.Id);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = role.Id },
                    ApiResponse<RoleDto>.Ok(role, "Role créé avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création d'un rôle : {@Dto}", dto);
                throw;
            }
        }

        // ── PUT api/roles/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<RoleDto>>> Update(Guid id, [FromBody] UpdateRoleDto dto)
        {
            try
            {
                _logger.LogInformation("Mise à jour du rôle Id {RoleId} : {@Dto}", id, dto);
                var role = await _service.UpdateRoleAsync(id, dto);
                if (role == null)
                {
                    _logger.LogWarning("Rôle Id {RoleId} non trouvé pour mise à jour", id);
                    throw new NotFoundException("Role not found");
                }

                _logger.LogInformation("Rôle Id {RoleId} mis à jour avec succès", id);
                return Ok(ApiResponse<RoleDto>.Ok(role, "Role mis à jour avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du rôle Id {RoleId} : {@Dto}", id, dto);
                throw;
            }
        }

        // ── DELETE api/roles/{id}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            try
            {
                _logger.LogInformation("Suppression du rôle Id {RoleId}", id);
                var deleted = await _service.DeleteRoleAsync(id);
                if (!deleted)
                {
                    _logger.LogWarning("Rôle Id {RoleId} non trouvé pour suppression", id);
                    throw new NotFoundException("Role not found");
                }

                _logger.LogInformation("Rôle Id {RoleId} supprimé avec succès", id);
                return Ok(ApiResponse.Ok("Role supprimé avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du rôle Id {RoleId}", id);
                throw;
            }
        }
    }
}