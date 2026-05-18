using PMHUB.Application.DTOs;

namespace PMHUB.Application.IServices
{
    public interface IInternService
    {
        Task<InternDto> CreateAsync(CreateInternDto dto, Guid currentUserId);
        Task<IEnumerable<InternDto>> GetAllAsync();
        Task<InternDto> GetByIdAsync(Guid id);
        Task<InternDto> UpdateAsync(UpdateInternDto dto, Guid currentUserId);
        Task DeleteAsync(Guid id, Guid currentUserId);
        Task<IEnumerable<InternDto>> GetBySupervisorIdAsync(Guid supervisorId);
    }
}
