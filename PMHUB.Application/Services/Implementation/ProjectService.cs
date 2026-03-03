using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Domain.Entities;
using PMHUB.Domain.Enums;
using PMHUB.Infrastructure.Repositories;
using Task = System.Threading.Tasks.Task;

namespace PMHUB.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IRepository<Project> _projectRepository;
        private readonly IRepository<Department> _departmentRepository;
        private readonly IRepository<BusinessUnit> _businessUnitRepository;
        private readonly IRepository<Technology> _technologyRepository;
        private readonly IRepository<SolutionDomain> _solutionDomainRepository;
        private readonly IRepository<User> _userRepository;

        public ProjectService(
            IRepository<Project> projectRepository,
            IRepository<Department> departmentRepository,
            IRepository<BusinessUnit> businessUnitRepository,
            IRepository<Technology> technologyRepository,
            IRepository<SolutionDomain> solutionDomainRepository,
            IRepository<User> userRepository)
        {
            _projectRepository = projectRepository;
            _departmentRepository = departmentRepository;
            _businessUnitRepository = businessUnitRepository;
            _technologyRepository = technologyRepository;
            _solutionDomainRepository = solutionDomainRepository;
            _userRepository = userRepository;
        }

        public async Task<ProjectDto> CreateAsync(CreateFullProjectDto dto)
        {
             var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId)
                ?? throw new NotFoundException("Department", dto.DepartmentId);

             var existing = await _projectRepository.FindAsync(
                p => p.Name == dto.Name && p.DepartmentId == dto.DepartmentId);
            if (existing.Any())
                throw new ConflictException("Project", dto.Name);

             if (dto.EndDate.HasValue && dto.EndDate <= dto.StartDate)
                throw new BadRequestException(
                    "La date de fin doit être supérieure à la date de début.");

             if (dto.ParentProjectId.HasValue)
            {
                var parent = await _projectRepository.GetByIdAsync(dto.ParentProjectId.Value)
                    ?? throw new NotFoundException("ParentProject", dto.ParentProjectId.Value);
            }

             foreach (var buId in dto.BusinessUnitIds)
            {
                var bu = await _businessUnitRepository.GetByIdAsync(buId)
                    ?? throw new NotFoundException("BusinessUnit", buId);
            }

             foreach (var techId in dto.TechnologyIds)
            {
                var tech = await _technologyRepository.GetByIdAsync(techId)
                    ?? throw new NotFoundException("Technology", techId);
            }

             foreach (var sdId in dto.SolutionDomainIds)
            {
                var sd = await _solutionDomainRepository.GetByIdAsync(sdId)
                    ?? throw new NotFoundException("SolutionDomain", sdId);
            }

             foreach (var memberId in dto.MemberIds)
            {
                var member = await _userRepository.GetByIdAsync(memberId)
                    ?? throw new NotFoundException("User", memberId);
            }

             var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                DepartmentId = dto.DepartmentId,
                Budget = dto.Budget,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Phase = dto.Phase,
                Status = dto.Status,
                ParentProjectId = dto.ParentProjectId,
                CreatedAt = DateTime.UtcNow
            };

             foreach (var buId in dto.BusinessUnitIds)
                project.ProjectBusinessUnits.Add(new ProjectBusinessUnit
                {
                    BusinessUnitId = buId
                });

             foreach (var techId in dto.TechnologyIds)
                project.ProjectTechnologies.Add(new ProjectTechnology
                {
                    TechnologyId = techId
                });

             foreach (var sdId in dto.SolutionDomainIds)
                project.ProjectSolutionDomains.Add(new ProjectSolutionDomain
                {
                    SolutionDomainId = sdId
                });

             foreach (var subDto in dto.SubProjects)
            {
                if (subDto.EndDate.HasValue && subDto.EndDate <= subDto.StartDate)
                    throw new BadRequestException(
                        $"Le sous-projet '{subDto.Name}' a une date de fin invalide.");

                project.SubProjects.Add(new Project
                {
                    Name = subDto.Name,
                    Description = subDto.Description,
                    Budget = subDto.Budget,
                    StartDate = subDto.StartDate,
                    EndDate = subDto.EndDate,
                    Phase = subDto.Phase,
                    Status = subDto.Status,
                    DepartmentId = dto.DepartmentId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _projectRepository.AddAsync(project);
            await _projectRepository.SaveChangesAsync();

            return await MapToDtoAsync(project, department);
        }

        public async Task<IEnumerable<ProjectSummaryDto>> GetAllAsync()
        {
            var projects = await _projectRepository.GetAllAsync();
            return projects.Select(MapToSummaryDto);
        }

        public async Task<ProjectDto?> GetByIdAsync(Guid id)
        {
            var project = await _projectRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Project", id);

            var department = await _departmentRepository.GetByIdAsync(project.DepartmentId);
            return await MapToDtoAsync(project, department);
        }

        public async Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto dto)
        {
            var project = await _projectRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Project", id);

            var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId)
                ?? throw new NotFoundException("Department", dto.DepartmentId);

             var existing = await _projectRepository.FindAsync(
                p => p.Name == dto.Name && p.DepartmentId == dto.DepartmentId && p.Id != id);
            if (existing.Any())
                throw new ConflictException("Project", dto.Name);

             if (dto.EndDate.HasValue && dto.EndDate <= dto.StartDate)
                throw new BadRequestException(
                    "La date de fin doit être supérieure à la date de début.");

             if (dto.ParentProjectId.HasValue)
            {
                if (dto.ParentProjectId.Value == id)
                    throw new BadRequestException(
                        "Un projet ne peut pas être son propre parent.");

                var parent = await _projectRepository.GetByIdAsync(dto.ParentProjectId.Value)
                    ?? throw new NotFoundException("ParentProject", dto.ParentProjectId.Value);
            }
            project.Name = dto.Name;
            project.Description = dto.Description;
            project.DepartmentId = dto.DepartmentId;
            project.Budget = dto.Budget;
            project.StartDate = dto.StartDate;
            project.EndDate = dto.EndDate;
            project.Phase = dto.Phase;
            project.Status = dto.Status;
            project.ParentProjectId = dto.ParentProjectId;
            project.UpdatedAt = DateTime.UtcNow;

            
            project.ProjectBusinessUnits.Clear();
            foreach (var buId in dto.BusinessUnitIds)
            {
                var bu = await _businessUnitRepository.GetByIdAsync(buId)
                    ?? throw new NotFoundException("BusinessUnit", buId);
                project.ProjectBusinessUnits.Add(new ProjectBusinessUnit
                {
                    BusinessUnitId = buId
                });
            }

            
            project.ProjectTechnologies.Clear();
            foreach (var techId in dto.TechnologyIds)
            {
                var tech = await _technologyRepository.GetByIdAsync(techId)
                    ?? throw new NotFoundException("Technology", techId);
                project.ProjectTechnologies.Add(new ProjectTechnology
                {
                    TechnologyId = techId
                });
            }

             project.ProjectSolutionDomains.Clear();
            foreach (var sdId in dto.SolutionDomainIds)
            {
                var sd = await _solutionDomainRepository.GetByIdAsync(sdId)
                    ?? throw new NotFoundException("SolutionDomain", sdId);
                project.ProjectSolutionDomains.Add(new ProjectSolutionDomain
                {
                    SolutionDomainId = sdId
                });
            }

            _projectRepository.Update(project);
            await _projectRepository.SaveChangesAsync();

            return await MapToDtoAsync(project, department);
        }

        public async Task DeleteAsync(Guid id)
        {
            var project = await _projectRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Project", id);

             if (project.SubProjects.Any())
                throw new BadRequestException(
                    "Impossible de supprimer ce projet car il contient des sous-projets. Supprimez-les d'abord.");

             if (project.ProjectAllocations.Any())
                throw new BadRequestException(
                    "Impossible de supprimer ce projet car il contient des allocations actives.");

            _projectRepository.Remove(project);
            await _projectRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<ProjectSummaryDto>> GetByDepartmentAsync(Guid departmentId)
        {
            await (_departmentRepository.GetByIdAsync(departmentId)
                ?? throw new NotFoundException("Department", departmentId));

            var projects = await _projectRepository.FindAsync(
                p => p.DepartmentId == departmentId);
            return projects.Select(MapToSummaryDto);
        }

        public async Task<IEnumerable<ProjectSummaryDto>> GetByBusinessUnitAsync(Guid businessUnitId)
        {
            await (_businessUnitRepository.GetByIdAsync(businessUnitId)
                ?? throw new NotFoundException("BusinessUnit", businessUnitId));

            var projects = await _projectRepository.FindAsync(
                p => p.ProjectBusinessUnits.Any(pbu => pbu.BusinessUnitId == businessUnitId));
            return projects.Select(MapToSummaryDto);
        }

        public async Task<IEnumerable<ProjectSummaryDto>> GetByPlantAsync(Guid plantId)
        {
            var projects = await _projectRepository.FindAsync(
                p => p.Department != null && p.Department.PlantId == plantId);
            return projects.Select(MapToSummaryDto);
        }

        public async Task<IEnumerable<ProjectSummaryDto>> GetByStatusAsync(ProjectStatus status)
        {
            var projects = await _projectRepository.FindAsync(p => p.Status == status);
            return projects.Select(MapToSummaryDto);
        }

        public async Task<IEnumerable<ProjectSummaryDto>> GetByPhaseAsync(ProjectPhase phase)
        {
            var projects = await _projectRepository.FindAsync(p => p.Phase == phase);
            return projects.Select(MapToSummaryDto);
        }

        public async Task<ProjectDto> AddSubProjectAsync(Guid parentId, CreateSubProjectDto dto)
        {
            var parent = await _projectRepository.GetByIdAsync(parentId)
                ?? throw new NotFoundException("Project", parentId);

            if (dto.EndDate.HasValue && dto.EndDate <= dto.StartDate)
                throw new BadRequestException(
                    "La date de fin doit être supérieure à la date de début.");

            var subProject = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                Budget = dto.Budget,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Phase = dto.Phase,
                Status = dto.Status,
                DepartmentId = parent.DepartmentId,
                ParentProjectId = parentId,
                CreatedAt = DateTime.UtcNow
            };

            await _projectRepository.AddAsync(subProject);
            await _projectRepository.SaveChangesAsync();

            var department = await _departmentRepository.GetByIdAsync(parent.DepartmentId);
            return await MapToDtoAsync(subProject, department);
        }

        public async Task AddMemberAsync(Guid projectId, Guid userId)
        {
            var project = await _projectRepository.GetByIdAsync(projectId)
                ?? throw new NotFoundException("Project", projectId);

            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException("User", userId);

            if (project.Members.Any(m => m.Id == userId))
                throw new ConflictException("Member", userId);

            project.Members.Add(user);
            _projectRepository.Update(project);
            await _projectRepository.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(Guid projectId, Guid userId)
        {
            var project = await _projectRepository.GetByIdAsync(projectId)
                ?? throw new NotFoundException("Project", projectId);

            var member = project.Members.FirstOrDefault(m => m.Id == userId)
                ?? throw new NotFoundException("Member", userId);

            project.Members.Remove(member);
            _projectRepository.Update(project);
            await _projectRepository.SaveChangesAsync();
        }
        private static async Task<ProjectDto> MapToDtoAsync(Project p, Department? department)
        {
            return new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Status = p.Status,
                Phase = p.Phase,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Budget = p.Budget,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                DepartmentId = p.DepartmentId,
                DepartmentName = department?.Name ?? string.Empty,
                BusinessUnitName = department?.BusinessUnit?.Name ?? string.Empty,
                PlantName = department?.Plant?.Name ?? string.Empty,
                ParentProjectId = p.ParentProjectId,
                ParentProjectName = p.ParentProject?.Name,
                BusinessUnits = p.ProjectBusinessUnits
                    .Select(pbu => pbu.BusinessUnit?.Name ?? string.Empty).ToList(),
                Technologies = p.ProjectTechnologies
                    .Select(pt => pt.Technology?.Name ?? string.Empty).ToList(),
                SolutionDomains = p.ProjectSolutionDomains
                    .Select(psd => psd.SolutionDomain?.Name ?? string.Empty).ToList(),
                Members = p.Members
                    .Select(m => $"{m.FirstName} {m.LastName}").ToList(),
                SubProjects = p.SubProjects.Select(MapToSummaryDto).ToList()
            };
        }
        private static ProjectSummaryDto MapToSummaryDto(Project p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Status = p.Status,
            Phase = p.Phase,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Budget = p.Budget,
            DepartmentName = p.Department?.Name ?? string.Empty,
            PlantName = p.Department?.Plant?.Name ?? string.Empty
        };
    }
}