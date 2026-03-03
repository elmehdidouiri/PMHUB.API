using PMHUB.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.Services
{
    public interface IPlantService
    {
        Task<PlantDto> CreateAsync(CreatePlantDto dto);
        Task<IEnumerable<PlantDto>> GetAllAsync();
        Task<PlantDto?> GetByIdAsync(Guid id);
        Task<PlantDto> UpdateAsync(Guid id, UpdatePlantDto dto);
        Task DeleteAsync(Guid id);

         Task<IEnumerable<PlantDto>> GetByBusinessUnitAsync(Guid businessUnitId);
    }
}
