using PMHUB.Application.DTOs;

namespace PMHUB.Application.IServices
{
    public interface IBusinessUnitService
    {
        Task<BusinessUnitDto> CreateAsync(CreateBusinessUnitDto dto);
        Task<IEnumerable<BusinessUnitDto>> GetAllAsync();
        Task<BusinessUnitDto?> GetByIdAsync(Guid id);
        Task<BusinessUnitDto> UpdateAsync(Guid id, UpdateBusinessUnitDto dto);
        Task DeleteAsync(Guid id);
    }
}