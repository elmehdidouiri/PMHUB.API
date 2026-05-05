using Microsoft.EntityFrameworkCore;
using PMHUB.Application.DTOs;
using PMHUB.Domain.Entities;
using PMHUB.Domain.Enums;
using PMHUB.Infrastructure.Persistence;

namespace PMHUB.Infrastructure.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly PMHubDbContext _context;

        public DashboardRepository(PMHubDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardOverviewDto> GetDashboardOverviewAsync(DashboardQueryDto query, bool isAdminScope, Guid? userId = null)
        {
            var topN = query.TopN <= 0 ? 5 : Math.Min(query.TopN, 20);
            var now = DateTime.UtcNow.Date;

            var projectsQuery = BuildScopedProjectsQuery(query, isAdminScope, userId);
            var projectIdsQuery = projectsQuery.Select(p => p.Id);

            var allHoursScopedQuery = _context.HourEntries
                .AsNoTracking()
                .Where(h => projectIdsQuery.Contains(h.ProjectId));

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

            var otdDenominator = await projectsQuery.CountAsync(p => p.EstimatedDueDate.HasValue);
            var otdNumerator = await projectsQuery.CountAsync(p =>
                p.EstimatedDueDate.HasValue &&
                (
                    (p.Status == ProjectStatus.Done && p.EndDate.HasValue && p.EndDate.Value <= p.EstimatedDueDate.Value) ||
                    (p.Status != ProjectStatus.Done && p.EstimatedDueDate.Value >= now)
                ));
            var averageOtd = otdDenominator == 0 ? 0m : Math.Round((decimal)otdNumerator * 100m / otdDenominator, 2);

            var avgEffectivenessRaw = await projectsQuery
                .Select(p => (decimal?)p.ProgressPercentage)
                .AverageAsync();
            var averageEffectiveness = Math.Round(avgEffectivenessRaw ?? 0m, 2);

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
                    ProjectId = g.Key.ProjectId,
                    ProjectName = g.Key.Name,
                    Value = g.Sum(x => x.TotalHours)
                })
                .OrderByDescending(x => x.Value)
                .Take(topN)
                .ToListAsync();

            var projectTeamMembersByRole = await _context.ProjectMembers
                .AsNoTracking()
                .Where(pm => projectIdsQuery.Contains(pm.ProjectId))
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
                    .GroupBy(u => new { u.RoleId, u.Role.Name })
                    .Select(g => new UsersByRoleDto
                    {
                        RoleId = g.Key.RoleId,
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
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    ApprovedUsers = approvedUsers
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
            var topN = query.TopN <= 0 ? 5 : Math.Min(query.TopN, 20);
            var now = DateTime.UtcNow.Date;
            var workloadYear = query.Year.HasValue && query.Year.Value > 0
                ? query.Year.Value
                : DateTime.UtcNow.Year;

            var scopedProjectsQuery = BuildScopedProjectsQuery(query, isAdminScope: false, userId, applyProjectPeriodFilter: false);
            var scopedProjectIdsQuery = scopedProjectsQuery.Select(p => p.Id);

            var allPersonalHoursQuery = _context.HourEntries
                .AsNoTracking()
                .Where(h => h.UserId == userId && scopedProjectIdsQuery.Contains(h.ProjectId));

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
                    ProjectId = x.ProjectId,
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
                    TotalCost = totalCost
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

        public async Task<DashboardExtendedAdminDto> GetExtendedAdminDashboardAsync(DashboardQueryDto query)
        {
            var topN = query.TopN <= 0 ? 5 : Math.Min(query.TopN, 20);
            var now = DateTime.UtcNow.Date;
            var workloadYear = query.Year.HasValue && query.Year.Value > 0
                ? query.Year.Value
                : DateTime.UtcNow.Year;

            var overview = await GetDashboardOverviewAsync(query, isAdminScope: true);
            var projectsQuery = BuildScopedProjectsQuery(query, isAdminScope: true, userId: null);
            var projectIdsQuery = projectsQuery.Select(p => p.Id);

            var allHoursScopedQuery = _context.HourEntries
                .AsNoTracking()
                .Where(h => projectIdsQuery.Contains(h.ProjectId));

            var trackedHoursQuery = ApplyYearMonthFilter(allHoursScopedQuery, query.Year, query.Month);

            var totalBudget = await projectsQuery.SumAsync(p => (decimal?)p.Budget) ?? 0m;
            var totalCostSaving = await projectsQuery.SumAsync(p => (decimal?)p.CostSaving) ?? 0m;
            var totalDigitalContribution = await projectsQuery.SumAsync(p => (decimal?)p.DigitalContribution) ?? 0m;
            var totalCost = await trackedHoursQuery.SumAsync(h => (decimal?)h.TotalCost) ?? 0m;
            var premiumApprovedHours = await trackedHoursQuery
                .Where(h => h.IsPremium && h.PremiumApprovalStatus == ApprovalStatus.Approved)
                .SumAsync(h => (decimal?)h.TotalHours) ?? 0m;
            var premiumPendingHours = await trackedHoursQuery
                .Where(h => h.IsPremium && h.PremiumApprovalStatus == ApprovalStatus.Pending)
                .SumAsync(h => (decimal?)h.TotalHours) ?? 0m;

            var ongoingProjects = await projectsQuery.CountAsync(p => p.Status == ProjectStatus.Ongoing);
            var plannedProjects = await projectsQuery.CountAsync(p => p.Status == ProjectStatus.Planned);
            var onHoldProjects = await projectsQuery.CountAsync(p => p.Status == ProjectStatus.OnHold);
            var doneProjects = await projectsQuery.CountAsync(p => p.Status == ProjectStatus.Done);
            var averageProgress = await projectsQuery.Select(p => (decimal?)p.ProgressPercentage).AverageAsync() ?? 0m;

            var usersQuery = BuildScopedUsersQuery(query, isAdminScope: true, projectIdsQuery);
            var totalUsers = await usersQuery.CountAsync();
            var activeUsers = await usersQuery.CountAsync(u => u.IsActive);
            var approvedUsers = await usersQuery.CountAsync(u => u.IsApproved);
            var usersByRole = await usersQuery
                .GroupBy(u => new { u.RoleId, u.Role.Name })
                .Select(g => new UsersByRoleDto
                {
                    RoleId = g.Key.RoleId,
                    RoleName = g.Key.Name,
                    Value = g.Count()
                })
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            var openRoadblocks = await _context.ProjectRoadblocks
                .AsNoTracking()
                .CountAsync(r => projectIdsQuery.Contains(r.ProjectId) && r.Status == RoadblockStatus.Open);
            var overdueRoadblocks = await _context.ProjectRoadblocks
                .AsNoTracking()
                .CountAsync(r => projectIdsQuery.Contains(r.ProjectId) && r.Status == RoadblockStatus.Open && r.DueAt < now);
            var dueSoonLimit = now.AddDays(14);
            var dueSoonProjects = await projectsQuery.CountAsync(p =>
                p.EstimatedDueDate.HasValue &&
                p.EstimatedDueDate.Value >= now &&
                p.EstimatedDueDate.Value <= dueSoonLimit &&
                p.Status != ProjectStatus.Done);

            var monthlyRows = await allHoursScopedQuery
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
                    h.Project.EstimatedHours,
                    h.Project.Budget,
                    h.Project.EstimatedDueDate
                })
                .Select(g => new
                {
                    ProjectId = g.Key.ProjectId,
                    ProjectName = g.Key.Name,
                    g.Key.Status,
                    g.Key.Phase,
                    ProgressPercentage = g.Key.ProgressPercentage,
                    TotalHours = g.Sum(x => x.TotalHours),
                    EstimatedHours = g.Key.EstimatedHours,
                    Budget = g.Key.Budget,
                    TotalCost = g.Sum(x => x.TotalCost),
                    EstimatedDueDate = g.Key.EstimatedDueDate
                })
                .OrderByDescending(x => x.TotalHours)
                .Take(topN)
                .ToListAsync();

            var topProjects = topProjectRows
                .Select(x => new DashboardTopProjectDto
                {
                    ProjectId = x.ProjectId,
                    ProjectName = x.ProjectName,
                    Status = x.Status.ToString(),
                    Phase = x.Phase.ToString(),
                    ProgressPercentage = x.ProgressPercentage,
                    TotalHours = x.TotalHours,
                    EstimatedHours = x.EstimatedHours,
                    Budget = x.Budget,
                    TotalCost = x.TotalCost,
                    EstimatedDueDate = x.EstimatedDueDate,
                    IsDelayed = x.EstimatedDueDate.HasValue &&
                        x.EstimatedDueDate.Value < now &&
                        x.Status != ProjectStatus.Done
                })
                .ToList();

            var alerts = BuildExtendedDashboardAlerts(
                overview.Summary.DelayedProjects,
                overdueRoadblocks,
                premiumPendingHours,
                dueSoonProjects,
                totalBudget,
                totalCost);

            return new DashboardExtendedAdminDto
            {
                Summary = overview.Summary,
                PortfolioHealth = new DashboardPortfolioHealthDto
                {
                    TotalProjects = overview.Summary.TotalProjects,
                    OngoingProjects = ongoingProjects,
                    PlannedProjects = plannedProjects,
                    OnHoldProjects = onHoldProjects,
                    DoneProjects = doneProjects,
                    DelayedProjects = overview.Summary.DelayedProjects,
                    AverageProgress = Math.Round(averageProgress, 2),
                    AverageOtd = overview.Summary.AverageOtd,
                    AverageEffectiveness = overview.Summary.AverageEffectiveness,
                    ProjectsByStatus = overview.Charts.ProjectsByStatus,
                    ProjectsByPhase = overview.Charts.ProjectsByPhase
                },
                Workload = new DashboardWorkloadDto
                {
                    TotalTrackedHours = overview.Summary.TotalTrackedHours,
                    YtdHours = overview.Summary.YtdHours,
                    PremiumApprovedHours = premiumApprovedHours,
                    PremiumPendingHours = premiumPendingHours,
                    MonthlyHoursByCategory = monthlyHoursByCategory,
                    CategoryBreakdown = overview.Charts.MonthlyHoursBreakdownByCategory,
                    HoursByStage = overview.Charts.HoursByStage
                },
                Users = new DashboardUsersDto
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    InactiveUsers = totalUsers - activeUsers,
                    ApprovedUsers = approvedUsers,
                    PendingApprovalUsers = totalUsers - approvedUsers,
                    UsersByRole = usersByRole
                },
                Risks = new DashboardRisksDto
                {
                    OpenRoadblocks = openRoadblocks,
                    OverdueRoadblocks = overdueRoadblocks,
                    DelayedProjects = overview.Summary.DelayedProjects,
                    OnHoldProjects = onHoldProjects,
                    DueSoonProjects = dueSoonProjects
                },
                Business = new DashboardBusinessDto
                {
                    TotalBudget = totalBudget,
                    TotalCost = totalCost,
                    TotalEstimatedHours = overview.Summary.TotalEstimatedHours,
                    TotalTrackedHours = overview.Summary.TotalTrackedHours,
                    TotalCostSaving = totalCostSaving,
                    TotalDigitalContribution = totalDigitalContribution,
                    BudgetConsumptionPercentage = totalBudget > 0
                        ? Math.Round(totalCost * 100m / totalBudget, 2)
                        : 0m
                },
                TopProjects = topProjects,
                Alerts = alerts
            };
        }

        public async Task<DashboardAdminBiDto> GetAdminBiDashboardAsync(DashboardQueryDto query)
        {
            var topN = query.TopN <= 0 ? 5 : Math.Min(query.TopN, 20);
            var now = DateTime.UtcNow.Date;
            var year = query.Year.HasValue && query.Year.Value > 0
                ? query.Year.Value
                : DateTime.UtcNow.Year;

            var extended = await GetExtendedAdminDashboardAsync(query);
            var projectsQuery = BuildScopedProjectsQuery(query, isAdminScope: true, userId: null);
            var projectIdsQuery = projectsQuery.Select(p => p.Id);

            var allHoursScopedQuery = _context.HourEntries
                .AsNoTracking()
                .Where(h => projectIdsQuery.Contains(h.ProjectId));
            var trackedHoursQuery = query.Ytd
                ? ApplyYtdFilter(allHoursScopedQuery, query.Year, query.Month)
                : ApplyYearMonthFilter(allHoursScopedQuery, query.Year, query.Month);

            var previousQuery = ResolvePreviousPeriodQuery(query);
            var previousProjectsQuery = BuildScopedProjectsQuery(previousQuery, isAdminScope: true, userId: null);
            var previousProjectIdsQuery = previousProjectsQuery.Select(p => p.Id);
            var previousHoursQuery = _context.HourEntries
                .AsNoTracking()
                .Where(h => previousProjectIdsQuery.Contains(h.ProjectId));
            previousHoursQuery = previousQuery.Ytd
                ? ApplyYtdFilter(previousHoursQuery, previousQuery.Year, previousQuery.Month)
                : ApplyYearMonthFilter(previousHoursQuery, previousQuery.Year, previousQuery.Month);

            var totalProjects = extended.Summary.TotalProjects;
            var previousTotalProjects = await previousProjectsQuery.CountAsync();
            var previousTrackedHours = await previousHoursQuery.SumAsync(h => (decimal?)h.TotalHours) ?? 0m;
            var previousDelayedProjects = await previousProjectsQuery.CountAsync(p =>
                p.EstimatedDueDate.HasValue &&
                p.EstimatedDueDate.Value < now &&
                p.Status != ProjectStatus.Done);
            var delayRate = totalProjects > 0
                ? Math.Round(extended.Summary.DelayedProjects * 100m / totalProjects, 2)
                : 0m;
            var trackedHoursVariancePercent = extended.Summary.TotalEstimatedHours > 0
                ? Math.Round((extended.Summary.TotalTrackedHours - extended.Summary.TotalEstimatedHours) * 100m / extended.Summary.TotalEstimatedHours, 2)
                : 0m;

            var monthlyWorkloadRows = await allHoursScopedQuery
                .Where(h => h.Date.Year == year)
                .GroupBy(h => h.Date.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalHours = g.Sum(x => x.TotalHours),
                    Cost = g.Sum(x => x.TotalCost)
                })
                .ToListAsync();

            var monthlyWorkloadTrend = Enumerable.Range(1, 12)
                .Select(month =>
                {
                    var row = monthlyWorkloadRows.FirstOrDefault(x => x.Month == month);
                    return new DashboardBiMonthlyTrendDto
                    {
                        Year = year,
                        Month = month,
                        MonthName = new DateTime(year, month, 1).ToString("MMM"),
                        TotalHours = row?.TotalHours ?? 0m,
                        EstimatedHours = extended.Summary.TotalEstimatedHours / 12m,
                        Cost = row?.Cost ?? 0m
                    };
                })
                .ToList();

            var performanceRows = await projectsQuery
                .Where(p => p.EstimatedDueDate.HasValue && p.EstimatedDueDate.Value.Year == year)
                .GroupBy(p => p.EstimatedDueDate!.Value.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count(),
                    OtdCount = g.Count(p =>
                        (p.Status == ProjectStatus.Done && p.EndDate.HasValue && p.EndDate.Value <= p.EstimatedDueDate!.Value) ||
                        (p.Status != ProjectStatus.Done && p.EstimatedDueDate!.Value >= now)),
                    Effectiveness = g.Average(p => (decimal?)p.ProgressPercentage) ?? 0m
                })
                .ToListAsync();

            var performanceTrend = Enumerable.Range(1, 12)
                .Select(month =>
                {
                    var row = performanceRows.FirstOrDefault(x => x.Month == month);
                    return new DashboardBiPerformanceTrendDto
                    {
                        Year = year,
                        Month = month,
                        MonthName = new DateTime(year, month, 1).ToString("MMM"),
                        Otd = row is null || row.Count == 0 ? 0m : Math.Round(row.OtdCount * 100m / row.Count, 2),
                        Effectiveness = Math.Round(row?.Effectiveness ?? 0m, 2)
                    };
                })
                .ToList();

            var workloadByRole = await trackedHoursQuery
                .GroupBy(h => new { h.User.RoleId, h.User.Role.Name })
                .Select(g => new DashboardLabelValueDto
                {
                    Label = g.Key.Name,
                    Value = g.Sum(x => x.TotalHours)
                })
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            var workloadByDepartment = await trackedHoursQuery
                .GroupBy(h => new { h.Project.DepartmentId, h.Project.Department!.Name })
                .Select(g => new DashboardLabelValueDto
                {
                    Label = g.Key.Name,
                    Value = g.Sum(x => x.TotalHours)
                })
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            var workloadByBusinessUnit = await trackedHoursQuery
                .SelectMany(h => h.Project.ProjectBusinessUnits.Select(pbu => new
                {
                    pbu.BusinessUnit.Name,
                    h.TotalHours
                }))
                .GroupBy(x => x.Name)
                .Select(g => new DashboardLabelValueDto
                {
                    Label = g.Key,
                    Value = g.Sum(x => x.TotalHours)
                })
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            var costSavingByDepartment = await projectsQuery
                .GroupBy(p => new { p.DepartmentId, p.Department!.Name })
                .Select(g => new DashboardLabelValueDto
                {
                    Label = g.Key.Name,
                    Value = g.Sum(x => x.CostSaving)
                })
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            var costSavingByBusinessUnit = await projectsQuery
                .SelectMany(p => p.ProjectBusinessUnits.Select(pbu => new
                {
                    pbu.BusinessUnit.Name,
                    p.CostSaving
                }))
                .GroupBy(x => x.Name)
                .Select(g => new DashboardLabelValueDto
                {
                    Label = g.Key,
                    Value = g.Sum(x => x.CostSaving)
                })
                .OrderByDescending(x => x.Value)
                .ToListAsync();

            var projectsByPlant = await projectsQuery
                .GroupBy(p => new { p.Department!.Plant.Id, p.Department.Plant.Name })
                .Select(g => new DashboardLabelValueDto
                {
                    Label = g.Key.Name,
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

            var actualHoursByProject = trackedHoursQuery
                .GroupBy(h => h.ProjectId)
                .Select(g => new
                {
                    ProjectId = g.Key,
                    ActualHours = g.Sum(x => x.TotalHours),
                    TotalCost = g.Sum(x => x.TotalCost)
                });

            var estimatedVsActualRows = await projectsQuery
                .GroupJoin(actualHoursByProject, p => p.Id, h => h.ProjectId, (p, hours) => new
                {
                    p.Id,
                    p.Name,
                    p.EstimatedHours,
                    p.Budget,
                    p.ProgressPercentage,
                    p.Status,
                    ActualHours = hours.Select(x => (decimal?)x.ActualHours).FirstOrDefault() ?? 0m
                })
                .OrderByDescending(x => x.ActualHours)
                .Take(Math.Max(topN, 20))
                .ToListAsync();

            var estimatedVsActualProjects = estimatedVsActualRows
                .Select(x => new DashboardEstimatedVsActualProjectDto
                {
                    ProjectId = x.Id,
                    ProjectName = x.Name,
                    EstimatedHours = x.EstimatedHours,
                    ActualHours = x.ActualHours,
                    Budget = x.Budget,
                    ProgressPercentage = x.ProgressPercentage,
                    Status = x.Status.ToString()
                })
                .ToList();

            var projectsOverEstimatedRows = await projectsQuery
                .GroupJoin(actualHoursByProject, p => p.Id, h => h.ProjectId, (p, hours) => new
                {
                    p.Id,
                    p.Name,
                    p.Status,
                    p.Phase,
                    p.ProgressPercentage,
                    p.EstimatedHours,
                    p.Budget,
                    p.EstimatedDueDate,
                    ActualHours = hours.Select(x => (decimal?)x.ActualHours).FirstOrDefault() ?? 0m,
                    TotalCost = hours.Select(x => (decimal?)x.TotalCost).FirstOrDefault() ?? 0m
                })
                .Where(x => x.EstimatedHours > 0 && x.ActualHours > x.EstimatedHours)
                .OrderByDescending(x => x.ActualHours - x.EstimatedHours)
                .Take(topN)
                .ToListAsync();

            var projectsOverEstimatedHours = projectsOverEstimatedRows
                .Select(x => ToTopProjectDto(x.Id, x.Name, x.Status, x.Phase, x.ProgressPercentage, x.ActualHours, x.EstimatedHours, x.Budget, x.TotalCost, x.EstimatedDueDate, now))
                .ToList();

            var delayedRows = await projectsQuery
                .Where(p => p.EstimatedDueDate.HasValue && p.EstimatedDueDate.Value < now && p.Status != ProjectStatus.Done)
                .OrderBy(p => p.EstimatedDueDate)
                .Take(topN)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Status,
                    p.Phase,
                    p.ProgressPercentage,
                    p.EstimatedHours,
                    p.Budget,
                    p.EstimatedDueDate
                })
                .ToListAsync();

            var topDelayedProjects = delayedRows
                .Select(x => ToTopProjectDto(x.Id, x.Name, x.Status, x.Phase, x.ProgressPercentage, 0m, x.EstimatedHours, x.Budget, 0m, x.EstimatedDueDate, now))
                .ToList();

            var onHoldRows = await projectsQuery
                .Where(p => p.Status == ProjectStatus.OnHold)
                .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
                .Take(topN)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Status,
                    p.Phase,
                    p.ProgressPercentage,
                    p.EstimatedHours,
                    p.Budget,
                    p.EstimatedDueDate
                })
                .ToListAsync();

            var topOnHoldProjects = onHoldRows
                .Select(x => ToTopProjectDto(x.Id, x.Name, x.Status, x.Phase, x.ProgressPercentage, 0m, x.EstimatedHours, x.Budget, 0m, x.EstimatedDueDate, now))
                .ToList();

            var dueSoonLimit = now.AddDays(14);
            var dueSoonRows = await projectsQuery
                .Where(p => p.EstimatedDueDate.HasValue &&
                    p.EstimatedDueDate.Value >= now &&
                    p.EstimatedDueDate.Value <= dueSoonLimit &&
                    p.Status != ProjectStatus.Done)
                .OrderBy(p => p.EstimatedDueDate)
                .Take(topN)
                .Select(p => new
                {
                    ProjectId = p.Id,
                    ProjectName = p.Name,
                    p.Status,
                    p.Phase,
                    p.EstimatedDueDate
                })
                .ToListAsync();

            var dueSoonProjects = dueSoonRows
                .Select(x => new DashboardAttentionProjectDto
                {
                    ProjectId = x.ProjectId,
                    ProjectName = x.ProjectName,
                    Status = x.Status.ToString(),
                    Phase = x.Phase.ToString(),
                    EstimatedDueDate = x.EstimatedDueDate,
                    Value = x.EstimatedDueDate.HasValue ? EFDateDiffDays(now, x.EstimatedDueDate.Value) : 0m
                })
                .ToList();

            var noActivityThreshold = now.AddDays(-30);
            var noRecentActivityRows = await projectsQuery
                .Where(p => p.Status != ProjectStatus.Done)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Status,
                    p.Phase,
                    p.EstimatedDueDate,
                    LastLoggedAt = p.HourEntries.Max(h => (DateTime?)h.Date),
                    TotalHours = p.HourEntries.Sum(h => (decimal?)h.TotalHours) ?? 0m
                })
                .Where(x => !x.LastLoggedAt.HasValue || x.LastLoggedAt.Value < noActivityThreshold)
                .OrderBy(x => x.LastLoggedAt ?? DateTime.MinValue)
                .Take(topN)
                .ToListAsync();

            var noRecentActivityProjects = noRecentActivityRows
                .Select(x => new DashboardAttentionProjectDto
                {
                    ProjectId = x.Id,
                    ProjectName = x.Name,
                    Status = x.Status.ToString(),
                    Phase = x.Phase.ToString(),
                    EstimatedDueDate = x.EstimatedDueDate,
                    LastLoggedAt = x.LastLoggedAt,
                    TotalHours = x.TotalHours,
                    Value = x.LastLoggedAt.HasValue ? EFDateDiffDays(x.LastLoggedAt.Value, now) : 999m
                })
                .ToList();

            var openRoadblockRows = await _context.ProjectRoadblocks
                .AsNoTracking()
                .Where(r => projectIdsQuery.Contains(r.ProjectId) && r.Status == RoadblockStatus.Open)
                .OrderBy(r => r.DueAt)
                .Take(Math.Max(topN, 10))
                .Select(r => new
                {
                    RoadblockId = r.Id,
                    ProjectId = r.ProjectId,
                    ProjectName = r.Project.Name,
                    Title = r.Title,
                    r.Status,
                    EnteredAt = r.EnteredAt,
                    r.DueAt
                })
                .ToListAsync();

            var openRoadblocks = openRoadblockRows
                .Select(x => new DashboardRoadblockDto
                {
                    RoadblockId = x.RoadblockId,
                    ProjectId = x.ProjectId,
                    ProjectName = x.ProjectName,
                    Title = x.Title,
                    Status = x.Status.ToString(),
                    EnteredAt = x.EnteredAt,
                    DueAt = x.DueAt,
                    DelayDays = x.DueAt < now ? EFDateDiffDays(x.DueAt, now) : 0m
                })
                .ToList();

            var riskRows = await projectsQuery
                .Where(p => p.Status != ProjectStatus.Done)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Status,
                    p.ProgressPercentage,
                    p.EstimatedDueDate
                })
                .ToListAsync();

            var riskMatrix = riskRows
                .Select(x =>
                {
                    var delayDays = x.EstimatedDueDate.HasValue && x.EstimatedDueDate.Value < now
                        ? EFDateDiffDays(x.EstimatedDueDate.Value, now)
                        : 0m;
                    var remainingProgress = Math.Max(100m - x.ProgressPercentage, 0m);
                    return new DashboardRiskMatrixProjectDto
                    {
                        ProjectId = x.Id,
                        ProjectName = x.Name,
                        DelayDays = delayDays,
                        RemainingProgress = remainingProgress,
                        RiskScore = Math.Round(delayDays * 2m + remainingProgress, 2),
                        Status = x.Status.ToString()
                    };
                })
                .OrderByDescending(x => x.RiskScore)
                .Take(Math.Max(topN, 20))
                .ToList();

            return new DashboardAdminBiDto
            {
                Filters = new DashboardBiFiltersDto
                {
                    Year = query.Year,
                    Month = query.Month,
                    Ytd = query.Ytd,
                    ProjectStatus = query.ProjectStatus,
                    ProjectPhase = query.ProjectPhase,
                    ProcessStatus = query.ProcessStatus,
                    DepartmentId = query.DepartmentId,
                    BusinessUnitId = query.BusinessUnitId,
                    PlantId = query.PlantId,
                    ProjectManagerId = query.ProjectManagerId,
                    RoleId = query.RoleId,
                    ProjectType = query.ProjectType,
                    ProjectManagementType = query.ProjectManagementType,
                    TopN = topN
                },
                Kpis = new DashboardBiKpisDto
                {
                    TotalProjects = totalProjects,
                    TotalTrackedHours = extended.Summary.TotalTrackedHours,
                    DelayedProjects = extended.Summary.DelayedProjects,
                    DelayRate = delayRate,
                    AverageOtd = extended.Summary.AverageOtd,
                    AverageEffectiveness = extended.Summary.AverageEffectiveness,
                    ActiveUsers = extended.Summary.ActiveUsers,
                    TrackedHoursVariancePercent = trackedHoursVariancePercent,
                    ProjectsKpiDelta = totalProjects - previousTotalProjects,
                    TrackedHoursDeltaPercent = previousTrackedHours > 0
                        ? Math.Round((extended.Summary.TotalTrackedHours - previousTrackedHours) * 100m / previousTrackedHours, 2)
                        : 0m,
                    DelayedProjectsDelta = extended.Summary.DelayedProjects - previousDelayedProjects
                },
                Charts = new DashboardBiChartsDto
                {
                    ProjectsByStatus = extended.PortfolioHealth.ProjectsByStatus,
                    ProjectsByPhase = extended.PortfolioHealth.ProjectsByPhase,
                    DelayRate = new List<DashboardLabelValueDto>
                    {
                        new() { Label = "Delayed", Value = delayRate },
                        new() { Label = "On Time", Value = Math.Max(100m - delayRate, 0m) }
                    },
                    HoursByCategory = extended.Workload.CategoryBreakdown,
                    HoursByStage = extended.Workload.HoursByStage,
                    MonthlyHoursByCategory = extended.Workload.MonthlyHoursByCategory,
                    MonthlyWorkloadTrend = monthlyWorkloadTrend,
                    PerformanceTrend = performanceTrend,
                    WorkloadByRole = workloadByRole,
                    WorkloadByDepartment = workloadByDepartment,
                    WorkloadByBusinessUnit = workloadByBusinessUnit,
                    CostSavingByDepartment = costSavingByDepartment,
                    CostSavingByBusinessUnit = costSavingByBusinessUnit,
                    ProjectsByPlant = projectsByPlant,
                    ProjectsByBusinessUnit = projectsByBusinessUnit,
                    EstimatedVsActualProjects = estimatedVsActualProjects,
                    RiskMatrix = riskMatrix
                },
                Tables = new DashboardBiTablesDto
                {
                    TopProjectsByHours = extended.TopProjects,
                    ProjectsOverEstimatedHours = projectsOverEstimatedHours,
                    TopDelayedProjects = topDelayedProjects,
                    TopOnHoldProjects = topOnHoldProjects,
                    DueSoonProjects = dueSoonProjects,
                    NoRecentActivityProjects = noRecentActivityProjects,
                    OpenRoadblocks = openRoadblocks
                },
                Alerts = extended.Alerts
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
                projectsQuery = projectsQuery.Where(p => p.ProjectManagementType == projectManagementType);

            var (periodStart, periodEnd) = ResolvePeriod(query.Year, query.Month);
            if (applyProjectPeriodFilter && periodStart.HasValue && periodEnd.HasValue)
            {
                var start = periodStart.Value;
                var end = periodEnd.Value;

                // Strict date filter by project start date.
                projectsQuery = projectsQuery.Where(p => p.StartDate >= start && p.StartDate <= end);
            }

            return projectsQuery;
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

            if (month.HasValue && !year.HasValue)
                year = DateTime.UtcNow.Year;

            if (year.HasValue && year.Value > 0)
                query = query.Where(h => h.Date.Year == year.Value);

            if (month.HasValue)
                query = query.Where(h => h.Date.Month == month.Value);

            return query;
        }

        private static IQueryable<HourEntry> ApplyYtdFilter(IQueryable<HourEntry> query, int? year, int? month)
        {
            if (month.HasValue && (month.Value < 1 || month.Value > 12))
                month = null;

            var ytdYear = year.HasValue && year.Value > 0 ? year.Value : DateTime.UtcNow.Year;
            query = query.Where(h => h.Date.Year == ytdYear);

            if (month.HasValue)
                query = query.Where(h => h.Date.Month <= month.Value);

            return query;
        }

        private static (DateTime? Start, DateTime? End) ResolvePeriod(int? year, int? month)
        {
            if (month.HasValue && (month.Value < 1 || month.Value > 12))
                month = null;

            if (!year.HasValue && !month.HasValue)
                return (null, null);

            var effectiveYear = year.HasValue && year.Value > 0 ? year.Value : DateTime.UtcNow.Year;

            if (month.HasValue)
            {
                var start = new DateTime(effectiveYear, month.Value, 1);
                var end = start.AddMonths(1).AddTicks(-1);
                return (start, end);
            }

            return (new DateTime(effectiveYear, 1, 1), new DateTime(effectiveYear, 12, 31, 23, 59, 59, 999));
        }

        private static decimal ResolveExpectedHours(DashboardQueryDto query, int workloadYear)
        {
            if (query.Month.HasValue && query.Month.Value >= 1 && query.Month.Value <= 12)
                return HourEntry.ExpectedMonthlyHours;

            var currentYear = DateTime.UtcNow.Year;
            var currentMonth = DateTime.UtcNow.Month;
            var months = query.Ytd && workloadYear == currentYear
                ? currentMonth
                : 12;

            return HourEntry.ExpectedMonthlyHours * months;
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

        private static bool TryParseProjectManagementType(string? value, out ProjectManagementType type)
        {
            return Enum.TryParse(value, true, out type);
        }

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

            if (query.Month.HasValue)
            {
                var year = query.Year.HasValue && query.Year.Value > 0
                    ? query.Year.Value
                    : DateTime.UtcNow.Year;
                var currentMonth = new DateTime(year, query.Month.Value, 1);
                var previousMonth = currentMonth.AddMonths(-1);
                previous.Year = previousMonth.Year;
                previous.Month = previousMonth.Month;
                previous.Ytd = false;
                return previous;
            }

            previous.Year = (query.Year.HasValue && query.Year.Value > 0 ? query.Year.Value : DateTime.UtcNow.Year) - 1;
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
    }
}
