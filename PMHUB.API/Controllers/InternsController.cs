using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using System.Security.Claims;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/interns")]
    [Authorize]
    public class InternsController : ControllerBase
    {
        private readonly IInternService _service;

        public InternsController(IInternService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<InternDto>>>> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<InternDto>>.Ok(result));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<InternDto>>> GetById(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(ApiResponse<InternDto>.Ok(result));
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<InternDto>>> Create([FromBody] CreateInternDto dto)
        {
            var result = await _service.CreateAsync(dto, GetAuthenticatedUserId());
            return Ok(ApiResponse<InternDto>.Ok(result, "Intern créé avec succès."));
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<InternDto>>> Update(Guid id, [FromBody] UpdateInternDto dto)
        {
            dto.Id = id;
            var result = await _service.UpdateAsync(dto, GetAuthenticatedUserId());
            return Ok(ApiResponse<InternDto>.Ok(result, "Intern mis à jour avec succès."));
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            await _service.DeleteAsync(id, GetAuthenticatedUserId());
            return Ok(ApiResponse.Ok("Intern supprimé avec succès."));
        }

        private Guid GetAuthenticatedUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedException("User is not authenticated.");

            return userId;
        }
    }
}
