using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using Task = System.Threading.Tasks.Task;

namespace PMHUB.Application.Services
{
    public class BusinessUnitService : IBusinessUnitService
    {
        private readonly IRepository<BusinessUnit> _repository;
        private readonly ILogger<BusinessUnitService> _logger;

        public BusinessUnitService(IRepository<BusinessUnit> repository, ILogger<BusinessUnitService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<BusinessUnitDto> CreateAsync(CreateBusinessUnitDto dto)
        {
            _logger.LogInformation("Création d'une BusinessUnit : {Name}", dto.Name);

            var existing = await _repository.FindAsync(b => b.Name == dto.Name);
            if (existing.Any())
            {
                _logger.LogWarning("La BusinessUnit {Name} existe déjà", dto.Name);
                throw new ConflictException("BusinessUnit", dto.Name);
            }

            var bu = new BusinessUnit
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(bu);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("BusinessUnit {Id} créée avec succès", bu.Id);

            return MapToDto(bu);
        }

        public async Task<IEnumerable<BusinessUnitDto>> GetAllAsync()
        {
            _logger.LogInformation("Récupération de toutes les BusinessUnits");

            var units = await _repository.GetAllAsync();
            return units.Select(MapToDto);
        }

        public async Task<BusinessUnitDto?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Récupération de la BusinessUnit {Id}", id);

            var bu = await _repository.GetByIdAsync(id)
                     ?? throw new NotFoundException("BusinessUnit", id);

            return MapToDto(bu);
        }

        public async Task<BusinessUnitDto> UpdateAsync(Guid id, UpdateBusinessUnitDto dto)
        {
            _logger.LogInformation("Mise à jour de la BusinessUnit {Id}", id);

            var bu = await _repository.GetByIdAsync(id)
                     ?? throw new NotFoundException("BusinessUnit", id);

            var existing = await _repository.FindAsync(
                b => b.Name == dto.Name && b.Id != id);
            if (existing.Any())
            {
                _logger.LogWarning("La BusinessUnit {Name} existe déjà", dto.Name);
                throw new ConflictException("BusinessUnit", dto.Name);
            }

            bu.Name = dto.Name;
            bu.Description = dto.Description;
            bu.UpdatedAt = DateTime.UtcNow;

            _repository.Update(bu);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("BusinessUnit {Id} mise à jour avec succès", id);

            return MapToDto(bu);
        }

        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("Suppression de la BusinessUnit {Id}", id);

            var bu = await _repository.GetByIdAsync(id)
                     ?? throw new NotFoundException("BusinessUnit", id);

            // Si lazy loading désactivé, inclure explicitement les départements
            if (bu.Departments.Any())
            {
                _logger.LogWarning("Impossible de supprimer la BusinessUnit {Id} : départements actifs", id);
                throw new BadRequestException(
                    "This business unit cannot be deleted because it still contains active departments.");
            }

            _repository.Remove(bu);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("BusinessUnit {Id} supprimée avec succès", id);
        }

        private static BusinessUnitDto MapToDto(BusinessUnit bu) => new()
        {
            Id = bu.Id,
            Name = bu.Name,
            Description = bu.Description,
            CreatedAt = bu.CreatedAt,
            UpdatedAt = bu.UpdatedAt
        };
    }
}
