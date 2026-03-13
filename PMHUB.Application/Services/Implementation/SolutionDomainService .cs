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

namespace PMHUB.Application.Services.Implementation
{
    public class SolutionDomainService : ISolutionDomainService
    {
        private readonly IRepository<SolutionDomain> _repository;
        private readonly ILogger<SolutionDomainService> _logger;

        public SolutionDomainService(IRepository<SolutionDomain> repository, ILogger<SolutionDomainService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        // ── CREATE
        public async Task<SolutionDomainDto> CreateAsync(CreateSolutionDomainDto dto)
        {
            _logger.LogInformation("Création d'un domaine de solution : {DomainName}", dto.Name);

            var existing = await _repository.FindAsync(s => s.Name == dto.Name);
            if (existing.Any())
            {
                _logger.LogWarning("Le domaine {DomainName} existe déjà", dto.Name);
                throw new ConflictException("SolutionDomain", dto.Name);
            }

            var domain = new SolutionDomain
            {
                Name = dto.Name,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(domain);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Domaine de solution {DomainId} créé avec succès", domain.Id);

            return MapToDto(domain);
        }

        // ── GET ALL
        public async Task<IEnumerable<SolutionDomainDto>> GetAllAsync()
        {
            _logger.LogInformation("Récupération de tous les domaines de solution");
            var domains = await _repository.GetAllAsync();
            return domains.Select(MapToDto);
        }

        // ── GET BY ID
        public async Task<SolutionDomainDto?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Récupération du domaine de solution {DomainId}", id);
            var domain = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("SolutionDomain", id);

            return MapToDto(domain);
        }

        // ── UPDATE
        public async Task<SolutionDomainDto> UpdateAsync(Guid id, UpdateSolutionDomainDto dto)
        {
            _logger.LogInformation("Mise à jour du domaine de solution {DomainId}", id);

            var domain = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("SolutionDomain", id);

            var existing = await _repository.FindAsync(s => s.Name == dto.Name && s.Id != id);
            if (existing.Any())
            {
                _logger.LogWarning("Le domaine {DomainName} existe déjà", dto.Name);
                throw new ConflictException("SolutionDomain", dto.Name);
            }

            domain.Name = dto.Name;
            domain.UpdatedAt = DateTime.UtcNow;

            _repository.Update(domain);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Domaine de solution {DomainId} mis à jour avec succès", id);

            return MapToDto(domain);
        }

        // ── DELETE
        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("Suppression du domaine de solution {DomainId}", id);

            var domain = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("SolutionDomain", id);

            if (domain.ProjectSolutionDomains.Any())
            {
                _logger.LogWarning("Impossible de supprimer le domaine {DomainId} car il est utilisé dans des projets", id);
                throw new BadRequestException(
                    "Impossible de supprimer ce domaine car il est utilisé dans des projets actifs.");
            }

            _repository.Remove(domain);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Domaine de solution {DomainId} supprimé avec succès", id);
        }

        private static SolutionDomainDto MapToDto(SolutionDomain s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }
}