using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.Mappings;
using PMHUB.Application.Services;
using PMHUB.Application.Validators;
using PMHUB.Domain.Entities;
using PMHUB.Domain.Enums;
using PMHUB.Infrastructure.Repositories;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IRepository<Department> _departmentRepository;
    private readonly IRepository<BusinessUnit> _businessUnitRepository;
    private readonly IRepository<Technology> _technologyRepository;
    private readonly IRepository<SolutionDomain> _solutionDomainRepository;
    private readonly IRepository<User> _userRepository;

    public ProjectService(
        IProjectRepository projectRepository,
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

         ProjectValidator.ValidateDates(dto);

         Project? parentProject = null;
        if (dto.ParentProjectId.HasValue)
            parentProject = await _projectRepository.GetByIdWithIncludesAsync(
                dto.ParentProjectId.Value);

         ProjectValidator.ValidateManagementType(dto, parentProject);

         if (parentProject != null &&
            dto.ProjectManagementType is ProjectManagementType.NewPhase
                or ProjectManagementType.Extension)
        {
            ProjectValidator.InheritFromParent(dto, parentProject);
            department = await _departmentRepository.GetByIdAsync(dto.DepartmentId)
                ?? department;
        }

         var project = BuildProject(dto);

         await AttachRelationsAsync(project, dto);

        await _projectRepository.AddAsync(project);
        await _projectRepository.SaveChangesAsync();

         var created = await _projectRepository.GetByIdWithIncludesAsync(project.Id);
        return ProjectMapper.ToDto(created!, department);
    }

    public async Task<IEnumerable<ProjectSummaryDto>> GetAllAsync()
    {
        var projects = await _projectRepository.GetAllWithIncludesAsync();
        return projects.Select(ProjectMapper.ToSummaryDto);
    }

    public async Task<ProjectDto?> GetByIdAsync(Guid id)
    {
        var project = await _projectRepository.GetByIdWithIncludesAsync(id)
            ?? throw new NotFoundException("Project", id);

        var department = await _departmentRepository.GetByIdAsync(project.DepartmentId);
        return ProjectMapper.ToDto(project, department);
    }

    public async Task<ProjectDto> UpdateAsync(Guid id, UpdateProjectDto dto)
    {
        var project = await _projectRepository.GetByIdWithIncludesAsync(id)
            ?? throw new NotFoundException("Project", id);

        var department = await _departmentRepository.GetByIdAsync(dto.DepartmentId)
            ?? throw new NotFoundException("Department", dto.DepartmentId);

        var existing = await _projectRepository.FindAsync(
            p => p.Name == dto.Name && p.DepartmentId == dto.DepartmentId && p.Id != id);
        if (existing.Any())
            throw new ConflictException("Project", dto.Name);

        ProjectValidator.ValidateDates(dto);

        if (dto.ParentProjectId.HasValue)
        {
            if (dto.ParentProjectId.Value == id)
                throw new BadRequestException(
                    "Un projet ne peut pas être son propre parent.");
        }

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
        project.ProjectManager = dto.ProjectManager;
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
        project.StrategicScore = dto.StrategicScore;
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

        _projectRepository.Update(project);
        await _projectRepository.SaveChangesAsync();

        var updated = await _projectRepository.GetByIdWithIncludesAsync(id);
        return ProjectMapper.ToDto(updated!, department);
    }

    public async Task DeleteAsync(Guid id)
    {
        var project = await _projectRepository.GetByIdWithIncludesAsync(id)
            ?? throw new NotFoundException("Project", id);

        if (project.SubProjects.Any())
            throw new BadRequestException(
                "Impossible de supprimer ce projet car il contient des sous-projets.");

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

        var projects = await _projectRepository.FindWithIncludesAsync(
            p => p.DepartmentId == departmentId);
        return projects.Select(ProjectMapper.ToSummaryDto);
    }

    public async Task<IEnumerable<ProjectSummaryDto>> GetByBusinessUnitAsync(Guid businessUnitId)
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
        var projects = await _projectRepository.FindWithIncludesAsync(p => p.Status == status);
        return projects.Select(ProjectMapper.ToSummaryDto);
    }

    public async Task<IEnumerable<ProjectSummaryDto>> GetByPhaseAsync(ProjectPhase phase)
    {
        var projects = await _projectRepository.FindWithIncludesAsync(p => p.Phase == phase);
        return projects.Select(ProjectMapper.ToSummaryDto);
    }

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

    public async Task AddMemberAsync(Guid projectId, Guid userId)
    {
        var project = await _projectRepository.GetByIdWithIncludesAsync(projectId)
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
        var project = await _projectRepository.GetByIdWithIncludesAsync(projectId)
            ?? throw new NotFoundException("Project", projectId);

        var member = project.Members.FirstOrDefault(m => m.Id == userId)
            ?? throw new NotFoundException("Member", userId);

        project.Members.Remove(member);
        _projectRepository.Update(project);
        await _projectRepository.SaveChangesAsync();
    }

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
        ProjectManager = dto.ProjectManager,
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
        StrategicScore = dto.StrategicScore,
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

        foreach (var memberId in dto.MemberIds)
        {
            var member = await _userRepository.GetByIdAsync(memberId)
                ?? throw new NotFoundException("User", memberId);
            project.Members.Add(member);
        }

        foreach (var kpiDto in dto.KPIs)
            project.KPIs.Add(new KPI
            {
                Name = kpiDto.Name,
                TargetValue = kpiDto.TargetValue,
                CurrentValue = kpiDto.CurrentValue,
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
                ProjectManagementType = dto.ProjectManagementType,
                DepartmentId = dto.DepartmentId,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}