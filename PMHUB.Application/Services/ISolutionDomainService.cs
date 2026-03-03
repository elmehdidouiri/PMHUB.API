using PMHUB.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.Services
{
    public interface ISolutionDomainService
    {
        Task<SolutionDomainDto> CreateAsync(CreateSolutionDomainDto dto);
        Task<IEnumerable<SolutionDomainDto>> GetAllAsync();
        Task<SolutionDomainDto?> GetByIdAsync(Guid id);
        Task<SolutionDomainDto> UpdateAsync(Guid id, UpdateSolutionDomainDto dto);
        Task DeleteAsync(Guid id);
    }
}
