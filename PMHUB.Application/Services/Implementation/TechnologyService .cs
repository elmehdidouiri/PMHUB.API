using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using Task = System.Threading.Tasks.Task;

namespace PMHUB.Application.Services
{
    public class TechnologyService : ITechnologyService
    {
        private readonly IRepository<Technology> _repository;

        public TechnologyService(IRepository<Technology> repository)
        {
            _repository = repository;
        }

        public async Task<TechnologyDto> CreateAsync(CreateTechnologyDto dto)
        {
             var existing = await _repository.FindAsync(t => t.Name == dto.Name);
            if (existing.Any())
                throw new ConflictException("Technology", dto.Name);

            var technology = new Technology
            {
                Name = dto.Name,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(technology);
            await _repository.SaveChangesAsync();

            return MapToDto(technology);
        }

        public async Task<IEnumerable<TechnologyDto>> GetAllAsync()
        {
            var technologies = await _repository.GetAllAsync();
            return technologies.Select(MapToDto);
        }

        public async Task<TechnologyDto?> GetByIdAsync(Guid id)
        {
            var technology = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("Technology", id);

            return MapToDto(technology);
        }

        public async Task<TechnologyDto> UpdateAsync(Guid id, UpdateTechnologyDto dto)
        {
            var technology = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("Technology", id);

             var existing = await _repository.FindAsync(
                t => t.Name == dto.Name && t.Id != id);
            if (existing.Any())
                throw new ConflictException("Technology", dto.Name);

            technology.Name = dto.Name;
            technology.UpdatedAt = DateTime.UtcNow;

            _repository.Update(technology);
            await _repository.SaveChangesAsync();

            return MapToDto(technology);
        }

        public async Task DeleteAsync(Guid id)
        {
            var technology = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("Technology", id);

             if (technology.ProjectTechnologies.Any())
                throw new BadRequestException(
                    "Impossible de supprimer cette technologie car elle est utilisée dans des projets actifs.");

            _repository.Remove(technology);
            await _repository.SaveChangesAsync();
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