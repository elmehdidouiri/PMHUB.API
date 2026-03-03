using PMHUB.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMHUB.Application.Services
{
    public interface IUserService
    {
        Task<UserDto?> GetByEmailAsync(string email);  
        Task<UserDto> CreateUserAsync(CreateUserDto dto);
//        Task ApproveUserAsync(ApproveUserDto dto);
        Task<UserDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task UpdateUserAsync(UpdateUserDto dto); 
        Task DeleteUserAsync(Guid userId);
        Task<bool> ValidateLoginAsync(string email, string password);
    }
}