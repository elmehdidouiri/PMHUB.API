using Microsoft.EntityFrameworkCore;
using PMHUB.Application.DTOs;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using PMHUB.Domain.Enums;
using PMHUB.Infrastructure.Persistence;
using PMHUB.Shared.Helpers;
using PMHUB.Shared.Models;
using Microsoft.Extensions.Options;

namespace PMHUB.Infrastructure.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private const string PlaceholderDepartmentName = "-";

        private readonly PMHubDbContext _context;
        private readonly ITargetSettingsService _targetSettingsService;
        private CompanyStandards _standards;

        public DashboardRepository(PMHubDbContext context, IOptions<CompanyStandards> standards, ITargetSettingsService targetSettingsService)
        {
            _context = context;
            _standards = standards.Value;
            _targetSettingsService = targetSettingsService;
        }

        public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(DashboardQueryDto query, bool isAdminScope, Guid? userId = null)
        {
            await LoadTargetSettingsAsync();

            var topN = query.TopN <= 0 ? 5 : Math.Min(query.TopN, 20);
            var now = DateTime.UtcNow.Date;

            var projectsQuery = BuildScopedProjectsQuery(query, isAdminScope, userId);
            var projectIdsQuery = projectsQuery.Select(p => p.Id);

            var allHoursScopedQuery = _context.HourEntries
                .AsNoTracking()
                .Where(h => h.ProjectId.HasValue && projectIdsQuery.Contains(h.ProjectId.Value));

            var trackedHoursQuery = ApplyYearMonthFilter(allHoursScopedQuery, query.Year, query.Month);
            var ytdHoursQuery = ApplyYtdFilter(allHoursScopedQuery, query.Year, query.Month);

            var totalProjects = await projectsQuery.CountAsync();
            var totalEstimatedHours = await projectsQuery.SumAsync(p => (decimal?)p.EstimatedHours) ?? 0m;
            var totalTrackedHours = await trackedHoursQuery.SumAsync(h => (decimal?)h.TotalHours) ?? 0m;
            var ytdHours = await ytdHoursQuery.SumAsync(h => (decimal?)h.TotalHours) ?? 0m;
            var delayedProjects = await projectsQuery.CountAsync(p =>
                p.EstimatedDueDate.HasValue &&
                p.EstimatedDueDate.Value < now &&
                p.Status != ProjectStatus.Done);
            var doneProjectsBelowTarget = await projectsQuery.CountAsync(p =>
                p.Status == ProjectStatus.Done &&
                p.EstimatedHours > 0 &&
                p.ActualHours < p.EstimatedHours);
            var doneProjectsAboveTarget = await projectsQuery.CountAsync(p =>
                p.Status == ProjectStatus.Done &&
                p.EstimatedHours > 0 &&
                p.ActualHours > p.EstimatedHours);

            var otdDenominator = await projectsQuery.CountAsync(p => p.EstimatedDueDate.HasValue);
            var otdNumerator = await projectsQuery.CountAsync(p =>
                p.EstimatedDueDate.HasValue &&
                (
                    (p.Status == ProjectStatus.Done && p.EndDate.HasValue && p.EndDate.Value <= p.EstimatedDueDate.Value) ||
                    (p.Status != ProjectStatus.Done && p.EstimatedDueDate.Value >= now)
                ));
            var averageOtd = otdDenominator == 0 ? 0m : Math.Round((decimal)otdNumerator * 100m / otdDenominator, 2);

            var effectivenessRows = await projectsQuery
                .Select(p => new DashboardEffectivenessMetricRow
                {
                    EstimatedHours = p.EstimatedHours,
                    ActualHours = p.ActualHours,
                    CurrentValue = p.KPIs
                        .Where(k => k.Name == "Effectiveness")
                        .Select(k => (decimal?)k.CurrentValue)
                        .FirstOrDefault(),
                    KpiEstimatedHours = p.KPIs
                        .Where(k => k.Name == "Effectiveness")
                        .Select(k => (decimal?)k.EstimatedHours)
                        .FirstOrDefault(),
                    KpiActualHours = p.KPIs
                        .Where(k => k.Name == "Effectiveness")
                        .Select(k => (decimal?)k.ActualHours)
                        .FirstOrDefault()
                })
                .ToListAsync();
            var averageEffectiveness = CalculateAverageEffectiveness(effectivenessRows);

            var projectsByStatusRaw = await projectsQuery
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var projectsByStatus = projectsByStatusRaw
                .Select(x => new DashboardLabelValueDto
                {
                    Label = x.Status.ToString(),
                    Value = x.Count
                })
                .OrderBy(x => x.Label)
                .ToList();

            var projectsByPhaseRaw = await projectsQuery
                .GroupBy(p => p.Phase)
                .Select(g => new { Phase = g.Key, Count = g.Count() })
                .ToListAsync();

            var projectsByPhase = projectsByPhaseRaw
                .Select(x => new DashboardLabelValueDto
                {
                    Label = x.Phase.ToString(),
                    Value = x.Count
                })
                .OrderBy(x => x.Label)
                .ToList();

            var topProjectsByHours = await trackedHoursQuery
                .GroupBy(h => new { h.ProjectId, h.Project.Name })
                .Select(g => new TopProjectByHoursDto
                {
                    ProjectId = g.Key.ProjectId!.Value,
                    ProjectName = g.Key.Name,
                    Value = g.Sum(x => x.TotalHours)
                })
                .OrderByDescending(x => x.Value)
                .Take(topN)
                .ToListAsync();

            var projectTeamMembersByRole = await _context.ProjectMembers
                .AsNoTracking()
                .Where(pm => projectIdsQuery.Contains(pm.ProjectId) &&
                    (!pm.Project.ProjectManagerId.HasValue || pm.UserId != pm.Project.ProjectManagerId.Value))
                .GroupBy(pm => new { pm.RoleId, pm.Role.Name })
                .Select(g => new UsersByRoleDto
                {
                    RoleId = g.Key.RoleId,
                    RoleName = g.Key.Name,
                    Value = g.Count()
                })
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            var monthlyHoursBreakdownByCategory = await trackedHoursQuery
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Execution = g.Sum(x => x.ExecutionHours),
                    Supervision = g.Sum(x => x.SupervisionHours),
                    Process = g.Sum(x => x.ProcessHours),
                    Management = g.Sum(x => x.ManagementHours),
                    RAndD = g.Sum(x => x.RAndDHours),
                    Workshop = g.Sum(x => x.WorkshopHours),
                    Other = g.Sum(x => x.OtherHours),
                    Interns = g.Sum(x => x.InternManagementHours)
                })
                .FirstOrDefaultAsync();

            var hoursByCategory = monthlyHoursBreakdownByCategory is null
                ? new List<DashboardLabelValueDto>()
                : new List<DashboardLabelValueDto>
                {
                    new() { Label = "Execution", Value = monthlyHoursBreakdownByCategory.Execution },
                    new() { Label = "Supervision", Value = monthlyHoursBreakdownByCategory.Supervision },
                    new() { Label = "Process", Value = monthlyHoursBreakdownByCategory.Process },
                    new() { Label = "Management", Value = monthlyHoursBreakdownByCategory.Management },
                    new() { Label = "R&D", Value = monthlyHoursBreakdownByCategory.RAndD },
                    new() { Label = "Workshop", Value = monthlyHoursBreakdownByCategory.Workshop },
                    new() { Label = "Other", Value = monthlyHoursBreakdownByCategory.Other },
                    new() { Label = "Interns", Value = monthlyHoursBreakdownByCategory.Interns }
                }
                .Where(x => x.Value > 0)
                .OrderByDescending(x => x.Value)
                .ToList();

            var hoursByStage = await trackedHoursQuery
                .GroupBy(h => h.Project.Phase)
                .Select(g => new DashboardLabelValueDto
                {
                    Label = g.Key.ToString(),
                    Value = g.Sum(x => x.TotalHours)
                })
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            int totalUsers;
            int activeUsers;
            int approvedUsers;
            List<UsersByRoleDto> usersByRole;

            if (totalProjects == 0)
            {
                totalUsers = 0;
                activeUsers = 0;
                approvedUsers = 0;
                usersByRole = new List<UsersByRoleDto>();
            }
            else
            {
                var usersBaseQuery = BuildScopedUsersQuery(query, isAdminScope, projectIdsQuery);
                totalUsers = await usersBaseQuery.CountAsync();
                activeUsers = await usersBaseQuery.CountAsync(u => u.IsActive);
                approvedUsers = await usersBaseQuery.CountAsync(u => u.IsApproved);

                usersByRole = await usersBaseQuery
                    .Where(u => u.RoleId.HasValue)
                    .GroupBy(u => new { u.RoleId, u.Role.Name })
                    .Select(g => new UsersByRoleDto
                    {
                        RoleId = g.Key.RoleId!.Value,
                        RoleName = g.Key.Name,
                        Value = g.Count()
                    })
                    .OrderByDescending(x => x.Value)
                    .ToListAsync();
            }

            var deliveryMetrics = new List<DashboardLabelValueDto>
            {
                new() { Label = "OTD", Value = averageOtd },
                new() { Label = "Effectiveness", Value = averageEffectiveness }
            };

            var annualGoalProgress = (activeUsers > 0 && _standards.AnnualHoursTarget > 0)
                ? Math.Round(ytdHours * 100m / (_standards.AnnualHoursTarget * activeUsers), 2)
                : 0m;

            return new DashboardOverviewDto
            {
                Summary = new DashboardSummaryDto
                {
                    TotalProjects = totalProjects,
                    TotalEstimatedHours = totalEstimatedHours,
                    TotalTrackedHours = totalTrackedHours,
                    YtdHours = ytdHours,
                    AverageOtd = averageOtd,
                    AverageEffectiveness = averageEffectiveness,
                    DelayedProjects = delayedProjects,
                    DoneProjectsBelowTarget = doneProjectsBelowTarget,
                    DoneProjectsAboveTarget = doneProjectsAboveTarget,
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    ApprovedUsers = approvedUsers,
                    AnnualGoalProgressPercentage = annualGoalProgress
                },
                Charts = new DashboardChartsDto
                {
                    ProjectsByStatus = projectsByStatus,
                    ProjectsByPhase = projectsByPhase,
                    TopProjectsByHours = topProjectsByHours,
                    UsersByRole = usersByRole,
                    ProjectTeamMembersByRole = projectTeamMembersByRole,
                    MonthlyHoursBreakdownByCategory = hoursByCategory,
                    HoursByStage = hoursByStage,
                    DeliveryMetrics = deliveryMetrics
                }
            };
        }

        public async Task<DashboardPersonalPerformanceDto> GetPersonalPerformanceDashboardAsync(Guid userId, DashboardQueryDto query)
        {
            await LoadTargetSettingsAsync();

            var topN = query.TopN <= 0 ? 5 : Math.Min(query.TopN, 20);
            var now = DateTime.UtcNow.Date;
            var workloadYear = query.Year.HasValue && query.Year.Value > 0
                ? query.Year.Value
                : CompanyYearHelper.GetCurrentCompanyYear(now);

            var scopedProjectsQuery = BuildScopedProjectsQuery(query, isAdminScope: false, userId, applyProjectPeriodFilter: false);
            var scopedProjectIdsQuery = scopedProjectsQuery.Select(p => p.Id);

            var allPersonalHoursQuery = _context.HourEntries
                .AsNoTracking()
                .Where(h => h.UserId == userId && h.ProjectId.HasValue && scopedProjectIdsQuery.Contains(h.ProjectId.Value));

            var trackedHoursQuery = ApplyYearMonthFilter(allPersonalHoursQuery, query.Year, query.Month);
            var ytdHoursQuery = ApplyYtdFilter(allPersonalHoursQuery, query.Year, query.Month);

            var totalLoggedHours = await trackedHoursQuery.SumAsync(h => (decimal?)h.TotalHours) ?? 0m;
            var ytdLoggedHours = await ytdHoursQuery.SumAsync(h => (decimal?)h.TotalHours) ?? 0m;
            var loggedDays = await trackedHoursQuery
                .Select(h => h.Date.Date)
                .Distinct()
                .CountAsync();
            var projectsWithLoggedHours = await trackedHoursQuery
                .Select(h => h.ProjectId)
                .Distinct()
                .CountAsync();
            var totalCost = await trackedHoursQuery.SumAsync(h => (decimal?)h.TotalCost) ?? 0m;
            var premiumApprovedHours = await trackedHoursQuery
                .Where(h => h.IsPremium && h.PremiumApprovalStatus == ApprovalStatus.Approved)
                .SumAsync(h => (decimal?)h.TotalHours) ?? 0m;
            var premiumPendingHours = await trackedHoursQuery
                .Where(h => h.IsPremium && h.PremiumApprovalStatus == ApprovalStatus.Pending)
                .SumAsync(h => (decimal?)h.TotalHours) ?? 0m;

            var assignedProjects = await scopedProjectsQuery.CountAsync();
            var delayedAssignedProjects = await scopedProjectsQuery.CountAsync(p =>
                p.EstimatedDueDate.HasValue &&
                p.EstimatedDueDate.Value < now &&
                p.Status != ProjectStatus.Done);

            var categoryBreakdown = await trackedHoursQuery
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Execution = g.Sum(x => x.ExecutionHours),
                    Supervision = g.Sum(x => x.SupervisionHours),
                    Process = g.Sum(x => x.ProcessHours),
                    Management = g.Sum(x => x.ManagementHours),
                    RAndD = g.Sum(x => x.RAndDHours),
                    Workshop = g.Sum(x => x.WorkshopHours),
                    Other = g.Sum(x => x.OtherHours),
                    Interns = g.Sum(x => x.InternManagementHours)
                })
                .FirstOrDefaultAsync();

            var hoursByCategory = categoryBreakdown is null
                ? new List<DashboardLabelValueDto>()
                : new List<DashboardLabelValueDto>
                {
                    new() { Label = "Execution", Value = categoryBreakdown.Execution },
                    new() { Label = "Supervision", Value = categoryBreakdown.Supervision },
                    new() { Label = "Process", Value = categoryBreakdown.Process },
                    new() { Label = "Management", Value = categoryBreakdown.Management },
                    new() { Label = "R&D", Value = categoryBreakdown.RAndD },
                    new() { Label = "Workshop", Value = categoryBreakdown.Workshop },
                    new() { Label = "Other", Value = categoryBreakdown.Other },
                    new() { Label = "Interns", Value = categoryBreakdown.Interns }
                }
                .Where(x => x.Value > 0)
                .OrderByDescending(x => x.Value)
                .ToList();

            var hoursByStage = await trackedHoursQuery
                .GroupBy(h => h.Project.Phase)
                .Select(g => new DashboardLabelValueDto
                {
                    Label = g.Key.ToString(),
                    Value = g.Sum(x => x.TotalHours)
                })
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            var monthlyRows = await allPersonalHoursQuery
                .Where(h => h.Date.Year == workloadYear)
                .GroupBy(h => h.Date.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalHours = g.Sum(x => x.TotalHours),
                    ExecutionHours = g.Sum(x => x.ExecutionHours),
                    SupervisionHours = g.Sum(x => x.SupervisionHours),
                    ProcessHours = g.Sum(x => x.ProcessHours),
                    ManagementHours = g.Sum(x => x.ManagementHours),
                    RAndDHours = g.Sum(x => x.RAndDHours),
                    WorkshopHours = g.Sum(x => x.WorkshopHours),
                    OtherHours = g.Sum(x => x.OtherHours),
                    InternManagementHours = g.Sum(x => x.InternManagementHours)
                })
                .ToListAsync();

            var monthlyHoursByCategory = Enumerable.Range(1, 12)
                .Select(month =>
                {
                    var row = monthlyRows.FirstOrDefault(x => x.Month == month);
                    return new DashboardMonthlyHoursByCategoryDto
                    {
                        Year = workloadYear,
                        Month = month,
                        MonthName = new DateTime(workloadYear, month, 1).ToString("MMM"),
                        TotalHours = row?.TotalHours ?? 0m,
                        ExecutionHours = row?.ExecutionHours ?? 0m,
                        SupervisionHours = row?.SupervisionHours ?? 0m,
                        ProcessHours = row?.ProcessHours ?? 0m,
                        ManagementHours = row?.ManagementHours ?? 0m,
                        RAndDHours = row?.RAndDHours ?? 0m,
                        WorkshopHours = row?.WorkshopHours ?? 0m,
                        OtherHours = row?.OtherHours ?? 0m,
                        InternManagementHours = row?.InternManagementHours ?? 0m
                    };
                })
                .ToList();

            var topProjectRows = await trackedHoursQuery
                .GroupBy(h => new
                {
                    h.ProjectId,
                    h.Project.Name,
                    h.Project.Status,
                    h.Project.Phase,
                    h.Project.ProgressPercentage,
                    h.Project.EstimatedDueDate
                })
                .Select(g => new
                {
                    ProjectId = g.Key.ProjectId,
                    ProjectName = g.Key.Name,
                    g.Key.Status,
                    g.Key.Phase,
                    ProjectProgressPercentage = g.Key.ProgressPercentage,
                    EstimatedDueDate = g.Key.EstimatedDueDate,
                    TotalHours = g.Sum(x => x.TotalHours),
                    TotalCost = g.Sum(x => x.TotalCost)
                })
                .OrderByDescending(x => x.TotalHours)
                .Take(topN)
                .ToListAsync();

            var topProjects = topProjectRows
                .Select(x => new DashboardPersonalProjectContributionDto
                {
                    ProjectId = x.ProjectId!.Value,
                    ProjectName = x.ProjectName,
                    Status = x.Status.ToString(),
                    Phase = x.Phase.ToString(),
                    ProjectProgressPercentage = x.ProjectProgressPercentage,
                    EstimatedDueDate = x.EstimatedDueDate,
                    TotalHours = x.TotalHours,
                    TotalCost = x.TotalCost,
                    IsDelayed = x.EstimatedDueDate.HasValue &&
                        x.EstimatedDueDate.Value < now &&
                        x.Status != ProjectStatus.Done
                })
                .ToList();

            var expectedHours = ResolveExpectedHours(query, workloadYear);
            var utilizationRate = expectedHours > 0
                ? Math.Round(totalLoggedHours * 100m / expectedHours, 2)
                : 0m;

            var annualGoalProgress = _standards.AnnualHoursTarget > 0
                ? Math.Round(ytdLoggedHours * 100m / _standards.AnnualHoursTarget, 2)
                : 0m;

            return new DashboardPersonalPerformanceDto
            {
                Summary = new DashboardPersonalPerformanceSummaryDto
                {
                    TotalLoggedHours = totalLoggedHours,
                    YtdLoggedHours = ytdLoggedHours,
                    ExpectedHours = expectedHours,
                    UtilizationRate = utilizationRate,
                    AverageHoursPerLoggedDay = loggedDays > 0 ? Math.Round(totalLoggedHours / loggedDays, 2) : 0m,
                    LoggedDays = loggedDays,
                    ProjectsWithLoggedHours = projectsWithLoggedHours,
                    AssignedProjects = assignedProjects,
                    DelayedAssignedProjects = delayedAssignedProjects,
                    PremiumApprovedHours = premiumApprovedHours,
                    PremiumPendingHours = premiumPendingHours,
                    TotalCost = totalCost,
                    AnnualGoalProgressPercentage = annualGoalProgress
                },
                Charts = new DashboardPersonalPerformanceChartsDto
                {
                    HoursByCategory = hoursByCategory,
                    HoursByStage = hoursByStage,
                    MonthlyHoursByCategory = monthlyHoursByCategory,
                    PremiumHours = new List<DashboardLabelValueDto>
                    {
                        new() { Label = "Approved", Value = premiumApprovedHours },
                        new() { Label = "Pending", Value = premiumPendingHours }
                    }
                    .Where(x => x.Value > 0)
                    .ToList()
                },
                TopProjects = topProjects
            };
        }

        public async Task<DashboardExtendedAdminDto> GetExtendedAdminDashboardAsync(
            DashboardQueryDto query,
            bool isAdminScope = true,
            Guid? userId = null)
        {
            await LoadTargetSettingsAsync();

            var metrics = await GetAdminDashboardLightweightMetricsAsync(query, isAdminScope, userId);

            return new DashboardExtendedAdminDto
            {
                Summary = metrics.Summary,
                Charts = new DashboardExtendedAdminChartsDto
                {
                    DeliveryMetrics = metrics.DeliveryMetrics,
                    ProjectsByBusinessUnit = metrics.ProjectsByBusinessUnit,
                    ProjectsByDepartment = metrics.ProjectsByDepartment,
                    ProjectsByPlant = metrics.ProjectsByPlant,
                    ProjectsByStatus = metrics.ProjectsByStatus,
                    ProjectsByPhase = metrics.ProjectsByPhase,
                    ProjectsByProjectManagementType = metrics.ProjectsByProjectManagementType
                }
            };
        }

        public async Task<DashboardAdminBiDto> GetAdminBiDashboardAsync(
            DashboardQueryDto query,
            bool isAdminScope = true,
            Guid? userId = null)
        {
            await LoadTargetSettingsAsync();

            var metrics = await GetAdminDashboardLightweightMetricsAsync(query, isAdminScope, userId);

            return new DashboardAdminBiDto
            {
                Summary = metrics.Summary,
                Charts = new DashboardBiChartsDto
                {
                    ProjectsByBusinessUnit = metrics.ProjectsByBusinessUnit,
                    ProjectsByDepartment = metrics.ProjectsByDepartment,
                    ProjectsByPlant = metrics.ProjectsByPlant,
                    ProjectsByStatus = metrics.ProjectsByStatus,
                    ProjectsByPhase = metrics.ProjectsByPhase,
                    ProjectsByProjectManagementType = metrics.ProjectsByProjectManagementType
                }
            };
        }

        private async Task<DashboardAdminLightweightMetrics> GetAdminDashboardLightweightMetricsAsync(
            DashboardQueryDto query,
            bool isAdminScope = true,
            Guid? userId = null)
        {
            var now = DateTime.UtcNow.Date;
            var projectsQuery = BuildScopedProjectsQuery(query, isAdminScope, userId);

            var totalProjects = await projectsQuery.CountAsync();
            var delayedProjects = await projectsQuery.CountAsync(p =>
                p.EstimatedDueDate.HasValue &&
                p.EstimatedDueDate.Value < now &&
                p.Status != ProjectStatus.Done);
            var doneProjectsBelowTarget = await projectsQuery.CountAsync(p =>
                p.Status == ProjectStatus.Done &&
                p.EstimatedHours > 0 &&
                p.ActualHours < p.EstimatedHours);
            var doneProjectsAboveTarget = await projectsQuery.CountAsync(p =>
                p.Status == ProjectStatus.Done &&
                p.EstimatedHours > 0 &&
                p.ActualHours > p.EstimatedHours);

            var otdDenominator = await projectsQuery.CountAsync(p => p.EstimatedDueDate.HasValue);
            var otdNumerator = await projectsQuery.CountAsync(p =>
                p.EstimatedDueDate.HasValue &&
                (
                    (p.Status == ProjectStatus.Done && p.EndDate.HasValue && p.EndDate.Value <= p.EstimatedDueDate.Value) ||
                    (p.Status != ProjectStatus.Done && p.EstimatedDueDate.Value >= now)
                ));
            var averageOtd = otdDenominator == 0
                ? 0m
                : Math.Round((decimal)otdNumerator * 100m / otdDenominator, 2);

            var effectivenessRows = await projectsQuery
                .Select(p => new DashboardEffectivenessMetricRow
                {
                    EstimatedHours = p.EstimatedHours,
                    ActualHours = p.ActualHours,
                    CurrentValue = p.KPIs
                        .Where(k => k.Name == "Effectiveness")
                        .Select(k => (decimal?)k.CurrentValue)
                        .FirstOrDefault(),
                    KpiEstimatedHours = p.KPIs
                        .Where(k => k.Name == "Effectiveness")
                        .Select(k => (decimal?)k.EstimatedHours)
                        .FirstOrDefault(),
                    KpiActualHours = p.KPIs
                        .Where(k => k.Name == "Effectiveness")
                        .Select(k => (decimal?)k.ActualHours)
                        .FirstOrDefault()
                })
                .ToListAsync();
            var averageEffectiveness = CalculateAverageEffectiveness(effectivenessRows);

            var projectsByStatusRaw = await projectsQuery
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var projectsByStatus = projectsByStatusRaw
                .Select(x => new DashboardLabelValueDto
                {
                    Label = x.Status.ToString(),
                    Value = x.Count
                })
                .OrderBy(x => x.Label)
                .ToList();

            var projectsByPhaseRaw = await projectsQuery
                .GroupBy(p => p.Phase)
                .Select(g => new { Phase = g.Key, Count = g.Count() })
                .ToListAsync();

            var projectsByPhase = projectsByPhaseRaw
                .Select(x => new DashboardLabelValueDto
                {
                    Label = x.Phase.ToString(),
                    Value = x.Count
                })
                .OrderBy(x => x.Label)
                .ToList();

            var projectsByProjectManagementTypeRaw = await projectsQuery
                .GroupBy(p => p.ProjectManagementType)
                .Select(g => new { ProjectManagementType = g.Key, Count = g.Count() })
                .ToListAsync();

            var projectsByProjectManagementType = projectsByProjectManagementTypeRaw
                .Select(x => new DashboardLabelValueDto
                {
                    Label = x.ProjectManagementType.ToString(),
                    Value = x.Count
                })
                .OrderByDescending(x => x.Value)
                .ToList();

            var projectsByDepartment = await projectsQuery
                .Where(p => p.Department != null && p.Department.Name.Trim() != PlaceholderDepartmentName)
                .GroupBy(p => p.Department!.Name)
                .Select(g => new DashboardLabelValueDto
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            var projectsByPlant = await projectsQuery
                .GroupBy(p => p.Department!.Plant!.Name)
                .Select(g => new DashboardLabelValueDto
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            var projectsByBusinessUnit = await projectsQuery
                .SelectMany(p => p.ProjectBusinessUnits.Select(pbu => pbu.BusinessUnit.Name))
                .GroupBy(name => name)
                .Select(g => new DashboardLabelValueDto
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            return new DashboardAdminLightweightMetrics
            {
                Summary = new DashboardAdminSummaryDto
                {
                    TotalProjects = totalProjects,
                    AverageOtd = averageOtd,
                    AverageEffectiveness = averageEffectiveness,
                    DelayedProjects = delayedProjects,
                    DoneProjectsAboveTarget = doneProjectsAboveTarget,
                    DoneProjectsBelowTarget = doneProjectsBelowTarget
                },
                DeliveryMetrics = new List<DashboardLabelValueDto>
                {
                    new() { Label = "otd", Value = averageOtd },
                    new() { Label = "effectiveness", Value = averageEffectiveness }
                },
                ProjectsByBusinessUnit = projectsByBusinessUnit,
                ProjectsByDepartment = projectsByDepartment,
                ProjectsByPlant = projectsByPlant,
                ProjectsByStatus = projectsByStatus,
                ProjectsByPhase = projectsByPhase,
                ProjectsByProjectManagementType = projectsByProjectManagementType
            };
        }

        public async Task<DashboardGroupedDistributionDto> GetGroupedDistributionAsync(DashboardQueryDto query)
        {
            await LoadTargetSettingsAsync();

            var now = DateTime.UtcNow.Date;
            var projectsQuery = BuildScopedProjectsQuery(query, isAdminScope: true, userId: null);

            var projectRows = await projectsQuery
                .Select(p => new DashboardProjectDistributionRow
                {
                    Id = p.Id,
                    Name = p.Name,
                    Status = p.Status,
                    Phase = p.Phase,
                    ProjectManagementType = p.ProjectManagementType,
                    ProjectType = p.ProjectType,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    EstimatedDueDate = p.EstimatedDueDate,
                    ProgressPercentage = p.ProgressPercentage,
                    Budget = p.Budget,
                    DepartmentId = p.DepartmentId,
                    DepartmentName = p.Department != null ? p.Department.Name : string.Empty,
                    PlantId = p.Department != null ? p.Department.Plant.Id : null,
                    PlantName = p.Department != null ? p.Department.Plant.Name : string.Empty,
                    Sponsor = p.Sponsor ?? string.Empty,
                    EstimatedHours = p.EstimatedHours,
                    ActualHours = p.ActualHours
                })
                .OrderBy(p => p.Name)
                .ToListAsync();

            var projectIds = projectRows.Select(p => p.Id).ToList();
            var projectLookup = projectRows.ToDictionary(p => p.Id, ToProjectSummaryDto);

            var businessUnitRows = await _context.ProjectBusinessUnits
                .AsNoTracking()
                .Where(pbu => projectIds.Contains(pbu.ProjectId))
                .Select(pbu => new
                {
                    pbu.ProjectId,
                    pbu.BusinessUnitId,
                    BusinessUnitName = pbu.BusinessUnit.Name
                })
                .ToListAsync();

            return new DashboardGroupedDistributionDto
            {
                ProjectManagement = BuildGroups(
                    projectRows.Select(p => new DashboardProjectGroupItem(
                        p.ProjectManagementType.ToString(),
                        p.ProjectManagementType.ToString(),
                        projectLookup[p.Id]))),
                ProjectTypes = BuildGroups(
                    projectRows.Select(p => new DashboardProjectGroupItem(
                        p.ProjectType.ToString(),
                        p.ProjectType.ToString(),
                        projectLookup[p.Id]))),
                Status = BuildGroups(
                    projectRows.Select(p => new DashboardProjectGroupItem(
                        p.Status.ToString(),
                        p.Status.ToString(),
                        projectLookup[p.Id]))),
                Phases = BuildGroups(
                    projectRows.Select(p => new DashboardProjectGroupItem(
                        p.Phase.ToString(),
                        p.Phase.ToString(),
                        projectLookup[p.Id]))),
                BusinessUnits = BuildGroups(
                    businessUnitRows
                        .Where(x => projectLookup.ContainsKey(x.ProjectId))
                        .Select(x => new DashboardProjectGroupItem(
                            x.BusinessUnitId.ToString(),
                            x.BusinessUnitName,
                            projectLookup[x.ProjectId]))),
                Departments = BuildGroups(
                    projectRows
                        .Where(p => !IsPlaceholderDepartment(p.DepartmentName))
                        .Select(p => new DashboardProjectGroupItem(
                            p.DepartmentId.ToString(),
                            p.DepartmentName,
                            projectLookup[p.Id]))),
                Plants = BuildGroups(
                    projectRows
                        .Where(p => p.PlantId.HasValue)
                        .Select(p => new DashboardProjectGroupItem(
                            p.PlantId!.Value.ToString(),
                            p.PlantName,
                            projectLookup[p.Id])))
            };

            ProjectSummaryDto ToProjectSummaryDto(DashboardProjectDistributionRow p)
            {
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
                    ProgressPercentage = p.ProgressPercentage,
                    IsDelayed = p.EstimatedDueDate.HasValue &&
                        p.EstimatedDueDate.Value.Date < now &&
                        p.Status != ProjectStatus.Done,
                    Budget = p.Budget,
                    DepartmentName = p.DepartmentName,
                    PlantName = p.PlantName,
                    Sponsor = p.Sponsor,
                    EstimatedHours = p.EstimatedHours,
                    ActualHours = p.ActualHours
                };
            }
        }

        public async Task<DashboardGroupedDistributionCountsDto> GetGroupedDistributionCountsAsync(DashboardQueryDto query)
        {
            await LoadTargetSettingsAsync();

            var now = DateTime.UtcNow.Date;
            var projectsQuery = BuildScopedProjectsQuery(query, isAdminScope: true, userId: null);
            var projectIdsQuery = projectsQuery.Select(p => p.Id);

            var totalProjects = await projectsQuery.CountAsync();
            var delayedProjects = await projectsQuery.CountAsync(p =>
                p.EstimatedDueDate.HasValue &&
                p.EstimatedDueDate.Value < now &&
                p.Status != ProjectStatus.Done);
            var doneProjectsBelowTarget = await projectsQuery.CountAsync(p =>
                p.Status == ProjectStatus.Done &&
                p.EstimatedHours > 0 &&
                p.ActualHours < p.EstimatedHours);
            var doneProjectsAboveTarget = await projectsQuery.CountAsync(p =>
                p.Status == ProjectStatus.Done &&
                p.EstimatedHours > 0 &&
                p.ActualHours > p.EstimatedHours);

            var otdDenominator = await projectsQuery.CountAsync(p => p.EstimatedDueDate.HasValue);
            var otdNumerator = await projectsQuery.CountAsync(p =>
                p.EstimatedDueDate.HasValue &&
                (
                    (p.Status == ProjectStatus.Done && p.EndDate.HasValue && p.EndDate.Value <= p.EstimatedDueDate.Value) ||
                    (p.Status != ProjectStatus.Done && p.EstimatedDueDate.Value >= now)
                ));
            var averageOtd = otdDenominator == 0 ? 0m : Math.Round((decimal)otdNumerator * 100m / otdDenominator, 2);

            var effectivenessRows = await projectsQuery
                .Select(p => new DashboardEffectivenessMetricRow
                {
                    EstimatedHours = p.EstimatedHours,
                    ActualHours = p.ActualHours,
                    CurrentValue = p.KPIs
                        .Where(k => k.Name == "Effectiveness")
                        .Select(k => (decimal?)k.CurrentValue)
                        .FirstOrDefault(),
                    KpiEstimatedHours = p.KPIs
                        .Where(k => k.Name == "Effectiveness")
                        .Select(k => (decimal?)k.EstimatedHours)
                        .FirstOrDefault(),
                    KpiActualHours = p.KPIs
                        .Where(k => k.Name == "Effectiveness")
                        .Select(k => (decimal?)k.ActualHours)
                        .FirstOrDefault()
                })
                .ToListAsync();
            var averageEffectiveness = CalculateAverageEffectiveness(effectivenessRows);

            var projectManagementRows = await projectsQuery
                .GroupBy(p => p.ProjectManagementType)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();

            var projectTypeRows = await projectsQuery
                .GroupBy(p => p.ProjectType)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();

            var statusRows = await projectsQuery
                .GroupBy(p => p.Status)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();

            var phaseRows = await projectsQuery
                .GroupBy(p => p.Phase)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToListAsync();

            var businessUnitRows = await _context.ProjectBusinessUnits
                .AsNoTracking()
                .Where(pbu => projectIdsQuery.Contains(pbu.ProjectId))
                .GroupBy(pbu => new { pbu.BusinessUnitId, pbu.BusinessUnit.Name })
                .Select(g => new
                {
                    Id = g.Key.BusinessUnitId,
                    Label = g.Key.Name,
                    Count = g.Select(x => x.ProjectId).Distinct().Count()
                })
                .ToListAsync();

            var allBusinessUnits = await _context.BusinessUnits
                .AsNoTracking()
                .Select(bu => new { bu.Id, Label = bu.Name })
                .ToListAsync();

            var departmentRows = await projectsQuery
                .Where(p => p.Department != null && p.Department.Name.Trim() != PlaceholderDepartmentName)
                .GroupBy(p => new { p.DepartmentId, p.Department!.Name })
                .Select(g => new
                {
                    Id = g.Key.DepartmentId,
                    Label = g.Key.Name,
                    Count = g.Count()
                })
                .ToListAsync();

            var allDepartments = await _context.Departments
                .AsNoTracking()
                .Where(d => d.Name.Trim() != PlaceholderDepartmentName)
                .Select(d => new { d.Id, Label = d.Name })
                .ToListAsync();

            var plantRows = await projectsQuery
                .Where(p => p.Department != null && p.Department.Plant != null)
                .GroupBy(p => new { p.Department!.Plant.Id, p.Department.Plant.Name })
                .Select(g => new
                {
                    Id = g.Key.Id,
                    Label = g.Key.Name,
                    Count = g.Count()
                })
                .ToListAsync();

            var allPlants = await _context.Plants
                .AsNoTracking()
                .Select(p => new { p.Id, Label = p.Name })
                .ToListAsync();

            var projectManagementCounts = projectManagementRows.ToDictionary(x => x.Id, x => x.Count);
            var projectTypeCounts = projectTypeRows.ToDictionary(x => x.Id, x => x.Count);
            var statusCounts = statusRows.ToDictionary(x => x.Id, x => x.Count);
            var phaseCounts = phaseRows.ToDictionary(x => x.Id, x => x.Count);
            var businessUnitCounts = businessUnitRows.ToDictionary(x => x.Id, x => x.Count);
            var departmentCounts = departmentRows.ToDictionary(x => x.Id, x => x.Count);
            var plantCounts = plantRows.ToDictionary(x => x.Id, x => x.Count);

            return new DashboardGroupedDistributionCountsDto
            {
                Summary = new DashboardGroupedDistributionSummaryDto
                {
                    TotalProjects = totalProjects,
                    AverageOtd = averageOtd,
                    AverageEffectiveness = averageEffectiveness,
                    DelayedProjects = delayedProjects,
                    DoneProjectsAboveTarget = doneProjectsAboveTarget,
                    DoneProjectsBelowTarget = doneProjectsBelowTarget
                },
                ProjectManagement = BuildEnumCountGroups(projectManagementCounts, includeOther: true),
                ProjectTypes = BuildEnumCountGroups(projectTypeCounts),
                Status = BuildEnumCountGroups(statusCounts),
                Phases = BuildEnumCountGroups(phaseCounts),
                BusinessUnits = BuildReferenceCountGroups(allBusinessUnits, businessUnitCounts, x => x.Id, x => x.Label),
                Departments = BuildReferenceCountGroups(allDepartments, departmentCounts, x => x.Id, x => x.Label),
                Plants = BuildReferenceCountGroups(allPlants, plantCounts, x => x.Id, x => x.Label)
            };
        }

        private IQueryable<Project> BuildScopedProjectsQuery(
            DashboardQueryDto query,
            bool isAdminScope,
            Guid? userId,
            bool applyProjectPeriodFilter = true)
        {
            var projectsQuery = _context.Projects.AsNoTracking().AsQueryable();

            if (!isAdminScope)
            {
                if (!userId.HasValue)
                    return projectsQuery.Where(_ => false);

                var uid = userId.Value;
                projectsQuery = projectsQuery.Where(p =>
                    p.ProjectManagerId == uid ||
                    p.ProjectMembers.Any(pm => pm.UserId == uid));
            }

            if (TryParseProjectStatus(query.ProjectStatus, out var projectStatus))
                projectsQuery = projectsQuery.Where(p => p.Status == projectStatus);

            if (TryParseProjectPhase(query.ProjectPhase, out var projectPhase))
                projectsQuery = projectsQuery.Where(p => p.Phase == projectPhase);

            if (TryParseProcessStatus(query.ProcessStatus, out var processStatus))
                projectsQuery = projectsQuery.Where(p => p.ProcessStatus == processStatus);

            if (query.DepartmentId.HasValue && query.DepartmentId.Value != Guid.Empty)
                projectsQuery = projectsQuery.Where(p => p.DepartmentId == query.DepartmentId.Value);

            if (query.BusinessUnitId.HasValue && query.BusinessUnitId.Value != Guid.Empty)
                projectsQuery = projectsQuery.Where(p => p.ProjectBusinessUnits.Any(pbu => pbu.BusinessUnitId == query.BusinessUnitId.Value));

            if (query.PlantId.HasValue && query.PlantId.Value != Guid.Empty)
                projectsQuery = projectsQuery.Where(p => p.Department != null && p.Department.PlantId == query.PlantId.Value);

            if (query.ProjectManagerId.HasValue && query.ProjectManagerId.Value != Guid.Empty)
                projectsQuery = projectsQuery.Where(p => p.ProjectManagerId == query.ProjectManagerId.Value);

            if (query.RoleId.HasValue && query.RoleId.Value != Guid.Empty)
                projectsQuery = projectsQuery.Where(p => p.ProjectMembers.Any(pm => pm.RoleId == query.RoleId.Value));

            if (TryParseProjectType(query.ProjectType, out var projectType))
                projectsQuery = projectsQuery.Where(p => p.ProjectType == projectType);

            if (TryParseProjectManagementType(query.ProjectManagementType, out var projectManagementType))
                projectsQuery = projectsQuery.Where((System.Linq.Expressions.Expression<Func<Project, bool>>)(p => p.ProjectManagementType == projectManagementType));

            var (periodStart, periodEnd) = ResolveProjectPeriod(query);
            if (applyProjectPeriodFilter && periodStart.HasValue && periodEnd.HasValue)
            {
                var start = periodStart.Value;
                var end = periodEnd.Value;
                projectsQuery = projectsQuery.Where(p => p.StartDate >= start && p.StartDate <= end);
            }

            return projectsQuery;
        }

        private static List<DashboardProjectGroupDto> BuildGroups(IEnumerable<DashboardProjectGroupItem> items)
        {
            return items
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .Where(x => !IsPlaceholderDepartment(x.Label))
                .GroupBy(x => new { x.Id, x.Label })
                .Select(g => new DashboardProjectGroupDto
                {
                    Id = g.Key.Id,
                    Label = string.IsNullOrWhiteSpace(g.Key.Label) ? "Unassigned" : g.Key.Label,
                    Count = g.Select(x => x.Project.Id).Distinct().Count(),
                    Projects = g
                        .Select(x => x.Project)
                        .GroupBy(p => p.Id)
                        .Select(gp => gp.First())
                        .OrderBy(p => p.Name)
                        .ToList()
                })
                .OrderBy(x => x.Label)
                .ToList();
        }

        private static List<DashboardProjectCountGroupDto> ToCountGroups(IEnumerable<DashboardProjectGroupDto> groups)
        {
            return groups
                .Select(g => new DashboardProjectCountGroupDto
                {
                    Id = g.Id,
                    Label = g.Label,
                    Count = g.Count
                })
                .ToList();
        }

        private static List<DashboardProjectCountGroupDto> BuildEnumCountGroups<TEnum>(
            IReadOnlyDictionary<TEnum, int> counts,
            bool includeOther = false)
            where TEnum : struct, Enum
        {
            var groups = Enum.GetValues<TEnum>()
                .Select(value => new DashboardProjectCountGroupDto
                {
                    Id = value.ToString(),
                    Label = value.ToString(),
                    Count = counts.TryGetValue(value, out var count) ? count : 0
                })
                .ToList();

            if (includeOther)
            {
                groups.Add(new DashboardProjectCountGroupDto
                {
                    Id = "Other",
                    Label = "Other",
                    Count = 0
                });
            }

            return groups;
        }

        private static List<DashboardProjectCountGroupDto> BuildReferenceCountGroups<TOption>(
            IEnumerable<TOption> options,
            IReadOnlyDictionary<Guid, int> counts,
            Func<TOption, Guid> idSelector,
            Func<TOption, string> labelSelector)
        {
            return options
                .Where(option => !IsPlaceholderDepartment(labelSelector(option)))
                .Select(option =>
                {
                    var id = idSelector(option);
                    var label = labelSelector(option);

                    return new DashboardProjectCountGroupDto
                    {
                        Id = id.ToString(),
                        Label = string.IsNullOrWhiteSpace(label) ? "Unassigned" : label,
                        Count = counts.TryGetValue(id, out var count) ? count : 0
                    };
                })
                .OrderBy(x => x.Label)
                .ToList();
        }

        private static decimal CalculateAverageEffectiveness(IEnumerable<DashboardEffectivenessMetricRow> rows)
        {
            var scores = rows
                .Select(row =>
                {
                    var estimatedHours = row.KpiEstimatedHours.GetValueOrDefault() > 0
                        ? row.KpiEstimatedHours!.Value
                        : row.EstimatedHours;
                    var actualHours = row.KpiActualHours.GetValueOrDefault() > 0
                        ? row.KpiActualHours!.Value
                        : row.ActualHours;

                    var calculated = CalculateEffectivenessPercentage(estimatedHours, actualHours);
                    return calculated > 0
                        ? calculated
                        : NormalizePercentage(row.CurrentValue.GetValueOrDefault());
                })
                .Where(score => score > 0)
                .ToList();

            return scores.Count == 0 ? 0m : Math.Round(scores.Average(), 2);
        }

        private static decimal CalculateEffectivenessPercentage(decimal estimatedHours, decimal actualHours)
        {
            return actualHours <= 0 ? 0m : NormalizePercentage(estimatedHours * 100m / actualHours);
        }

        private static decimal NormalizePercentage(decimal value)
        {
            if (value <= 0)
                return 0m;

            var normalized = value <= 1m ? value * 100m : value;
            return Math.Round(Math.Min(normalized, 100m), 2);
        }

        private sealed record DashboardProjectGroupItem(string Id, string Label, ProjectSummaryDto Project);

        private sealed class DashboardEffectivenessMetricRow
        {
            public decimal EstimatedHours { get; set; }
            public decimal ActualHours { get; set; }
            public decimal? CurrentValue { get; set; }
            public decimal? KpiEstimatedHours { get; set; }
            public decimal? KpiActualHours { get; set; }
        }

        private sealed class DashboardAdminLightweightMetrics
        {
            public DashboardAdminSummaryDto Summary { get; set; } = new();
            public List<DashboardLabelValueDto> DeliveryMetrics { get; set; } = new();
            public List<DashboardLabelValueDto> ProjectsByBusinessUnit { get; set; } = new();
            public List<DashboardLabelValueDto> ProjectsByDepartment { get; set; } = new();
            public List<DashboardLabelValueDto> ProjectsByPlant { get; set; } = new();
            public List<DashboardLabelValueDto> ProjectsByStatus { get; set; } = new();
            public List<DashboardLabelValueDto> ProjectsByPhase { get; set; } = new();
            public List<DashboardLabelValueDto> ProjectsByProjectManagementType { get; set; } = new();
        }

        private sealed class DashboardProjectDistributionRow
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public ProjectStatus Status { get; set; }
            public ProjectPhase Phase { get; set; }
            public Category ProjectManagementType { get; set; }
            public ProjectType ProjectType { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public DateTime? EstimatedDueDate { get; set; }
            public int ProgressPercentage { get; set; }
            public decimal Budget { get; set; }
            public Guid DepartmentId { get; set; }
            public string DepartmentName { get; set; } = string.Empty;
            public Guid? PlantId { get; set; }
            public string PlantName { get; set; } = string.Empty;
            public string Sponsor { get; set; } = string.Empty;
            public decimal EstimatedHours { get; set; }
            public decimal ActualHours { get; set; }
        }

        private IQueryable<NormalUser> BuildScopedUsersQuery(DashboardQueryDto query, bool isAdminScope, IQueryable<Guid> projectIdsQuery)
        {
            IQueryable<NormalUser> usersQuery;

            if (isAdminScope)
            {
                usersQuery = _context.Users.OfType<NormalUser>().AsNoTracking();
            }
            else
            {
                var scopedUserIds = _context.ProjectMembers
                    .AsNoTracking()
                    .Where(pm => projectIdsQuery.Contains(pm.ProjectId))
                    .Select(pm => pm.UserId)
                    .Distinct();

                usersQuery = _context.Users
                    .OfType<NormalUser>()
                    .AsNoTracking()
                    .Where(u => scopedUserIds.Contains(u.Id));
            }

            if (query.RoleId.HasValue && query.RoleId.Value != Guid.Empty)
                usersQuery = usersQuery.Where(u => u.RoleId == query.RoleId.Value);

            return usersQuery;
        }

        private static IQueryable<HourEntry> ApplyYearMonthFilter(IQueryable<HourEntry> query, int? year, int? month)
        {
            if (month.HasValue && (month.Value < 1 || month.Value > 12))
                month = null;

            if (year.HasValue && year.Value > 0)
            {
                if (month.HasValue)
                {
                    var calendarYear = month.Value >= 10 ? year.Value - 1 : year.Value;
                    query = query.Where(h => h.Date.Year == calendarYear && h.Date.Month == month.Value);
                }
                else
                {
                    var start = CompanyYearHelper.GetCompanyYearStart(year.Value);
                    var end = CompanyYearHelper.GetCompanyYearEnd(year.Value);
                    query = query.Where(h => h.Date >= start && h.Date <= end);
                }
            }
            else if (month.HasValue)
            {
                query = query.Where(h => h.Date.Month == month.Value);
            }

            return query;
        }

        private static IQueryable<HourEntry> ApplyYtdFilter(IQueryable<HourEntry> query, int? year, int? month)
        {
            if (month.HasValue && (month.Value < 1 || month.Value > 12))
                month = null;

            var fy = year.HasValue && year.Value > 0 ? year.Value : CompanyYearHelper.GetCurrentCompanyYear();
            var start = CompanyYearHelper.GetCompanyYearStart(fy);
            
            DateTime end;
            if (month.HasValue)
            {
                var calendarYear = month.Value >= 10 ? fy - 1 : fy;
                end = new DateTime(calendarYear, month.Value, DateTime.DaysInMonth(calendarYear, month.Value), 23, 59, 59);
            }
            else
            {
                end = DateTime.UtcNow;
                var fyEnd = CompanyYearHelper.GetCompanyYearEnd(fy);
                if (end > fyEnd) end = fyEnd;
            }

            query = query.Where(h => h.Date >= start && h.Date <= end);

            return query;
        }

        private static (DateTime? Start, DateTime? End) ResolvePeriod(int? year, int? month)
        {
            if (month.HasValue && (month.Value < 1 || month.Value > 12))
                month = null;

            if (!year.HasValue && !month.HasValue)
                return (null, null);

            var fy = year.HasValue && year.Value > 0 ? year.Value : CompanyYearHelper.GetCurrentCompanyYear();

            if (month.HasValue)
            {
                var calendarYear = month.Value >= 10 ? fy - 1 : fy;
                var start = new DateTime(calendarYear, month.Value, 1);
                var end = start.AddMonths(1).AddTicks(-1);
                return (start, end);
            }

            return (CompanyYearHelper.GetCompanyYearStart(fy), CompanyYearHelper.GetCompanyYearEnd(fy));
        }

        private static (DateTime? Start, DateTime? End) ResolveProjectPeriod(DashboardQueryDto query)
        {
            if (query.StartDate.HasValue || query.EndDate.HasValue)
            {
                var start = query.StartDate?.Date ?? DateTime.MinValue;
                var end = query.EndDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;

                return start <= end
                    ? (start, end)
                    : (end.Date, start.Date.AddDays(1).AddTicks(-1));
            }

            return ResolvePeriod(query.Year, query.Month);
        }

        private decimal ResolveExpectedHours(DashboardQueryDto query, int workloadYear)
        {
            var monthlyTarget = _standards.MonthlyHoursTarget;
            
            if (query.Month.HasValue && query.Month.Value >= 1 && query.Month.Value <= 12)
                return monthlyTarget;

            var today = DateTime.UtcNow.Date;
            var currentCompanyYear = CompanyYearHelper.GetCurrentCompanyYear(today);
            var months = query.Ytd && workloadYear == currentCompanyYear
                ? ((today.Year - CompanyYearHelper.GetCompanyYearStart(currentCompanyYear).Year) * 12) +
                  today.Month -
                  CompanyYearHelper.GetCompanyYearStart(currentCompanyYear).Month +
                  1
                : 12;

            return monthlyTarget * months;
        }

        private static bool TryParseProjectStatus(string? value, out ProjectStatus status)
        {
            return Enum.TryParse(value, true, out status);
        }

        private static bool TryParseProjectPhase(string? value, out ProjectPhase phase)
        {
            return Enum.TryParse(value, true, out phase);
        }

        private static bool TryParseProcessStatus(string? value, out ProcessStatus status)
        {
            status = default;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (Enum.TryParse(value, true, out status))
                return true;

            return value.Trim().ToLowerInvariant() switch
            {
                "asis" => TrySet(out status, ProcessStatus.AsIsProcessUnderstanding),
                "tobe" => TrySet(out status, ProcessStatus.ToBeProcessDefinition),
                "implemented" => TrySet(out status, ProcessStatus.ImplementedInPDMlink),
                _ => false
            };
        }

        private static bool TryParseProjectType(string? value, out ProjectType type)
        {
            return Enum.TryParse(value, true, out type);
        }

        private static bool TryParseProjectManagementType(string? value, out Category type)
        {
            return Enum.TryParse(value, true, out type);
        }

        private static bool IsPlaceholderDepartment(string? name) =>
            string.Equals(name?.Trim(), PlaceholderDepartmentName, StringComparison.Ordinal);

        private static DashboardQueryDto ResolvePreviousPeriodQuery(DashboardQueryDto query)
        {
            var previous = new DashboardQueryDto
            {
                Year = query.Year,
                Month = query.Month,
                Ytd = query.Ytd,
                RoleId = query.RoleId,
                ProjectStatus = query.ProjectStatus,
                ProjectPhase = query.ProjectPhase,
                ProcessStatus = query.ProcessStatus,
                DepartmentId = query.DepartmentId,
                BusinessUnitId = query.BusinessUnitId,
                PlantId = query.PlantId,
                ProjectManagerId = query.ProjectManagerId,
                ProjectType = query.ProjectType,
                ProjectManagementType = query.ProjectManagementType,
                TopN = query.TopN
            };

            if (query.StartDate.HasValue || query.EndDate.HasValue)
            {
                var (currentStart, currentEnd) = ResolveProjectPeriod(query);
                if (currentStart.HasValue && currentEnd.HasValue)
                {
                    var periodLength = currentEnd.Value.Date - currentStart.Value.Date;
                    previous.EndDate = currentStart.Value.Date.AddDays(-1);
                    previous.StartDate = previous.EndDate.Value.Date.Subtract(periodLength);
                    previous.Year = null;
                    previous.Month = null;
                    previous.Ytd = false;
                    return previous;
                }
            }

            if (query.Month.HasValue)
            {
                var year = query.Year.HasValue && query.Year.Value > 0
                    ? query.Year.Value
                    : CompanyYearHelper.GetCurrentCompanyYear(DateTime.UtcNow.Date);
                var calendarYear = query.Month.Value >= 10 ? year - 1 : year;
                var currentMonth = new DateTime(calendarYear, query.Month.Value, 1);
                var previousMonth = currentMonth.AddMonths(-1);
                previous.Year = CompanyYearHelper.GetCurrentCompanyYear(previousMonth);
                previous.Month = previousMonth.Month;
                previous.Ytd = false;
                return previous;
            }

            previous.Year = (query.Year.HasValue && query.Year.Value > 0
                ? query.Year.Value
                : CompanyYearHelper.GetCurrentCompanyYear(DateTime.UtcNow.Date)) - 1;
            previous.Month = null;
            previous.Ytd = query.Ytd;
            return previous;
        }

        private static DashboardTopProjectDto ToTopProjectDto(
            Guid projectId,
            string projectName,
            ProjectStatus status,
            ProjectPhase phase,
            decimal progressPercentage,
            decimal totalHours,
            decimal estimatedHours,
            decimal budget,
            decimal totalCost,
            DateTime? estimatedDueDate,
            DateTime now)
        {
            return new DashboardTopProjectDto
            {
                ProjectId = projectId,
                ProjectName = projectName,
                Status = status.ToString(),
                Phase = phase.ToString(),
                ProgressPercentage = progressPercentage,
                TotalHours = totalHours,
                EstimatedHours = estimatedHours,
                Budget = budget,
                TotalCost = totalCost,
                EstimatedDueDate = estimatedDueDate,
                IsDelayed = estimatedDueDate.HasValue &&
                    estimatedDueDate.Value < now &&
                    status != ProjectStatus.Done
            };
        }

        private static decimal EFDateDiffDays(DateTime start, DateTime end)
        {
            return (decimal)(end.Date - start.Date).TotalDays;
        }

        private static List<DashboardAlertDto> BuildExtendedDashboardAlerts(
            int delayedProjects,
            int overdueRoadblocks,
            decimal premiumPendingHours,
            int dueSoonProjects,
            decimal totalBudget,
            decimal totalCost)
        {
            var alerts = new List<DashboardAlertDto>();

            if (delayedProjects > 0)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Severity = "warning",
                    Type = "DelayedProjects",
                    Message = $"{delayedProjects} project(s) are delayed.",
                    Value = delayedProjects
                });
            }

            if (overdueRoadblocks > 0)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Severity = "critical",
                    Type = "OverdueRoadblocks",
                    Message = $"{overdueRoadblocks} open roadblock(s) are overdue.",
                    Value = overdueRoadblocks
                });
            }

            if (premiumPendingHours > 0)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Severity = "info",
                    Type = "PendingPremiumHours",
                    Message = $"{premiumPendingHours} premium hour(s) are pending approval.",
                    Value = premiumPendingHours
                });
            }

            if (dueSoonProjects > 0)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Severity = "info",
                    Type = "DueSoonProjects",
                    Message = $"{dueSoonProjects} project(s) are due within 14 days.",
                    Value = dueSoonProjects
                });
            }

            if (totalBudget > 0 && totalCost > totalBudget)
            {
                alerts.Add(new DashboardAlertDto
                {
                    Severity = "critical",
                    Type = "BudgetOverrun",
                    Message = "Tracked cost exceeds portfolio budget.",
                    Value = Math.Round(totalCost - totalBudget, 2)
                });
            }

            return alerts;
        }

        private static bool TrySet<T>(out T target, T value)
        {
            target = value;
            return true;
        }

        private async Task LoadTargetSettingsAsync()
        {
            _standards = await _targetSettingsService.GetCompanyStandardsAsync();
        }
    }
}
