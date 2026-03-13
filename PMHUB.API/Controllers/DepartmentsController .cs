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
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentService _service;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(IDepartmentService service, ILogger<DepartmentsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // POST api/departments
        [HttpPost]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> Create([FromBody] CreateDepartmentDto dto)
        {
            try
            {
                _logger.LogInformation("Création d'un département : {@Dto}", dto);
                var result = await _service.CreateAsync(dto);
                _logger.LogInformation("Département créé avec succès : {Id}", result.Id);

                return CreatedAtAction(nameof(GetById),
                    new { id = result.Id },
                    ApiResponse<DepartmentDto>.Ok(result, "Département créé avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création d'un département");
                throw;
            }
        }

        // GET api/departments
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<DepartmentDto>>>> GetAll()
        {
            try
            {
                _logger.LogInformation("Récupération de tous les départements");
                var result = await _service.GetAllAsync();
                return Ok(ApiResponse<IEnumerable<DepartmentDto>>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des départements");
                throw;
            }
        }

        // GET api/departments/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetById(Guid id)
        {
            try
            {
                _logger.LogInformation("Récupération du département Id {Id}", id);
                var result = await _service.GetByIdAsync(id);
                return Ok(ApiResponse<DepartmentDto>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du département Id {Id}", id);
                throw;
            }
        }

        // PUT api/departments/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> Update(
            Guid id, [FromBody] UpdateDepartmentDto dto)
        {
            try
            {
                _logger.LogInformation("Mise à jour du département Id {Id} : {@Dto}", id, dto);
                var result = await _service.UpdateAsync(id, dto);
                _logger.LogInformation("Département Id {Id} mis à jour avec succès", id);

                return Ok(ApiResponse<DepartmentDto>.Ok(result, "Département mis à jour avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du département Id {Id}", id);
                throw;
            }
        }

        // DELETE api/departments/{id}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            try
            {
                _logger.LogInformation("Suppression du département Id {Id}", id);
                await _service.DeleteAsync(id);
                _logger.LogInformation("Département Id {Id} supprimé avec succès", id);

                return Ok(ApiResponse.Ok("Département supprimé avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du département Id {Id}", id);
                throw;
            }
        }
    }
}