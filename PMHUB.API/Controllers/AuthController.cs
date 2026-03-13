using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using System.Security.Claims;

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IUserService userService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _userService = userService;
            _logger = logger;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _authService.RegisterAsync(dto);
            return Ok(ApiResponse.Ok("Inscription réussie, en attente de validation par un admin."));
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<AuthSuccessDto>>> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(dto);
            return Ok(ApiResponse<AuthSuccessDto>.Ok(result));
        }

        // GET api/auth/pending ← Admin uniquement
        [HttpGet("pending")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetPendingUsers()
        {
            var pendingUsers = await _authService.GetPendingUsersAsync();
            return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(pendingUsers));
        }

        // PUT api/auth/approve ← Admin uniquement
        [HttpPut("approve")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> ApproveUser([FromBody] ApproveUserDto dto)
        {
            var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _authService.ApproveUserAsync(dto, adminId);
            var message = dto.IsApproved
                ? "Utilisateur approuvé avec succès."
                : "Utilisateur refusé avec succès.";
            return Ok(ApiResponse.Ok(message));
        }

        // GET api/auth/users/{id}
        [HttpGet("users/{id:guid}")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            return Ok(ApiResponse<UserDto>.Ok(user));
        }
    }
}