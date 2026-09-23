using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IRepositories;
using PMHUB.Application.IServices;
using PMHUB.Domain.Entities;
using PMHUB.Domain.Enums;
using PMHUB.Infrastructure.Persistence;
using PMHUB.Shared.Models;

namespace PMHUB.Infrastructure.Repositories.Implementation
{
    public class AnalyticsRepository : IAnalyticsRepository
    {
        private const string PlaceholderDepartmentName = "-";

        private static readonly IReadOnlyDictionary<string, decimal> DefaultKpiTargets =
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["OTD"] = 85m,
                ["Effectiveness"] = 85m,
                ["CSA"] = 85m,
                ["MonthlyWorkingHours"] = 182.75m,
                ["TAH"] = 171.9m
            };

        private const decimal DefaultTahMonthlyHoursTarget = 171.9m;

        private readonly PMHubDbContext _context;
        private readonly ITargetSettingsService _targetSettingsService;
        private CompanyStandards _standards;
        private decimal _monthlyHoursTargetPerMember;

        public AnalyticsRepository(PMHubDbContext context, IOptions<CompanyStandards> standards, ITargetSettingsService targetSettingsService)
        {
            _context = context;
            _standards = standards.Value;
            _targetSettingsService = targetSettingsService;
            _monthlyHoursTargetPerMember = _standards.MonthlyHoursTarget;
        }

        public async Task<AnalyticsDashboardDto> GetDashboardAsync(AnalyticsQueryDto query)
        {
            return await BuildDashboardAsync(query, includeKpis: true, includeHours: true);
        }

        public async Task<AnalyticsSummaryDto> GetSummaryAsync(AnalyticsQueryDto query)
        {
            var dashboard = await BuildDashboardAsync(query, includeKpis: false, includeHours: false);
            return dashboard.Summary;
        }

        public async Task<AnalyticsKpisDto> GetKpisAsync(AnalyticsQueryDto query)
        {
            var dashboard = await BuildDashboardAsync(query, includeKpis: true, includeHours: false);
            return dashboard.Kpis;
        }

        public async Task<AnalyticsHoursDto> GetHoursAsync(AnalyticsQueryDto query)
        {
            var dashboard = await BuildDashboardAsync(query, includeKpis: false, includeHours: true);
            return dashboard.Hours;
        }

        private async Task<AnalyticsDashboardDto> BuildDashboardAsync(
            AnalyticsQueryDto query,
            bool includeKpis,
            bool includeHours)
        {
            await LoadTargetSettingsAsync();

            var period = ResolvePeriod(query);
            var startDate = period.StartDate.ToDateTime(TimeOnly.MinValue);
            var endDate = period.EndDate.ToDateTime(TimeOnly.MaxValue);
            var projectsQuery = BuildProjectsQuery(query);
            var projectIds = await projectsQuery.Select(p => p.Id).ToListAsync();

            var selectedHoursQuery = BuildHoursQuery(projectIds, query.UserId, startDate, endDate);

            var selectedHours = await selectedHoursQuery.ToListAsync();
            var selectedKpis = await BuildKpiQuery(projectIds).ToListAsync();

            var totalProjects = await projectsQuery.CountAsync();
            var activeTeamMembers = selectedHours.Select(h => h.UserId).Distinct().Count();
            var totalHours = selectedHours.Sum(h => h.TotalHours);
            var targetHours = activeTeamMembers * _standards.MonthlyHoursTarget;
            var averageUtilization = CalculatePercentage(totalHours, targetHours);
            var averageMonthlyHours = CalculateAverageMonthlyHours(totalHours, startDate, endDate);

            var projectMetricRows = await projectsQuery
                .Select(p => new ProjectMetricRow
                {
                    Id = p.Id,
                    Status = p.Status,
                    StartDate = p.StartDate,
                    EstimatedHours = p.EstimatedHours,
                    ActualHours = p.ActualHours,
                    EndDate = p.EndDate,
                    EstimatedDueDate = p.EstimatedDueDate
                })
                .ToListAsync();

            var periodProjectIds = ResolvePeriodProjectIds(projectMetricRows, selectedHours, selectedKpis, startDate, endDate);
            var periodProjectMetrics = projectMetricRows
                .Where(p => periodProjectIds.Contains(p.Id))
                .ToList();
            var periodKpis = selectedKpis
                .Where(k => periodProjectIds.Contains(k.ProjectId))
                .ToList();

            var projectsWithData = periodProjectIds
                .Distinct()
                .Count();

            var monthlyPeriods = BuildMonthlyPeriods(startDate, endDate);
            var monthlyTrend = includeKpis
                ? BuildKpiTrend(projectMetricRows, selectedKpis, selectedHours, monthlyPeriods)
                : new List<AnalyticsKpiMonthlyTrendDto>();
            var monthlyByCategory = includeHours
                ? BuildMonthlyHoursByCategory(selectedHours, monthlyPeriods, _monthlyHoursTargetPerMember)
                : new List<AnalyticsMonthlyHoursByCategoryDto>();
            var utilizationTrend = includeHours
                ? BuildUtilizationTrend(selectedHours, monthlyPeriods)
                : new List<AnalyticsUtilizationTrendDto>();
            var businessUnitEffort = includeHours
                ? await BuildBusinessUnitEffortAsync(projectIds, query.UserId, startDate, endDate)
                : new List<AnalyticsBusinessUnitEffortDto>();

            return new AnalyticsDashboardDto
            {
                Period = period,
                Summary = new AnalyticsSummaryDto
                {
                    AverageEffectiveness = CalculateEffectiveness(periodProjectMetrics, periodKpis),
                    AverageOtd = CalculateOtd(periodProjectMetrics, periodKpis),
                    OtdDiagnostics = CalculateOtdDiagnostics(periodProjectMetrics, periodKpis),
                    AverageCsat = CalculateNamedKpiAverage(periodKpis, "CSA"),
                    TotalProjects = totalProjects,
                    ProjectsWithData = projectsWithData,
                    TotalHours = Math.Round(totalHours, 2),
                    YtdHours = Math.Round(totalHours, 2),
                    AverageMonthlyHours = averageMonthlyHours,
                    AverageUtilization = averageUtilization,
                    ActiveTeamMembers = activeTeamMembers
                },
                Kpis = new AnalyticsKpisDto
                {
                    MonthlyTrend = monthlyTrend
                },
                Hours = new AnalyticsHoursDto
                {
                    MonthlyByCategory = monthlyByCategory,
                    UtilizationTrend = utilizationTrend,
                    ByBusinessUnit = businessUnitEffort
                }
            };
        }

        public async Task<AnalyticsFiltersDto> GetFiltersAsync()
        {
            await LoadTargetSettingsAsync();

            var fiscalYearStartMonth = GetFiscalYearStartMonth();
            var currentFiscalYear = ResolveFiscalYear(DateTime.UtcNow.Year, DateTime.UtcNow.Month, fiscalYearStartMonth);
            var projectYears = await _context.Projects
                .AsNoTracking()
                .Select(p => p.StartDate)
                .ToListAsync();
            var hourYears = await _context.HourEntries
                .AsNoTracking()
                .Select(h => h.Date)
                .ToListAsync();

            var fiscalYears = projectYears
                .Concat(hourYears)
                .Select(d => ResolveFiscalYear(d.Year, d.Month, fiscalYearStartMonth))
                .Append(currentFiscalYear)
                .Distinct()
                .OrderBy(y => y)
                .ToList();

            return new AnalyticsFiltersDto
            {
                FiscalYears = fiscalYears,
                Months = Enumerable.Range(0, 12)
                    .Select(offset => ((fiscalYearStartMonth - 1 + offset) % 12) + 1)
                    .Select(month => new AnalyticsMonthOptionDto
                    {
                        Value = month,
                        Label = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month)
                    })
                    .ToList(),
                Users = await _context.Users
                    .OfType<NormalUser>()
                    .AsNoTracking()
                    .Where(u => u.IsActive && u.IsApproved)
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Select(u => new AnalyticsOptionDto<Guid>
                    {
                        Id = u.Id,
                        Label = (u.FirstName + " " + u.LastName).Trim()
                    })
                    .ToListAsync(),
                Projects = await _context.Projects
                    .AsNoTracking()
                    .OrderBy(p => p.Name)
                    .Select(p => new AnalyticsOptionDto<Guid>
                    {
                        Id = p.Id,
                        Label = p.Name
                    })
                    .ToListAsync(),
                Departments = await _context.Departments
                    .AsNoTracking()
                    .Where(d => d.Name.Trim() != PlaceholderDepartmentName)
                    .OrderBy(d => d.Name)
                    .Select(d => new AnalyticsOptionDto<Guid>
                    {
                        Id = d.Id,
                        Label = d.Name
                    })
                    .ToListAsync(),
                BusinessUnits = await _context.BusinessUnits
                    .AsNoTracking()
                    .OrderBy(bu => bu.Name)
                    .Select(bu => new AnalyticsOptionDto<Guid>
                    {
                        Id = bu.Id,
                        Label = bu.Name
                    })
                    .ToListAsync(),
                Plants = await _context.Plants
                    .AsNoTracking()
                    .OrderBy(p => p.Name)
                    .Select(p => new AnalyticsOptionDto<Guid>
                    {
                        Id = p.Id,
                        Label = p.Name
                    })
                    .ToListAsync()
            };
        }

        private IQueryable<Project> BuildProjectsQuery(AnalyticsQueryDto query)
        {
            var projectsQuery = _context.Projects.AsNoTracking().AsQueryable();

            if (query.ProjectId.HasValue && query.ProjectId.Value != Guid.Empty)
                projectsQuery = projectsQuery.Where(p => p.Id == query.ProjectId.Value);

            if (query.UserId.HasValue && query.UserId.Value != Guid.Empty)
            {
                var userId = query.UserId.Value;
                projectsQuery = projectsQuery.Where(p =>
                    p.ProjectManagerId == userId ||
                    p.ProjectMembers.Any(pm => pm.UserId == userId) ||
                    p.HourEntries.Any(h => h.UserId == userId));
            }

            if (TryParseProjectStatus(query.ProjectStatus, out var projectStatus))
                projectsQuery = projectsQuery.Where(p => p.Status == projectStatus);

            if (TryParseProjectPhase(query.ProjectPhase, out var projectPhase))
                projectsQuery = projectsQuery.Where(p => p.Phase == projectPhase);

            if (query.DepartmentId.HasValue && query.DepartmentId.Value != Guid.Empty)
                projectsQuery = projectsQuery.Where(p => p.DepartmentId == query.DepartmentId.Value);

            if (query.BusinessUnitId.HasValue && query.BusinessUnitId.Value != Guid.Empty)
                projectsQuery = projectsQuery.Where(p => p.ProjectBusinessUnits.Any(pbu => pbu.BusinessUnitId == query.BusinessUnitId.Value));

            if (query.PlantId.HasValue && query.PlantId.Value != Guid.Empty)
                projectsQuery = projectsQuery.Where(p => p.Department != null && p.Department.PlantId == query.PlantId.Value);

            return projectsQuery;
        }

        private IQueryable<HourEntry> BuildHoursQuery(IEnumerable<Guid> projectIds, Guid? userId, DateTime startDate, DateTime endDate)
        {
            var ids = projectIds.ToList();
            var query = _context.HourEntries
                .AsNoTracking()
                .Where(h => h.ProjectId.HasValue && ids.Contains(h.ProjectId.Value) && h.Date >= startDate && h.Date <= endDate &&
                            h.User.IsActive && h.User.IsApproved &&
                            h.User.RoleId.HasValue && h.User.Role != null &&
                            h.User.Role.Name.ToLower() != "intern" &&
                            h.User.Role.Name.ToLower() != "stagiaire");

            if (userId.HasValue && userId.Value != Guid.Empty)
                query = query.Where(h => h.UserId == userId.Value);

            return query;
        }

        private IQueryable<KPI> BuildKpiQuery(IEnumerable<Guid> projectIds)
        {
            var ids = projectIds.ToList();
            return _context.KPIs
                .AsNoTracking()
                .Where(k => ids.Contains(k.ProjectId));
        }

        private AnalyticsPeriodDto ResolvePeriod(AnalyticsQueryDto query)
        {
            var today = DateTime.UtcNow.Date;
            var month = query.Month is >= 1 and <= 12 ? query.Month.Value : today.Month;
            var fiscalYearStartMonth = GetFiscalYearStartMonth();
            var isMonthMode = query.Month.HasValue &&
                (IsMode(query.PeriodMode, "month") || IsMode(query.QuickSelect, "month") || query.Year.HasValue);

            int fiscalYear;
            int calendarYear;

            if (isMonthMode && query.Year.HasValue && query.Year.Value > 0)
            {
                calendarYear = query.Year.Value;
                fiscalYear = ResolveFiscalYear(calendarYear, month, fiscalYearStartMonth);
            }
            else
            {
                fiscalYear = query.FiscalYear.GetValueOrDefault(
                    query.Year.GetValueOrDefault(ResolveFiscalYear(today.Year, today.Month, fiscalYearStartMonth)));
                calendarYear = month >= fiscalYearStartMonth
                    ? fiscalYear - (fiscalYearStartMonth == 1 ? 0 : 1)
                    : fiscalYear;
            }

            var fiscalYearStartDate = GetFiscalYearStartDate(fiscalYear, fiscalYearStartMonth);
            var fiscalYearEndDate = fiscalYearStartDate.AddYears(1).AddTicks(-1);
            var startDate = query.Month.HasValue
                ? new DateTime(calendarYear, month, 1)
                : fiscalYearStartDate;
            var endDate = query.Month.HasValue
                ? startDate.AddMonths(1).AddTicks(-1)
                : today;
            if (endDate > fiscalYearEndDate)
                endDate = fiscalYearEndDate;

            return new AnalyticsPeriodDto
            {
                Year = calendarYear,
                Month = month,
                MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                StartDate = DateOnly.FromDateTime(startDate),
                EndDate = DateOnly.FromDateTime(endDate),
                FiscalYear = fiscalYear,
                FiscalYearStartMonth = fiscalYearStartMonth,
                FiscalYearStartDate = DateOnly.FromDateTime(fiscalYearStartDate),
                FiscalYearEndDate = DateOnly.FromDateTime(fiscalYearEndDate)
            };
        }

        private int GetFiscalYearStartMonth()
        {
            return _standards.FiscalYearStartMonth is >= 1 and <= 12
                ? _standards.FiscalYearStartMonth
                : 10;
        }

        private static int ResolveFiscalYear(int year, int month, int fiscalYearStartMonth)
        {
            return month >= fiscalYearStartMonth
                ? year + (fiscalYearStartMonth == 1 ? 0 : 1)
                : year;
        }

        private static bool IsMode(string? value, string expected)
        {
            return string.Equals(value?.Trim(), expected, StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime GetFiscalYearStartDate(int fiscalYear, int fiscalYearStartMonth)
        {
            var startYear = fiscalYear - (fiscalYearStartMonth == 1 ? 0 : 1);
            return new DateTime(startYear, fiscalYearStartMonth, 1);
        }

        private static List<(DateTime Start, DateTime End)> BuildMonthlyPeriods(DateTime startDate, DateTime endDate)
        {
            var months = new List<(DateTime Start, DateTime End)>();
            var current = new DateTime(startDate.Year, startDate.Month, 1);
            var last = new DateTime(endDate.Year, endDate.Month, 1);

            while (current <= last)
            {
                months.Add((current, current.AddMonths(1).AddTicks(-1)));
                current = current.AddMonths(1);
            }

            return months;
        }

        private static List<AnalyticsKpiMonthlyTrendDto> BuildKpiTrend(
            List<ProjectMetricRow> projects,
            List<KPI> kpis,
            List<HourEntry> hours,
            List<(DateTime Start, DateTime End)> monthlyPeriods)
        {
            return monthlyPeriods
                .Select(period =>
                {
                    var activeProjectIds = projects
                        .Where(p => IsProjectInPeriod(p, period.Start, period.End))
                        .Select(p => p.Id);
                    var monthProjectIds = hours
                        .Where(h => h.Date >= period.Start && h.Date <= period.End)
                        .Where(h => h.ProjectId.HasValue)
                        .Select(h => h.ProjectId!.Value)
                        .Concat(activeProjectIds)
                        .Distinct()
                        .ToList();
                    var monthKpis = kpis
                        .Where(k => monthProjectIds.Contains(k.ProjectId))
                        .ToList();

                    return new AnalyticsKpiMonthlyTrendDto
                    {
                        Year = period.Start.Year,
                        Month = period.Start.Month,
                        MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(period.Start.Month),
                        Effectiveness = CalculateEffectiveness(projects.Where(p => monthProjectIds.Contains(p.Id)).ToList(), monthKpis),
                        Otd = CalculateOtd(projects.Where(p => monthProjectIds.Contains(p.Id)).ToList(), monthKpis),
                        Csat = CalculateNamedKpiAverage(monthKpis, "CSA"),
                        ProjectsWithData = monthProjectIds.Count
                    };
                })
                .ToList();
        }

        private static HashSet<Guid> ResolvePeriodProjectIds(
            List<ProjectMetricRow> projects,
            List<HourEntry> hours,
            List<KPI> kpis,
            DateTime startDate,
            DateTime endDate)
        {
            return projects
                .Where(p => IsProjectInPeriod(p, startDate, endDate))
                .Select(p => p.Id)
                .Concat(hours.Where(h => h.ProjectId.HasValue).Select(h => h.ProjectId!.Value))
                .ToHashSet();
        }

        private static bool IsProjectInPeriod(ProjectMetricRow project, DateTime startDate, DateTime endDate)
        {
            if (project.Status == ProjectStatus.Done)
            {
                return project.EndDate.HasValue &&
                    project.EndDate.Value >= startDate &&
                    project.EndDate.Value <= endDate;
            }

            return project.StartDate <= endDate &&
                (!project.EndDate.HasValue || project.EndDate.Value >= startDate);
        }

        private static List<AnalyticsMonthlyHoursByCategoryDto> BuildMonthlyHoursByCategory(
            List<HourEntry> hours,
            List<(DateTime Start, DateTime End)> monthlyPeriods,
            decimal targetHoursPerMember)
        {
            return monthlyPeriods
                .Select(period =>
                {
                    var monthHours = hours
                        .Where(h => h.Date >= period.Start && h.Date <= period.End)
                        .ToList();
                    var activeNonInternMembers = monthHours.Select(h => h.UserId).Distinct().Count();
                    var targetHours = activeNonInternMembers * targetHoursPerMember;

                    return new AnalyticsMonthlyHoursByCategoryDto
                    {
                        Year = period.Start.Year,
                        Month = period.Start.Month,
                        MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(period.Start.Month),
                        ExecutionHours = Math.Round(monthHours.Sum(h => h.ExecutionHours), 2),
                        TechnicalSupervisionHours = Math.Round(monthHours.Sum(h => h.SupervisionHours), 2),
                        ProcessHours = Math.Round(monthHours.Sum(h => h.ProcessHours), 2),
                        ProjectManagementHours = Math.Round(monthHours.Sum(h => h.ManagementHours), 2),
                        ResearchAndDevHours = Math.Round(monthHours.Sum(h => h.RAndDHours), 2),
                        WorkshopHours = Math.Round(monthHours.Sum(h => h.WorkshopHours), 2),
                        OtherHours = Math.Round(monthHours.Sum(h => h.OtherHours), 2),
                        InternManagementHours = Math.Round(monthHours.Sum(h => h.InternManagementHours), 2),
                        TotalHours = Math.Round(monthHours.Sum(h => h.TotalHours), 2),
                        ActiveNonInternMembers = activeNonInternMembers,
                        TargetHoursPerMember = Math.Round(targetHoursPerMember, 2),
                        TargetHours = Math.Round(targetHours, 2)
                    };
                })
                .ToList();
        }

        private List<AnalyticsUtilizationTrendDto> BuildUtilizationTrend(
            List<HourEntry> hours,
            List<(DateTime Start, DateTime End)> monthlyPeriods)
        {
            return monthlyPeriods
                .Select(period =>
                {
                    var monthHours = hours
                        .Where(h => h.Date >= period.Start && h.Date <= period.End)
                        .ToList();
                    var activeUsers = monthHours.Select(h => h.UserId).Distinct().Count();
                    var loggedHours = monthHours.Sum(h => h.TotalHours);
                    var targetHours = activeUsers * _monthlyHoursTargetPerMember;

                    return new AnalyticsUtilizationTrendDto
                    {
                        Year = period.Start.Year,
                        Month = period.Start.Month,
                        MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(period.Start.Month),
                        LoggedHours = Math.Round(loggedHours, 2),
                        TargetHours = Math.Round(targetHours, 2),
                        UtilizationPercentage = CalculatePercentage(loggedHours, targetHours)
                    };
                })
                .ToList();
        }

        private async Task<List<AnalyticsBusinessUnitEffortDto>> BuildBusinessUnitEffortAsync(
            IEnumerable<Guid> projectIds,
            Guid? userId,
            DateTime startDate,
            DateTime endDate)
        {
            var ids = projectIds.ToList();
            if (ids.Count == 0)
                return new List<AnalyticsBusinessUnitEffortDto>();

            var query = _context.HourEntries
                .AsNoTracking()
                .Where(h => h.ProjectId.HasValue && ids.Contains(h.ProjectId.Value) && h.Date >= startDate && h.Date <= endDate &&
                            h.User.IsActive && h.User.IsApproved);

            if (userId.HasValue && userId.Value != Guid.Empty)
                query = query.Where(h => h.UserId == userId.Value);

            var rows = await query
                .SelectMany(h => h.Project!.ProjectBusinessUnits.Select(pbu => new
                {
                    pbu.BusinessUnitId,
                    BusinessUnitName = pbu.BusinessUnit.Name,
                    ProjectId = h.ProjectId!.Value,
                    h.TotalHours
                }))
                .GroupBy(x => new { x.BusinessUnitId, x.BusinessUnitName })
                .Select(g => new
                {
                    g.Key.BusinessUnitId,
                    g.Key.BusinessUnitName,
                    TotalHours = g.Sum(x => x.TotalHours),
                    ProjectCount = g.Select(x => x.ProjectId).Distinct().Count()
                })
                .OrderByDescending(x => x.TotalHours)
                .ToListAsync();

            var totalHours = rows.Sum(x => x.TotalHours);

            return rows
                .Select(x => new AnalyticsBusinessUnitEffortDto
                {
                    BusinessUnitId = x.BusinessUnitId,
                    BusinessUnitName = x.BusinessUnitName,
                    TotalHours = Math.Round(x.TotalHours, 2),
                    ProjectCount = x.ProjectCount,
                    Percentage = CalculatePercentage(x.TotalHours, totalHours)
                })
                .ToList();
        }

        private static decimal CalculateEffectiveness(List<ProjectMetricRow> projects, List<KPI> kpis)
        {
            var kpiAverage = CalculateNamedKpiAverage(kpis, "Effectiveness");
            if (kpiAverage > 0)
                return kpiAverage;

            return CalculateAverage(projects.Select(p => CalculateEffectivenessPercentage(p.EstimatedHours, p.ActualHours)));
        }

        private static decimal CalculateNamedKpiAverage(List<KPI> kpis, string name)
        {
            return TryCalculateNamedKpiAverage(kpis, name) ?? 0m;
        }

        private static decimal? TryCalculateNamedKpiAverage(List<KPI> kpis, string name)
        {
            var values = kpis
                .Where(k => IsMatchingKpiName(k.Name, name))
                .Select(k => CalculateKpiScore(k, name))
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            return values.Count == 0 ? null : Math.Round(values.Average(), 2);
        }

        private static decimal CalculateOtd(List<ProjectMetricRow> projects, List<KPI> kpis)
        {
            var projectsById = projects.ToDictionary(p => p.Id);
            var kpiScores = kpis
                .Where(k => IsMatchingKpiName(k.Name, "OTD"))
                .Select(k => CalculateOtdKpiScore(k, projectsById))
                .Where(score => score.HasValue)
                .Select(score => score!.Value)
                .ToList();

            // When OTD KPIs exist, their manual value (or their date calculation) is the source of truth.
            if (kpiScores.Count > 0)
                return Math.Round(kpiScores.Average(), 2);

            var completedProjects = projects
                .Where(p => p.Status == ProjectStatus.Done && p.EstimatedDueDate.HasValue && p.EndDate.HasValue)
                .ToList();

            if (completedProjects.Count == 0)
                return 0m;

            var completedOnTime = completedProjects.Count(p =>
                p.EndDate!.Value.Date <= p.EstimatedDueDate!.Value.Date);

            return Math.Round(completedOnTime * 100m / completedProjects.Count, 2);
        }

        private static decimal? CalculateOtdKpiScore(KPI kpi, IReadOnlyDictionary<Guid, ProjectMetricRow> projectsById)
        {
            // CurrentValue is the manually entered KPI value and is intentionally evaluated first.
            if (kpi.IsManualValue || kpi.CurrentValue > 0)
                return NormalizePercentage(kpi.CurrentValue);

            projectsById.TryGetValue(kpi.ProjectId, out var project);
            var dueDate = kpi.EstimatedDueDate ?? project?.EstimatedDueDate;
            var endDate = kpi.ActualEndDate ?? project?.EndDate;

            if (!dueDate.HasValue || !endDate.HasValue)
                return null;

            if (endDate.Value.Date <= dueDate.Value.Date)
                return 100m;

            if (project is null || endDate.Value <= project.StartDate)
                return null;

            var plannedDuration = dueDate.Value - project.StartDate;
            var actualDuration = endDate.Value - project.StartDate;
            if (plannedDuration <= TimeSpan.Zero || actualDuration <= TimeSpan.Zero)
                return null;

            return Math.Round(Math.Min(100m, (decimal)(plannedDuration.TotalDays / actualDuration.TotalDays) * 100m), 2);
        }

        private static AnalyticsOtdDiagnosticsDto CalculateOtdDiagnostics(
            List<ProjectMetricRow> projects,
            List<KPI> kpis)
        {
            var completedProjects = projects
                .Where(p => p.Status == ProjectStatus.Done)
                .ToList();
            var eligibleProjects = completedProjects
                .Where(p => p.EstimatedDueDate.HasValue && p.EndDate.HasValue)
                .ToList();
            var manualOtdKpis = kpis
                .Count(k => IsMatchingKpiName(k.Name, "OTD") && (k.IsManualValue || k.CurrentValue > 0));
            var projectsById = projects.ToDictionary(p => p.Id);
            var calculatedOtdKpis = kpis
                .Where(k => IsMatchingKpiName(k.Name, "OTD") && !k.IsManualValue && k.CurrentValue <= 0)
                .Count(k => CalculateOtdKpiScore(k, projectsById).HasValue);

            return new AnalyticsOtdDiagnosticsDto
            {
                CompletedProjectsWithOtdDates = eligibleProjects.Count,
                CompletedOnTimeProjects = eligibleProjects.Count(p =>
                    p.EndDate!.Value.Date <= p.EstimatedDueDate!.Value.Date),
                CompletedLateProjects = eligibleProjects.Count(p =>
                    p.EndDate!.Value.Date > p.EstimatedDueDate!.Value.Date),
                ProjectsExcludedForMissingOtdDates = completedProjects.Count(p =>
                    !p.EstimatedDueDate.HasValue || !p.EndDate.HasValue),
                IncompleteProjectsExcludedFromOtd = projects.Count(p => p.Status != ProjectStatus.Done),
                ManualOtdKpis = manualOtdKpis,
                CalculatedOtdKpis = calculatedOtdKpis
            };
        }

        private static decimal? CalculateKpiScore(KPI kpi, string name)
        {
            var rawValue = kpi.CalculatedValue ?? (kpi.IsManualValue || kpi.CurrentValue > 0 ? kpi.CurrentValue : null);
            if (!rawValue.HasValue)
                return null;

            if (string.Equals(name, "OTD", StringComparison.OrdinalIgnoreCase))
                return NormalizePercentage(rawValue.Value);

            if (string.Equals(name, "Effectiveness", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "CSA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "CSAT", StringComparison.OrdinalIgnoreCase))
            {
                return NormalizePercentage(rawValue.Value);
            }

            var target = kpi.TargetValue > 0
                ? kpi.TargetValue
                : GetDefaultKpiTarget(kpi.Name);

            if (target <= 0)
                return NormalizePercentage(rawValue.Value);

            return CalculateTargetScore(rawValue.Value, target);
        }

        private static bool IsMatchingKpiName(string actualName, string requestedName)
        {
            if (string.Equals(requestedName, "CSA", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(requestedName, "CSAT", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(actualName, "CSAT", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(actualName, "CSA", StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(actualName, requestedName, StringComparison.OrdinalIgnoreCase);
        }

        private static decimal GetDefaultKpiTarget(string name)
        {
            return string.Equals(name, "CSAT", StringComparison.OrdinalIgnoreCase)
                ? DefaultKpiTargets.GetValueOrDefault("CSA", 0m)
                : DefaultKpiTargets.GetValueOrDefault(name, 0m);
        }

        private static decimal CalculateTargetScore(decimal rawValue, decimal target)
        {
            if (rawValue <= 0)
                return 0m;

            return target <= 0
                ? NormalizePercentage(rawValue)
                : NormalizePercentage(rawValue * 100m / target);
        }

        private static decimal CalculatePercentage(decimal numerator, decimal denominator)
        {
            return denominator <= 0 ? 0m : NormalizePercentage(numerator * 100m / denominator);
        }

        private static decimal CalculateAchievementPercentage(decimal numerator, decimal denominator)
        {
            if (denominator <= 0m || numerator <= 0m)
                return 0m;

            return Math.Round(numerator * 100m / denominator, 2);
        }

        private static decimal CalculateEffectivenessPercentage(decimal estimatedHours, decimal actualHours)
        {
            return actualHours <= 0 ? 0m : NormalizePercentage(estimatedHours * 100m / actualHours);
        }

        private static decimal CalculateAverage(IEnumerable<decimal> values)
        {
            var normalized = values.Where(v => v > 0).ToList();
            return normalized.Count == 0 ? 0m : Math.Round(normalized.Average(), 2);
        }

        private static decimal NormalizePercentage(decimal value)
        {
            if (value <= 0)
                return 0m;

            var normalized = value <= 1m ? value * 100m : value;
            return Math.Round(Math.Min(normalized, 100m), 2);
        }

        private static decimal CalculateAverageMonthlyHours(decimal totalHours, DateTime startDate, DateTime endDate)
        {
            var elapsedMonths = ((endDate.Year - startDate.Year) * 12) +
                endDate.Month -
                startDate.Month +
                1;

            return elapsedMonths <= 0 ? 0m : Math.Round(totalHours / elapsedMonths, 2);
        }

        private static bool TryParseProjectStatus(string? value, out ProjectStatus status)
        {
            return Enum.TryParse(value, true, out status);
        }

        private static bool TryParseProjectPhase(string? value, out ProjectPhase phase)
        {
            return Enum.TryParse(value, true, out phase);
        }

        private async Task LoadTargetSettingsAsync()
        {
            _standards = await _targetSettingsService.GetCompanyStandardsAsync();
            var kpiTargets = await _targetSettingsService.GetActiveKpiTargetValuesAsync();
            _monthlyHoursTargetPerMember = kpiTargets.TryGetValue("MonthlyWorkingHours", out var target)
                ? target
                : _standards.MonthlyHoursTarget;
        }

        private class ProjectMetricRow
        {
            public Guid Id { get; set; }
            public ProjectStatus Status { get; set; }
            public DateTime StartDate { get; set; }
            public decimal EstimatedHours { get; set; }
            public decimal ActualHours { get; set; }
            public DateTime? EndDate { get; set; }
            public DateTime? EstimatedDueDate { get; set; }
        }

        public async Task<CapacityPriceDashboardDto> GetCapacityPriceDashboardAsync(CapacityPriceQueryDto query)
        {
            await LoadTargetSettingsAsync();

            var year = query.Year > 0 ? query.Year : DateTime.UtcNow.Year;
            var month = query.Month > 0 ? query.Month : DateTime.UtcNow.Month;

            if (month is < 1 or > 12)
                throw new ArgumentOutOfRangeException(nameof(query.Month), "Month must be between 1 and 12.");

            if (query.TargetHoursPerMember < 0)
                throw new ArgumentOutOfRangeException(nameof(query.TargetHoursPerMember), "Target hours per member cannot be negative.");

            if (query.HourlyRate < 0)
                throw new ArgumentOutOfRangeException(nameof(query.HourlyRate), "Hourly rate cannot be negative.");

            var targetHoursPerMember = query.TargetHoursPerMember is > 0
                ? query.TargetHoursPerMember.Value
                : _standards.MonthlyHoursTarget;

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddTicks(-1);

            // The normal-member dashboard includes both internal employees and
            // subcontractors. Interns are deliberately kept out: their hours can
            // be booked through allocations as well as user accounts and are
            // therefore reported by the dedicated intern dashboard.
            var activeMembers = await _context.Users
                .OfType<NormalUser>()
                .AsNoTracking()
                .Where(u => u.IsActive && u.IsApproved &&
                            (u.MemberType == MemberType.Employee || u.MemberType == MemberType.Subcontractor) &&
                            (u.Role == null ||
                             (u.Role.Name.ToLower() != "intern" && u.Role.Name.ToLower() != "stagiaire")) &&
                            !_context.Interns.Any(i => i.Name == (u.FirstName + " " + u.LastName).Trim()))
                .Select(u => new 
                { 
                    u.Id, 
                    u.FirstName, 
                    u.LastName,
                    u.MemberType
                })
                .ToListAsync();

            var activeMemberIds = activeMembers.Select(u => u.Id).ToList();

            var memberCounts = await _context.Users
                .OfType<NormalUser>()
                .AsNoTracking()
                .Where(u => u.IsActive && u.IsApproved)
                .GroupBy(_ => 1)
                .Select(g => new MemberPopulationDto
                {
                    EmployeeCount = g.Count(u => u.MemberType == MemberType.Employee &&
                                                 (u.Role == null ||
                                                  (u.Role.Name.ToLower() != "intern" && u.Role.Name.ToLower() != "stagiaire")) &&
                                                 !_context.Interns.Any(i => i.Name == (u.FirstName + " " + u.LastName).Trim())),
                    InternCount = g.Count(u => u.MemberType == MemberType.intern ||
                                               (u.Role != null &&
                                                (u.Role.Name.ToLower() == "intern" || u.Role.Name.ToLower() == "stagiaire"))),
                    SubcontractorCount = g.Count(u => u.MemberType == MemberType.Subcontractor &&
                                                     (u.Role == null ||
                                                      (u.Role.Name.ToLower() != "intern" && u.Role.Name.ToLower() != "stagiaire")))
                })
                .FirstOrDefaultAsync() ?? new MemberPopulationDto();

            // Interns are managed in their own table and do not inherit from
            // NormalUser, so their population cannot be derived from Users.
            memberCounts.InternCount = await _context.Interns
                .AsNoTracking()
                .CountAsync();

            var hoursDataDict = new Dictionary<Guid, decimal>();
            if (activeMemberIds.Count > 0)
            {
                var queryResult = await _context.HourEntries
                    .AsNoTracking()
                    .Where(h => h.Date >= startDate && h.Date <= endDate && activeMemberIds.Contains(h.UserId))
                    .GroupBy(h => h.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        TotalBooked = g.Sum(x => x.TotalHours)
                    })
                    .ToListAsync();
                    
                foreach (var item in queryResult)
                {
                    hoursDataDict[item.UserId] = item.TotalBooked;
                }
            }

            var activeUsersCount = activeMembers.Count;
            
            var totalTargetHours = targetHoursPerMember * activeUsersCount;
            var totalBookedHours = hoursDataDict.Values.Sum();

            var memberCapacities = activeMembers.Select(member =>
            {
                decimal booked = hoursDataDict.TryGetValue(member.Id, out var value) ? value : 0m;
                return new MemberCapacityDto
                {
                    UserId = member.Id,
                    UserName = (member.FirstName + " " + member.LastName).Trim(),
                    MemberType = member.MemberType == MemberType.Subcontractor ? "Subcontractor" : "Employee",
                    BookedHours = Math.Round(booked, 2),
                    TargetHours = Math.Round(targetHoursPerMember, 2),
                    RemainingHours = Math.Round(Math.Max(0m, targetHoursPerMember - booked), 2),
                    VarianceHours = Math.Round(booked - targetHoursPerMember, 2),
                    Percentage = targetHoursPerMember > 0
                        ? Math.Round((booked / targetHoursPerMember) * 100m, 2)
                        : 0m
                };
            }).OrderByDescending(x => x.BookedHours).ToList();

            var capacityTarget = new CapacityTargetDto
            {
                ActualBookedHours = Math.Round(totalBookedHours, 2),
                TargetHours = Math.Round(totalTargetHours, 2),
                RemainingHours = Math.Round(Math.Max(0m, totalTargetHours - totalBookedHours), 2),
                VarianceHours = Math.Round(totalBookedHours - totalTargetHours, 2),
                AchievementPercentage = totalTargetHours > 0
                    ? Math.Round(totalBookedHours / totalTargetHours * 100m, 2)
                    : 0m
            };

            var totalPriceTarget = totalTargetHours * query.HourlyRate;
            var totalBookedPrice = totalBookedHours * query.HourlyRate;

            var memberPrices = activeMembers.Select(member =>
            {
                decimal booked = hoursDataDict.TryGetValue(member.Id, out var value) ? value : 0m;
                decimal bookedPrice = booked * query.HourlyRate;
                decimal targetPrice = targetHoursPerMember * query.HourlyRate;
                return new MemberPriceDto
                {
                    UserId = member.Id,
                    UserName = (member.FirstName + " " + member.LastName).Trim(),
                    MemberType = member.MemberType == MemberType.Subcontractor ? "Subcontractor" : "Employee",
                    BookedPrice = Math.Round(bookedPrice, 2),
                    TargetPrice = Math.Round(targetPrice, 2),
                    RemainingPrice = Math.Round(Math.Max(0m, targetPrice - bookedPrice), 2),
                    VariancePrice = Math.Round(bookedPrice - targetPrice, 2),
                    Percentage = targetPrice > 0
                        ? Math.Round((bookedPrice / targetPrice) * 100m, 2)
                        : 0m
                };
            }).OrderByDescending(x => x.BookedPrice).ToList();

            var priceTarget = new PriceTargetDto
            {
                BookedPrice = Math.Round(totalBookedPrice, 2),
                TargetPrice = Math.Round(totalPriceTarget, 2),
                RemainingPrice = Math.Round(Math.Max(0m, totalPriceTarget - totalBookedPrice), 2),
                VariancePrice = Math.Round(totalBookedPrice - totalPriceTarget, 2),
                AchievementPercentage = totalPriceTarget > 0
                    ? Math.Round(totalBookedPrice / totalPriceTarget * 100m, 2)
                    : 0m
            };

            return new CapacityPriceDashboardDto
            {
                MemberCounts = memberCounts,
                MemberCapacities = memberCapacities,
                CapacityTarget = capacityTarget,
                MemberPrices = memberPrices,
                PriceTarget = priceTarget
            };
        }

        public async Task<ProjectCapacityPriceDashboardDto> GetProjectCapacityPriceDashboardAsync(AnalyticsQueryDto query)
        {
            // Capacity and price are lifecycle measures: the project estimate and
            // budget must be compared with all actual bookings, not only those in
            // the currently selected month.
            var projectRows = await BuildProjectsQuery(query)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Status,
                    p.Phase,
                    TargetHours = p.EstimatedHours,
                    TargetPrice = p.Budget,
                    BookedHours = p.HourEntries.Sum(h => (decimal?)h.TotalHours) ?? 0m,
                    LabourCost = p.HourEntries.Sum(h => (decimal?)h.TotalCost) ?? 0m,
                    ResourceCost = p.ProjectResources.Sum(r => (decimal?)(r.PricePerUnit * r.Quantity)) ?? 0m
                })
                .ToListAsync();

            var projects = projectRows
                .Select(row =>
                {
                    var bookedPrice = row.LabourCost + row.ResourceCost;
                    return new ProjectCapacityPriceDto
                    {
                        ProjectId = row.Id,
                        ProjectName = row.Name,
                        Status = row.Status,
                        Phase = row.Phase,
                        BookedHours = Math.Round(row.BookedHours, 2),
                        TargetHours = Math.Round(row.TargetHours, 2),
                        RemainingHours = Math.Round(Math.Max(0m, row.TargetHours - row.BookedHours), 2),
                        VarianceHours = Math.Round(row.BookedHours - row.TargetHours, 2),
                        CapacityAchievementPercentage = row.TargetHours > 0
                            ? Math.Round(row.BookedHours / row.TargetHours * 100m, 2)
                            : 0m,
                        LabourCost = Math.Round(row.LabourCost, 2),
                        ResourceCost = Math.Round(row.ResourceCost, 2),
                        BookedPrice = Math.Round(bookedPrice, 2),
                        TargetPrice = Math.Round(row.TargetPrice, 2),
                        RemainingPrice = Math.Round(Math.Max(0m, row.TargetPrice - bookedPrice), 2),
                        VariancePrice = Math.Round(bookedPrice - row.TargetPrice, 2),
                        PriceAchievementPercentage = row.TargetPrice > 0
                            ? Math.Round(bookedPrice / row.TargetPrice * 100m, 2)
                            : 0m
                    };
                })
                .OrderByDescending(project => project.BookedPrice)
                .ToList();

            var totalBookedHours = projects.Sum(project => project.BookedHours);
            var totalTargetHours = projects.Sum(project => project.TargetHours);
            var totalBookedPrice = projects.Sum(project => project.BookedPrice);
            var totalTargetPrice = projects.Sum(project => project.TargetPrice);

            return new ProjectCapacityPriceDashboardDto
            {
                ProjectCount = projects.Count,
                CapacityTarget = new CapacityTargetDto
                {
                    ActualBookedHours = Math.Round(totalBookedHours, 2),
                    TargetHours = Math.Round(totalTargetHours, 2),
                    RemainingHours = Math.Round(Math.Max(0m, totalTargetHours - totalBookedHours), 2),
                    VarianceHours = Math.Round(totalBookedHours - totalTargetHours, 2),
                    AchievementPercentage = totalTargetHours > 0
                        ? Math.Round(totalBookedHours / totalTargetHours * 100m, 2)
                        : 0m
                },
                PriceTarget = new PriceTargetDto
                {
                    BookedPrice = Math.Round(totalBookedPrice, 2),
                    TargetPrice = Math.Round(totalTargetPrice, 2),
                    RemainingPrice = Math.Round(Math.Max(0m, totalTargetPrice - totalBookedPrice), 2),
                    VariancePrice = Math.Round(totalBookedPrice - totalTargetPrice, 2),
                    AchievementPercentage = totalTargetPrice > 0
                        ? Math.Round(totalBookedPrice / totalTargetPrice * 100m, 2)
                        : 0m
                },
                Projects = projects
            };
        }

        public async Task<InternCapacityPriceDashboardDto> GetInternCapacityPriceDashboardAsync(InternCapacityPriceQueryDto query)
        {
            await LoadTargetSettingsAsync();

            var year = query.Year > 0 ? query.Year : DateTime.UtcNow.Year;
            var month = query.Month > 0 ? query.Month : DateTime.UtcNow.Month;

            if (month is < 1 or > 12)
                throw new ArgumentOutOfRangeException(nameof(query.Month), "Month must be between 1 and 12.");
            if (query.TargetHoursPerIntern < 0)
                throw new ArgumentOutOfRangeException(nameof(query.TargetHoursPerIntern), "Target hours per intern cannot be negative.");
            if (query.HourlyRate < 0)
                throw new ArgumentOutOfRangeException(nameof(query.HourlyRate), "Hourly rate cannot be negative.");

            var targetHoursPerIntern = query.TargetHoursPerIntern is > 0
                ? query.TargetHoursPerIntern.Value
                : _standards.MonthlyHoursTarget;

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddTicks(-1);

            var interns = await _context.Interns
                .AsNoTracking()
                .Select(i => new
                {
                    i.Id,
                    i.Name,
                    RoleName = i.Role.Name,
                    i.SupervisorId,
                    SupervisorName = i.Supervisor.FirstName + " " + i.Supervisor.LastName
                })
                .OrderBy(i => i.Name)
                .ToListAsync();

            // Interns who have a platform account log their work in HourEntries.
            // This is the same population excluded from the normal-user capacity endpoint.
            var accountInterns = await _context.Users
                .OfType<NormalUser>()
                .AsNoTracking()
                .Where(u => u.IsActive && u.IsApproved &&
                    (u.MemberType == MemberType.intern ||
                     (u.Role != null &&
                      (u.Role.Name.ToLower() == "intern" || u.Role.Name.ToLower() == "stagiaire"))) &&
                    !_context.Interns.Any(i => i.Name == (u.FirstName + " " + u.LastName).Trim()))
                .Select(u => new
                {
                    u.Id,
                    Name = u.FirstName + " " + u.LastName,
                    RoleName = u.Role != null ? u.Role.Name : string.Empty
                })
                .OrderBy(i => i.Name)
                .ToListAsync();

            var accountInternIds = accountInterns.Select(i => i.Id).ToList();
            var accountInternMetrics = await _context.HourEntries
                .AsNoTracking()
                .Where(h => accountInternIds.Contains(h.UserId) && h.Date >= startDate && h.Date <= endDate)
                .GroupBy(h => h.UserId)
                .Select(g => new
                {
                    InternId = g.Key,
                    BookedHours = g.Sum(h => h.TotalHours),
                    HourEntryCount = g.Count()
                })
                .ToListAsync();

            var accountInternProjects = await _context.HourEntries
                .AsNoTracking()
                .Where(h => accountInternIds.Contains(h.UserId) && h.ProjectId.HasValue && h.Date >= startDate && h.Date <= endDate)
                .GroupBy(h => new { ProjectId = h.ProjectId!.Value, h.Project!.Name })
                .Select(g => new
                {
                    g.Key.ProjectId,
                    ProjectName = g.Key.Name,
                    BookedHours = g.Sum(h => h.TotalHours),
                    HourEntryCount = g.Count()
                })
                .ToListAsync();

            // Direct entries are created from the intern allocation screen.
            var directInternMetrics = await _context.InternHourEntries
                .AsNoTracking()
                .Where(e => e.Date >= startDate && e.Date <= endDate)
                .GroupBy(e => e.InternAllocation.InternId)
                .Select(g => new
                {
                    InternId = g.Key,
                    BookedHours = g.Sum(e => e.Hours),
                    HourEntryCount = g.Count()
                })
                .ToListAsync();

            // Supervisor bookings are stored separately on the normal user's HourEntry.
            // They must be included because this is the source used by the regular booking form.
            var supervisionMetrics = await _context.HourEntryInternSupervisions
                .AsNoTracking()
                .Where(s => s.HourEntry.Date >= startDate && s.HourEntry.Date <= endDate)
                .GroupBy(s => s.InternAllocation.InternId)
                .Select(g => new
                {
                    InternId = g.Key,
                    BookedHours = g.Sum(s => s.Hours),
                    HourEntryCount = g.Count()
                })
                .ToListAsync();

            var directProjects = await _context.InternHourEntries
                .AsNoTracking()
                .Where(e => e.Date >= startDate && e.Date <= endDate)
                .GroupBy(e => new { e.InternAllocation.ProjectId, e.InternAllocation.Project.Name })
                .Select(g => new
                {
                    g.Key.ProjectId,
                    ProjectName = g.Key.Name,
                    BookedHours = g.Sum(e => e.Hours),
                    InternCount = g.Select(e => e.InternAllocation.InternId).Distinct().Count(),
                    HourEntryCount = g.Count()
                })
                .OrderByDescending(p => p.BookedHours)
                .ToListAsync();

            var supervisionProjects = await _context.HourEntryInternSupervisions
                .AsNoTracking()
                .Where(s => s.HourEntry.Date >= startDate && s.HourEntry.Date <= endDate)
                .GroupBy(s => new { s.InternAllocation.ProjectId, s.InternAllocation.Project.Name })
                .Select(g => new
                {
                    g.Key.ProjectId,
                    ProjectName = g.Key.Name,
                    BookedHours = g.Sum(s => s.Hours),
                    InternCount = g.Select(s => s.InternAllocation.InternId).Distinct().Count(),
                    HourEntryCount = g.Count()
                })
                .ToListAsync();

            var projectInternPairs = await _context.InternHourEntries
                .AsNoTracking()
                .Where(e => e.Date >= startDate && e.Date <= endDate)
                .Select(e => new { e.InternAllocation.ProjectId, e.InternAllocation.InternId })
                .Concat(_context.HourEntryInternSupervisions
                    .AsNoTracking()
                    .Where(s => s.HourEntry.Date >= startDate && s.HourEntry.Date <= endDate)
                    .Select(s => new { s.InternAllocation.ProjectId, s.InternAllocation.InternId }))
                .Concat(_context.HourEntries
                    .AsNoTracking()
                    .Where(h => accountInternIds.Contains(h.UserId) && h.ProjectId.HasValue && h.Date >= startDate && h.Date <= endDate)
                    .Select(h => new { ProjectId = h.ProjectId!.Value, InternId = h.UserId }))
                .Distinct()
                .ToListAsync();

            var directMetricsByIntern = directInternMetrics.ToDictionary(x => x.InternId);
            var supervisionMetricsByIntern = supervisionMetrics.ToDictionary(x => x.InternId);
            var accountMetricsByIntern = accountInternMetrics.ToDictionary(x => x.InternId);
            var internCountByProject = projectInternPairs
                .GroupBy(pair => pair.ProjectId)
                .ToDictionary(group => group.Key, group => group.Count());
            var totalDirectHours = directInternMetrics.Sum(x => x.BookedHours);
            var totalSupervisionHours = supervisionMetrics.Sum(x => x.BookedHours);
            var totalAccountInternHours = accountInternMetrics.Sum(x => x.BookedHours);
            var totalBookedHours = totalDirectHours + totalSupervisionHours + totalAccountInternHours;
            var totalInternCount = interns.Count + accountInterns.Count;
            var totalTargetHours = totalInternCount * targetHoursPerIntern;
            var totalBookedPrice = totalBookedHours * query.HourlyRate;
            var totalTargetPrice = totalTargetHours * query.HourlyRate;

            return new InternCapacityPriceDashboardDto
            {
                TotalInterns = totalInternCount,
                InternsWithLoggedHours = interns.Count(i =>
                    directMetricsByIntern.ContainsKey(i.Id) || supervisionMetricsByIntern.ContainsKey(i.Id)) +
                    accountInterns.Count(i => accountMetricsByIntern.ContainsKey(i.Id)),
                CapacityTarget = new CapacityTargetDto
                {
                    ActualBookedHours = Math.Round(totalBookedHours, 2),
                    TargetHours = Math.Round(totalTargetHours, 2),
                    RemainingHours = Math.Round(Math.Max(0m, totalTargetHours - totalBookedHours), 2),
                    VarianceHours = Math.Round(totalBookedHours - totalTargetHours, 2),
                    AchievementPercentage = totalTargetHours > 0
                        ? Math.Round(totalBookedHours / totalTargetHours * 100m, 2)
                        : 0m
                },
                PriceTarget = new PriceTargetDto
                {
                    BookedPrice = Math.Round(totalBookedPrice, 2),
                    TargetPrice = Math.Round(totalTargetPrice, 2),
                    RemainingPrice = Math.Round(Math.Max(0m, totalTargetPrice - totalBookedPrice), 2),
                    VariancePrice = Math.Round(totalBookedPrice - totalTargetPrice, 2),
                    AchievementPercentage = totalTargetPrice > 0
                        ? Math.Round(totalBookedPrice / totalTargetPrice * 100m, 2)
                        : 0m
                },
                Interns = interns.Select(intern =>
                {
                    var directMetric = directMetricsByIntern.GetValueOrDefault(intern.Id);
                    var supervisionMetric = supervisionMetricsByIntern.GetValueOrDefault(intern.Id);
                    var directHours = directMetric?.BookedHours ?? 0m;
                    var supervisionHours = supervisionMetric?.BookedHours ?? 0m;
                    var bookedHours = directHours + supervisionHours;
                    var bookedPrice = bookedHours * query.HourlyRate;
                    var targetPrice = targetHoursPerIntern * query.HourlyRate;

                    return new InternCapacityPriceDto
                    {
                        InternId = intern.Id,
                        InternName = intern.Name,
                        Source = "InternAllocation",
                        RoleName = intern.RoleName,
                        SupervisorId = intern.SupervisorId,
                        SupervisorName = intern.SupervisorName.Trim(),
                        DirectBookedHours = Math.Round(directHours, 2),
                        SupervisionHours = Math.Round(supervisionHours, 2),
                        BookedHours = Math.Round(bookedHours, 2),
                        TargetHours = Math.Round(targetHoursPerIntern, 2),
                        RemainingHours = Math.Round(Math.Max(0m, targetHoursPerIntern - bookedHours), 2),
                        Percentage = targetHoursPerIntern > 0
                            ? Math.Round(bookedHours / targetHoursPerIntern * 100m, 2)
                            : 0m,
                        BookedPrice = Math.Round(bookedPrice, 2),
                        TargetPrice = Math.Round(targetPrice, 2),
                        RemainingPrice = Math.Round(Math.Max(0m, targetPrice - bookedPrice), 2),
                        HourEntryCount = (directMetric?.HourEntryCount ?? 0) + (supervisionMetric?.HourEntryCount ?? 0)
                    };
                })
                    .Concat(accountInterns.Select(intern =>
                    {
                        var metric = accountMetricsByIntern.GetValueOrDefault(intern.Id);
                        var bookedHours = metric?.BookedHours ?? 0m;
                        var bookedPrice = bookedHours * query.HourlyRate;
                        var targetPrice = targetHoursPerIntern * query.HourlyRate;

                        return new InternCapacityPriceDto
                        {
                            InternId = intern.Id,
                            InternName = intern.Name.Trim(),
                            Source = "UserAccount",
                            RoleName = intern.RoleName,
                            BookedHours = Math.Round(bookedHours, 2),
                            TargetHours = Math.Round(targetHoursPerIntern, 2),
                            RemainingHours = Math.Round(Math.Max(0m, targetHoursPerIntern - bookedHours), 2),
                            Percentage = targetHoursPerIntern > 0
                                ? Math.Round(bookedHours / targetHoursPerIntern * 100m, 2)
                                : 0m,
                            BookedPrice = Math.Round(bookedPrice, 2),
                            TargetPrice = Math.Round(targetPrice, 2),
                            RemainingPrice = Math.Round(Math.Max(0m, targetPrice - bookedPrice), 2),
                            HourEntryCount = metric?.HourEntryCount ?? 0
                        };
                    }))
                    .OrderByDescending(i => i.BookedHours)
                    .ToList(),
                Projects = directProjects
                    .Select(project => project.ProjectId)
                    .Concat(supervisionProjects.Select(project => project.ProjectId))
                    .Concat(accountInternProjects.Select(project => project.ProjectId))
                    .Distinct()
                    .Select(projectId =>
                    {
                        var directProject = directProjects.FirstOrDefault(project => project.ProjectId == projectId);
                        var supervisionProject = supervisionProjects.FirstOrDefault(project => project.ProjectId == projectId);
                        var accountProject = accountInternProjects.FirstOrDefault(project => project.ProjectId == projectId);
                        var directHours = directProject?.BookedHours ?? 0m;
                        var supervisionHours = supervisionProject?.BookedHours ?? 0m;
                        var accountHours = accountProject?.BookedHours ?? 0m;
                        var bookedHours = directHours + supervisionHours + accountHours;

                        return new InternProjectCapacityPriceDto
                        {
                            ProjectId = projectId,
                            ProjectName = directProject?.ProjectName ?? supervisionProject?.ProjectName ?? accountProject?.ProjectName ?? string.Empty,
                            DirectBookedHours = Math.Round(directHours + accountHours, 2),
                            SupervisionHours = Math.Round(supervisionHours, 2),
                            BookedHours = Math.Round(bookedHours, 2),
                            BookedPrice = Math.Round(bookedHours * query.HourlyRate, 2),
                            // An intern present in both sources is counted only once.
                            InternCount = internCountByProject.GetValueOrDefault(projectId),
                            HourEntryCount = (directProject?.HourEntryCount ?? 0) + (supervisionProject?.HourEntryCount ?? 0) + (accountProject?.HourEntryCount ?? 0)
                        };
                    })
                    .OrderByDescending(project => project.BookedHours)
                    .ToList()
            };
        }

        public async Task<BookingTargetComparisonDashboardDto> GetBookingTargetComparisonAsync(BookingTargetComparisonQueryDto query)
        {
            await LoadTargetSettingsAsync();

            if (query.TargetHoursPerMember < 0)
                throw new BadRequestException("Target hours per member cannot be negative.");
            if (query.HourlyRate < 0)
                throw new BadRequestException("Hourly rate cannot be negative.");
            if (query.Month.HasValue && query.Month is < 1 or > 12)
                throw new BadRequestException("Month must be between 1 and 12.");

            var calculationMode = ResolveCalculationMode(query.CalculationMode);
            var useNet = string.Equals(calculationMode, "Net", StringComparison.OrdinalIgnoreCase);
            var hourlyRate = query.HourlyRate;
            var monthlyTargetHours = query.TargetHoursPerMember is > 0
                ? query.TargetHoursPerMember.Value
                : _monthlyHoursTargetPerMember;

            var period = ResolveComparisonPeriod(query);
            var startDate = period.StartDate.ToDateTime(TimeOnly.MinValue);
            var endDate = period.EndDate.ToDateTime(TimeOnly.MaxValue);
            var monthlyPeriods = BuildMonthlyPeriods(startDate, endDate);
            var monthCount = Math.Max(1, monthlyPeriods.Count);

            var hasStructureFilter =
                (query.ProjectId.HasValue && query.ProjectId.Value != Guid.Empty) ||
                (query.DepartmentId.HasValue && query.DepartmentId.Value != Guid.Empty) ||
                (query.BusinessUnitId.HasValue && query.BusinessUnitId.Value != Guid.Empty) ||
                (query.PlantId.HasValue && query.PlantId.Value != Guid.Empty);

            List<Guid>? projectIds = null;
            HashSet<Guid>? scopedUserIds = null;
            HashSet<Guid>? scopedInternIds = null;
            if (hasStructureFilter)
            {
                projectIds = await BuildProjectsQuery(new AnalyticsQueryDto
                {
                    ProjectId = query.ProjectId,
                    DepartmentId = query.DepartmentId,
                    BusinessUnitId = query.BusinessUnitId,
                    PlantId = query.PlantId
                }).Select(p => p.Id).ToListAsync();

                scopedUserIds = (await _context.ProjectMembers
                    .AsNoTracking()
                    .Where(pm => projectIds.Contains(pm.ProjectId))
                    .Select(pm => pm.UserId)
                    .Distinct()
                    .ToListAsync()).ToHashSet();

                var bookedUserIds = await _context.HourEntries
                    .AsNoTracking()
                    .Where(h => h.Date >= startDate && h.Date <= endDate &&
                                h.ProjectId.HasValue && projectIds.Contains(h.ProjectId.Value))
                    .Select(h => h.UserId)
                    .Distinct()
                    .ToListAsync();
                foreach (var userId in bookedUserIds)
                    scopedUserIds.Add(userId);

                scopedInternIds = (await _context.InternAllocations
                    .AsNoTracking()
                    .Where(a => projectIds.Contains(a.ProjectId))
                    .Select(a => a.InternId)
                    .Distinct()
                    .ToListAsync()).ToHashSet();
            }

            var activeMembers = await _context.Users
                .OfType<NormalUser>()
                .AsNoTracking()
                .Where(u => u.IsActive && u.IsApproved &&
                            (u.MemberType == MemberType.Employee || u.MemberType == MemberType.Subcontractor) &&
                            (u.Role == null ||
                             (u.Role.Name.ToLower() != "intern" && u.Role.Name.ToLower() != "stagiaire")) &&
                            !_context.Interns.Any(i => i.Name == (u.FirstName + " " + u.LastName).Trim()))
                .Select(u => new { u.Id, u.MemberType })
                .ToListAsync();

            if (scopedUserIds != null)
                activeMembers = activeMembers.Where(u => scopedUserIds.Contains(u.Id)).ToList();

            var employeeIds = activeMembers
                .Where(u => u.MemberType == MemberType.Employee)
                .Select(u => u.Id)
                .ToList();
            var subcontractorIds = activeMembers
                .Where(u => u.MemberType == MemberType.Subcontractor)
                .Select(u => u.Id)
                .ToList();

            var internRecords = await _context.Interns
                .AsNoTracking()
                .Select(i => new { i.Id, i.Name })
                .ToListAsync();
            if (scopedInternIds != null)
                internRecords = internRecords.Where(i => scopedInternIds.Contains(i.Id)).ToList();

            var internNames = internRecords
                .Select(i => i.Name.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var accountInterns = await _context.Users
                .OfType<NormalUser>()
                .AsNoTracking()
                .Where(u => u.IsActive && u.IsApproved &&
                            (u.MemberType == MemberType.intern ||
                             (u.Role != null &&
                              (u.Role.Name.ToLower() == "intern" || u.Role.Name.ToLower() == "stagiaire"))) &&
                            !_context.Interns.Any(i => i.Name == (u.FirstName + " " + u.LastName).Trim()))
                .Select(u => new { u.Id, Name = (u.FirstName + " " + u.LastName).Trim() })
                .ToListAsync();

            if (scopedUserIds != null)
                accountInterns = accountInterns.Where(u => scopedUserIds.Contains(u.Id)).ToList();

            accountInterns = accountInterns
                .Where(u => !internNames.Contains(u.Name))
                .ToList();

            var internTableIds = internRecords.Select(i => i.Id).ToList();
            var accountInternIds = accountInterns.Select(i => i.Id).ToList();
            var employeeCount = employeeIds.Count;
            var subcontractorCount = subcontractorIds.Count;
            var internCount = internTableIds.Count + accountInternIds.Count;

            var memberIds = employeeIds.Concat(subcontractorIds).ToList();
            var memberHours = new List<(Guid UserId, int Year, int Month, CategoryWork Category, decimal Hours)>();
            if (memberIds.Count > 0)
            {
                var hoursQuery = _context.HourEntries
                    .AsNoTracking()
                    .Where(h => h.Date >= startDate && h.Date <= endDate && memberIds.Contains(h.UserId));

                if (projectIds != null)
                    hoursQuery = hoursQuery.Where(h => h.ProjectId.HasValue && projectIds.Contains(h.ProjectId.Value));

                memberHours = (await hoursQuery
                        .Select(h => new { h.UserId, h.Date, h.Category, h.TotalHours })
                        .ToListAsync())
                    .Select(h => (h.UserId, h.Date.Year, h.Date.Month, h.Category, h.TotalHours))
                    .ToList();
            }

            var internHours = await LoadInternPopulationHoursAsync(
                startDate,
                endDate,
                projectIds,
                internTableIds,
                accountInternIds);

            var employeeHours = memberHours.Where(h => employeeIds.Contains(h.UserId)).ToList();
            var subcontractorHours = memberHours.Where(h => subcontractorIds.Contains(h.UserId)).ToList();

            var employeeGross = employeeHours.Sum(h => h.Hours);
            var employeeNet = employeeHours.Where(h => h.Category == CategoryWork.Project).Sum(h => h.Hours);
            var subcontractorGross = subcontractorHours.Sum(h => h.Hours);
            var internGross = internHours.Sum(h => h.Hours);

            // Build per-month active-member counts from actual hours data so
            // that the target adapts when people join or leave mid-year.
            var employeeIdSet = employeeIds.ToHashSet();
            var subcontractorIdSet = subcontractorIds.ToHashSet();

            var employeeActiveByMonth = employeeHours
                .GroupBy(h => (h.Year, h.Month))
                .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).Distinct().Count());
            var subcontractorActiveByMonth = subcontractorHours
                .GroupBy(h => (h.Year, h.Month))
                .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).Distinct().Count());
            var internActiveByMonth = internHours
                .GroupBy(h => (h.Year, h.Month))
                .ToDictionary(g => g.Key, g => g.Select(x => x.InternId).Distinct().Count());

            var employeeTarget = monthlyPeriods.Sum(p =>
                monthlyTargetHours * employeeActiveByMonth.GetValueOrDefault((p.Start.Year, p.Start.Month)));
            var subcontractorTarget = monthlyPeriods.Sum(p =>
                monthlyTargetHours * subcontractorActiveByMonth.GetValueOrDefault((p.Start.Year, p.Start.Month)));
            var internTarget = monthlyPeriods.Sum(p =>
                monthlyTargetHours * internActiveByMonth.GetValueOrDefault((p.Start.Year, p.Start.Month)));

            var displayEmployeeCount = monthCount == 1
                ? employeeActiveByMonth.GetValueOrDefault((monthlyPeriods[0].Start.Year, monthlyPeriods[0].Start.Month))
                : (employeeHours.Count > 0 ? employeeHours.Select(h => h.UserId).Distinct().Count() : employeeCount);

            var displaySubcontractorCount = monthCount == 1
                ? subcontractorActiveByMonth.GetValueOrDefault((monthlyPeriods[0].Start.Year, monthlyPeriods[0].Start.Month))
                : (subcontractorHours.Count > 0 ? subcontractorHours.Select(h => h.UserId).Distinct().Count() : subcontractorCount);

            var displayInternCount = monthCount == 1
                ? internActiveByMonth.GetValueOrDefault((monthlyPeriods[0].Start.Year, monthlyPeriods[0].Start.Month))
                : (internHours.Count > 0 ? internHours.Select(h => h.InternId).Distinct().Count() : internCount);

            var employees = BuildPopulationComparison(
                "Employees",
                "employees",
                displayEmployeeCount,
                employeeGross,
                employeeNet,
                employeeTarget,
                hourlyRate,
                useNet);
            var subcontractors = BuildPopulationComparison(
                "Subcontractors",
                "subcontractors",
                displaySubcontractorCount,
                subcontractorGross,
                subcontractorGross,
                subcontractorTarget,
                hourlyRate,
                useNet);
            var interns = BuildPopulationComparison(
                "Interns",
                "interns",
                displayInternCount,
                internGross,
                internGross,
                internTarget,
                hourlyRate,
                useNet);
            var total = BuildPopulationComparison(
                "Total",
                "total",
                displayEmployeeCount + displaySubcontractorCount + displayInternCount,
                employeeGross + subcontractorGross + internGross,
                employeeNet + subcontractorGross + internGross,
                employeeTarget + subcontractorTarget + internTarget,
                hourlyRate,
                useNet);

            var employeeHoursByMonth = employeeHours
                .GroupBy(h => (h.Year, h.Month))
                .ToDictionary(
                    g => g.Key,
                    g => (
                        Gross: g.Sum(x => x.Hours),
                        Net: g.Where(x => x.Category == CategoryWork.Project).Sum(x => x.Hours)));
            var subcontractorHoursByMonth = subcontractorHours
                .GroupBy(h => (h.Year, h.Month))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Hours));
            var internHoursByMonth = internHours
                .GroupBy(h => (h.Year, h.Month))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Hours));

            decimal cumulativeBooked = 0m;
            decimal cumulativeTarget = 0m;
            decimal cumulativeRevenue = 0m;
            decimal cumulativeTargetRevenue = 0m;
            decimal cumulativeGrossBooked = 0m;
            decimal cumulativeNetBooked = 0m;

            var monthlyTrend = monthlyPeriods.Select(monthPeriod =>
            {
                var year = monthPeriod.Start.Year;
                var month = monthPeriod.Start.Month;
                var key = (year, month);

                employeeHoursByMonth.TryGetValue(key, out var employeeMonth);
                var subcontractorMonthHours = subcontractorHoursByMonth.GetValueOrDefault(key);
                var internMonthHours = internHoursByMonth.GetValueOrDefault(key);

                var monthEmployeeCount = employeeActiveByMonth.GetValueOrDefault(key);
                var monthSubcontractorCount = subcontractorActiveByMonth.GetValueOrDefault(key);
                var monthInternCount = internActiveByMonth.GetValueOrDefault(key);

                var employeeMonthMetric = BuildPopulationMonthlyMetric(
                    monthEmployeeCount,
                    employeeMonth.Gross,
                    employeeMonth.Net,
                    monthlyTargetHours * monthEmployeeCount,
                    hourlyRate,
                    useNet);
                var subcontractorMonthMetric = BuildPopulationMonthlyMetric(
                    monthSubcontractorCount,
                    subcontractorMonthHours,
                    subcontractorMonthHours,
                    monthlyTargetHours * monthSubcontractorCount,
                    hourlyRate,
                    useNet);
                var internMonthMetric = BuildPopulationMonthlyMetric(
                    monthInternCount,
                    internMonthHours,
                    internMonthHours,
                    monthlyTargetHours * monthInternCount,
                    hourlyRate,
                    useNet);

                var bookedHours = employeeMonthMetric.BookedHours + subcontractorMonthMetric.BookedHours + internMonthMetric.BookedHours;
                var targetHours = employeeMonthMetric.TargetHours + subcontractorMonthMetric.TargetHours + internMonthMetric.TargetHours;
                var bookedRevenue = employeeMonthMetric.BookedRevenue + subcontractorMonthMetric.BookedRevenue + internMonthMetric.BookedRevenue;
                var targetRevenue = employeeMonthMetric.TargetRevenue + subcontractorMonthMetric.TargetRevenue + internMonthMetric.TargetRevenue;

                var grossBookedHours = employeeMonth.Gross + subcontractorMonthHours + internMonthHours;
                var netBookedHours = employeeMonth.Net + subcontractorMonthHours + internMonthHours;

                cumulativeBooked += bookedHours;
                cumulativeTarget += targetHours;
                cumulativeRevenue += bookedRevenue;
                cumulativeTargetRevenue += targetRevenue;
                cumulativeGrossBooked += grossBookedHours;
                cumulativeNetBooked += netBookedHours;

                var fiscalMonth = ((month - period.FiscalYearStartMonth + 12) % 12) + 1;

                return new BookingTargetMonthlyTrendDto
                {
                    Year = year,
                    Month = month,
                    MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                    FiscalMonth = fiscalMonth,
                    FiscalMonthIndex = fiscalMonth,
                    BookedHours = Math.Round(bookedHours, 2),
                    TargetHours = Math.Round(targetHours, 2),
                    HoursAchievementPercentage = CalculateAchievementPercentage(bookedHours, targetHours),
                    BookedRevenue = Math.Round(bookedRevenue, 2),
                    TargetRevenue = Math.Round(targetRevenue, 2),
                    RevenueAchievementPercentage = CalculateAchievementPercentage(bookedRevenue, targetRevenue),
                    GrossBookedHours = Math.Round(grossBookedHours, 2),
                    NetBookedHours = Math.Round(netBookedHours, 2),
                    CumulativeBookedHours = Math.Round(cumulativeBooked, 2),
                    CumulativeTargetHours = Math.Round(cumulativeTarget, 2),
                    CumulativeHoursAchievementPercentage = CalculateAchievementPercentage(cumulativeBooked, cumulativeTarget),
                    CumulativeBookedRevenue = Math.Round(cumulativeRevenue, 2),
                    CumulativeTargetRevenue = Math.Round(cumulativeTargetRevenue, 2),
                    CumulativeRevenueAchievementPercentage = CalculateAchievementPercentage(cumulativeRevenue, cumulativeTargetRevenue),
                    CumulativeGrossBookedHours = Math.Round(cumulativeGrossBooked, 2),
                    CumulativeNetBookedHours = Math.Round(cumulativeNetBooked, 2),
                    Employees = employeeMonthMetric,
                    Subcontractors = subcontractorMonthMetric,
                    Interns = internMonthMetric
                };
            }).ToList();

            var summary = BuildBookingTargetSummary(total, employees, subcontractors, interns);
            var periodMode = string.IsNullOrWhiteSpace(query.PeriodMode)
                ? (string.IsNullOrWhiteSpace(query.QuickSelect) ? "ytd" : query.QuickSelect.Trim().ToLowerInvariant())
                : query.PeriodMode.Trim().ToLowerInvariant();

            return new BookingTargetComparisonDashboardDto
            {
                Period = period,
                FiscalYear = period.FiscalYear,
                PeriodMode = periodMode,
                StartDate = period.StartDate.ToString("yyyy-MM-dd"),
                EndDate = period.EndDate.ToString("yyyy-MM-dd"),
                HourlyRate = hourlyRate,
                TargetHoursPerMember = Math.Round(monthlyTargetHours, 2),
                CalculationMode = calculationMode,
                Summary = summary,
                Populations = new List<PopulationComparisonDto> { employees, subcontractors, interns, total },
                MonthlyTrend = monthlyTrend
            };
        }

        public async Task<MemberTahDashboardDto> GetMemberTahDashboardAsync(MemberTahQueryDto query)
        {
            if (query.TahMonthlyHoursTarget < 0)
                throw new ArgumentOutOfRangeException(nameof(query.TahMonthlyHoursTarget), "TAH monthly hours target cannot be negative.");

            await LoadTargetSettingsAsync();

            var period = ResolvePeriod(query);
            var startDate = period.StartDate.ToDateTime(TimeOnly.MinValue);
            var endDate = period.EndDate.ToDateTime(TimeOnly.MaxValue);
            var tahTarget = await ResolveTahMonthlyHoursTargetAsync(query.TahMonthlyHoursTarget);

            var membersQuery = _context.Users
                .OfType<NormalUser>()
                .AsNoTracking()
                .Where(u => u.IsActive && u.IsApproved &&
                            (u.MemberType == MemberType.Employee || u.MemberType == MemberType.Subcontractor) &&
                            (u.Role == null ||
                             (u.Role.Name.ToLower() != "intern" && u.Role.Name.ToLower() != "stagiaire")) &&
                            !_context.Interns.Any(i => i.Name == (u.FirstName + " " + u.LastName).Trim()));

            if (query.UserId.HasValue && query.UserId.Value != Guid.Empty)
                membersQuery = membersQuery.Where(u => u.Id == query.UserId.Value);

            var members = await membersQuery
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.MemberType
                })
                .ToListAsync();

            var memberIds = members.Select(m => m.Id).ToList();
            var projectsQuery = BuildProjectsQuery(query);
            var projectIds = await projectsQuery.Select(p => p.Id).ToListAsync();
            var selectedHours = memberIds.Count == 0
                ? new List<HourEntry>()
                : await BuildHoursQuery(projectIds, query.UserId, startDate, endDate)
                    .Where(h => memberIds.Contains(h.UserId))
                    .ToListAsync();
            var selectedKpis = await BuildKpiQuery(projectIds).ToListAsync();
            var projectMetricRows = await projectsQuery
                .Select(p => new ProjectMetricRow
                {
                    Id = p.Id,
                    Status = p.Status,
                    StartDate = p.StartDate,
                    EstimatedHours = p.EstimatedHours,
                    ActualHours = p.ActualHours,
                    EndDate = p.EndDate,
                    EstimatedDueDate = p.EstimatedDueDate
                })
                .ToListAsync();

            var periodProjectIds = ResolvePeriodProjectIds(projectMetricRows, selectedHours, selectedKpis, startDate, endDate);
            var periodProjects = projectMetricRows.Where(p => periodProjectIds.Contains(p.Id)).ToList();
            var periodKpis = selectedKpis.Where(k => periodProjectIds.Contains(k.ProjectId)).ToList();
            var projectEffectiveness = periodProjects.ToDictionary(
                p => p.Id,
                p => CalculateEffectiveness(new List<ProjectMetricRow> { p }, selectedKpis.Where(k => k.ProjectId == p.Id).ToList()));

            var monthlyPeriods = BuildMonthlyPeriods(startDate, endDate);
            var companyMonthlyEffectiveness = BuildKpiTrend(projectMetricRows, selectedKpis, selectedHours, monthlyPeriods)
                .ToDictionary(m => (m.Year, m.Month), m => m.Effectiveness);
            var companyEffectiveness = CalculateEffectiveness(periodProjects, periodKpis);

            var hoursByUserMonth = selectedHours
                .Where(h => h.ProjectId.HasValue)
                .GroupBy(h => (h.UserId, h.Date.Year, h.Date.Month))
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(h => (ProjectId: h.ProjectId!.Value, Hours: h.TotalHours)).ToList());

            var memberDtos = members.Select(member =>
            {
                var monthly = monthlyPeriods.Select(monthPeriod =>
                {
                    var year = monthPeriod.Start.Year;
                    var month = monthPeriod.Start.Month;
                    var monthHours = hoursByUserMonth.GetValueOrDefault((member.Id, year, month))
                        ?? new List<(Guid ProjectId, decimal Hours)>();
                    var bookedHours = monthHours.Sum(h => h.Hours);
                    var companyMonthEffectiveness = companyMonthlyEffectiveness.GetValueOrDefault((year, month));
                    var effectiveness = CalculateHoursWeightedEffectiveness(monthHours, projectEffectiveness, companyMonthEffectiveness);
                    return new MemberTahMemberMonthDto
                    {
                        Year = year,
                        Month = month,
                        MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                        BookedHours = Math.Round(bookedHours, 2),
                        Effectiveness = effectiveness,
                        TahHours = CalculateTahHours(tahTarget, effectiveness)
                    };
                }).ToList();

                var periodHours = monthly.Sum(m => m.BookedHours);
                var periodEffectiveness = monthly.Count == 0
                    ? companyEffectiveness
                    : CalculateHoursWeightedEffectiveness(
                        monthly.Select(m => (Weight: m.BookedHours, Value: m.Effectiveness)),
                        companyEffectiveness);

                return new MemberTahMemberDto
                {
                    UserId = member.Id,
                    UserName = (member.FirstName + " " + member.LastName).Trim(),
                    MemberType = member.MemberType,
                    MemberTypeLabel = member.MemberType == MemberType.Subcontractor ? "Subcontractor" : "TE",
                    BookedHours = Math.Round(periodHours, 2),
                    Effectiveness = periodEffectiveness,
                    TahHours = Math.Round(monthly.Sum(m => m.TahHours), 1),
                    Monthly = monthly
                };
            }).ToList();

            var employees = memberDtos.Where(m => m.MemberType == MemberType.Employee).ToList();
            var subcontractors = memberDtos.Where(m => m.MemberType == MemberType.Subcontractor).ToList();
            var employeeTah = employees.Sum(m => m.TahHours);
            var subcontractorTah = subcontractors.Sum(m => m.TahHours);
            var cumulativeTah = employeeTah + subcontractorTah;

            var monthlyBreakdown = monthlyPeriods.Select(monthPeriod =>
            {
                var year = monthPeriod.Start.Year;
                var month = monthPeriod.Start.Month;
                var employeeMonthTah = employees.Sum(m => m.Monthly.First(row => row.Year == year && row.Month == month).TahHours);
                var subcontractorMonthTah = subcontractors.Sum(m => m.Monthly.First(row => row.Year == year && row.Month == month).TahHours);
                var monthTah = employeeMonthTah + subcontractorMonthTah;
                var monthEffectiveness = companyMonthlyEffectiveness.GetValueOrDefault((year, month));

                return new MemberTahMonthlyBreakdownDto
                {
                    Year = year,
                    Month = month,
                    MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                    EmployeeCount = employees.Count,
                    SubcontractorCount = subcontractors.Count,
                    AverageEffectiveness = monthEffectiveness,
                    TahHours = Math.Round(monthTah, 1),
                    EmployeeTahHours = Math.Round(employeeMonthTah, 1),
                    SubcontractorTahHours = Math.Round(subcontractorMonthTah, 1),
                    EmployeeSharePercentage = CalculatePercentage(employeeMonthTah, monthTah),
                    SubcontractorSharePercentage = CalculatePercentage(subcontractorMonthTah, monthTah)
                };
            }).ToList();

            return new MemberTahDashboardDto
            {
                Period = period,
                TahMonthlyHoursTarget = tahTarget,
                Summary = new MemberTahSummaryDto
                {
                    EmployeeCount = employees.Count,
                    SubcontractorCount = subcontractors.Count,
                    AverageEffectiveness = companyEffectiveness,
                    CumulativeTahHours = Math.Round(cumulativeTah, 1),
                    EmployeeTahHours = Math.Round(employeeTah, 1),
                    SubcontractorTahHours = Math.Round(subcontractorTah, 1),
                    EmployeeSharePercentage = CalculatePercentage(employeeTah, cumulativeTah),
                    SubcontractorSharePercentage = CalculatePercentage(subcontractorTah, cumulativeTah)
                },
                MonthlyBreakdown = monthlyBreakdown,
                Members = memberDtos
            };
        }

        private async Task<decimal> ResolveTahMonthlyHoursTargetAsync(decimal? overrideTarget)
        {
            if (overrideTarget is > 0)
                return overrideTarget.Value;

            var kpiTargets = await _targetSettingsService.GetActiveKpiTargetValuesAsync();
            if (kpiTargets.TryGetValue("TAH", out var storedTarget) && storedTarget > 0)
                return storedTarget;

            return DefaultTahMonthlyHoursTarget;
        }

        private static decimal CalculateHoursWeightedEffectiveness(
            IEnumerable<(Guid ProjectId, decimal Hours)> hours,
            IReadOnlyDictionary<Guid, decimal> projectEffectiveness,
            decimal fallbackEffectiveness)
        {
            var weighted = hours
                .Where(h => h.Hours > 0 && projectEffectiveness.ContainsKey(h.ProjectId))
                .Select(h => (Weight: h.Hours, Value: projectEffectiveness[h.ProjectId]))
                .ToList();

            return CalculateHoursWeightedEffectiveness(weighted, fallbackEffectiveness);
        }

        private static decimal CalculateHoursWeightedEffectiveness(
            IEnumerable<(decimal Weight, decimal Value)> values,
            decimal fallbackEffectiveness)
        {
            var rows = values.Where(v => v.Weight > 0 && v.Value > 0).ToList();
            if (rows.Count == 0)
                return fallbackEffectiveness;

            var totalWeight = rows.Sum(v => v.Weight);
            return totalWeight <= 0
                ? fallbackEffectiveness
                : Math.Round(rows.Sum(v => v.Weight * v.Value) / totalWeight, 2);
        }

        private static decimal CalculateTahHours(decimal monthlyHoursTarget, decimal effectiveness)
        {
            var ratio = effectiveness <= 1m ? effectiveness : effectiveness / 100m;
            return Math.Round(monthlyHoursTarget * ratio, 1);
        }

        private AnalyticsPeriodDto ResolveComparisonPeriod(BookingTargetComparisonQueryDto query)
        {
            var today = DateTime.UtcNow.Date;
            var fiscalYearStartMonth = GetFiscalYearStartMonth();
            var isMonthMode = IsMode(query.PeriodMode, "month") || IsMode(query.QuickSelect, "month");
            var isFullFiscalYear = IsMode(query.PeriodMode, "fiscal_year") || IsMode(query.QuickSelect, "fiscal_year");
            var month = query.Month is >= 1 and <= 12 ? query.Month.Value : today.Month;

            int fiscalYear;
            int calendarYear;
            if (isMonthMode && query.Year.HasValue && query.Year.Value > 0)
            {
                calendarYear = query.Year.Value;
                fiscalYear = ResolveFiscalYear(calendarYear, month, fiscalYearStartMonth);
            }
            else
            {
                fiscalYear = query.FiscalYear.GetValueOrDefault(
                    query.Year.GetValueOrDefault(ResolveFiscalYear(today.Year, today.Month, fiscalYearStartMonth)));
                calendarYear = month >= fiscalYearStartMonth
                    ? fiscalYear - (fiscalYearStartMonth == 1 ? 0 : 1)
                    : fiscalYear;
            }

            var fiscalYearStartDate = GetFiscalYearStartDate(fiscalYear, fiscalYearStartMonth);
            var fiscalYearEndDate = fiscalYearStartDate.AddYears(1).AddTicks(-1);

            DateTime startDate;
            DateTime endDate;
            if (isMonthMode)
            {
                startDate = new DateTime(calendarYear, month, 1);
                endDate = startDate.AddMonths(1).AddTicks(-1);
            }
            else
            {
                startDate = fiscalYearStartDate;
                endDate = isFullFiscalYear ? fiscalYearEndDate : today;
            }

            if (endDate > fiscalYearEndDate)
                endDate = fiscalYearEndDate;
            if (endDate < startDate)
                endDate = startDate;

            return new AnalyticsPeriodDto
            {
                Year = calendarYear,
                Month = month,
                MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month),
                StartDate = DateOnly.FromDateTime(startDate),
                EndDate = DateOnly.FromDateTime(endDate),
                FiscalYear = fiscalYear,
                FiscalYearStartMonth = fiscalYearStartMonth,
                FiscalYearStartDate = DateOnly.FromDateTime(fiscalYearStartDate),
                FiscalYearEndDate = DateOnly.FromDateTime(fiscalYearEndDate)
            };
        }

        private static string ResolveCalculationMode(string? mode)
        {
            if (string.IsNullOrWhiteSpace(mode) ||
                IsMode(mode, "Brut") ||
                IsMode(mode, "Gross"))
            {
                return "Brut";
            }

            if (IsMode(mode, "Net"))
                return "Net";

            throw new BadRequestException("CalculationMode must be Brut or Net.");
        }

        private async Task<List<(Guid InternId, int Year, int Month, decimal Hours)>> LoadInternPopulationHoursAsync(
            DateTime startDate,
            DateTime endDate,
            List<Guid>? projectIds,
            List<Guid> internTableIds,
            List<Guid> accountInternIds)
        {
            var hours = new List<(Guid InternId, int Year, int Month, decimal Hours)>();

            if (internTableIds.Count > 0)
            {
                var directQuery = _context.InternHourEntries
                    .AsNoTracking()
                    .Where(e => e.Date >= startDate && e.Date <= endDate && internTableIds.Contains(e.InternAllocation.InternId));
                if (projectIds != null)
                    directQuery = directQuery.Where(e => projectIds.Contains(e.InternAllocation.ProjectId));

                hours.AddRange((await directQuery
                        .Select(e => new { e.InternAllocation.InternId, e.Date, e.Hours })
                        .ToListAsync())
                    .Select(e => (e.InternId, e.Date.Year, e.Date.Month, e.Hours)));

                var supervisionQuery = _context.HourEntryInternSupervisions
                    .AsNoTracking()
                    .Where(s => s.HourEntry.Date >= startDate && s.HourEntry.Date <= endDate && internTableIds.Contains(s.InternAllocation.InternId));
                if (projectIds != null)
                    supervisionQuery = supervisionQuery.Where(s => projectIds.Contains(s.InternAllocation.ProjectId));

                hours.AddRange((await supervisionQuery
                        .Select(s => new { s.InternAllocation.InternId, s.HourEntry.Date, s.Hours })
                        .ToListAsync())
                    .Select(s => (s.InternId, s.Date.Year, s.Date.Month, s.Hours)));
            }

            if (accountInternIds.Count > 0)
            {
                var accountQuery = _context.HourEntries
                    .AsNoTracking()
                    .Where(h => accountInternIds.Contains(h.UserId) && h.Date >= startDate && h.Date <= endDate);
                if (projectIds != null)
                    accountQuery = accountQuery.Where(h => h.ProjectId.HasValue && projectIds.Contains(h.ProjectId.Value));

                hours.AddRange((await accountQuery
                        .Select(h => new { InternId = h.UserId, h.Date, Hours = h.TotalHours })
                        .ToListAsync())
                    .Select(h => (h.InternId, h.Date.Year, h.Date.Month, h.Hours)));
            }

            return hours;
        }

        private static PopulationComparisonDto BuildPopulationComparison(
            string population,
            string populationKey,
            int people,
            decimal grossHours,
            decimal netHours,
            decimal targetHours,
            decimal hourlyRate,
            bool useNet)
        {
            var bookedHours = useNet ? netHours : grossHours;
            var remainingHours = targetHours - bookedHours;
            var bookedRevenue = bookedHours * hourlyRate;
            var targetRevenue = targetHours * hourlyRate;
            var remainingRevenue = targetRevenue - bookedRevenue;
            var grossRevenue = grossHours * hourlyRate;
            var netRevenue = netHours * hourlyRate;

            return new PopulationComparisonDto
            {
                Label = population,
                Population = population,
                PopulationKey = populationKey,
                Headcount = people,
                People = people,
                BookedHours = Math.Round(bookedHours, 2),
                TargetHours = Math.Round(targetHours, 2),
                RemainingHours = Math.Round(remainingHours, 2),
                VarianceHours = Math.Round(bookedHours - targetHours, 2),
                HoursAchievementPercentage = CalculateAchievementPercentage(bookedHours, targetHours),
                HoursAchievementPercent = CalculateAchievementPercentage(bookedHours, targetHours),
                BookedRevenue = Math.Round(bookedRevenue, 2),
                TargetRevenue = Math.Round(targetRevenue, 2),
                RemainingRevenue = Math.Round(remainingRevenue, 2),
                VarianceRevenue = Math.Round(bookedRevenue - targetRevenue, 2),
                RevenueAchievementPercentage = CalculateAchievementPercentage(bookedRevenue, targetRevenue),
                RevenueAchievementPercent = CalculateAchievementPercentage(bookedRevenue, targetRevenue),
                GrossBookedHours = Math.Round(grossHours, 2),
                NetBookedHours = Math.Round(netHours, 2),
                GrossRevenue = Math.Round(grossRevenue, 2),
                NetRevenue = Math.Round(netRevenue, 2),
                GrossBookedRevenue = Math.Round(grossRevenue, 2),
                NetBookedRevenue = Math.Round(netRevenue, 2),
                GrossHoursAchievementPercentage = CalculateAchievementPercentage(grossHours, targetHours),
                NetHoursAchievementPercentage = CalculateAchievementPercentage(netHours, targetHours),
                GrossRevenueAchievementPercentage = CalculateAchievementPercentage(grossRevenue, targetRevenue),
                NetRevenueAchievementPercentage = CalculateAchievementPercentage(netRevenue, targetRevenue)
            };
        }

        private static PopulationMonthlyMetricDto BuildPopulationMonthlyMetric(
            int people,
            decimal grossHours,
            decimal netHours,
            decimal targetHours,
            decimal hourlyRate,
            bool useNet)
        {
            var bookedHours = useNet ? netHours : grossHours;
            var bookedRevenue = bookedHours * hourlyRate;
            var targetRevenue = targetHours * hourlyRate;
            var grossRevenue = grossHours * hourlyRate;
            var netRevenue = netHours * hourlyRate;

            return new PopulationMonthlyMetricDto
            {
                People = people,
                BookedHours = Math.Round(bookedHours, 2),
                TargetHours = Math.Round(targetHours, 2),
                HoursAchievementPercentage = CalculateAchievementPercentage(bookedHours, targetHours),
                BookedRevenue = Math.Round(bookedRevenue, 2),
                TargetRevenue = Math.Round(targetRevenue, 2),
                RevenueAchievementPercentage = CalculateAchievementPercentage(bookedRevenue, targetRevenue),
                GrossBookedHours = Math.Round(grossHours, 2),
                NetBookedHours = Math.Round(netHours, 2),
                GrossBookedRevenue = Math.Round(grossRevenue, 2),
                NetBookedRevenue = Math.Round(netRevenue, 2)
            };
        }

        private static BookingTargetSummaryDto BuildBookingTargetSummary(
            PopulationComparisonDto total,
            PopulationComparisonDto employees,
            PopulationComparisonDto subcontractors,
            PopulationComparisonDto interns)
        {
            return new BookingTargetSummaryDto
            {
                TotalPeople = total.People,
                BookedHours = total.BookedHours,
                TargetHours = total.TargetHours,
                RemainingHours = total.RemainingHours,
                VarianceHours = total.VarianceHours,
                HoursAchievementPercentage = total.HoursAchievementPercentage,
                BookedRevenue = total.BookedRevenue,
                TargetRevenue = total.TargetRevenue,
                RemainingRevenue = total.RemainingRevenue,
                VarianceRevenue = total.VarianceRevenue,
                RevenueAchievementPercentage = total.RevenueAchievementPercentage,
                GrossBookedHours = total.GrossBookedHours,
                NetBookedHours = total.NetBookedHours,
                GrossBookedRevenue = total.GrossBookedRevenue,
                NetBookedRevenue = total.NetBookedRevenue,
                Employees = employees,
                Subcontractors = subcontractors,
                Interns = interns,
                Total = total
            };
        }
    }
}
