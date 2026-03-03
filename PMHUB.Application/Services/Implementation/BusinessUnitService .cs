using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using Task = System.Threading.Tasks.Task;

namespace PMHUB.Application.Services
{
    public class BusinessUnitService : IBusinessUnitService
    {
        private readonly IRepository<BusinessUnit> _repository;

        public BusinessUnitService(IRepository<BusinessUnit> repository)
        {
            _repository = repository;
        }

        public async Task<BusinessUnitDto> CreateAsync(CreateBusinessUnitDto dto)
        {
            // Vérifier doublon nom
            var existing = await _repository.FindAsync(b => b.Name == dto.Name);
            if (existing.Any())
                throw new ConflictException("BusinessUnit", dto.Name);

            var bu = new BusinessUnit
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(bu);
            await _repository.SaveChangesAsync();

            return MapToDto(bu);
        }

        public async Task<IEnumerable<BusinessUnitDto>> GetAllAsync()
        {
            var units = await _repository.GetAllAsync();
            return units.Select(MapToDto);
        }

        public async Task<BusinessUnitDto?> GetByIdAsync(Guid id)
        {
            var bu = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("BusinessUnit", id);

            return MapToDto(bu);
        }

        public async Task<BusinessUnitDto> UpdateAsync(Guid id, UpdateBusinessUnitDto dto)
        {
            var bu = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("BusinessUnit", id);

            // Vérifier doublon nom (exclure lui-même)
            var existing = await _repository.FindAsync(
                b => b.Name == dto.Name && b.Id != id);
            if (existing.Any())
                throw new ConflictException("BusinessUnit", dto.Name);

            bu.Name = dto.Name;
            bu.Description = dto.Description;
            bu.UpdatedAt = DateTime.UtcNow;

            _repository.Update(bu);
            await _repository.SaveChangesAsync();

            return MapToDto(bu);
        }

        public async Task DeleteAsync(Guid id)
        {
            var bu = await _repository.GetByIdAsync(id)
                ?? throw new NotFoundException("BusinessUnit", id);

            // Vérifier si des départements sont liés
            if (bu.Departments.Any())
                throw new BadRequestException(
                    "Impossible de supprimer cette BusinessUnit car elle contient des départements actifs.");

            _repository.Remove(bu);
            await _repository.SaveChangesAsync();
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