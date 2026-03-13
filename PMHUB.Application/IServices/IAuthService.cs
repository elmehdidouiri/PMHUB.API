using PMHUB.Application.DTOs;

namespace PMHUB.Application.IServices
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterDto dto);
        Task<AuthSuccessDto> LoginAsync(LoginDto dto);
        Task<IEnumerable<UserDto>> GetPendingUsersAsync();
         Task ApproveUserAsync(ApproveUserDto dto, Guid adminId);

    }
}