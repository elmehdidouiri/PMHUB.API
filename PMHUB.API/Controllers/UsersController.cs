using Microsoft.AspNetCore.Mvc;
using PMHUB.Application.Exceptions;
using PMHUB.Application.DTOs;
using PMHUB.Application.Services;

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
        public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<UserDto>>.Ok(users));
        }

        // GET api/users/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            return Ok(ApiResponse<UserDto>.Ok(user!));
        }

        // POST api/users
        [HttpPost]
        public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserDto dto)
        {
            var user = await _userService.CreateUserAsync(dto);
            return CreatedAtAction(nameof(GetById),
                new { id = user.Id },
                ApiResponse<UserDto>.Ok(user, "Utilisateur créé avec succès."));
        }

        // PUT api/users/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse>> Update(Guid id, [FromBody] UpdateUserDto dto)
        {
            dto.Id = id;
            await _userService.UpdateUserAsync(dto);
            return Ok(ApiResponse.Ok("Utilisateur mis à jour avec succès."));
        }

        // DELETE api/users/{id}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            await _userService.DeleteUserAsync(id);
            return Ok(ApiResponse.Ok("Utilisateur supprimé avec succès."));
        }

        // POST api/users/{id}/approve
      //  [HttpPost("{id:guid}/approve")]
        //public async Task<ActionResult<ApiResponse>> Approve(Guid id, [FromBody] ApproveUserDto dto)
        //{
            // adminId sera lu depuis le token JWT plus tard
          //  var adminId = Guid.Parse(User.FindFirst("sub")?.Value
            //    ?? throw new UnauthorizedAccessException("Token invalide."));

            //dto.UserId = id;
            //await _userService.ApproveUserAsync(dto, adminId);
            //return Ok(ApiResponse.Ok(dto.IsApproved
              //  ? "Utilisateur approuvé avec succès."
                //: "Utilisateur refusé avec succès."));
        //}

        // POST api/users/login
     //   [HttpPost("login")]
       // public async Task<ActionResult<ApiResponse>> Login([FromBody] LoginDto dto)
        //{
          //  var isValid = await _userService.ValidateLoginAsync(dto.Email, dto.Password);
            //return Ok(ApiResponse.Ok("Connexion réussie."));
       // }
    }
}