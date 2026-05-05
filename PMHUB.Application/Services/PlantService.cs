using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using Task = System.Threading.Tasks.Task;

namespace PMHUB.Application.Services
{
    public class PlantService : IPlantService
    {
        private readonly IRepository<Plant> _plantRepository;
        private readonly IRepository<BusinessUnit> _businessUnitRepository;
        private readonly ILogger<PlantService> _logger;

        public PlantService(
            IRepository<Plant> plantRepository,
            IRepository<BusinessUnit> businessUnitRepository,
            ILogger<PlantService> logger)
        {
            _plantRepository = plantRepository;
            _businessUnitRepository = businessUnitRepository;
            _logger = logger;
        }

        public async Task<PlantDto> CreateAsync(CreatePlantDto dto)
        {
            _logger.LogInformation("Création d'un plant : {Name}", dto.Name);

             var existing = await _plantRepository.FindAsync(p => p.Name == dto.Name);
            if (existing.Any())
            {
                _logger.LogWarning("Plant {Name} existe déjà", dto.Name);
                throw new ConflictException("Plant", dto.Name);
            }

            var plant = new Plant
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _plantRepository.AddAsync(plant);
            await _plantRepository.SaveChangesAsync();

            _logger.LogInformation("Plant {PlantId} créé avec succès", plant.Id);

            return MapToDto(plant);
        }

        public async Task<IEnumerable<PlantDto>> GetAllAsync()
        {
            _logger.LogInformation("Récupération de tous les plants");

            var plants = await _plantRepository.GetAllAsync();
            return plants.Select(MapToDto);
        }

        public async Task<PlantDto?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Récupération du plant {PlantId}", id);

            var plant = await _plantRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Plant", id);

            return MapToDto(plant);
        }

        public async Task<PlantDto> UpdateAsync(Guid id, UpdatePlantDto dto)
        {
            _logger.LogInformation("Mise à jour du plant {PlantId}", id);

            var plant = await _plantRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Plant", id);

             var existing = await _plantRepository.FindAsync(
                p => p.Name == dto.Name && p.Id != id);
            if (existing.Any())
            {
                _logger.LogWarning("Plant {Name} existe déjà", dto.Name);
                throw new ConflictException("Plant", dto.Name);
            }

            plant.Name = dto.Name;
            plant.Description = dto.Description;
            plant.UpdatedAt = DateTime.UtcNow;

            _plantRepository.Update(plant);
            await _plantRepository.SaveChangesAsync();

            _logger.LogInformation("Plant {PlantId} mis à jour avec succès", id);

            return MapToDto(plant);
        }

        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("Suppression du plant {PlantId}", id);

            var plant = await _plantRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Plant", id);

             if (plant.Departments.Any())
            {
                _logger.LogWarning("Impossible de supprimer le plant {PlantId} : départements actifs", id);
                throw new BadRequestException(
                    "This plant cannot be deleted because it still contains active departments.");
            }

            _plantRepository.Remove(plant);
            await _plantRepository.SaveChangesAsync();

            _logger.LogInformation("Plant {PlantId} supprimé avec succès", id);
        }

        public async Task<IEnumerable<PlantDto>> GetByBusinessUnitAsync(Guid businessUnitId)
        {
            _logger.LogInformation("Récupération des plants pour la BU {BUId}", businessUnitId);

             await (_businessUnitRepository.GetByIdAsync(businessUnitId)
                ?? throw new NotFoundException("BusinessUnit", businessUnitId));

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
