using PMHUB.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.Services
{
    public interface ITechnologyService
    {
        Task<TechnologyDto> CreateAsync(CreateTechnologyDto dto);
        Task<IEnumerable<TechnologyDto>> GetAllAsync();
        Task<TechnologyDto?> GetByIdAsync(Guid id);
        Task<TechnologyDto> UpdateAsync(Guid id, UpdateTechnologyDto dto);
        Task DeleteAsync(Guid id);
    }
}
