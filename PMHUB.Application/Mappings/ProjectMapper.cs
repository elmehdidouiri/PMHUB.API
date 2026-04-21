using PMHUB.Application.DTOs;
using PMHUB.Domain.Entities;

namespace PMHUB.Application.Mappings
{
    public static class ProjectMapper
    {
        public static ProjectDto ToDto(Project p, Department? department) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Status = p.Status,

            Phase = p.Phase,
            ProcessStatus = p.ProcessStatus,
            ProjectManagementType = p.ProjectManagementType,
            ProjectType = p.ProjectType,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            EstimatedDueDate = p.EstimatedDueDate,
            Budget = p.Budget,
            ProjectManagerId = p.ProjectManagerId,
            ProjectManagerName = p.ProjectManager != null
            ? $"{p.ProjectManager.FirstName} {p.ProjectManager.LastName}"
                             : null,
            Sponsor = p.Sponsor,
            DigitalContribution = p.DigitalContribution,
            CostCenter = p.CostCenter,
            CostSaving = p.CostSaving,
            ProgressPercentage = p.ProgressPercentage,
            ActualHours = p.ActualHours,
            CodeSourceLink = p.CodeSourceLink,
            SolutionLink = p.SolutionLink,
            ServerHostName = p.ServerHostName,
            CurrentState = p.CurrentState,
            NextSteps = p.NextSteps,
            Enhancements = p.Enhancements,
            EstimatedHours = p.EstimatedHours,
            StrategicScore = p.StrategicCriteria.Sum(sc => (decimal)(int)sc.Score),
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            DepartmentId = p.DepartmentId,
            DepartmentName = p.Department?.Name ?? department?.Name ?? string.Empty,
            PlantName = p.Department?.Plant?.Name
                ?? department?.Plant?.Name
                ?? string.Empty,
            ParentProjectId = p.ParentProjectId,
            ParentProjectName = p.ParentProject?.Name,
            BusinessUnits = p.ProjectBusinessUnits
                .Select(pbu => pbu.BusinessUnit?.Name ?? string.Empty).ToList(),
            Technologies = p.ProjectTechnologies
                .Select(pt => pt.Technology?.Name ?? string.Empty).ToList(),
            SolutionDomains = p.ProjectSolutionDomains
                .Select(psd => psd.SolutionDomain?.Name ?? string.Empty).ToList(),
             Members = p.ProjectMembers
                .Select(pm => new ProjectMemberDto
                {
                    ProjectMemberId = pm.Id,
                    UserId = pm.UserId,
                    FullName = $"{pm.User?.FirstName} {pm.User?.LastName}",
                    Email = pm.User?.Email,
                    RoleId = pm.RoleId,
                    RoleName = pm.Role?.Name,
                    JoinedAt = pm.JoinedAt
                }).ToList(),
            InternMembers = p.InternAllocations
                .OrderByDescending(ia => ia.CreatedAt)
                .Select(ProjectInternMapper.ToDto)
                .ToList(),
             KPIs = p.KPIs.Select(k => new KpiDto
            {
                Id = k.Id,
                Name = k.Name,
                TargetValue = k.TargetValue,
                CurrentValue = k.CurrentValue,
                CalculatedValue = k.CalculatedValue,
                EstimatedHours = k.EstimatedHours,
                ActualHours = k.ActualHours,
                EstimatedDueDate = k.EstimatedDueDate,
                ActualEndDate = k.ActualEndDate,
                Description = k.Description,
                CreatedAt = k.CreatedAt,
                UpdatedAt = k.UpdatedAt
            }).ToList(),
            ProjectResources = p.ProjectResources.Select(r => new ProjectResourceDto
            {
                Id = r.Id,
                ProjectId = p.Id,
                ItemName = r.ItemName,
                PricePerUnit = r.PricePerUnit,
                Quantity = r.Quantity,
                TotalCost = r.TotalCost,
                CostCenter = r.CostCenter,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList(),
            StrategicCriteria = p.StrategicCriteria.Select(sc => new StrategicCriterionDto
            {
                Id = sc.Id,
                Type = sc.Type,
                Score = sc.Score,
                Comment = sc.Comment,
                CreatedAt = sc.CreatedAt,
                UpdatedAt = sc.UpdatedAt
            }).ToList(),
            Deliverables = p.Deliverables
                .OrderBy(d => d.CreatedAt)
                .Select(ProjectPlanningMapper.ToDto)
                .ToList(),
            TimelineEntries = p.TimelineEntries
                .OrderBy(t => t.PhaseOrder)
                .ThenBy(t => t.Date)
                .Select(ProjectPlanningMapper.ToDto)
                .ToList(),
            RoadblockEntries = p.RoadblockEntries
                .OrderByDescending(r => r.CreatedAt)
                .Select(ProjectPlanningMapper.ToDto)
                .ToList(),
            SubProjects = p.SubProjects.Select(ToSummaryDto).ToList()
        };

        public static ProjectSummaryDto ToSummaryDto(Project p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Status = p.Status,
            Phase = p.Phase,
            ProjectType = p.ProjectType,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Budget = p.Budget,
            DepartmentName = p.Department?.Name ?? string.Empty,
            PlantName = p.Department?.Plant?.Name ?? string.Empty,
            Sponsor = p.Sponsor ?? string.Empty,
            EstimatedHours = p.EstimatedHours,
            ActualHours = p.ActualHours
        };
    }

}
