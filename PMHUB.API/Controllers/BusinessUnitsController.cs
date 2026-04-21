using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/businessunits")]
    [Authorize]   
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
        [Authorize(Policy = "AdminOnly")]   
        public async Task<ActionResult<ApiResponse<BusinessUnitDto>>> Create(
            [FromBody] CreateBusinessUnitDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Id },
                ApiResponse<BusinessUnitDto>.Ok(result, "BusinessUnit créée avec succès."));
        }

        // GET api/businessunits
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<BusinessUnitDto>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<BusinessUnitDto>>.Ok(result));
        }

        // GET api/businessunits/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<BusinessUnitDto>>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null)
                return NotFound(ApiResponse<BusinessUnitDto>.Fail("Business unit was not found."));
            return Ok(ApiResponse<BusinessUnitDto>.Ok(result));
        }

        // PUT api/businessunits/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]   
        public async Task<ActionResult<ApiResponse<BusinessUnitDto>>> Update(
            Guid id, [FromBody] UpdateBusinessUnitDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<BusinessUnitDto>.Ok(result, "BusinessUnit mise à jour avec succès."));
        }

        // DELETE api/businessunits/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]   
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse.Ok("BusinessUnit supprimée avec succès."));
        }
    }
}
