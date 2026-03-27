using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/departments")]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentService _service;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(
            IDepartmentService service,
            ILogger<DepartmentsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // POST api/departments
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> Create(
            [FromBody] CreateDepartmentDto dto)
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Id },
                ApiResponse<DepartmentDto>.Ok(result, "Département créé avec succès."));
        }

        // GET api/departments
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<DepartmentDto>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<DepartmentDto>>.Ok(result));
        }

        // GET api/departments/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(ApiResponse<DepartmentDto>.Ok(result));
        }

        // PUT api/departments/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<DepartmentDto>>> Update(
            Guid id, [FromBody] UpdateDepartmentDto dto)
        {
            var result = await _service.UpdateAsync(id, dto);
            return Ok(ApiResponse<DepartmentDto>.Ok(result, "Département mis à jour avec succès."));
        }

        // DELETE api/departments/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Département supprimé avec succès."));
        }
    }
}