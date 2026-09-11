using PMHUB.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMHUB.Application.IServices
{
    public interface IUserService
    {
        Task<UserDto?> GetByEmailAsync(string email);  
        Task<UserDto> CreateUserAsync(CreateUserDto dto);
        //        Task ApproveUserAsync(ApproveUserDto dto);
        Task<UserDto> GetByIdAsync(Guid id);
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<IEnumerable<UserDto>> GetTeamMemberCandidatesAsync();
        Task<IEnumerable<UserDto>> GetByRoleIdAsync(Guid roleId);
        Task UpdateUserAsync(UpdateUserDto dto);
        Task UpdateMemberTypeAsync(Guid userId, UpdateMemberTypeDto dto);
        Task ToggleEmailNotificationsAsync(Guid userId, ToggleEmailNotificationsDto dto);
        Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
        Task DeleteUserAsync(Guid userId);
        Task<bool> ValidateLoginAsync(string email, string password);

    }
}
