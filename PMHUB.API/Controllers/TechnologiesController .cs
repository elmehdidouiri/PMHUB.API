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
    [Route("api/[controller]")]
    public class TechnologiesController : ControllerBase
    {
        private readonly ITechnologyService _service;
        private readonly ILogger<TechnologiesController> _logger;

        public TechnologiesController(ITechnologyService service, ILogger<TechnologiesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // POST api/technologies
        [HttpPost]
        public async Task<ActionResult<ApiResponse<TechnologyDto>>> Create([FromBody] CreateTechnologyDto dto)
        {
            try
            {
                _logger.LogInformation("Création d'une technologie : {@Dto}", dto);
                var result = await _service.CreateAsync(dto);
                _logger.LogInformation("Technologie créée avec succès Id {Id}", result.Id);
                return CreatedAtAction(nameof(GetById),
                    new { id = result.Id },
                    ApiResponse<TechnologyDto>.Ok(result, "Technologie créée avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la technologie : {@Dto}", dto);
                throw;
            }
        }

        // GET api/technologies
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<TechnologyDto>>>> GetAll()
        {
            try
            {
                _logger.LogInformation("Récupération de toutes les technologies");
                var result = await _service.GetAllAsync();
                _logger.LogInformation("{Count} technologies récupérées", result?.Count() ?? 0);
                return Ok(ApiResponse<IEnumerable<TechnologyDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des technologies");
                throw;
            }
        }

        // GET api/technologies/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<TechnologyDto>>> GetById(Guid id)
        {
            try
            {
                _logger.LogInformation("Récupération de la technologie Id {Id}", id);
                var result = await _service.GetByIdAsync(id);
                if (result == null)
                {
                    _logger.LogWarning("Technologie Id {Id} non trouvée", id);
                    throw new NotFoundException("Technology not found");
                }
                return Ok(ApiResponse<TechnologyDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la technologie Id {Id}", id);
                throw;
            }
        }

        // PUT api/technologies/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<TechnologyDto>>> Update(Guid id, [FromBody] UpdateTechnologyDto dto)
        {
            try
            {
                _logger.LogInformation("Mise à jour de la technologie Id {Id} : {@Dto}", id, dto);
                var result = await _service.UpdateAsync(id, dto);
                _logger.LogInformation("Technologie Id {Id} mise à jour avec succès", id);
                return Ok(ApiResponse<TechnologyDto>.Ok(result, "Technologie mise à jour avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la technologie Id {Id} : {@Dto}", id, dto);
                throw;
            }
        }

        // DELETE api/technologies/{id}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            try
            {
                _logger.LogInformation("Suppression de la technologie Id {Id}", id);
                await _service.DeleteAsync(id);
                _logger.LogInformation("Technologie Id {Id} supprimée avec succès", id);
                return Ok(ApiResponse.Ok("Technologie supprimée avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la technologie Id {Id}", id);
                throw;
            }
        }
    }
}