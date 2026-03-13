using PMHUB.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMHUB.Application.IServices
{
    public interface IRoleService
    {
        Task<RoleDto> CreateRoleAsync(CreateRoleDto dto);

        Task<RoleDto?> GetRoleByIdAsync(Guid id);

        Task<IEnumerable<RoleDto>> GetAllRolesAsync();

        Task<RoleDto?> UpdateRoleAsync(Guid id, UpdateRoleDto dto);

        Task<bool> DeleteRoleAsync(Guid id);
    }
}