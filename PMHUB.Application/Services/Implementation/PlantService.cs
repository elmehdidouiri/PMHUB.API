using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using Task = System.Threading.Tasks.Task;

namespace PMHUB.Application.Services
{
    public class PlantService : IPlantService
    {
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BusinessUnit> _businessUnitRepository;

        public PlantService(
            IRepository<Plant> plantRepository,
            IRepository<BusinessUnit> businessUnitRepository)
        {
            _plantRepository = plantRepository;
            _businessUnitRepository = businessUnitRepository;
        }

        public async Task<PlantDto> CreateAsync(CreatePlantDto dto)
        {
            // Vérifier doublon nom
            var existing = await _plantRepository.FindAsync(p => p.Name == dto.Name);
            if (existing.Any())
                throw new ConflictException("Plant", dto.Name);

            var plant = new Plant
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _plantRepository.AddAsync(plant);
            await _plantRepository.SaveChangesAsync();

            return MapToDto(plant);
        }

        public async Task<IEnumerable<PlantDto>> GetAllAsync()
        {
            var plants = await _plantRepository.GetAllAsync();
            return plants.Select(MapToDto);
        }

        public async Task<PlantDto?> GetByIdAsync(Guid id)
        {
            var plant = await _plantRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Plant", id);

            return MapToDto(plant);
        }

        public async Task<PlantDto> UpdateAsync(Guid id, UpdatePlantDto dto)
        {
            var plant = await _plantRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Plant", id);

            // Vérifier doublon nom (exclure lui-même)
            var existing = await _plantRepository.FindAsync(
                p => p.Name == dto.Name && p.Id != id);
            if (existing.Any())
                throw new ConflictException("Plant", dto.Name);

            plant.Name = dto.Name;
            plant.Description = dto.Description;
            plant.UpdatedAt = DateTime.UtcNow;

            _plantRepository.Update(plant);
            await _plantRepository.SaveChangesAsync();

            return MapToDto(plant);
        }

        public async Task DeleteAsync(Guid id)
        {
            var plant = await _plantRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Plant", id);

            // Vérifier si des départements sont liés
            if (plant.Departments.Any())
                throw new BadRequestException(
                    "Impossible de supprimer ce plant car il contient des départements actifs.");

            _plantRepository.Remove(plant);
            await _plantRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<PlantDto>> GetByBusinessUnitAsync(Guid businessUnitId)
        {
            // Vérifier que la BU existe
            await (_businessUnitRepository.GetByIdAsync(businessUnitId)
                ?? throw new NotFoundException("BusinessUnit", businessUnitId));

            // Récupérer les plants via les départements de cette BU
            var plants = await _plantRepository.FindAsync(
                p => p.Departments.Any(d => d.BusinessUnitId == businessUnitId));

            return plants.Select(MapToDto);
        }

        private static PlantDto MapToDto(Plant p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            DepartmentCount = p.Departments.Count
        };
    }
}