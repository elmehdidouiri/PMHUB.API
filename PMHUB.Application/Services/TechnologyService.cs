using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace PMHUB.Application.Services
{
    public class TechnologyService : ITechnologyService
    {
        private readonly IRepository<Technology> _repository;
        private readonly ILogger<TechnologyService> _logger;

        public TechnologyService(IRepository<Technology> repository, ILogger<TechnologyService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // ── CREATE
        public async Task<TechnologyDto> CreateAsync(CreateTechnologyDto dto)
        {
            _logger.LogInformation("Création d'une technologie : {TechnologyName}", dto.Name);

            var existing = await _repository.FindAsync(t => t.Name == dto.Name);
            if (existing.Any())
            {
                _logger.LogWarning("La technologie {TechnologyName} existe déjà", dto.Name);
                throw new ConflictException("Technology", dto.Name);
            }

            var technology = new Technology
            {
                Name = dto.Name,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(technology);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Technologie {TechnologyId} créée avec succès", technology.Id);

            return MapToDto(technology);
        }

        // ── GET ALL
        public async Task<IEnumerable<TechnologyDto>> GetAllAsync()
        {
            _logger.LogInformation("Récupération de toutes les technologies");
            var technologies = await _repository.GetAllAsync();
            return technologies.Select(MapToDto);
        }

        // ── GET BY ID
        public async Task<TechnologyDto?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Récupération de la technologie {TechnologyId}", id);
            var technology = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("Technology", id);

            return MapToDto(technology);
        }

        // ── UPDATE
        public async Task<TechnologyDto> UpdateAsync(Guid id, UpdateTechnologyDto dto)
        {
            _logger.LogInformation("Mise à jour de la technologie {TechnologyId}", id);

            var technology = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("Technology", id);

            var existing = await _repository.FindAsync(t => t.Name == dto.Name && t.Id != id);
            if (existing.Any())
            {
                _logger.LogWarning("La technologie {TechnologyName} existe déjà", dto.Name);
                throw new ConflictException("Technology", dto.Name);
            }

            technology.Name = dto.Name;
            technology.UpdatedAt = DateTime.UtcNow;

            _repository.Update(technology);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Technologie {TechnologyId} mise à jour avec succès", id);

            return MapToDto(technology);
        }

        // ── DELETE
        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("Suppression de la technologie {TechnologyId}", id);

            var technology = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("Technology", id);

            if (technology.ProjectTechnologies.Any())
            {
                _logger.LogWarning("Impossible de supprimer la technologie {TechnologyId} car elle est utilisée dans des projets actifs", id);
                throw new BadRequestException(
                    "This technology cannot be deleted because it is still used by active projects.");
            }

            _repository.Remove(technology);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Technologie {TechnologyId} supprimée avec succès", id);
        }

        private static TechnologyDto MapToDto(Technology t) => new()
        {
            Id = t.Id,
            Name = t.Name,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }
}
