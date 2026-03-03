using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace PMHUB.Application.Services.Implementation
{
    public class SolutionDomainService : ISolutionDomainService
    {
        private readonly IRepository<SolutionDomain> _repository;

        public SolutionDomainService(IRepository<SolutionDomain> repository)
        {
            _repository = repository;
        }

        public async Task<SolutionDomainDto> CreateAsync(CreateSolutionDomainDto dto)
        {
            // Vérifier doublon nom
            var existing = await _repository.FindAsync(s => s.Name == dto.Name);
            if (existing.Any())
                throw new ConflictException("SolutionDomain", dto.Name);

            var domain = new SolutionDomain
            {
                Name = dto.Name,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(domain);
            await _repository.SaveChangesAsync();
            return MapToDto(domain);
        }

        public async Task<IEnumerable<SolutionDomainDto>> GetAllAsync()
        {
            var domains = await _repository.GetAllAsync();
            return domains.Select(MapToDto);
        }

        public async Task<SolutionDomainDto?> GetByIdAsync(Guid id)
        {
            var domain = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("SolutionDomain", id);

            return MapToDto(domain);
        }

        public async Task<SolutionDomainDto> UpdateAsync(Guid id, UpdateSolutionDomainDto dto)
        {
            var domain = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("SolutionDomain", id);

            // Vérifier doublon nom (exclure lui-même)
            var existing = await _repository.FindAsync(
                s => s.Name == dto.Name && s.Id != id);
            if (existing.Any())
                throw new ConflictException("SolutionDomain", dto.Name);

            domain.Name = dto.Name;
            domain.UpdatedAt = DateTime.UtcNow;

            _repository.Update(domain);
            await _repository.SaveChangesAsync();
            return MapToDto(domain);
        }

        public async Task DeleteAsync(Guid id)
        {
            var domain = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("SolutionDomain", id);

            // Vérifier si utilisé dans des projets
            if (domain.ProjectSolutionDomains.Any())
                throw new BadRequestException(
                    "Impossible de supprimer ce domaine car il est utilisé dans des projets actifs.");

            _repository.Remove(domain);
            await _repository.SaveChangesAsync();
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
