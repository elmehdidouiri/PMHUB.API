using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
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
        private readonly IRepository<DeliverableBreakdown> _deliverableRepository;
        private readonly IRepository<DeliverableTask> _deliverableTaskRepository;
        private readonly IRepository<ProjectMember> _projectMemberRepository;
        private readonly IRepository<InternAllocation> _internAllocationRepository;
        private readonly IRepository<ProjectTimelineEntry> _timelineRepository;
        private readonly IRepository<ProjectRoadblock> _roadblockRepository;
        private readonly IRepository<Intern> _internRepository;
        private readonly IRepository<InternHourEntry> _internHourEntryRepository;
        private readonly ILogger<ProjectService> _logger;

        private readonly IRepository<Role> _roleRepository;
        private readonly IExcelExportService _excelExportService;

        public ProjectService(
            IProjectRepository projectRepository,
            IRepository<Department> departmentRepository,
            IRepository<BusinessUnit> businessUnitRepository,
            IRepository<Technology> technologyRepository,
            IRepository<SolutionDomain> solutionDomainRepository,
            IRepository<User> userRepository,
            IRepository<DeliverableBreakdown> deliverableRepository,
            IRepository<DeliverableTask> deliverableTaskRepository,
            IRepository<ProjectMember> projectMemberRepository,
            IRepository<InternAllocation> internAllocationRepository,
            IRepository<ProjectTimelineEntry> timelineRepository,
            IRepository<ProjectRoadblock> roadblockRepository,
            IRepository<Intern> internRepository,
            IRepository<InternHourEntry> internHourEntryRepository,
            IRepository<Role> roleRepository, 
            IExcelExportService excelExportService,
            ILogger<ProjectService> logger)
        {
            _projectRepository = projectRepository;
            _departmentRepository = departmentRepository;
            _businessUnitRepository = businessUnitRepository;
            _technologyRepository = technologyRepository;
            _solutionDomainRepository = solutionDomainRepository;
            _userRepository = userRepository;
            _deliverableRepository = deliverableRepository;
            _deliverableTaskRepository = deliverableTaskRepository;
            _projectMemberRepository = projectMemberRepository;
            _internAllocationRepository = internAllocationRepository;
            _timelineRepository = timelineRepository;
            _roadblockRepository = roadblockRepository;
            _internRepository = internRepository;
            _internHourEntryRepository = internHourEntryRepository;
            _roleRepository = roleRepository;
            _excelExportService = excelExportService;
            _logger = logger;


        }

        // ── CREATE ────────────────────────────────────────────
        public async Task<ProjectDto> CreateAsync(CreateFullProjectDto dto)
        {
            _logger.LogInformation("Création du projet {ProjectName}", dto.Name);

            ProjectValidator.ValidateForCreate(dto);

            var departmentIds = ResolveDepartmentIds(dto.DepartmentId, dto.DepartmentIds);
            var primaryDepartmentId = departmentIds.First();
            var projectManagementType = dto.ProjectManagementType
                ?? throw new BadRequestException("Project management type is required.");
            var projectType = dto.ProjectType
                ?? throw new BadRequestException("Project type is required.");

            var departments = await GetDepartmentsOrThrowAsync(departmentIds);

            var existing = await _projectRepository.FindAsync(
                p => p.Name == dto.Name &&
                    (departmentIds.Contains(p.DepartmentId) ||
                     p.ProjectDepartments.Any(pd => departmentIds.Contains(pd.DepartmentId))));
            if (existing.Any()) throw new ConflictException("Project", dto.Name);

            if (dto.ProjectManagerId.HasValue)
                await (_userRepository.GetByIdAsync(dto.ProjectManagerId.Value)
                    ?? throw new NotFoundException("User", dto.ProjectManagerId.Value));

            if (dto.ParentProjectId.HasValue)
            {
                var parentProject = await _projectRepository.GetByIdWithIncludesAsync(dto.ParentProjectId.Value);
                ProjectValidator.ValidateManagementType(dto, parentProject);
            }

            var project = BuildProject(dto, primaryDepartmentId, projectManagementType, projectType);

            SyncDepartments(project, departments.Select(d => d.Id));
            await AttachRelationsAsync(project, dto);

            await _projectRepository.AddAsync(project);
            await _projectRepository.SaveChangesAsync();
            await RecalculateProjectActualHoursAndProgressAsync(project.Id);

            _logger.LogInformation("Projet {ProjectId} créé avec succès", project.Id);

            var createdProject = await _projectRepository.GetByIdWithIncludesAsync(project.Id)
                ?? throw new NotFoundException("Project", project.Id);

            return ProjectMapper.ToDto(createdProject, departments.First());
        }

        // ── GET ALL ───────────────────────────────────────────
        public async Task<IEnumerable<ProjectSummaryDto>> GetAllAsync()
        {
            var projects = await _projectRepository.GetAllSummariesAsync();
            return projects.Select(ProjectMapper.ToSummaryDto);
        }

        public async Task<DashboardStatsDto> GetUserDashboardStatsAsync(Guid userId)
        {
            var projects = await _projectRepository.FindAsync(
                p => p.ProjectManagerId == userId || 
                     p.ProjectMembers.Any(m => m.UserId == userId));

            return CalculateDashboardStats(projects);
        }

        public async Task<DashboardStatsDto> GetAdminDashboardStatsAsync()
        {
            var projects = await _projectRepository.GetAllAsync();  
            return CalculateDashboardStats(projects);
        }

        private DashboardStatsDto CalculateDashboardStats(IEnumerable<Project> projects)
        {
            var projectList = projects.ToList();
            var stats = new DashboardStatsDto
            {
                TotalProjects = projectList.Count,
                AverageEffectiveness = (double)CalculateAverageEffectiveness(projectList),
                AverageOtd = (double)CalculateAverageOtd(projectList),
                DelayedProjects = projectList.Count(p => p.EstimatedDueDate.HasValue && p.EstimatedDueDate.Value.Date < DateTime.UtcNow.Date && p.Status != ProjectStatus.Done),
                DoneProjectsBelowTarget = projectList.Count(p => p.Status == ProjectStatus.Done && p.EstimatedHours > 0 && p.ActualHours < p.EstimatedHours),
                DoneProjectsAboveTarget = projectList.Count(p => p.Status == ProjectStatus.Done && p.EstimatedHours > 0 && p.ActualHours > p.EstimatedHours),
                ProjectsByPhase = projectList
                    .GroupBy(p => p.Phase.ToString())
                    .ToDictionary(g => g.Key, g => g.Count())
            };
            return stats;
        }

        private static decimal CalculateAverageEffectiveness(IEnumerable<Project> projects)
        {
            var scores = projects
                .Select(CalculateProjectEffectiveness)
                .ToList();

            return scores.Count == 0 ? 0m : Math.Round(scores.Average(), 2);
        }

        private static decimal CalculateProjectEffectiveness(Project project)
        {
            var effectivenessKpi = project.KPIs.FirstOrDefault(k =>
                string.Equals(k.Name, "Effectiveness", StringComparison.OrdinalIgnoreCase));
            var rawValue = effectivenessKpi?.CalculatedValue ??
                (effectivenessKpi?.CurrentValue > 0 ? effectivenessKpi.CurrentValue : project.ProgressPercentage);
            var target = effectivenessKpi?.TargetValue > 0
                ? effectivenessKpi.TargetValue
                : GetDefaultKpiTarget("Effectiveness");

            return CalculateTargetScore(rawValue, target);
        }

        private static decimal CalculateTargetScore(decimal rawValue, decimal target)
        {
            if (rawValue <= 0)
                return 0m;

            if (target <= 0)
                return NormalizePercentage(rawValue);

            return NormalizePercentage(rawValue * 100m / target);
        }

        private static decimal NormalizePercentage(decimal value)
        {
            if (value <= 0)
                return 0m;

            var normalized = value <= 1m ? value * 100m : value;
            return Math.Round(Math.Min(normalized, 100m), 2);
        }

        private static decimal CalculateAverageOtd(IEnumerable<Project> projects)
        {
            var now = DateTime.UtcNow.Date;
            var datedProjects = projects.Where(p => p.EstimatedDueDate.HasValue).ToList();
            if (!datedProjects.Any())
            {
                return 0m;
            }

            var onTimeProjects = datedProjects.Count(p =>
                (p.Status == ProjectStatus.Done && p.EndDate.HasValue && p.EndDate.Value.Date <= p.EstimatedDueDate!.Value.Date) ||
                (p.Status != ProjectStatus.Done && p.EstimatedDueDate!.Value.Date >= now));

            return Math.Round(onTimeProjects * 100m / datedProjects.Count, 2);
        }

        // ── GET PAGED ─────────────────────────────────────────
        public async Task<PaginatedResultDto<ProjectSummaryDto>> GetPagedAsync(
            ProjectSearchDto query)
        {
            var (items, totalCount) = await _projectRepository.GetPagedAsync(query);
            return new PaginatedResultDto<ProjectSummaryDto>
            {
                Data = items.Select(ProjectMapper.ToSummaryDto),
                PageNumber = query.PageNumber,
                PageSize = query.All ? totalCount : query.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<string> ExportProjectsAsync(ProjectSearchDto query)
        {
            var projects = await _projectRepository.GetFilteredAsync(query);
            var dtos = projects.Select(ProjectMapper.ToSummaryDto);
            return _excelExportService.GenerateProjectsExcel(dtos);
        }

        // ── GET BY ID ─────────────────────────────────────────
        public async Task<ProjectDto?> GetByIdAsync(Guid id)
        {
            var project = await _projectRepository.GetByIdWithIncludesAsync(id)
                ?? throw new NotFoundException("Project", id);

            return ProjectMapper.ToDto(project, null);
        }

        // ── UPDATE ────────────────────────────────────────────
        public async Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto dto)
        {
            _logger.LogInformation("Mise à jour du projet {ProjectId}", id);

            var project = await _projectRepository.GetByIdForUpdateAsync(id)
                ?? throw new NotFoundException("Project", id);

            ProjectValidator.ValidateForUpdate(id, dto);

            var departmentIds = ResolveDepartmentIds(dto.DepartmentId, dto.DepartmentIds);
            var departments = await GetDepartmentsOrThrowAsync(departmentIds);

            var existing = await _projectRepository.FindAsync(
                p => p.Name == dto.Name && p.Id != id &&
                    (departmentIds.Contains(p.DepartmentId) ||
                     p.ProjectDepartments.Any(pd => departmentIds.Contains(pd.DepartmentId))));
            if (existing.Any())
                throw new ConflictException("Project", dto.Name);

             if (dto.ProjectManagerId.HasValue)
                await (_userRepository.GetByIdAsync(dto.ProjectManagerId.Value)
                    ?? throw new NotFoundException("User", dto.ProjectManagerId.Value));

            project.Name = dto.Name;
            project.Description = dto.Description;
            SyncDepartments(project, departmentIds);
            project.Budget = dto.Budget.Value;
            project.StartDate = dto.StartDate.Value;
            project.EndDate = dto.EndDate;
            project.EstimatedDueDate = dto.EstimatedDueDate;
            project.Phase = dto.Phase.Value;
            project.Status = dto.Status.Value;
            project.ProcessStatus = dto.ProcessStatus;
            project.ProjectManagementType = dto.ProjectManagementType.Value;
            project.ProjectType = dto.ProjectType.Value;
            project.ParentProjectId = dto.ParentProjectId;
            project.ProjectManagerId = dto.ProjectManagerId;
            project.Sponsor = dto.Sponsor;
            project.DigitalContribution = dto.DigitalContribution;
            project.CostCenter = dto.CostCenter;
            project.CostSaving = dto.CostSaving;
            project.CodeSourceLink = dto.CodeSourceLink;
            project.SolutionLink = dto.SolutionLink;
            project.ServerHostName = dto.ServerHostName;
            project.CurrentState = dto.CurrentState;
            project.NextSteps = dto.NextSteps;
            project.Enhancements = dto.Enhancements;
            project.EstimatedHours = dto.EstimatedHours;
            project.UpdatedAt = DateTime.UtcNow;

            var incomingBusinessUnitIds = dto.BusinessUnitIds.ToHashSet();
            var businessUnitsToRemove = project.ProjectBusinessUnits
                .Where(pbu => !incomingBusinessUnitIds.Contains(pbu.BusinessUnitId))
                .ToList();
            foreach (var item in businessUnitsToRemove)
                project.ProjectBusinessUnits.Remove(item);
            foreach (var buId in incomingBusinessUnitIds)
            {
                await (_businessUnitRepository.GetByIdAsync(buId)
                    ?? throw new NotFoundException("BusinessUnit", buId));
                if (!project.ProjectBusinessUnits.Any(pbu => pbu.BusinessUnitId == buId))
                {
                    project.ProjectBusinessUnits.Add(new ProjectBusinessUnit { BusinessUnitId = buId });
                }
            }

            var incomingTechnologyIds = dto.TechnologyIds.ToHashSet();
            var technologiesToRemove = project.ProjectTechnologies
                .Where(pt => !incomingTechnologyIds.Contains(pt.TechnologyId))
                .ToList();
            foreach (var item in technologiesToRemove)
                project.ProjectTechnologies.Remove(item);
            foreach (var techId in incomingTechnologyIds)
            {
                await (_technologyRepository.GetByIdAsync(techId)
                    ?? throw new NotFoundException("Technology", techId));
                if (!project.ProjectTechnologies.Any(pt => pt.TechnologyId == techId))
                {
                    project.ProjectTechnologies.Add(new ProjectTechnology { TechnologyId = techId });
                }
            }

            var incomingSolutionDomainIds = dto.SolutionDomainIds.ToHashSet();
            var solutionDomainsToRemove = project.ProjectSolutionDomains
                .Where(psd => !incomingSolutionDomainIds.Contains(psd.SolutionDomainId))
                .ToList();
            foreach (var item in solutionDomainsToRemove)
                project.ProjectSolutionDomains.Remove(item);
            foreach (var sdId in incomingSolutionDomainIds)
            {
                await (_solutionDomainRepository.GetByIdAsync(sdId)
                    ?? throw new NotFoundException("SolutionDomain", sdId));
                if (!project.ProjectSolutionDomains.Any(psd => psd.SolutionDomainId == sdId))
                {
                    project.ProjectSolutionDomains.Add(new ProjectSolutionDomain { SolutionDomainId = sdId });
                }
            }

            await SyncMembersAsync(project, GetProjectMembers(dto));

            var incomingCriteriaByType = dto.StrategicCriteria
                .GroupBy(sc => sc.Type)
                .ToDictionary(g => g.Key, g => g.Last());

            var criteriaToRemove = project.StrategicCriteria
                .Where(sc => !incomingCriteriaByType.ContainsKey(sc.Type))
                .ToList();
            foreach (var item in criteriaToRemove)
                project.StrategicCriteria.Remove(item);

            foreach (var kvp in incomingCriteriaByType)
            {
                var criterionDto = kvp.Value;
                var existingCriterion = project.StrategicCriteria.FirstOrDefault(sc => sc.Type == criterionDto.Type);
                if (existingCriterion is null)
                {
                    project.StrategicCriteria.Add(new StrategicCriterion
                    {
                        Type = criterionDto.Type,
                        Score = criterionDto.Score,
                        Comment = criterionDto.Comment,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existingCriterion.Score = criterionDto.Score;
                    existingCriterion.Comment = criterionDto.Comment;
                    existingCriterion.UpdatedAt = DateTime.UtcNow;
                }
            }

            var incomingResources = GetProjectResources(dto).ToList();
            foreach (var item in project.ProjectResources.ToList())
                project.ProjectResources.Remove(item);

            foreach (var resourceDto in incomingResources)
            {
                project.ProjectResources.Add(new ProjectResource
                {
                    ItemName = resourceDto.ItemName,
                    PricePerUnit = resourceDto.PricePerUnit,
                    Quantity = resourceDto.Quantity,
                    CostCenter = resourceDto.CostCenter,
                    CreatedAt = DateTime.UtcNow
                });
            }

            SyncKpis(project, dto.KPIs, includeDefaults: true);

            try
            {
                await _projectRepository.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while updating project {ProjectId}", id);
                throw new ConflictException("The project was modified or deleted by another operation. Refresh and retry.");
            }
            _logger.LogInformation("Projet {ProjectId} mis à jour avec succès", id);


            await RecalculateProjectActualHoursAndProgressAsync(id);
            var updated = await _projectRepository.GetByIdWithIncludesAsync(id);
            return ProjectMapper.ToDto(updated!, departments.First());
        }

        // ── PATCH ─────────────────────────────────────────────
        public async Task<ProjectDto> PatchAsync(Guid id, PatchProjectDto dto)
        {
            var project = await _projectRepository.GetByIdForUpdateAsync(id)
                ?? throw new NotFoundException("Project", id);

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                var targetDepartmentIds = dto.DepartmentIds is not null || dto.DepartmentId.HasValue
                    ? ResolveDepartmentIds(dto.DepartmentId, dto.DepartmentIds)
                    : GetCurrentDepartmentIds(project);
                var existing = await _projectRepository.FindAsync(
                    p => p.Name == dto.Name && p.Id != id &&
                        (targetDepartmentIds.Contains(p.DepartmentId) ||
                         p.ProjectDepartments.Any(pd => targetDepartmentIds.Contains(pd.DepartmentId))));
                if (existing.Any())
                    throw new ConflictException("Project", dto.Name);

                project.Name = dto.Name;
            }

            if (dto.Description is not null)
                project.Description = dto.Description;

            if (dto.DepartmentIds is not null || dto.DepartmentId.HasValue)
            {
                var departmentIds = ResolveDepartmentIds(dto.DepartmentId, dto.DepartmentIds);
                await GetDepartmentsOrThrowAsync(departmentIds);
                SyncDepartments(project, departmentIds);
            }

            if (dto.Status.HasValue)
                project.Status = dto.Status.Value;

            if (dto.Phase.HasValue)
                project.Phase = dto.Phase.Value;

            ProjectValidator.ValidatePhaseStatus(project.Phase, project.Status);

            if (dto.ProcessStatus.HasValue)
                project.ProcessStatus = dto.ProcessStatus.Value;

            if (dto.ProjectManagementType.HasValue)
                project.ProjectManagementType = dto.ProjectManagementType.Value;

            if (dto.ProjectType.HasValue)
                project.ProjectType = dto.ProjectType.Value;

            if (dto.ParentProjectId.HasValue)
            {
                if (dto.ParentProjectId.Value == id)
                    throw new BadRequestException("A project cannot reference itself as its parent project.");

                await (_projectRepository.GetByIdAsync(dto.ParentProjectId.Value)
                    ?? throw new NotFoundException("ParentProject", dto.ParentProjectId.Value));
                project.ParentProjectId = dto.ParentProjectId.Value;
            }

            if (dto.CurrentState is not null)
                project.CurrentState = dto.CurrentState;

            if (dto.NextSteps is not null)
                project.NextSteps = dto.NextSteps;

            if (dto.Enhancements is not null)
                project.Enhancements = dto.Enhancements;

            if (dto.StartDate.HasValue)
                project.StartDate = dto.StartDate.Value;

            if (dto.EndDate.HasValue)
                project.EndDate = dto.EndDate.Value;

            if (dto.EstimatedDueDate.HasValue)
                project.EstimatedDueDate = dto.EstimatedDueDate.Value;

            if (project.EndDate.HasValue && project.EndDate <= project.StartDate)
                throw new BadRequestException("The project end date must be greater than the project start date.");

            if (project.EstimatedDueDate.HasValue && project.EstimatedDueDate <= project.StartDate)
                throw new BadRequestException("The estimated due date must be greater than the project start date.");

            if (dto.Budget.HasValue)
                project.Budget = dto.Budget.Value;

            if (dto.CostSaving.HasValue)
                project.CostSaving = dto.CostSaving.Value;

            if (dto.EstimatedHours.HasValue)
                project.EstimatedHours = dto.EstimatedHours.Value;

            if (dto.ActualHours.HasValue)
                project.ActualHours = dto.ActualHours.Value;

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

            if (dto.CostCenter is not null)
                project.CostCenter = dto.CostCenter;

            if (dto.ProgressPercentage.HasValue)
                project.ProgressPercentage = Math.Clamp(dto.ProgressPercentage.Value, 0, 100);

            if (dto.BusinessUnitIds is not null)
                await SyncBusinessUnitsAsync(project, dto.BusinessUnitIds);

            if (dto.TechnologyIds is not null)
                await SyncTechnologiesAsync(project, dto.TechnologyIds);

            if (dto.SolutionDomainIds is not null)
                await SyncSolutionDomainsAsync(project, dto.SolutionDomainIds);

            if (dto.Members is not null || dto.TeamMembers is not null)
                await SyncMembersAsync(project, GetProjectMembers(dto));
            else
                await RemoveProjectManagerFromTeamMembersAsync(project);

            if (dto.StrategicCriteria is not null)
                SyncStrategicCriteria(project, dto.StrategicCriteria);

            if (dto.ProjectResources is not null || dto.BudgetItems is not null)
                SyncProjectResources(project, GetProjectResources(dto));

            if (dto.KPIs is not null)
                SyncKpis(project, dto.KPIs, includeDefaults: true);

            project.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _projectRepository.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict while patching project {ProjectId}", id);
                throw new ConflictException("The project was modified or deleted by another operation. Refresh and retry.");
            }

            if (!dto.ActualHours.HasValue && !dto.ProgressPercentage.HasValue)
                await RecalculateProjectActualHoursAndProgressAsync(id);

            var updated = await _projectRepository.GetByIdWithIncludesAsync(id);
            return ProjectMapper.ToDto(updated!, null);
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

            var projects = await _projectRepository.FindSummariesAsync(
                p => p.DepartmentId == departmentId ||
                     p.ProjectDepartments.Any(pd => pd.DepartmentId == departmentId));
            return projects.Select(ProjectMapper.ToSummaryDto);
        }

        public async Task<IEnumerable<ProjectSummaryDto>> GetByBusinessUnitAsync(
            Guid businessUnitId)
        {
            await (_businessUnitRepository.GetByIdAsync(businessUnitId)
                ?? throw new NotFoundException("BusinessUnit", businessUnitId));

            var projects = await _projectRepository.FindSummariesAsync(
                p => p.ProjectBusinessUnits.Any(pbu => pbu.BusinessUnitId == businessUnitId));
            return projects.Select(ProjectMapper.ToSummaryDto);
        }

        public async Task<IEnumerable<ProjectSummaryDto>> GetByPlantAsync(Guid plantId)
        {
            var projects = await _projectRepository.FindSummariesAsync(
                p => (p.Department != null && p.Department.PlantId == plantId) ||
                     p.ProjectDepartments.Any(pd => pd.Department.PlantId == plantId));
            return projects.Select(ProjectMapper.ToSummaryDto);
        }

        public async Task<IEnumerable<ProjectSummaryDto>> GetByStatusAsync(ProjectStatus status)
        {
            var projects = await _projectRepository.FindSummariesAsync(
                p => p.Status == status);
            return projects.Select(ProjectMapper.ToSummaryDto);
        }

        public async Task<IEnumerable<ProjectSummaryDto>> GetByPhaseAsync(ProjectPhase phase)
        {
            var projects = await _projectRepository.FindSummariesAsync(
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
                    "The subproject end date must be greater than the start date.");

            ProjectValidator.ValidatePhaseStatus(dto.Phase, dto.Status);

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
                ProjectType = parent.ProjectType,
                DepartmentId = parent.DepartmentId,
                ParentProjectId = parentId,
                CreatedAt = DateTime.UtcNow
            };

            SyncDepartments(subProject, GetCurrentDepartmentIds(parent));

            await _projectRepository.AddAsync(subProject);
            await _projectRepository.SaveChangesAsync();

            var created = await _projectRepository.GetByIdWithIncludesAsync(subProject.Id);
            return ProjectMapper.ToDto(created!, null);
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

            if (project.ProjectManagerId.HasValue && project.ProjectManagerId.Value == userId)
                throw new BadRequestException("The project manager must not be added as a team member.");

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

        public async Task<IEnumerable<ProjectMemberDto>> GetMembersAsync(Guid projectId)
        {
            var project = await _projectRepository.GetByIdWithIncludesAsync(projectId)
                ?? throw new NotFoundException("Project", projectId);

            return project.ProjectMembers
                .Where(pm => !project.ProjectManagerId.HasValue || pm.UserId != project.ProjectManagerId.Value)
                .OrderBy(pm => pm.User.FirstName)
                .ThenBy(pm => pm.User.LastName)
                .Select(pm => new ProjectMemberDto
                {
                    ProjectMemberId = pm.Id,
                    UserId = pm.UserId,
                    FullName = $"{pm.User?.FirstName} {pm.User?.LastName}".Trim(),
                    Email = pm.User?.Email,
                    RoleId = pm.RoleId,
                    RoleName = pm.Role?.Name,
                    JoinedAt = pm.JoinedAt
                })
                .ToList();
        }

        public async Task RemoveMemberAsync(Guid projectId, Guid userId)
        {
            var project = await _projectRepository.GetByIdWithIncludesAsync(projectId)
                ?? throw new NotFoundException("Project", projectId);

            var member = project.ProjectMembers.FirstOrDefault(m => m.UserId == userId)
                ?? throw new NotFoundException("Member", userId);

            var tasksAssignedToMember = await _deliverableTaskRepository.FindAsync(
                t => t.ProjectMemberId == member.Id);

            foreach (var task in tasksAssignedToMember)
            {
                task.ProjectMemberId = null;
                task.UpdatedAt = DateTime.UtcNow;
                _deliverableTaskRepository.Update(task);
            }

            if (tasksAssignedToMember.Any())
                await _deliverableTaskRepository.SaveChangesAsync();

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

        public async Task<IEnumerable<DeliverableBreakdownDto>> GetDeliverablesAsync(Guid projectId)
        {
            var project = await GetProjectWithIncludesAsync(projectId);
            return project.Deliverables
                .OrderBy(d => d.CreatedAt)
                .Select(ProjectPlanningMapper.ToDto)
                .ToList();
        }

        public async Task<DeliverableBreakdownDto> AddDeliverableAsync(Guid projectId, CreateDeliverableBreakdownDto dto)
        {
            await EnsureProjectExistsAsync(projectId);

            var deliverable = new DeliverableBreakdown
            {
                ProjectId = projectId,
                Name = dto.Name,
                Description = dto.Description,
                Priority = dto.Priority,
                CreatedAt = DateTime.UtcNow
            };

            await _deliverableRepository.AddAsync(deliverable);
            await _deliverableRepository.SaveChangesAsync();
            await RecalculateProjectEstimatedHoursAsync(projectId);

            return await GetDeliverableDtoAsync(projectId, deliverable.Id);
        }

        public async Task<DeliverableBreakdownDto> UpdateDeliverableAsync(Guid projectId, Guid deliverableId, UpdateDeliverableBreakdownDto dto)
        {
            await EnsureProjectExistsAsync(projectId);

            var deliverable = await _deliverableRepository.GetByIdAsync(deliverableId)
                ?? throw new NotFoundException("DeliverableBreakdown", deliverableId);

            EnsureDeliverableBelongsToProject(deliverable, projectId);

            deliverable.Name = dto.Name;
            deliverable.Description = dto.Description;
            deliverable.Priority = dto.Priority;
            deliverable.UpdatedAt = DateTime.UtcNow;

            _deliverableRepository.Update(deliverable);
            await _deliverableRepository.SaveChangesAsync();
            await RecalculateProjectEstimatedHoursAsync(projectId);

            return await GetDeliverableDtoAsync(projectId, deliverableId);
        }

        public async Task DeleteDeliverableAsync(Guid projectId, Guid deliverableId)
        {
            await EnsureProjectExistsAsync(projectId);

            var deliverable = await _deliverableRepository.GetByIdAsync(deliverableId)
                ?? throw new NotFoundException("DeliverableBreakdown", deliverableId);

            EnsureDeliverableBelongsToProject(deliverable, projectId);

            _deliverableRepository.Remove(deliverable);
            await _deliverableRepository.SaveChangesAsync();
            await RecalculateProjectEstimatedHoursAsync(projectId);
        }

        public async Task<DeliverableTaskDto> AddDeliverableTaskAsync(Guid projectId, Guid deliverableId, CreateDeliverableTaskDto dto)
        {
            await EnsureProjectExistsAsync(projectId);
            var deliverable = await _deliverableRepository.GetByIdAsync(deliverableId)
                ?? throw new NotFoundException("DeliverableBreakdown", deliverableId);

            EnsureDeliverableBelongsToProject(deliverable, projectId);
            await ValidateDeliverableAssignmentAsync(projectId, dto.ProjectMemberId, dto.InternAllocationId);
            ValidateDeliverableTaskDates(dto.StartDate, dto.EndDate);
            ValidateDeliverableTaskEffort(dto.Category, dto.Hours, dto.DevHours, dto.UxHours, dto.TestingHours, dto.StoryPoints);

            var task = new DeliverableTask
            {
                DeliverableId = deliverableId,
                Name = dto.Name,
                Description = dto.Description,
                Category = dto.Category,
                StoryPoints = dto.StoryPoints,
                DevHours = dto.DevHours,
                UxHours = dto.UxHours,
                TestingHours = dto.TestingHours,
                Hours = dto.Hours,
                EstimatedHours = CalculateDeliverableTaskEstimatedHours(
                    dto.Category, dto.Hours, dto.DevHours, dto.UxHours, dto.TestingHours),
                Status = dto.Status,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Notes = dto.Notes,
                ProjectMemberId = dto.ProjectMemberId,
                InternAllocationId = dto.InternAllocationId,
                CreatedAt = DateTime.UtcNow
            };

            await _deliverableTaskRepository.AddAsync(task);
            await _deliverableTaskRepository.SaveChangesAsync();
            await RecalculateEstimatedHoursAsync(projectId, deliverableId);

            return await GetDeliverableTaskDtoAsync(projectId, deliverableId, task.Id);
        }

        public async Task<DeliverableTaskDto> UpdateDeliverableTaskAsync(Guid projectId, Guid deliverableId, Guid taskId, UpdateDeliverableTaskDto dto)
        {
            await EnsureProjectExistsAsync(projectId);

            var task = await _deliverableTaskRepository.GetByIdAsync(taskId)
                ?? throw new NotFoundException("DeliverableTask", taskId);

            if (task.DeliverableId != deliverableId)
                throw new BadRequestException("The selected task does not belong to the specified deliverable.");

            await ValidateDeliverableAssignmentAsync(projectId, dto.ProjectMemberId, dto.InternAllocationId);
            ValidateDeliverableTaskDates(dto.StartDate, dto.EndDate);
            ValidateDeliverableTaskEffort(dto.Category, dto.Hours, dto.DevHours, dto.UxHours, dto.TestingHours, dto.StoryPoints);

            var deliverable = await _deliverableRepository.GetByIdAsync(deliverableId)
                ?? throw new NotFoundException("DeliverableBreakdown", deliverableId);

            EnsureDeliverableBelongsToProject(deliverable, projectId);

            task.Name = dto.Name;
            task.Description = dto.Description;
            task.Category = dto.Category;
            task.StoryPoints = dto.StoryPoints;
            task.DevHours = dto.DevHours;
            task.UxHours = dto.UxHours;
            task.TestingHours = dto.TestingHours;
            task.Hours = dto.Hours;
            task.EstimatedHours = CalculateDeliverableTaskEstimatedHours(
                dto.Category, dto.Hours, dto.DevHours, dto.UxHours, dto.TestingHours);
            task.Status = dto.Status;
            task.StartDate = dto.StartDate;
            task.EndDate = dto.EndDate;
            task.Notes = dto.Notes;
            task.ProjectMemberId = dto.ProjectMemberId;
            task.InternAllocationId = dto.InternAllocationId;
            task.UpdatedAt = DateTime.UtcNow;

            _deliverableTaskRepository.Update(task);
            await _deliverableTaskRepository.SaveChangesAsync();
            await RecalculateEstimatedHoursAsync(projectId, deliverableId);

            return await GetDeliverableTaskDtoAsync(projectId, deliverableId, taskId);
        }

        public async Task DeleteDeliverableTaskAsync(Guid projectId, Guid deliverableId, Guid taskId)
        {
            await EnsureProjectExistsAsync(projectId);

            var task = await _deliverableTaskRepository.GetByIdAsync(taskId)
                ?? throw new NotFoundException("DeliverableTask", taskId);

            if (task.DeliverableId != deliverableId)
                throw new BadRequestException("The selected task does not belong to the specified deliverable.");

            var deliverable = await _deliverableRepository.GetByIdAsync(deliverableId)
                ?? throw new NotFoundException("DeliverableBreakdown", deliverableId);

            EnsureDeliverableBelongsToProject(deliverable, projectId);

            _deliverableTaskRepository.Remove(task);
            await _deliverableTaskRepository.SaveChangesAsync();
            await RecalculateEstimatedHoursAsync(projectId, deliverableId);
        }

        public async Task<IEnumerable<ProjectTimelineEntryDto>> GetTimelineAsync(Guid projectId)
        {
            var project = await GetProjectWithIncludesAsync(projectId);
            return project.TimelineEntries
                .OrderBy(t => t.PhaseOrder)
                .ThenBy(t => t.Date)
                .Select(ProjectPlanningMapper.ToDto)
                .ToList();
        }

        public async Task<ProjectTimelineEntryDto> AddTimelineEntryAsync(Guid projectId, CreateProjectTimelineEntryDto dto)
        {
            await EnsureProjectExistsAsync(projectId);
            await EnsureNormalUserExistsAsync(dto.SponsorUserId.Value);

            var entry = new ProjectTimelineEntry
            {
                ProjectId = projectId,
                Date = dto.Date.Value,
                Phase = dto.Phase.Value,
                PhaseOrder = dto.PhaseOrder.Value,
                PhaseLabel = dto.PhaseLabel,
                SponsorUserId = dto.SponsorUserId.Value,
                CreatedAt = DateTime.UtcNow
            };

            await _timelineRepository.AddAsync(entry);
            await _timelineRepository.SaveChangesAsync();

            return await GetTimelineEntryDtoAsync(projectId, entry.Id);
        }

        public async Task<ProjectTimelineEntryDto> UpdateTimelineEntryAsync(Guid projectId, Guid entryId, UpdateProjectTimelineEntryDto dto)
        {
            await EnsureProjectExistsAsync(projectId);
            await EnsureNormalUserExistsAsync(dto.SponsorUserId);

            var entry = await _timelineRepository.GetByIdAsync(entryId)
                ?? throw new NotFoundException("ProjectTimelineEntry", entryId);

            if (entry.ProjectId != projectId)
                throw new BadRequestException("The selected timeline entry does not belong to this project.");

            entry.Date = dto.Date.Value;
            entry.Phase = dto.Phase.Value;
            entry.PhaseOrder = dto.PhaseOrder.Value;
            entry.PhaseLabel = dto.PhaseLabel;
            entry.SponsorUserId = dto.SponsorUserId;
            entry.UpdatedAt = DateTime.UtcNow;

            _timelineRepository.Update(entry);
            await _timelineRepository.SaveChangesAsync();

            return await GetTimelineEntryDtoAsync(projectId, entryId);
        }

        public async Task DeleteTimelineEntryAsync(Guid projectId, Guid entryId)
        {
            await EnsureProjectExistsAsync(projectId);

            var entry = await _timelineRepository.GetByIdAsync(entryId)
                ?? throw new NotFoundException("ProjectTimelineEntry", entryId);

            if (entry.ProjectId != projectId)
                throw new BadRequestException("The selected timeline entry does not belong to this project.");

            _timelineRepository.Remove(entry);
            await _timelineRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<ProjectRoadblockDto>> GetRoadblocksAsync(Guid projectId)
        {
            var project = await GetProjectWithIncludesAsync(projectId);
            return project.RoadblockEntries
                .OrderByDescending(r => r.CreatedAt)
                .Select(ProjectPlanningMapper.ToDto)
                .ToList();
        }

        public async Task<IEnumerable<ProjectRoadblockDto>> GetDelayedRoadblocksAsync(Guid projectId)
        {
            return (await GetRoadblocksAsync(projectId))
                .Where(r => r.IsDelayed)
                .ToList();
        }

        public async Task<ProjectRoadblockDto> AddRoadblockAsync(Guid projectId, CreateProjectRoadblockDto dto)
        {
            await EnsureProjectExistsAsync(projectId);
            ValidateRoadblock(dto.Status, dto.DueAt.Value, dto.ResolvedAt);

            var roadblock = new ProjectRoadblock
            {
                ProjectId = projectId,
                Title = dto.Title,
                Description = dto.Description,
                Status = dto.Status,
                EnteredAt = dto.EnteredAt ?? DateTime.UtcNow,
                DueAt = dto.DueAt.Value,
                ResolvedAt = dto.ResolvedAt,
                CreatedAt = DateTime.UtcNow
            };

            await _roadblockRepository.AddAsync(roadblock);
            await _roadblockRepository.SaveChangesAsync();

            return await GetRoadblockDtoAsync(projectId, roadblock.Id);
        }

        public async Task<ProjectRoadblockDto> UpdateRoadblockAsync(Guid projectId, Guid roadblockId, UpdateProjectRoadblockDto dto)
        {
            await EnsureProjectExistsAsync(projectId);
            ValidateRoadblock(dto.Status, dto.DueAt.Value, dto.ResolvedAt);

            var roadblock = await _roadblockRepository.GetByIdAsync(roadblockId)
                ?? throw new NotFoundException("ProjectRoadblock", roadblockId);

            if (roadblock.ProjectId != projectId)
                throw new BadRequestException("The selected roadblock does not belong to this project.");

              roadblock.Title = dto.Title;
              roadblock.Description = dto.Description;
              roadblock.Status = dto.Status;
              roadblock.EnteredAt = dto.EnteredAt ?? roadblock.EnteredAt;
              if (roadblock.EnteredAt == default)
                  roadblock.EnteredAt = roadblock.CreatedAt;
              roadblock.DueAt = dto.DueAt.Value;
              roadblock.ResolvedAt = dto.ResolvedAt;
              roadblock.UpdatedAt = DateTime.UtcNow;

            _roadblockRepository.Update(roadblock);
            await _roadblockRepository.SaveChangesAsync();

            return await GetRoadblockDtoAsync(projectId, roadblockId);
        }

        public async Task DeleteRoadblockAsync(Guid projectId, Guid roadblockId)
        {
            await EnsureProjectExistsAsync(projectId);

            var roadblock = await _roadblockRepository.GetByIdAsync(roadblockId)
                ?? throw new NotFoundException("ProjectRoadblock", roadblockId);

            if (roadblock.ProjectId != projectId)
                throw new BadRequestException("The selected roadblock does not belong to this project.");

            _roadblockRepository.Remove(roadblock);
            await _roadblockRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<ProjectInternAllocationDto>> GetInternAllocationsAsync(Guid projectId)
        {
            var project = await GetProjectWithIncludesAsync(projectId);
            return project.InternAllocations
                .OrderByDescending(ia => ia.CreatedAt)
                .Select(ProjectInternMapper.ToDto)
                .ToList();
        }

        public async Task<ProjectInternAllocationDto> AddInternAllocationAsync(Guid projectId, CreateProjectInternAllocationDto dto)
        {
            await EnsureProjectExistsAsync(projectId);
            var intern = await _internRepository.GetByIdAsync(dto.InternId)
                ?? throw new NotFoundException("Intern", dto.InternId);

            var existing = await _internAllocationRepository.FindAsync(
                ia => ia.ProjectId == projectId && ia.InternId == dto.InternId);

            if (existing.Any())
                throw new ConflictException("InternAllocation", $"{projectId}-{dto.InternId}");

            var allocation = new InternAllocation
            {
                ProjectId = projectId,
                InternId = dto.InternId,
                AllocatedHours = dto.AllocatedHours,
                HoursWorked = 0m,
                AllocationDate = dto.AllocationDate ?? DateTime.UtcNow,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _internAllocationRepository.AddAsync(allocation);
            await _internAllocationRepository.SaveChangesAsync();

            return await GetInternAllocationDtoAsync(projectId, allocation.Id);
        }

        public async Task<ProjectInternAllocationDto> UpdateInternAllocationAsync(Guid projectId, Guid allocationId, UpdateProjectInternAllocationDto dto)
        {
            await EnsureProjectExistsAsync(projectId);
            var allocation = await _internAllocationRepository.GetByIdAsync(allocationId)
                ?? throw new NotFoundException("InternAllocation", allocationId);

            if (allocation.ProjectId != projectId)
                throw new BadRequestException("The selected intern allocation does not belong to this project.");

            if (dto.AllocatedHours < allocation.HoursWorked)
                throw new BadRequestException("Allocated hours cannot be lower than the hours that have already been booked.");

            allocation.AllocatedHours = dto.AllocatedHours;
            allocation.AllocationDate = dto.AllocationDate;
            allocation.Notes = dto.Notes;
            allocation.UpdatedAt = DateTime.UtcNow;

            _internAllocationRepository.Update(allocation);
            await _internAllocationRepository.SaveChangesAsync();

            return await GetInternAllocationDtoAsync(projectId, allocationId);
        }

        public async Task DeleteInternAllocationAsync(Guid projectId, Guid allocationId)
        {
            await EnsureProjectExistsAsync(projectId);
            var allocation = await _internAllocationRepository.GetByIdAsync(allocationId)
                ?? throw new NotFoundException("InternAllocation", allocationId);

            if (allocation.ProjectId != projectId)
                throw new BadRequestException("The selected intern allocation does not belong to this project.");

            var tasks = await _deliverableTaskRepository.FindAsync(t => t.InternAllocationId == allocationId);
            foreach (var task in tasks)
            {
                task.InternAllocationId = null;
                task.UpdatedAt = DateTime.UtcNow;
                _deliverableTaskRepository.Update(task);
            }

            if (tasks.Any())
                await _deliverableTaskRepository.SaveChangesAsync();

            _internAllocationRepository.Remove(allocation);
            await _internAllocationRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<InternHourEntryDto>> GetInternHourEntriesAsync(Guid projectId, Guid allocationId)
        {
            var allocation = await GetInternAllocationWithEntriesAsync(projectId, allocationId);
            return allocation.InternHourEntries
                .OrderByDescending(e => e.Date)
                .ThenByDescending(e => e.CreatedAt)
                .Select(ProjectInternMapper.ToDto)
                .ToList();
        }

        public async Task<InternHourEntryDto> AddInternHourEntryAsync(Guid projectId, Guid allocationId, CreateInternHourEntryDto dto, Guid bookedByUserId)
        {
            var allocation = await GetInternAllocationWithEntriesAsync(projectId, allocationId);
            await EnsureSupervisorCanBookInternHoursAsync(allocation, bookedByUserId);
            EnsureInternBookedHoursWithinAllocation(allocation, dto.Hours);

            var hourEntry = new InternHourEntry
            {
                InternAllocationId = allocationId,
                BookedByUserId = bookedByUserId,
                Date = dto.Date.Date,
                Hours = dto.Hours,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            await _internHourEntryRepository.AddAsync(hourEntry);
            await _internHourEntryRepository.SaveChangesAsync();
            await RecalculateInternAllocationHoursWorkedAsync(allocationId);

            return await GetInternHourEntryDtoAsync(projectId, allocationId, hourEntry.Id);
        }

        public async Task<InternHourEntryDto> UpdateInternHourEntryAsync(Guid projectId, Guid allocationId, Guid hourEntryId, UpdateInternHourEntryDto dto, Guid bookedByUserId)
        {
            var allocation = await GetInternAllocationWithEntriesAsync(projectId, allocationId);
            await EnsureSupervisorCanBookInternHoursAsync(allocation, bookedByUserId);

            var hourEntry = await _internHourEntryRepository.GetByIdAsync(hourEntryId)
                ?? throw new NotFoundException("InternHourEntry", hourEntryId);

            if (hourEntry.InternAllocationId != allocationId)
                throw new BadRequestException("The selected hour entry does not belong to this intern allocation.");

            var otherHours = allocation.InternHourEntries
                .Where(e => e.Id != hourEntryId)
                .Sum(e => e.Hours);

            if (otherHours + dto.Hours > allocation.AllocatedHours)
                throw new BadRequestException("Booked hours exceed the hours allocated to this intern on the project.");

            hourEntry.Date = dto.Date.Date;
            hourEntry.Hours = dto.Hours;
            hourEntry.Notes = dto.Notes;
            hourEntry.UpdatedAt = DateTime.UtcNow;

            _internHourEntryRepository.Update(hourEntry);
            await _internHourEntryRepository.SaveChangesAsync();
            await RecalculateInternAllocationHoursWorkedAsync(allocationId);

            return await GetInternHourEntryDtoAsync(projectId, allocationId, hourEntryId);
        }

        public async Task DeleteInternHourEntryAsync(Guid projectId, Guid allocationId, Guid hourEntryId, Guid bookedByUserId)
        {
            var allocation = await GetInternAllocationWithEntriesAsync(projectId, allocationId);
            await EnsureSupervisorCanBookInternHoursAsync(allocation, bookedByUserId);

            var hourEntry = await _internHourEntryRepository.GetByIdAsync(hourEntryId)
                ?? throw new NotFoundException("InternHourEntry", hourEntryId);

            if (hourEntry.InternAllocationId != allocationId)
                throw new BadRequestException("The selected hour entry does not belong to this intern allocation.");

            _internHourEntryRepository.Remove(hourEntry);
            await _internHourEntryRepository.SaveChangesAsync();
            await RecalculateInternAllocationHoursWorkedAsync(allocationId);
        }
        // ── HELPERS PRIVÉS ────────────────────────────────────
        private static IReadOnlyCollection<Guid> ResolveDepartmentIds(Guid? departmentId, IEnumerable<Guid>? departmentIds)
        {
            var normalizedIds = (departmentIds ?? Enumerable.Empty<Guid>())
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (normalizedIds.Count == 0 && departmentId.HasValue && departmentId.Value != Guid.Empty)
                normalizedIds.Add(departmentId.Value);

            if (normalizedIds.Count == 0)
                throw new BadRequestException("At least one department is required.");

            return normalizedIds;
        }

        private async Task<List<Department>> GetDepartmentsOrThrowAsync(IEnumerable<Guid> departmentIds)
        {
            var departments = new List<Department>();

            foreach (var departmentId in departmentIds.Distinct())
            {
                var department = await _departmentRepository.GetByIdAsync(departmentId)
                    ?? throw new NotFoundException("Department", departmentId);
                departments.Add(department);
            }

            return departments;
        }

        private static IReadOnlyCollection<Guid> GetCurrentDepartmentIds(Project project)
        {
            var departmentIds = project.ProjectDepartments
                .Select(pd => pd.DepartmentId)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (departmentIds.Count == 0 && project.DepartmentId != Guid.Empty)
                departmentIds.Add(project.DepartmentId);

            return departmentIds;
        }

        private static void SyncDepartments(Project project, IEnumerable<Guid> departmentIds)
        {
            var incomingDepartmentIds = departmentIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();

            if (incomingDepartmentIds.Count == 0)
                throw new BadRequestException("At least one department is required.");

            project.DepartmentId = incomingDepartmentIds.First();

            foreach (var item in project.ProjectDepartments
                .Where(pd => !incomingDepartmentIds.Contains(pd.DepartmentId))
                .ToList())
            {
                project.ProjectDepartments.Remove(item);
            }

            foreach (var departmentId in incomingDepartmentIds)
            {
                if (!project.ProjectDepartments.Any(pd => pd.DepartmentId == departmentId))
                {
                    project.ProjectDepartments.Add(new ProjectDepartment
                    {
                        DepartmentId = departmentId
                    });
                }
            }
        }

        private static Project BuildProject(
            CreateFullProjectDto dto,
            Guid departmentId,
            Category projectManagementType,
            ProjectType projectType) => new()
        {
            Name = dto.Name,
            Description = dto.Description,
            DepartmentId = departmentId,
            Budget = 0m,
            StartDate = DateTime.UtcNow.Date,
            EndDate = null,
            EstimatedDueDate = null,
            Phase = dto.Phase,
            Status = dto.Status,
            ProcessStatus = projectType == ProjectType.NewProcessProject
                ? dto.ProcessStatus
                : ProcessStatus.NotStarted,
            ProjectManagementType = projectManagementType,
            ProjectType = projectType,
            ParentProjectId = UsesParentProject(projectType)
                ? dto.ParentProjectId
                : null,
            ProjectManagerId = dto.ProjectManagerId,
            Sponsor = dto.Sponsor,
            DigitalContribution = 0m,
            CostCenter = dto.CostCenter,
            CostSaving = dto.CostSaving,
            ProgressPercentage = 0,
            CodeSourceLink = null,
            SolutionLink = null,
            ServerHostName = null,
            CurrentState = null,
            NextSteps = null,
            Enhancements = null,
            EstimatedHours = 0m,
            ActualHours = 0,
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

            foreach (var memberDto in NormalizeProjectMembers(GetProjectMembers(dto), project.ProjectManagerId))
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

            SyncKpis(project, dto.KPIs, includeDefaults: true);

        }

        private static bool UsesParentProject(ProjectType projectType) =>
            projectType is ProjectType.NewPhase or ProjectType.Extension or ProjectType.Sustain;

        private static IEnumerable<CreateProjectMemberDto> GetProjectMembers(CreateFullProjectDto dto) =>
            dto.TeamMembers.Any() ? dto.TeamMembers : dto.Members;

        private static IEnumerable<CreateProjectMemberDto> GetProjectMembers(UpdateProjectDto dto) =>
            dto.TeamMembers.Any() ? dto.TeamMembers : dto.Members;

        private static IEnumerable<CreateProjectMemberDto> GetProjectMembers(PatchProjectDto dto) =>
            dto.TeamMembers is { Count: > 0 } ? dto.TeamMembers : dto.Members ?? Enumerable.Empty<CreateProjectMemberDto>();

        private static IEnumerable<CreateProjectResourceDto> GetProjectResources(UpdateProjectDto dto) =>
            dto.BudgetItems.Any() ? dto.BudgetItems : dto.ProjectResources;

        private static IEnumerable<CreateProjectResourceDto> GetProjectResources(PatchProjectDto dto) =>
            dto.BudgetItems is { Count: > 0 } ? dto.BudgetItems : dto.ProjectResources ?? Enumerable.Empty<CreateProjectResourceDto>();

        private static IReadOnlyDictionary<string, decimal> DefaultKpiTargets { get; } =
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["OTD"] = 85m,
                ["Effectiveness"] = 85m,
                ["CSA"] = 85m,
                ["MonthlyWorkingHours"] = 161.5m
            };

        private static void SyncKpis(Project project, IEnumerable<CreateKpiDto>? kpis, bool includeDefaults)
        {
            var incomingByName = (kpis ?? Enumerable.Empty<CreateKpiDto>())
                .Where(k => !string.IsNullOrWhiteSpace(k.Name))
                .GroupBy(k => k.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);

            if (includeDefaults)
            {
                foreach (var defaultKpi in DefaultKpiTargets)
                {
                    incomingByName.TryAdd(defaultKpi.Key, new CreateKpiDto
                    {
                        Name = defaultKpi.Key,
                        TargetValue = defaultKpi.Value
                    });
                }
            }

            foreach (var item in project.KPIs.Where(k => !incomingByName.ContainsKey(k.Name)).ToList())
                project.KPIs.Remove(item);

            foreach (var kvp in incomingByName)
            {
                var kpiDto = kvp.Value;
                var name = kvp.Key;
                var targetValue = kpiDto.TargetValue > 0
                    ? kpiDto.TargetValue
                    : GetDefaultKpiTarget(name);

                var existingKpi = project.KPIs.FirstOrDefault(k =>
                    string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase));

                if (existingKpi is null)
                {
                    project.KPIs.Add(new KPI
                    {
                        Name = name,
                        TargetValue = targetValue,
                        CurrentValue = kpiDto.CurrentValue,
                        EstimatedDueDate = kpiDto.EstimatedDueDate,
                        ActualEndDate = kpiDto.ActualEndDate,
                        EstimatedHours = kpiDto.EstimatedHours,
                        ActualHours = kpiDto.ActualHours,
                        Description = kpiDto.Description,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existingKpi.Name = name;
                    existingKpi.TargetValue = targetValue;
                    existingKpi.CurrentValue = kpiDto.CurrentValue;
                    existingKpi.EstimatedDueDate = kpiDto.EstimatedDueDate;
                    existingKpi.ActualEndDate = kpiDto.ActualEndDate;
                    existingKpi.EstimatedHours = kpiDto.EstimatedHours;
                    existingKpi.ActualHours = kpiDto.ActualHours;
                    existingKpi.Description = kpiDto.Description;
                    existingKpi.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        private static decimal GetDefaultKpiTarget(string name)
        {
            return string.Equals(name, "CSAT", StringComparison.OrdinalIgnoreCase)
                ? DefaultKpiTargets.GetValueOrDefault("CSA", 0m)
                : DefaultKpiTargets.GetValueOrDefault(name, 0m);
        }

        private async Task SyncBusinessUnitsAsync(Project project, IEnumerable<Guid> businessUnitIds)
        {
            var incomingIds = businessUnitIds.ToHashSet();
            foreach (var item in project.ProjectBusinessUnits.Where(pbu => !incomingIds.Contains(pbu.BusinessUnitId)).ToList())
                project.ProjectBusinessUnits.Remove(item);

            foreach (var businessUnitId in incomingIds)
            {
                await (_businessUnitRepository.GetByIdAsync(businessUnitId)
                    ?? throw new NotFoundException("BusinessUnit", businessUnitId));

                if (!project.ProjectBusinessUnits.Any(pbu => pbu.BusinessUnitId == businessUnitId))
                    project.ProjectBusinessUnits.Add(new ProjectBusinessUnit { BusinessUnitId = businessUnitId });
            }
        }

        private async Task SyncTechnologiesAsync(Project project, IEnumerable<Guid> technologyIds)
        {
            var incomingIds = technologyIds.ToHashSet();
            foreach (var item in project.ProjectTechnologies.Where(pt => !incomingIds.Contains(pt.TechnologyId)).ToList())
                project.ProjectTechnologies.Remove(item);

            foreach (var technologyId in incomingIds)
            {
                await (_technologyRepository.GetByIdAsync(technologyId)
                    ?? throw new NotFoundException("Technology", technologyId));

                if (!project.ProjectTechnologies.Any(pt => pt.TechnologyId == technologyId))
                    project.ProjectTechnologies.Add(new ProjectTechnology { TechnologyId = technologyId });
            }
        }

        private async Task SyncSolutionDomainsAsync(Project project, IEnumerable<Guid> solutionDomainIds)
        {
            var incomingIds = solutionDomainIds.ToHashSet();
            foreach (var item in project.ProjectSolutionDomains.Where(psd => !incomingIds.Contains(psd.SolutionDomainId)).ToList())
                project.ProjectSolutionDomains.Remove(item);

            foreach (var solutionDomainId in incomingIds)
            {
                await (_solutionDomainRepository.GetByIdAsync(solutionDomainId)
                    ?? throw new NotFoundException("SolutionDomain", solutionDomainId));

                if (!project.ProjectSolutionDomains.Any(psd => psd.SolutionDomainId == solutionDomainId))
                    project.ProjectSolutionDomains.Add(new ProjectSolutionDomain { SolutionDomainId = solutionDomainId });
            }
        }

        private async Task SyncMembersAsync(Project project, IEnumerable<CreateProjectMemberDto> members)
        {
            var normalizedMembers = NormalizeProjectMembers(members, project.ProjectManagerId);
            var incomingMembersByUser = normalizedMembers
                .GroupBy(m => m.UserId)
                .ToDictionary(g => g.Key, g => g.Last());

            foreach (var item in project.ProjectMembers.Where(pm => !incomingMembersByUser.ContainsKey(pm.UserId)).ToList())
                project.ProjectMembers.Remove(item);

            foreach (var kvp in incomingMembersByUser)
            {
                var memberDto = kvp.Value;
                await (_userRepository.GetByIdAsync(memberDto.UserId)
                    ?? throw new NotFoundException("User", memberDto.UserId));
                await (_roleRepository.GetByIdAsync(memberDto.RoleId)
                    ?? throw new NotFoundException("Role", memberDto.RoleId));

                var existingMember = project.ProjectMembers.FirstOrDefault(pm => pm.UserId == memberDto.UserId);
                if (existingMember is null)
                {
                    project.ProjectMembers.Add(new ProjectMember
                    {
                        UserId = memberDto.UserId,
                        RoleId = memberDto.RoleId,
                        JoinedAt = DateTime.UtcNow
                    });
                }
                else if (existingMember.RoleId != memberDto.RoleId)
                {
                    existingMember.RoleId = memberDto.RoleId;
                    existingMember.UpdatedAt = DateTime.UtcNow;
                }
            }

            await RemoveProjectManagerFromTeamMembersAsync(project);
        }

        private static List<CreateProjectMemberDto> NormalizeProjectMembers(
            IEnumerable<CreateProjectMemberDto> members,
            Guid? projectManagerId)
        {
            var memberList = members.ToList();

            if (projectManagerId.HasValue && memberList.Any(m => m.UserId == projectManagerId.Value))
                throw new BadRequestException("The project manager must not be included as a team member.");

            return memberList;
        }

        private async Task RemoveProjectManagerFromTeamMembersAsync(Project project)
        {
            if (!project.ProjectManagerId.HasValue)
                return;

            var projectManagerMember = project.ProjectMembers
                .FirstOrDefault(pm => pm.UserId == project.ProjectManagerId.Value);

            if (projectManagerMember is null)
                return;

            var tasksAssignedToProjectManagerMember = await _deliverableTaskRepository.FindAsync(
                t => t.ProjectMemberId == projectManagerMember.Id);

            foreach (var task in tasksAssignedToProjectManagerMember)
            {
                task.ProjectMemberId = null;
                task.UpdatedAt = DateTime.UtcNow;
                _deliverableTaskRepository.Update(task);
            }

            if (tasksAssignedToProjectManagerMember.Any())
                await _deliverableTaskRepository.SaveChangesAsync();

            project.ProjectMembers.Remove(projectManagerMember);
        }

        private static void SyncStrategicCriteria(Project project, IEnumerable<CreateStrategicCriterionDto> strategicCriteria)
        {
            var incomingCriteriaByType = strategicCriteria
                .GroupBy(sc => sc.Type)
                .ToDictionary(g => g.Key, g => g.Last());

            foreach (var item in project.StrategicCriteria.Where(sc => !incomingCriteriaByType.ContainsKey(sc.Type)).ToList())
                project.StrategicCriteria.Remove(item);

            foreach (var kvp in incomingCriteriaByType)
            {
                var criterionDto = kvp.Value;
                var existingCriterion = project.StrategicCriteria.FirstOrDefault(sc => sc.Type == criterionDto.Type);
                if (existingCriterion is null)
                {
                    project.StrategicCriteria.Add(new StrategicCriterion
                    {
                        Type = criterionDto.Type,
                        Score = criterionDto.Score,
                        Comment = criterionDto.Comment,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existingCriterion.Score = criterionDto.Score;
                    existingCriterion.Comment = criterionDto.Comment;
                    existingCriterion.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        private static void SyncProjectResources(Project project, IEnumerable<CreateProjectResourceDto> resources)
        {
            foreach (var item in project.ProjectResources.ToList())
                project.ProjectResources.Remove(item);

            foreach (var resourceDto in resources)
            {
                project.ProjectResources.Add(new ProjectResource
                {
                    ItemName = resourceDto.ItemName,
                    PricePerUnit = resourceDto.PricePerUnit,
                    Quantity = resourceDto.Quantity,
                    CostCenter = resourceDto.CostCenter,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        private async Task<Project> GetProjectWithIncludesAsync(Guid projectId)
        {
            return await _projectRepository.GetByIdWithIncludesAsync(projectId)
                ?? throw new NotFoundException("Project", projectId);
        }

        private async Task<InternAllocation> GetInternAllocationWithEntriesAsync(Guid projectId, Guid allocationId)
        {
            var project = await GetProjectWithIncludesAsync(projectId);
            var allocation = project.InternAllocations.FirstOrDefault(ia => ia.Id == allocationId)
                ?? throw new NotFoundException("InternAllocation", allocationId);

            return allocation;
        }

        private async Task EnsureProjectExistsAsync(Guid projectId)
        {
            _ = await _projectRepository.GetByIdAsync(projectId)
                ?? throw new NotFoundException("Project", projectId);
        }

        private async Task EnsureNormalUserExistsAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId)
                ?? throw new NotFoundException("User", userId);

            if (user is not NormalUser)
                throw new BadRequestException("The selected sponsor must be a normal user.");
        }

        private static void EnsureDeliverableBelongsToProject(DeliverableBreakdown deliverable, Guid projectId)
        {
            if (deliverable.ProjectId != projectId)
                throw new BadRequestException("The selected deliverable does not belong to this project.");
        }

        private static void ValidateDeliverableTaskDates(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue && endDate.HasValue && endDate < startDate)
                throw new BadRequestException("The task end date must be greater than or equal to the task start date.");
        }

        private static void ValidateDeliverableTaskEffort(
            DeliverableTaskCategory category,
            decimal? hours,
            decimal? devHours,
            decimal? uxHours,
            decimal? testingHours,
            int? storyPoints)
        {
            if (category == DeliverableTaskCategory.Development)
            {
                if (hours.HasValue && hours.Value > 0)
                    throw new BadRequestException("The 'Hours' field is not allowed for development tasks. Use Dev, UX, and Testing hours instead.");

                if (!devHours.HasValue && !uxHours.HasValue && !testingHours.HasValue)
                    throw new BadRequestException("A development task must include at least one Dev, UX, or Testing effort.");

                if (storyPoints.HasValue && storyPoints.Value < 0)
                    throw new BadRequestException("Story points must be greater than or equal to zero.");

                return;
            }

            if (!hours.HasValue)
                throw new BadRequestException("The 'Hours' field is required for non-development task categories.");

            if ((devHours ?? 0) > 0 || (uxHours ?? 0) > 0 || (testingHours ?? 0) > 0)
                throw new BadRequestException("Dev, UX, and Testing hours can only be used for development tasks.");

            if ((storyPoints ?? 0) > 0)
                throw new BadRequestException("Story points can only be used for development tasks.");
        }

        private static decimal CalculateDeliverableTaskEstimatedHours(
            DeliverableTaskCategory category,
            decimal? hours,
            decimal? devHours,
            decimal? uxHours,
            decimal? testingHours)
        {
            if (category == DeliverableTaskCategory.Development)
            {
                return (devHours ?? 0m) + (uxHours ?? 0m) + (testingHours ?? 0m);
            }

            return hours ?? 0m;
        }

        private async Task ValidateDeliverableAssignmentAsync(Guid projectId, Guid? projectMemberId, Guid? internAllocationId)
        {
            if (projectMemberId.HasValue && internAllocationId.HasValue)
                throw new BadRequestException("A task can be assigned either to a project member or to an intern allocation, but not both.");

            if (projectMemberId.HasValue)
            {
                var projectMember = await _projectMemberRepository.GetByIdAsync(projectMemberId.Value)
                    ?? throw new NotFoundException("ProjectMember", projectMemberId.Value);

                if (projectMember.ProjectId != projectId)
                    throw new BadRequestException("The selected project member does not belong to this project.");
            }

            if (internAllocationId.HasValue)
            {
                var internAllocation = await _internAllocationRepository.GetByIdAsync(internAllocationId.Value)
                    ?? throw new NotFoundException("InternAllocation", internAllocationId.Value);

                if (internAllocation.ProjectId != projectId)
                    throw new BadRequestException("The selected intern allocation does not belong to this project.");
            }
        }

        private async Task EnsureSupervisorCanBookInternHoursAsync(InternAllocation allocation, Guid bookedByUserId)
        {
            if (allocation.Intern?.SupervisorId != bookedByUserId)
                throw new ForbiddenException("Only the intern's supervisor can book hours for this intern.");

            var user = await _userRepository.GetByIdAsync(bookedByUserId)
                ?? throw new NotFoundException("User", bookedByUserId);

            if (user is not NormalUser)
                throw new BadRequestException("Intern hours can only be booked by a normal user.");
        }

        private static void EnsureInternBookedHoursWithinAllocation(InternAllocation allocation, decimal requestedHours)
        {
            var totalHours = allocation.InternHourEntries.Sum(e => e.Hours) + requestedHours;
            if (totalHours > allocation.AllocatedHours)
                throw new BadRequestException("Booked hours exceed the hours allocated to this intern on the project.");
        }

        private static void ValidateRoadblock(RoadblockStatus status, DateTime dueAt, DateTime? resolvedAt)
        {
            if (status == RoadblockStatus.Resolved && !resolvedAt.HasValue)
                throw new BadRequestException("A resolved roadblock must include a resolution date.");

            if (status == RoadblockStatus.Open && resolvedAt.HasValue)
                throw new BadRequestException("An open roadblock cannot include a resolution date.");
        }

        private async Task RecalculateEstimatedHoursAsync(Guid projectId, Guid? deliverableId = null)
        {
            if (deliverableId.HasValue)
            {
                await RecalculateDeliverableEstimatedHoursAsync(deliverableId.Value);
            }

            await RecalculateProjectEstimatedHoursAsync(projectId);
        }

        private async Task RecalculateDeliverableEstimatedHoursAsync(Guid deliverableId)
        {
            var deliverableWithTasks = await GetDeliverableWithTasksAsync(deliverableId);
            var deliverable = await _deliverableRepository.GetByIdAsync(deliverableId)
                ?? throw new NotFoundException("DeliverableBreakdown", deliverableId);

            deliverable.EstimatedHours = deliverableWithTasks.Tasks.Sum(t => t.EstimatedHours);
            deliverable.UpdatedAt = DateTime.UtcNow;

            _deliverableRepository.Update(deliverable);
            await _deliverableRepository.SaveChangesAsync();
        }

        private async Task RecalculateProjectEstimatedHoursAsync(Guid projectId)
        {
            var projectWithIncludes = await GetProjectWithIncludesAsync(projectId);
            var totalEstimatedHours = projectWithIncludes.Deliverables
                .SelectMany(d => d.Tasks)
                .Sum(t => t.EstimatedHours);

            var project = await _projectRepository.GetByIdAsync(projectId)
                ?? throw new NotFoundException("Project", projectId);

            project.EstimatedHours = totalEstimatedHours;
            project.UpdatedAt = DateTime.UtcNow;

            _projectRepository.Update(project);
            await _projectRepository.SaveChangesAsync();
            await RecalculateProjectActualHoursAndProgressAsync(projectId);
        }

        private async Task RecalculateProjectActualHoursAndProgressAsync(Guid projectId)
        {
            var projectWithIncludes = await GetProjectWithIncludesAsync(projectId);
            var totalActualHours = projectWithIncludes.HourEntries.Sum(h => h.TotalHours);

            var project = await _projectRepository.GetByIdAsync(projectId)
                ?? throw new NotFoundException("Project", projectId);

            project.ActualHours = totalActualHours;
            project.ProgressPercentage = CalculateProjectProgressPercentage(totalActualHours, project.EstimatedHours);
            project.UpdatedAt = DateTime.UtcNow;

            _projectRepository.Update(project);
            await _projectRepository.SaveChangesAsync();
        }

        private static int CalculateProjectProgressPercentage(decimal actualHours, decimal estimatedHours)
        {
            if (estimatedHours <= 0)
            {
                return 0;
            }

            var percentage = Math.Round((actualHours / estimatedHours) * 100m, MidpointRounding.AwayFromZero);
            return (int)Math.Clamp(percentage, 0m, 100m);
        }

        private async Task RecalculateInternAllocationHoursWorkedAsync(Guid allocationId)
        {
            var project = await _projectRepository.FindWithIncludesAsync(
                p => p.InternAllocations.Any(ia => ia.Id == allocationId));

            var allocationWithEntries = project
                .SelectMany(p => p.InternAllocations)
                .FirstOrDefault(ia => ia.Id == allocationId)
                ?? throw new NotFoundException("InternAllocation", allocationId);

            var allocation = await _internAllocationRepository.GetByIdAsync(allocationId)
                ?? throw new NotFoundException("InternAllocation", allocationId);

            allocation.HoursWorked = allocationWithEntries.InternHourEntries.Sum(e => e.Hours);
            allocation.UpdatedAt = DateTime.UtcNow;

            _internAllocationRepository.Update(allocation);
            await _internAllocationRepository.SaveChangesAsync();
        }

        private async Task<DeliverableBreakdown> GetDeliverableWithTasksAsync(Guid deliverableId)
        {
            var project = await _projectRepository.FindWithIncludesAsync(
                p => p.Deliverables.Any(d => d.Id == deliverableId));

            var deliverable = project
                .SelectMany(p => p.Deliverables)
                .FirstOrDefault(d => d.Id == deliverableId);

            return deliverable ?? throw new NotFoundException("DeliverableBreakdown", deliverableId);
        }

        private async Task<DeliverableBreakdownDto> GetDeliverableDtoAsync(Guid projectId, Guid deliverableId)
        {
            var project = await GetProjectWithIncludesAsync(projectId);
            var deliverable = project.Deliverables.FirstOrDefault(d => d.Id == deliverableId)
                ?? throw new NotFoundException("DeliverableBreakdown", deliverableId);

            return ProjectPlanningMapper.ToDto(deliverable);
        }

        private async Task<DeliverableTaskDto> GetDeliverableTaskDtoAsync(Guid projectId, Guid deliverableId, Guid taskId)
        {
            var task = await _projectRepository.GetDeliverableTaskWithIncludesAsync(taskId)
                ?? throw new NotFoundException("DeliverableTask", taskId);

            if (task.DeliverableId != deliverableId)
                throw new BadRequestException("The selected task does not belong to the specified deliverable.");

            var deliverable = await _deliverableRepository.GetByIdAsync(deliverableId)
                ?? throw new NotFoundException("DeliverableBreakdown", deliverableId);

            EnsureDeliverableBelongsToProject(deliverable, projectId);

            return ProjectPlanningMapper.ToDto(task);
        }

        private async Task<ProjectTimelineEntryDto> GetTimelineEntryDtoAsync(Guid projectId, Guid entryId)
        {
            var entry = (await GetProjectWithIncludesAsync(projectId)).TimelineEntries
                .FirstOrDefault(t => t.Id == entryId)
                ?? throw new NotFoundException("ProjectTimelineEntry", entryId);

            return ProjectPlanningMapper.ToDto(entry);
        }

        private async Task<ProjectRoadblockDto> GetRoadblockDtoAsync(Guid projectId, Guid roadblockId)
        {
            var roadblock = (await GetProjectWithIncludesAsync(projectId)).RoadblockEntries
                .FirstOrDefault(r => r.Id == roadblockId)
                ?? throw new NotFoundException("ProjectRoadblock", roadblockId);

            return ProjectPlanningMapper.ToDto(roadblock);
        }

        private async Task<ProjectInternAllocationDto> GetInternAllocationDtoAsync(Guid projectId, Guid allocationId)
        {
            var allocation = await GetInternAllocationWithEntriesAsync(projectId, allocationId);
            return ProjectInternMapper.ToDto(allocation);
        }

        private async Task<InternHourEntryDto> GetInternHourEntryDtoAsync(Guid projectId, Guid allocationId, Guid hourEntryId)
        {
            var allocation = await GetInternAllocationWithEntriesAsync(projectId, allocationId);
            var entry = allocation.InternHourEntries.FirstOrDefault(e => e.Id == hourEntryId)
                ?? throw new NotFoundException("InternHourEntry", hourEntryId);

            return ProjectInternMapper.ToDto(entry);
        }
    }
}
