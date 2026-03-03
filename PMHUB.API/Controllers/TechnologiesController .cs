using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.Services;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TechnologiesController : ControllerBase
    {
        private readonly ITechnologyService _service;

        public TechnologiesController(ITechnologyService service)
        {
            _service = service;
        }

        // POST api/technologies
        [HttpPost]
        public async Task<ActionResult<ApiResponse<TechnologyDto>>> Create(
            [FromBody] CreateTechnologyDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Id },
                ApiResponse<TechnologyDto>.Ok(result, "Technologie créée avec succès."));
        }

        // GET api/technologies
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<TechnologyDto>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<TechnologyDto>>.Ok(result));
        }

        // GET api/technologies/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<TechnologyDto>>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(ApiResponse<TechnologyDto>.Ok(result));
        }

        // PUT api/technologies/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<TechnologyDto>>> Update(
            Guid id, [FromBody] UpdateTechnologyDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<TechnologyDto>.Ok(result, "Technologie mise à jour avec succès."));
        }

        // DELETE api/technologies/{id}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Technologie supprimée avec succès."));
        }
    }
}