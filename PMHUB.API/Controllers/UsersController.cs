using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]

    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // GET api/users
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(users));
        }

        // GET api/users/team-member-candidates
        [HttpGet("team-member-candidates")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetTeamMemberCandidates()
        {
            var users = await _userService.GetTeamMemberCandidatesAsync();
            return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(users));
        }

        // GET api/users/role/{roleId}
        [HttpGet("role/{roleId:guid}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetByRole(Guid roleId)
        {
            var users = await _userService.GetByRoleIdAsync(roleId);
            return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(users));
        }

        // GET api/users/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            return Ok(ApiResponse<UserDto>.Ok(user!));
        }
 

        // DELETE api/users/{id}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            await _userService.DeleteUserAsync(id);
            return Ok(ApiResponse.Ok("Utilisateur supprimé avec succès."));
        }

 
    }
}
