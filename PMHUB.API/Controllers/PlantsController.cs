using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.Services;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlantsController : ControllerBase
    {
        private readonly IPlantService _service;

        public PlantsController(IPlantService service)
        {
            _service = service;
        }

        // POST api/plants
        [HttpPost]
        public async Task<ActionResult<ApiResponse<PlantDto>>> Create(
            [FromBody] CreatePlantDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Id },
                ApiResponse<PlantDto>.Ok(result, "Plant créé avec succès."));
        }

        // GET api/plants
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<PlantDto>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<PlantDto>>.Ok(result));
        }

        // GET api/plants/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<PlantDto>>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(ApiResponse<PlantDto>.Ok(result));
        }

        // PUT api/plants/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<PlantDto>>> Update(
            Guid id, [FromBody] UpdatePlantDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<PlantDto>.Ok(result, "Plant mis à jour avec succès."));
        }

        // DELETE api/plants/{id}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Plant supprimé avec succès."));
        }

        // GET api/plants/businessunit/{businessUnitId}
        [HttpGet("businessunit/{businessUnitId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<PlantDto>>>> GetByBusinessUnit(
            Guid businessUnitId)
        {
            var result = await _service.GetByBusinessUnitAsync(businessUnitId);
            return Ok(ApiResponse<IEnumerable<PlantDto>>.Ok(result));
        }
    }
}