using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;

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

        // GET api/roles  
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetAll()
        {
            var roles = await _service.GetAllRolesAsync();
            return Ok(ApiResponse<IEnumerable<RoleDto>>.Ok(roles));
        }

        // GET api/roles/{id} ← Public pour dropdown inscription
        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<RoleDto>>> GetById(Guid id)
        {
            var role = await _service.GetRoleByIdAsync(id);
            if (role is null)
                return NotFound(ApiResponse<RoleDto>.Fail("Role was not found."));
            return Ok(ApiResponse<RoleDto>.Ok(role));
        }

        // POST api/roles 
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<RoleDto>>> Create(
            [FromBody] CreateRoleDto dto)
        {
            var role = await _service.CreateRoleAsync(dto);
            return CreatedAtAction(nameof(GetById),
                new { id = role.Id },
                ApiResponse<RoleDto>.Ok(role, "Role créé avec succès."));
        }

        // PUT api/roles/{id} 
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<RoleDto>>> Update(
            Guid id, [FromBody] UpdateRoleDto dto)
        {
            var role = await _service.UpdateRoleAsync(id, dto);
            if (role is null)
                return NotFound(ApiResponse<RoleDto>.Fail("Role was not found."));
            return Ok(ApiResponse<RoleDto>.Ok(role, "Role mis à jour avec succès."));
        }

        // DELETE api/roles/{id}  
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            await _service.DeleteRoleAsync(id);
            return Ok(ApiResponse.Ok("Role supprimé avec succès."));
        }
    }
}
