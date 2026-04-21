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

            var otdDenominator = await projectsQuery.CountAsync(p => p.Status == ProjectStatus.Done && p.EstimatedDueDate.HasValue);
            var otdNumerator = await projectsQuery.CountAsync(p =>
                p.Status == ProjectStatus.Done &&
                p.EstimatedDueDate.HasValue &&
                p.EndDate.HasValue &&
                p.EndDate.Value <= p.EstimatedDueDate.Value);
            var averageOtd = otdDenominator == 0 ? 0m : Math.Round((decimal)otdNumerator * 100m / otdDenominator, 2);

            var avgEffectivenessRaw = await projectsQuery
                .Where(p => p.EstimatedHours > 0 && p.ActualHours > 0)
                .Select(p => (decimal?)((p.EstimatedHours / p.ActualHours) * 100m))
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
                    DeliveryMetrics = deliveryMetrics
                }
            };
        }

        private IQueryable<Project> BuildScopedProjectsQuery(DashboardQueryDto query, bool isAdminScope, Guid? userId)
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

            if (query.RoleId.HasValue && query.RoleId.Value != Guid.Empty)
                projectsQuery = projectsQuery.Where(p => p.ProjectMembers.Any(pm => pm.RoleId == query.RoleId.Value));

            var (periodStart, periodEnd) = ResolvePeriod(query.Year, query.Month);
            if (periodStart.HasValue && periodEnd.HasValue)
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

        private static bool TrySet<T>(out T target, T value)
        {
            target = value;
            return true;
        }
    }
}