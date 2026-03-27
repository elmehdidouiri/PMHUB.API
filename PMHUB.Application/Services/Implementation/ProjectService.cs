using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Application.Mappings;
using PMHUB.Application.Validators;
using PMHUB.Domain.Entities;
using PMHUB.Domain.Enums;
using PMHUB.Infrastructure.Repositories;

namespace PMHUB.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IRepository<Department> _departmentRepository;
        private readonly IRepository<BusinessUnit> _businessUnitRepository;
        private readonly IRepository<Technology> _technologyRepository;
        private readonly IRepository<SolutionDomain> _solutionDomainRepository;
        private readonly IRepository<User> _userRepository;
        private readonly ILogger<ProjectService> _logger;

        private readonly IRepository<Role> _roleRepository;

        public ProjectService(
            IProjectRepository projectRepository,
            IRepository<Department> departmentRepository,
            IRepository<BusinessUnit> businessUnitRepository,
            IRepository<Technology> technologyRepository,
            IRepository<SolutionDomain> solutionDomainRepository,
            IRepository<User> userRepository,
            IRepository<Role> roleRepository, 
            ILogger<ProjectService> logger)
        {
            _projectRepository = projectRepository;
            _departmentRepository = departmentRepository;
            _businessUnitRepository = businessUnitRepository;
            _technologyRepository = technologyRepository;
            _solutionDomainRepository = solutionDomainRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _logger = logger;


        }

        // ── CREATE ────────────────────────────────────────────
        public async Task<ProjectDto> CreateAsync(CreateFullProjectDto dto)
        {
            _logger.LogInformation("Création du projet {ProjectName}", dto.Name);

            var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId)
                ?? throw new NotFoundException("Department", dto.DepartmentId);

            var existing = await _projectRepository.FindAsync(
                p => p.Name == dto.Name && p.DepartmentId == dto.DepartmentId);
            if (existing.Any()) throw new ConflictException("Project", dto.Name);

            ProjectValidator.ValidateDates(dto);

            var project = BuildProject(dto);

             await AttachRelationsAsync(project, dto);

            await _projectRepository.AddAsync(project);
            await _projectRepository.SaveChangesAsync();

            _logger.LogInformation("Projet {ProjectId} créé avec succès", project.Id);

            return ProjectMapper.ToDto(project, department);
        }

        // ── GET ALL ───────────────────────────────────────────
        public async Task<IEnumerable<ProjectSummaryDto>> GetAllAsync()
        {
            var projects = await _projectRepository.GetAllWithIncludesAsync();
            return projects.Select(ProjectMapper.ToSummaryDto);
        }

        // ── GET PAGED ─────────────────────────────────────────
        public async Task<PaginatedResultDto<ProjectSummaryDto>> GetPagedAsync(
            PaginationQueryDto query)
        {
            var (items, totalCount) = await _projectRepository.GetPagedAsync(query);
            return new PaginatedResultDto<ProjectSummaryDto>
            {
                Data = items.Select(ProjectMapper.ToSummaryDto),
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }

        // ── GET BY ID ─────────────────────────────────────────
        public async Task<ProjectDto?> GetByIdAsync(Guid id)
        {
            var project = await _projectRepository.GetByIdWithIncludesAsync(id)
                ?? throw new NotFoundException("Project", id);

            var department = await _departmentRepository.GetByIdAsync(project.DepartmentId);
            return ProjectMapper.ToDto(project, department);
        }

        // ── UPDATE ────────────────────────────────────────────
        public async Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto dto)
        {
            _logger.LogInformation("Mise à jour du projet {ProjectId}", id);

            var project = await _projectRepository.GetByIdWithIncludesAsync(id)
                ?? throw new NotFoundException("Project", id);

            var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId)
                ?? throw new NotFoundException("Department", dto.DepartmentId);

            var existing = await _projectRepository.FindAsync(
                p => p.Name == dto.Name && p.DepartmentId == dto.DepartmentId && p.Id != id);
            if (existing.Any())
                throw new ConflictException("Project", dto.Name);

            ProjectValidator.ValidateDates(dto);

             if (dto.ProjectManagerId.HasValue)
                await (_userRepository.GetByIdAsync(dto.ProjectManagerId.Value)
                    ?? throw new NotFoundException("User", dto.ProjectManagerId.Value));

            if (dto.ParentProjectId.HasValue && dto.ParentProjectId.Value == id)
                throw new BadRequestException(
                    "Un projet ne peut pas être son propre parent.");

            project.Name = dto.Name;
            project.Description = dto.Description;
            project.DepartmentId = dto.DepartmentId;
            project.Budget = dto.Budget;
            project.StartDate = dto.StartDate;
            project.EndDate = dto.EndDate;
            project.EstimatedDueDate = dto.EstimatedDueDate;
            project.Phase = dto.Phase;
            project.Status = dto.Status;
            project.ProcessStatus = dto.ProcessStatus;
            project.ProjectManagementType = dto.ProjectManagementType;
            project.ParentProjectId = dto.ParentProjectId;
            project.ProjectManagerId = dto.ProjectManagerId;
            project.Sponsor = dto.Sponsor;
            project.DigitalContribution = dto.DigitalContribution;
            project.CostCenter = dto.CostCenter;
            project.CostSaving = dto.CostSaving;
            project.ProgressPercentage = dto.ProgressPercentage;
            project.CodeSourceLink = dto.CodeSourceLink;
            project.SolutionLink = dto.SolutionLink;
            project.ServerHostName = dto.ServerHostName;
            project.CurrentState = dto.CurrentState;
            project.Roadblocks = dto.Roadblocks;
            project.NextSteps = dto.NextSteps;
            project.Enhancements = dto.Enhancements;
            project.EstimatedHours = dto.EstimatedHours;
            project.UpdatedAt = DateTime.UtcNow;

            project.ProjectBusinessUnits.Clear();
            foreach (var buId in dto.BusinessUnitIds)
            {
                await (_businessUnitRepository.GetByIdAsync(buId)
                    ?? throw new NotFoundException("BusinessUnit", buId));
                project.ProjectBusinessUnits.Add(
                    new ProjectBusinessUnit { BusinessUnitId = buId });
            }

            project.ProjectTechnologies.Clear();
            foreach (var techId in dto.TechnologyIds)
            {
                await (_technologyRepository.GetByIdAsync(techId)
                    ?? throw new NotFoundException("Technology", techId));
                project.ProjectTechnologies.Add(
                    new ProjectTechnology { TechnologyId = techId });
            }

            project.ProjectSolutionDomains.Clear();
            foreach (var sdId in dto.SolutionDomainIds)
            {
                await (_solutionDomainRepository.GetByIdAsync(sdId)
                    ?? throw new NotFoundException("SolutionDomain", sdId));
                project.ProjectSolutionDomains.Add(
                    new ProjectSolutionDomain { SolutionDomainId = sdId });
            }

            project.ProjectMembers.Clear();
            foreach (var memberDto in dto.Members)
            {
                await (_userRepository.GetByIdAsync(memberDto.UserId)
                    ?? throw new NotFoundException("User", memberDto.UserId));

                 await (_roleRepository.GetByIdAsync(memberDto.RoleId)
                    ?? throw new NotFoundException("Role", memberDto.RoleId));

                project.ProjectMembers.Add(new ProjectMember
                {
                    UserId = memberDto.UserId,
                    RoleId = memberDto.RoleId,
                    JoinedAt = DateTime.UtcNow
                });
            }

            project.StrategicCriteria.Clear();
            foreach (var criterionDto in dto.StrategicCriteria)
            {
                if (project.StrategicCriteria.Any(sc => sc.Type == criterionDto.Type))
                    throw new BadRequestException(
                        $"Le critère '{criterionDto.Type}' est déjà défini pour ce projet.");

                project.StrategicCriteria.Add(new StrategicCriterion
                {
                    Type = criterionDto.Type,
                    Score = criterionDto.Score,
                    Comment = criterionDto.Comment,
                    CreatedAt = DateTime.UtcNow
                });
            }
            project.StrategicScore = project.StrategicCriteria.Sum(sc => (int)sc.Score);

            _projectRepository.Update(project);
            await _projectRepository.SaveChangesAsync();
            _logger.LogInformation("Projet {ProjectId} mis à jour avec succès", id);


            var updated = await _projectRepository.GetByIdWithIncludesAsync(id);
            return ProjectMapper.ToDto(updated!, department);
        }

        // ── PATCH ─────────────────────────────────────────────
        public async Task<ProjectDto> PatchAsync(Guid id, PatchProjectDto dto)
        {
            var project = await _projectRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Project", id);

            if (dto.Status.HasValue)
                project.Status = dto.Status.Value;

            if (dto.ProcessStatus.HasValue)
                project.ProcessStatus = dto.ProcessStatus.Value;

            if (dto.ProgressPercentage.HasValue)
                project.ProgressPercentage = dto.ProgressPercentage.Value;

            if (dto.CurrentState is not null)
                project.CurrentState = dto.CurrentState;

            if (dto.Roadblocks is not null)
                project.Roadblocks = dto.Roadblocks;

            if (dto.NextSteps is not null)
                project.NextSteps = dto.NextSteps;

            if (dto.Enhancements is not null)
                project.Enhancements = dto.Enhancements;

            if (dto.EstimatedDueDate.HasValue)
                project.EstimatedDueDate = dto.EstimatedDueDate.Value;

            if (dto.Budget.HasValue)
                project.Budget = dto.Budget.Value;

            if (dto.CostSaving.HasValue)
                project.CostSaving = dto.CostSaving.Value;

            if (dto.Sponsor is not null)
                project.Sponsor = dto.Sponsor;

            if (dto.ProjectManagerId.HasValue)
            {
                await (_userRepository.GetByIdAsync(dto.ProjectManagerId.Value)
                    ?? throw new NotFoundException("User", dto.ProjectManagerId.Value));

                project.ProjectManagerId = dto.ProjectManagerId.Value;
            }

            if (dto.CodeSourceLink is not null)
                project.CodeSourceLink = dto.CodeSourceLink;

            if (dto.SolutionLink is not null)
                project.SolutionLink = dto.SolutionLink;

            if (dto.ServerHostName is not null)
                project.ServerHostName = dto.ServerHostName;

            project.UpdatedAt = DateTime.UtcNow;

            _projectRepository.Update(project);
            await _projectRepository.SaveChangesAsync();

            var updated = await _projectRepository.GetByIdWithIncludesAsync(id);
            var department = await _departmentRepository.GetByIdAsync(updated!.DepartmentId);
            return ProjectMapper.ToDto(updated, department);
        }

        // ── DELETE ────────────────────────────────────────────
        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("Suppression du projet {ProjectId}", id);

            var project = await _projectRepository.GetByIdAsync(id)
                ?? throw new NotFoundException("Project", id);

            var hasSubProjects = await _projectRepository.FindAsync(
                p => p.ParentProjectId == id);
            if (hasSubProjects.Any())
                throw new BadRequestException(
                    "Impossible de supprimer ce projet car il contient des sous-projets.");

            _projectRepository.Remove(project);
            await _projectRepository.SaveChangesAsync();
            _logger.LogInformation("Projet {ProjectId} supprimé avec succès", id);
        }

        // ── FILTRES ───────────────────────────────────────────
        public async Task<IEnumerable<ProjectSummaryDto>> GetByDepartmentAsync(Guid departmentId)
        {
            await (_departmentRepository.GetByIdAsync(departmentId)
                ?? throw new NotFoundException("Department", departmentId));

            var projects = await _projectRepository.FindWithIncludesAsync(
                p => p.DepartmentId == departmentId);
            return projects.Select(ProjectMapper.ToSummaryDto);
        }

        public async Task<IEnumerable<ProjectSummaryDto>> GetByBusinessUnitAsync(
            Guid businessUnitId)
        {
            await (_businessUnitRepository.GetByIdAsync(businessUnitId)
                ?? throw new NotFoundException("BusinessUnit", businessUnitId));

            var projects = await _projectRepository.FindWithIncludesAsync(
                p => p.ProjectBusinessUnits.Any(pbu => pbu.BusinessUnitId == businessUnitId));
            return projects.Select(ProjectMapper.ToSummaryDto);
        }

        public async Task<IEnumerable<ProjectSummaryDto>> GetByPlantAsync(Guid plantId)
        {
            var projects = await _projectRepository.FindWithIncludesAsync(
                p => p.Department != null && p.Department.PlantId == plantId);
            return projects.Select(ProjectMapper.ToSummaryDto);
        }

        public async Task<IEnumerable<ProjectSummaryDto>> GetByStatusAsync(ProjectStatus status)
        {
            var projects = await _projectRepository.FindWithIncludesAsync(
                p => p.Status == status);
            return projects.Select(ProjectMapper.ToSummaryDto);
        }

        public async Task<IEnumerable<ProjectSummaryDto>> GetByPhaseAsync(ProjectPhase phase)
        {
            var projects = await _projectRepository.FindWithIncludesAsync(
                p => p.Phase == phase);
            return projects.Select(ProjectMapper.ToSummaryDto);
        }

        // ── SOUS-PROJETS ──────────────────────────────────────
        public async Task<ProjectDto> AddSubProjectAsync(Guid parentId, CreateSubProjectDto dto)
        {
            var parent = await _projectRepository.GetByIdWithIncludesAsync(parentId)
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
                ProjectManagementType = parent.ProjectManagementType,
                DepartmentId = parent.DepartmentId,
                ParentProjectId = parentId,
                CreatedAt = DateTime.UtcNow
            };

            await _projectRepository.AddAsync(subProject);
            await _projectRepository.SaveChangesAsync();

            var department = await _departmentRepository.GetByIdAsync(parent.DepartmentId);
            var created = await _projectRepository.GetByIdWithIncludesAsync(subProject.Id);
            return ProjectMapper.ToDto(created!, department);
        }

        // ── MEMBRES ───────────────────────────────────────────
        public async Task AddMemberAsync(Guid projectId, Guid userId, Guid roleId)
        {
            var project = await _projectRepository.GetByIdWithIncludesAsync(projectId)
                ?? throw new NotFoundException("Project", projectId);

            await (_userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException("User", userId));

             await (_roleRepository.GetByIdAsync(roleId)
                ?? throw new NotFoundException("Role", roleId));

            if (project.ProjectMembers.Any(m => m.UserId == userId))
                throw new ConflictException("Member", userId);

            project.ProjectMembers.Add(new ProjectMember
            {
                UserId = userId,
                RoleId = roleId,
                JoinedAt = DateTime.UtcNow
            });

            _projectRepository.Update(project);
            await _projectRepository.SaveChangesAsync();
        }

        public async Task RemoveMemberAsync(Guid projectId, Guid userId)
        {
            var project = await _projectRepository.GetByIdWithIncludesAsync(projectId)
                ?? throw new NotFoundException("Project", projectId);

            var member = project.ProjectMembers.FirstOrDefault(m => m.UserId == userId)
                ?? throw new NotFoundException("Member", userId);

            project.ProjectMembers.Remove(member);
            _projectRepository.Update(project);
            await _projectRepository.SaveChangesAsync();
        }
        public async Task<IEnumerable<ProjectExportDto>> GetForExportAsync(DateTime startDate, DateTime endDate)
        {
            var projects = await _projectRepository.FindWithIncludesAsync(
                                p => p.HourEntries.Any(h => h.Date >= startDate && h.Date <= endDate));

            return projects.Select(p => new ProjectExportDto
            {
                Project = p.Name,
                Phase = p.Phase.ToString(),
                EstimatedHours = p.EstimatedHours,
 
                TotalBookingHoursJanuary = p.HourEntries
                    .Where(h => h.Date.Month == 1 && h.Date.Year == startDate.Year)
                    .Sum(h => h.TotalHours),
         
                TotalBookingHoursFebruary = p.HourEntries
                    .Where(h => h.Date.Month == 2 && h.Date.Year == startDate.Year)
                    .Sum(h => h.TotalHours),

                Department = p.Department?.Name ?? "N/A",
                Sponsor = p.Sponsor ?? "N/A",
                CostCenter = p.CostCenter ?? "N/A"
            }).ToList();
        }
        // ── HELPERS PRIVÉS ────────────────────────────────────
        private static Project BuildProject(CreateFullProjectDto dto) => new()
        {
            Name = dto.Name,
            Description = dto.Description,
            DepartmentId = dto.DepartmentId,
            Budget = dto.Budget,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            EstimatedDueDate = dto.EstimatedDueDate,
            Phase = dto.Phase,
            Status = dto.Status,
            ProcessStatus = dto.ProcessStatus,
            ProjectManagementType = dto.ProjectManagementType,
            ParentProjectId = dto.ParentProjectId,
            ProjectManagerId = dto.ProjectManagerId,
            Sponsor = dto.Sponsor,
            DigitalContribution = dto.DigitalContribution,
            CostCenter = dto.CostCenter,
            CostSaving = dto.CostSaving,
            ProgressPercentage = dto.ProgressPercentage,
            CodeSourceLink = dto.CodeSourceLink,
            SolutionLink = dto.SolutionLink,
            ServerHostName = dto.ServerHostName,
            CurrentState = dto.CurrentState,
            Roadblocks = dto.Roadblocks,
            NextSteps = dto.NextSteps,
            Enhancements = dto.Enhancements,
            EstimatedHours = dto.EstimatedHours,
            StrategicScore = 0,
            CreatedAt = DateTime.UtcNow
        };

        private async Task AttachRelationsAsync(Project project, CreateFullProjectDto dto)
        {
            foreach (var buId in dto.BusinessUnitIds)
            {
                await (_businessUnitRepository.GetByIdAsync(buId)
                    ?? throw new NotFoundException("BusinessUnit", buId));
                project.ProjectBusinessUnits.Add(
                    new ProjectBusinessUnit { BusinessUnitId = buId });
            }

            foreach (var techId in dto.TechnologyIds)
            {
                await (_technologyRepository.GetByIdAsync(techId)
                    ?? throw new NotFoundException("Technology", techId));
                project.ProjectTechnologies.Add(
                    new ProjectTechnology { TechnologyId = techId });
            }

            foreach (var sdId in dto.SolutionDomainIds)
            {
                await (_solutionDomainRepository.GetByIdAsync(sdId)
                    ?? throw new NotFoundException("SolutionDomain", sdId));
                project.ProjectSolutionDomains.Add(
                    new ProjectSolutionDomain { SolutionDomainId = sdId });
            }

            foreach (var memberDto in dto.Members)
            {
                await (_userRepository.GetByIdAsync(memberDto.UserId)
                    ?? throw new NotFoundException("User", memberDto.UserId));

                 await (_roleRepository.GetByIdAsync(memberDto.RoleId)
                    ?? throw new NotFoundException("Role", memberDto.RoleId));

                if (project.ProjectMembers.Any(m => m.UserId == memberDto.UserId))
                    throw new BadRequestException(
                        $"L'utilisateur '{memberDto.UserId}' est déjà membre du projet.");

                project.ProjectMembers.Add(new ProjectMember
                {
                    UserId = memberDto.UserId,
                    RoleId = memberDto.RoleId,
                    JoinedAt = DateTime.UtcNow
                });
            }

            foreach (var kpiDto in dto.KPIs)
                project.KPIs.Add(new KPI
                {
                    ProjectId = project.Id,
                    Name = kpiDto.Name,
                    TargetValue = kpiDto.TargetValue,
                    CurrentValue = kpiDto.CurrentValue,
                    EstimatedHours = kpiDto.EstimatedHours,
                    ActualHours = kpiDto.ActualHours,
                    EstimatedDueDate = kpiDto.EstimatedDueDate,
                    ActualEndDate = kpiDto.ActualEndDate,
                    Description = kpiDto.Description,
                    CreatedAt = DateTime.UtcNow
                });

            foreach (var resourceDto in dto.ProjectResources)
                project.ProjectResources.Add(new ProjectResource
                {
                    ItemName = resourceDto.ItemName,
                    PricePerUnit = resourceDto.PricePerUnit,
                    Quantity = resourceDto.Quantity,
                    CostCenter = resourceDto.CostCenter,
                    CreatedAt = DateTime.UtcNow
                });

            foreach (var criterionDto in dto.StrategicCriteria)
            {
                if (project.StrategicCriteria.Any(sc => sc.Type == criterionDto.Type))
                    throw new BadRequestException(
                        $"Le critère '{criterionDto.Type}' est déjà défini pour ce projet.");

                project.StrategicCriteria.Add(new StrategicCriterion
                {
                    ProjectId = project.Id,
                    Type = criterionDto.Type,
                    Score = criterionDto.Score,
                    Comment = criterionDto.Comment,
                    CreatedAt = DateTime.UtcNow
                });
            }

            project.StrategicScore = project.StrategicCriteria.Sum(sc => (int)sc.Score);
        }
    }
}