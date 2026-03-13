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
    public class BusinessUnitsController : ControllerBase
    {
        private readonly IBusinessUnitService _service;
        private readonly ILogger<BusinessUnitsController> _logger;

        public BusinessUnitsController(
            IBusinessUnitService service,
            ILogger<BusinessUnitsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // POST api/businessunits
        [HttpPost]
        public async Task<ActionResult<ApiResponse<BusinessUnitDto>>> Create(
            [FromBody] CreateBusinessUnitDto dto)
        {
            try
            {
                _logger.LogInformation("Création d'une BusinessUnit : {@Dto}", dto);
                var result = await _service.CreateAsync(dto);
                _logger.LogInformation("BusinessUnit créée avec succès : {Id}", result.Id);

                return CreatedAtAction(nameof(GetById),
                    new { id = result.Id },
                    ApiResponse<BusinessUnitDto>.Ok(result, "BusinessUnit créée avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création d'une BusinessUnit");
                throw;
            }
        }

        // GET api/businessunits
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<BusinessUnitDto>>>> GetAll()
        {
            try
            {
                _logger.LogInformation("Récupération de toutes les BusinessUnits");
                var result = await _service.GetAllAsync();
                return Ok(ApiResponse<IEnumerable<BusinessUnitDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des BusinessUnits");
                throw;
            }
        }

        // GET api/businessunits/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<BusinessUnitDto>>> GetById(Guid id)
        {
            try
            {
                _logger.LogInformation("Récupération de la BusinessUnit avec Id {Id}", id);
                var result = await _service.GetByIdAsync(id);
                return Ok(ApiResponse<BusinessUnitDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la BusinessUnit Id {Id}", id);
                throw;
            }
        }

        // PUT api/businessunits/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<BusinessUnitDto>>> Update(
            Guid id, [FromBody] UpdateBusinessUnitDto dto)
        {
            try
            {
                _logger.LogInformation("Mise à jour de la BusinessUnit Id {Id} : {@Dto}", id, dto);
                var result = await _service.UpdateAsync(id, dto);
                _logger.LogInformation("BusinessUnit Id {Id} mise à jour avec succès", id);

                return Ok(ApiResponse<BusinessUnitDto>.Ok(result, "BusinessUnit mise à jour avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la BusinessUnit Id {Id}", id);
                throw;
            }
        }

        // DELETE api/businessunits/{id}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            try
            {
                _logger.LogInformation("Suppression de la BusinessUnit Id {Id}", id);
                await _service.DeleteAsync(id);
                _logger.LogInformation("BusinessUnit Id {Id} supprimée avec succès", id);

                return Ok(ApiResponse.Ok("BusinessUnit supprimée avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la BusinessUnit Id {Id}", id);
                throw;
            }
        }
    }
}