using PMHUB.Application.DTOs;

namespace PMHUB.Application.IServices
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterDto dto);
        Task<AuthSuccessDto> LoginAsync(LoginDto dto);
        Task<AuthSuccessDto> RefreshTokensAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default);
        Task<ForgotPasswordResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto dto);
        Task<VerifyPasswordResetCodeResponseDto> VerifyPasswordResetCodeAsync(VerifyPasswordResetCodeDto dto);
        Task ResetPasswordAsync(ResetPasswordDto dto);
        Task<IEnumerable<UserDto>> GetPendingUsersAsync();
         Task ApproveUserAsync(ApproveUserDto dto, Guid adminId);

    }
}
