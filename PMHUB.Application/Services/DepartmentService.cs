using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using Task = System.Threading.Tasks.Task;

namespace PMHUB.Application.Services
{
    public class DepartmentService : IDepartmentService
    {
        private const string PlaceholderDepartmentName = "-";

        private readonly IRepository<Department> _departmentRepository;
        private readonly IRepository<BusinessUnit> _businessUnitRepository;
        private readonly IRepository<Plant> _plantRepository;
        private readonly ILogger<DepartmentService> _logger;

        public DepartmentService(
            IRepository<Department> departmentRepository,
            IRepository<BusinessUnit> businessUnitRepository,
            IRepository<Plant> plantRepository,
            ILogger<DepartmentService> logger)
        {
            _departmentRepository = departmentRepository;
            _businessUnitRepository = businessUnitRepository;
            _plantRepository = plantRepository;
            _logger = logger;
        }

        public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto)
        {
            _logger.LogInformation("Création d'un département : {Name}", dto.Name);

            var bu = await _businessUnitRepository.GetByIdAsync(dto.BusinessUnitId)
                     ?? throw new NotFoundException("BusinessUnit", dto.BusinessUnitId);
            var plant = await _plantRepository.GetByIdAsync(dto.PlantId)
                        ?? throw new NotFoundException("Plant", dto.PlantId);

            var existing = await _departmentRepository.FindAsync(
                d => d.Name == dto.Name && d.BusinessUnitId == dto.BusinessUnitId);
            if (existing.Any())
            {
                _logger.LogWarning("Département {Name} existe déjà dans la BU {BUId}", dto.Name, dto.BusinessUnitId);
                throw new ConflictException("Department", dto.Name);
            }

            var department = new Department
            {
                Name = dto.Name,
                BusinessUnitId = dto.BusinessUnitId,
                PlantId = dto.PlantId,
                CreatedAt = DateTime.UtcNow
            };

            await _departmentRepository.AddAsync(department);
            await _departmentRepository.SaveChangesAsync();

            _logger.LogInformation("Département {DepartmentId} créé avec succès", department.Id);

            return MapToDto(department, bu.Name, plant.Name);
        }

        public async Task<IEnumerable<DepartmentDto>> GetAllAsync()
        {
            _logger.LogInformation("Récupération de tous les départements");

            var departments = await _departmentRepository.GetAllAsync();
            return departments
                .Where(d => !IsPlaceholderDepartment(d.Name))
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    BusinessUnitId = d.BusinessUnitId,
                    PlantId = d.PlantId,
                    CreatedAt = d.CreatedAt,
                    UpdatedAt = d.UpdatedAt
                });
        }

        public async Task<DepartmentDto?> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Récupération du département {Id}", id);

            var department = await _departmentRepository.GetByIdAsync(id)
                             ?? throw new NotFoundException("Department", id);

            var bu = await _businessUnitRepository.GetByIdAsync(department.BusinessUnitId);
            var plant = await _plantRepository.GetByIdAsync(department.PlantId);

            return MapToDto(department, bu?.Name ?? "", plant?.Name ?? "");
        }

        public async Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentDto dto)
        {
            _logger.LogInformation("Mise à jour du département {Id}", id);

            var department = await _departmentRepository.GetByIdAsync(id)
                             ?? throw new NotFoundException("Department", id);

            var bu = await _businessUnitRepository.GetByIdAsync(dto.BusinessUnitId)
                     ?? throw new NotFoundException("BusinessUnit", dto.BusinessUnitId);
            var plant = await _plantRepository.GetByIdAsync(dto.PlantId)
                        ?? throw new NotFoundException("Plant", dto.PlantId);

            var existing = await _departmentRepository.FindAsync(
                d => d.Name == dto.Name && d.BusinessUnitId == dto.BusinessUnitId && d.Id != id);
            if (existing.Any())
            {
                _logger.LogWarning("Département {Name} existe déjà dans la BU {BUId}", dto.Name, dto.BusinessUnitId);
                throw new ConflictException("Department", dto.Name);
            }

            department.Name = dto.Name;
            department.BusinessUnitId = dto.BusinessUnitId;
            department.PlantId = dto.PlantId;
            department.UpdatedAt = DateTime.UtcNow;

            _departmentRepository.Update(department);
            await _departmentRepository.SaveChangesAsync();

            _logger.LogInformation("Département {DepartmentId} mis à jour avec succès", id);

            return MapToDto(department, bu.Name, plant.Name);
        }

        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("Suppression du département {Id}", id);

            var department = await _departmentRepository.GetByIdAsync(id)
                             ?? throw new NotFoundException("Department", id);

            var hasProjects = await _departmentRepository.FindAsync(
                d => d.Id == id && d.Projects.Any());
            if (hasProjects.Any())
            {
                _logger.LogWarning("Impossible de supprimer le département {Id} : projets actifs", id);
                throw new BadRequestException(
                    "This department cannot be deleted because it still contains active projects.");
            }

            _departmentRepository.Remove(department);
            await _departmentRepository.SaveChangesAsync();

            _logger.LogInformation("Département {Id} supprimé avec succès", id);
        }

        private static DepartmentDto MapToDto(Department d, string buName, string plantName) => new()
        {
            Id = d.Id,
            Name = d.Name,
            BusinessUnitId = d.BusinessUnitId,
            BusinessUnitName = buName,
            PlantId = d.PlantId,
            PlantName = plantName,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt
        };

        private static bool IsPlaceholderDepartment(string? name) =>
            string.Equals(name?.Trim(), PlaceholderDepartmentName, StringComparison.Ordinal);
    }
}
