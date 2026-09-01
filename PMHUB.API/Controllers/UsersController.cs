using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using System.Security.Claims;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // GET api/users
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(users));
        }

        // GET api/users/team-member-candidates
        [HttpGet("team-member-candidates")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetTeamMemberCandidates()
        {
            var users = await _userService.GetTeamMemberCandidatesAsync();
            return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(users));
        }

        // GET api/users/role/{roleId}
        [HttpGet("role/{roleId:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetByRole(Guid roleId)
        {
            var users = await _userService.GetByRoleIdAsync(roleId);
            return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(users));
        }

        // GET api/users/{id}
        [HttpGet("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            return Ok(ApiResponse<UserDto>.Ok(user!));
        }

        // PUT api/users/{id}
        [HttpPut("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse>> Update(Guid id, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (dto.Id != Guid.Empty && dto.Id != id)
                throw new BadRequestException("User ID in route and body must match.");

            dto.Id = id;
            await _userService.UpdateUserAsync(dto);
            return Ok(ApiResponse.Ok("Utilisateur mis à jour avec succès."));
        }

        // PUT api/users/{id}/member-type
        [HttpPut("{id:guid}/member-type")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse>> UpdateMemberType(Guid id, [FromBody] UpdateMemberTypeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _userService.UpdateMemberTypeAsync(id, dto);
            return Ok(ApiResponse.Ok("MemberType mis à jour avec succès."));
        }

        // PUT api/users/me/password
        [HttpPut("me/password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse>> ChangeMyPassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new BadRequestException("Invalid authenticated user.");

            await _userService.ChangePasswordAsync(userId, dto);
            return Ok(ApiResponse.Ok("Mot de passe mis à jour avec succès."));
        }

        // DELETE api/users/{id}
        [HttpDelete("{id:guid}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            await _userService.DeleteUserAsync(id);
            return Ok(ApiResponse.Ok("Utilisateur supprimé avec succès."));
        }

 
    }
}
