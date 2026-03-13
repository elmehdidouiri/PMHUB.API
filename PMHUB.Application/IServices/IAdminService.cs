using PMHUB.Application.DTOs;

namespace PMHUB.Application.IServices
{
    public interface IAdminService
    {
        Task<AdminDto> CreateAdminAsync(CreateAdminDto dto);
        Task<AdminDto> UpdateAdminAsync(Guid id, UpdateAdminDto dto);
        Task<bool> DeleteAdminAsync(Guid id);
        Task<IEnumerable<AdminDto>> GetAllAdminsAsync();
        Task<AdminDto?> GetAdminByIdAsync(Guid id);
    }
}