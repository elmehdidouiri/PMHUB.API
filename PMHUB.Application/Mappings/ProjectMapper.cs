using PMHUB.Application.DTOs;
using PMHUB.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            EstimatedDueDate = p.EstimatedDueDate,
            Budget = p.Budget,
            ProjectManager = p.ProjectManager,
            Sponsor = p.Sponsor,
            DigitalContribution = p.DigitalContribution,
            CostCenter = p.CostCenter,
            CostSaving = p.CostSaving,
            ProgressPercentage = p.ProgressPercentage,
            CodeSourceLink = p.CodeSourceLink,
            SolutionLink = p.SolutionLink,
            ServerHostName = p.ServerHostName,
            CurrentState = p.CurrentState,
            Roadblocks = p.Roadblocks,
            NextSteps = p.NextSteps,
            Enhancements = p.Enhancements,
            EstimatedHours = p.EstimatedHours,
            StrategicScore = p.StrategicScore,
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
            KPIs = p.KPIs.Select(k => new KpiDto
            {
                Id = k.Id,
                Name = k.Name,
                TargetValue = k.TargetValue,
                CurrentValue = k.CurrentValue,
                Description = k.Description,
                CreatedAt = k.CreatedAt,
                UpdatedAt = k.UpdatedAt
            }).ToList(),
            ProjectResources = p.ProjectResources.Select(r => new ProjectResourceDto
            {
                Id = r.Id,
                ItemName = r.ItemName,
                PricePerUnit = r.PricePerUnit,
                Quantity = r.Quantity,
                TotalCost = r.TotalCost,
                CostCenter = r.CostCenter,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList(),
            SubProjects = p.SubProjects.Select(ToSummaryDto).ToList()
        };

        public static ProjectSummaryDto ToSummaryDto(Project p) => new()
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
