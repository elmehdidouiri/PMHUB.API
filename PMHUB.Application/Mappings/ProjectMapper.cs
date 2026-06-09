using PMHUB.Application.DTOs;
using PMHUB.Domain.Entities;
using PMHUB.Domain.Enums;

namespace PMHUB.Application.Mappings
{
    public static class ProjectMapper
    {
        private sealed record CompletionField(string Label, bool IsCompleted);

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
            DepartmentIds = GetProjectDepartments(p, department)
                .Select(d => d.Id)
                .ToList(),
            DepartmentName = string.Join(", ", GetProjectDepartments(p, department)
                .Select(d => d.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()),
            Departments = GetProjectDepartments(p, department)
                .Select(ToDepartmentDto)
                .ToList(),
            PlantName = string.Join(", ", GetProjectDepartments(p, department)
                .Select(d => d.Plant?.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()),
            ParentProjectId = p.ParentProjectId,
            ParentProjectName = p.ParentProject?.Name,
            BusinessUnits = p.ProjectBusinessUnits
                .Select(pbu => pbu.BusinessUnit?.Name ?? string.Empty).ToList(),
            Technologies = p.ProjectTechnologies
                .Select(pt => pt.Technology?.Name ?? string.Empty).ToList(),
            SolutionDomains = p.ProjectSolutionDomains
                .Select(psd => psd.SolutionDomain?.Name ?? string.Empty).ToList(),
             Members = p.ProjectMembers
                .Where(pm => !p.ProjectManagerId.HasValue || pm.UserId != p.ProjectManagerId.Value)
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

        public static ProjectSummaryDto ToSummaryDto(Project p)
        {
            var completionFields = GetCompletionFields(p).ToList();
            var missingFields = completionFields
                .Where(f => !f.IsCompleted)
                .Select(f => f.Label)
                .ToList();

            return new ProjectSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Status = p.Status.ToString(),
                Phase = p.Phase.ToString(),
                ProjectManagementType = p.ProjectManagementType,
                ProjectType = p.ProjectType.ToString(),
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                EstimatedDueDate = p.EstimatedDueDate,
                ProgressPercentage = p.ProgressPercentage,
                IsDelayed = p.EstimatedDueDate.HasValue &&
                    p.EstimatedDueDate.Value.Date < DateTime.UtcNow.Date &&
                    p.Status != ProjectStatus.Done,
                Budget = p.Budget,
                DepartmentIds = GetProjectDepartments(p, null)
                    .Select(d => d.Id)
                    .ToList(),
                DepartmentName = string.Join(", ", GetProjectDepartments(p, null)
                    .Select(d => d.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct()),
                DepartmentNames = GetProjectDepartments(p, null)
                    .Select(d => d.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct()
                    .ToList(),
                PlantName = string.Join(", ", GetProjectDepartments(p, null)
                    .Select(d => d.Plant?.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct()),
                Sponsor = p.Sponsor ?? string.Empty,
                CostCenter = p.CostCenter ?? string.Empty,
                EstimatedHours = p.EstimatedHours,
                ActualHours = p.ActualHours,
                TotalBookingHoursCurrentMonth = CalculateInternBookingHoursCurrentMonth(p),
                TotalBookingHoursFiscalYtd = CalculateInternBookingHoursFiscalYtd(p),
                FiscalYtdMonthlyBookingHours = CalculateInternBookingHoursByMonth(p),
                IsDataComplete = missingFields.Count == 0,
                DataCompletionPercentage = CalculateCompletionPercentage(completionFields),
                MissingFields = missingFields
            };
        }

        private static decimal CalculateInternBookingHoursCurrentMonth(Project project)
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            return SumInternBookingHours(project, monthStart, today);
        }

        private static decimal CalculateInternBookingHoursFiscalYtd(Project project)
        {
            var today = DateTime.Today;
            var fiscalYearStart = GetFiscalYearStart(today);

            return SumInternBookingHours(project, fiscalYearStart, today);
        }

        private static ICollection<ProjectMonthlyBookingHoursDto> CalculateInternBookingHoursByMonth(Project project)
        {
            return project.InternAllocations
                .SelectMany(allocation => allocation.InternHourEntries)
                .GroupBy(entry => new DateTime(entry.Date.Year, entry.Date.Month, 1))
                .Select(group => new ProjectMonthlyBookingHoursDto
                {
                    MonthStart = group.Key,
                    Hours = group.Sum(entry => entry.Hours)
                })
                .ToList();
        }

        private static DateTime GetFiscalYearStart(DateTime today)
        {
            return today.Month < 10
                ? new DateTime(today.Year - 1, 10, 1)
                : new DateTime(today.Year, 10, 1);
        }

        private static decimal SumInternBookingHours(Project project, DateTime startDate, DateTime endDate)
        {
            return project.InternAllocations
                .SelectMany(allocation => allocation.InternHourEntries)
                .Where(entry => entry.Date.Date >= startDate && entry.Date.Date <= endDate)
                .Sum(entry => entry.Hours);
        }

        private static IEnumerable<CompletionField> GetCompletionFields(Project p)
        {
            yield return new("Name", HasValue(p.Name));
            yield return new("Description", HasValue(p.Description));
            yield return new("Department", p.DepartmentId != Guid.Empty || p.ProjectDepartments.Any());
            yield return new("Budget", p.Budget > 0);
            yield return new("StartDate", p.StartDate != default);
            yield return new("EstimatedDueDate", p.EstimatedDueDate.HasValue);
            yield return new("EndDate", p.Status != ProjectStatus.Done || p.EndDate.HasValue);
            yield return new("ProjectManager", p.ProjectManagerId.HasValue);
            yield return new("Sponsor", HasValue(p.Sponsor));
            yield return new("DigitalContribution", p.DigitalContribution > 0);
            yield return new("CostCenter", HasValue(p.CostCenter));
            yield return new("CostSaving", p.CostSaving > 0);
            yield return new("CodeSourceLink", HasValue(p.CodeSourceLink));
            yield return new("SolutionLink", HasValue(p.SolutionLink));
            yield return new("ServerHostName", HasValue(p.ServerHostName));
            yield return new("CurrentState", HasValue(p.CurrentState));
            yield return new("NextSteps", HasValue(p.NextSteps));
            yield return new("Enhancements", HasValue(p.Enhancements));
            yield return new("EstimatedHours", p.EstimatedHours > 0);
            yield return new("BusinessUnits", p.ProjectBusinessUnits.Any());
            yield return new("Technologies", p.ProjectTechnologies.Any());
            yield return new("SolutionDomains", p.ProjectSolutionDomains.Any());
            yield return new("Members", p.ProjectMembers.Any());
            yield return new("ProjectResources", p.ProjectResources.Any());
            yield return new("StrategicCriteria", p.StrategicCriteria.Any());
            yield return new("KPIs", p.KPIs.Any());
        }

        private static bool HasValue(string? value) =>
            !string.IsNullOrWhiteSpace(value) &&
            !string.Equals(value.Trim(), "VIDE", StringComparison.OrdinalIgnoreCase);

        private static IEnumerable<Department> GetProjectDepartments(Project project, Department? fallbackDepartment)
        {
            var departments = project.ProjectDepartments
                .Select(pd => pd.Department)
                .Where(d => d is not null)
                .Cast<Department>()
                .ToList();

            if (departments.Count > 0)
                return departments;

            if (project.Department is not null)
                return new[] { project.Department };

            if (fallbackDepartment is not null)
                return new[] { fallbackDepartment };

            return Enumerable.Empty<Department>();
        }

        private static DepartmentDto ToDepartmentDto(Department department) => new()
        {
            Id = department.Id,
            Name = department.Name,
            BusinessUnitId = department.BusinessUnitId,
            BusinessUnitName = department.BusinessUnit?.Name ?? string.Empty,
            PlantId = department.PlantId,
            PlantName = department.Plant?.Name ?? string.Empty,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt
        };

        private static int CalculateCompletionPercentage(IReadOnlyCollection<CompletionField> fields)
        {
            if (fields.Count == 0)
                return 0;

            var completedCount = fields.Count(f => f.IsCompleted);
            return (int)Math.Round(completedCount * 100m / fields.Count, MidpointRounding.AwayFromZero);
        }
    }

}
