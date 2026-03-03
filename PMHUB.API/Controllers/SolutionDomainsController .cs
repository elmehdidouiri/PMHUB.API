using Microsoft.AspNetCore.Mvc;
 
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.Services;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SolutionDomainsController : ControllerBase
    {
        private readonly ISolutionDomainService _service;

        public SolutionDomainsController(ISolutionDomainService service)
        {
            _service = service;
        }

        // POST api/solutiondomains
        [HttpPost]
        public async Task<ActionResult<ApiResponse<SolutionDomainDto>>> Create(
            [FromBody] CreateSolutionDomainDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Id },
                ApiResponse<SolutionDomainDto>.Ok(result, "Domaine de solution créé avec succès."));
        }

        // GET api/solutiondomains
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<SolutionDomainDto>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<SolutionDomainDto>>.Ok(result));
        }

        // GET api/solutiondomains/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<SolutionDomainDto>>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(ApiResponse<SolutionDomainDto>.Ok(result));
        }

        // PUT api/solutiondomains/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<SolutionDomainDto>>> Update(
            Guid id, [FromBody] UpdateSolutionDomainDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<SolutionDomainDto>.Ok(result, "Domaine de solution mis à jour avec succès."));
        }

        // DELETE api/solutiondomains/{id}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Domaine de solution supprimé avec succès."));
        }
    }
}