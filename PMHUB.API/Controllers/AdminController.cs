using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/admins")]
    [Authorize(Policy = "AdminOnly")]   
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _service;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService service, ILogger<AdminController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET api/admins
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<AdminDto>>>> GetAll()
        {
            var admins = await _service.GetAllAdminsAsync();
            return Ok(ApiResponse<IEnumerable<AdminDto>>.Ok(admins));
        }

        // GET api/admins/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<AdminDto>>> GetById(Guid id)
        {
            var admin = await _service.GetAdminByIdAsync(id);
            if (admin == null) return NotFound($"Admin {id} introuvable.");
            return Ok(ApiResponse<AdminDto>.Ok(admin));
        }

        // POST api/admins
        [HttpPost]
        public async Task<ActionResult<ApiResponse<AdminDto>>> Create(
            [FromBody] CreateAdminDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var admin = await _service.CreateAdminAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = admin.Id },
                ApiResponse<AdminDto>.Ok(admin));
        }

        // PUT api/admins/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<AdminDto>>> Update(
            Guid id, [FromBody] UpdateAdminDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var admin = await _service.UpdateAdminAsync(id, dto);
            if (admin == null) return NotFound($"Admin {id} introuvable.");
            return Ok(ApiResponse<AdminDto>.Ok(admin));
        }

        // DELETE api/admins/{id}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var deleted = await _service.DeleteAdminAsync(id);
            if (!deleted) return NotFound($"Admin {id} introuvable.");
            return NoContent();
        }
    }
}