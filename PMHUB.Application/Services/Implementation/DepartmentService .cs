using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using Task = System.Threading.Tasks.Task;

namespace PMHUB.Application.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IRepository<Department> _departmentRepository;
        private readonly IRepository<BusinessUnit> _businessUnitRepository;
        private readonly IRepository<Plant> _plantRepository;

        public DepartmentService(
            IRepository<Department> departmentRepository,
            IRepository<BusinessUnit> businessUnitRepository,
            IRepository<Plant> plantRepository)
        {
            _departmentRepository = departmentRepository;
            _businessUnitRepository = businessUnitRepository;
            _plantRepository = plantRepository;
        }

        public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto)
        {
             var bu = await _businessUnitRepository.GetByIdAsync(dto.BusinessUnitId)
                ?? throw new NotFoundException("BusinessUnit", dto.BusinessUnitId);
            var plant = await _plantRepository.GetByIdAsync(dto.PlantId)
                ?? throw new NotFoundException("Plant", dto.PlantId);
             var existing = await _departmentRepository.FindAsync(
                d => d.Name == dto.Name && d.BusinessUnitId == dto.BusinessUnitId);
            if (existing.Any())
                throw new ConflictException("Department", dto.Name);

            var department = new Department
            {
                Name = dto.Name,
                BusinessUnitId = dto.BusinessUnitId,
                PlantId = dto.PlantId,
                CreatedAt = DateTime.UtcNow
            };

            await _departmentRepository.AddAsync(department);
            await _departmentRepository.SaveChangesAsync();

            return MapToDto(department, bu.Name, plant.Name);
        }

        public async Task<IEnumerable<DepartmentDto>> GetAllAsync()
        {
            var departments = await _departmentRepository.GetAllAsync();
            return departments.Select(d => new DepartmentDto
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
            var department = await _departmentRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Department", id);

            var bu = await _businessUnitRepository.GetByIdAsync(department.BusinessUnitId);
            var plant = await _plantRepository.GetByIdAsync(department.PlantId);

            return MapToDto(department, bu?.Name ?? "", plant?.Name ?? "");
        }

        public async Task<DepartmentDto> UpdateAsync(Guid id, UpdateDepartmentDto dto)
        {
            var department = await _departmentRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Department", id);

            var bu = await _businessUnitRepository.GetByIdAsync(dto.BusinessUnitId)
                ?? throw new NotFoundException("BusinessUnit", dto.BusinessUnitId);

            var plant = await _plantRepository.GetByIdAsync(dto.PlantId)
                ?? throw new NotFoundException("Plant", dto.PlantId);

             var existing = await _departmentRepository.FindAsync(
                d => d.Name == dto.Name && d.BusinessUnitId == dto.BusinessUnitId && d.Id != id);
            if (existing.Any())
                throw new ConflictException("Department", dto.Name);

            department.Name = dto.Name;
            department.BusinessUnitId = dto.BusinessUnitId;
            department.PlantId = dto.PlantId;
            department.UpdatedAt = DateTime.UtcNow;

            _departmentRepository.Update(department);
            await _departmentRepository.SaveChangesAsync();

            return MapToDto(department, bu.Name, plant.Name);
        }

        public async Task DeleteAsync(Guid id)
        {
            var department = await _departmentRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Department", id);

             var hasProjects = await _departmentRepository.FindAsync(
                d => d.Id == id && d.Projects.Any());
            if (hasProjects.Any())
                throw new BadRequestException(
                    "Impossible de supprimer ce département car il contient des projets actifs.");

            _departmentRepository.Remove(department);
            await _departmentRepository.SaveChangesAsync();
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
    }
}