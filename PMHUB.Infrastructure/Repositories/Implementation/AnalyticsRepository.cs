using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PMHUB.Application.DTOs;
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

        public AnalyticsRepository(PMHubDbContext context, IOptions<CompanyStandards> standards, ITargetSettingsService targetSettingsService)
        {
            _context = context;
            _standards = standards.Value;
            _targetSettingsService = targetSettingsService;
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
                ? BuildMonthlyHoursByCategory(selectedHours, monthlyPeriods)
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
                .Where(h => h.ProjectId.HasValue && ids.Contains(h.ProjectId.Value) && h.Date >= startDate && h.Date <= endDate);

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
            List<(DateTime Start, DateTime End)> monthlyPeriods)
        {
            return monthlyPeriods
                .Select(period =>
                {
                    var monthHours = hours
                        .Where(h => h.Date >= period.Start && h.Date <= period.End)
                        .ToList();

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
                        TotalHours = Math.Round(monthHours.Sum(h => h.TotalHours), 2)
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
                    var targetHours = activeUsers * _standards.MonthlyHoursTarget;

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
                .Where(h => h.ProjectId.HasValue && ids.Contains(h.ProjectId.Value) && h.Date >= startDate && h.Date <= endDate);

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

            // The target is based on every active normal user, excluding interns.
            // Interns can be identified either by their member type or by their role.
            var activeMembers = await _context.Users
                .OfType<NormalUser>()
                .AsNoTracking()
                .Where(u => u.IsActive &&
                            u.MemberType != MemberType.intern &&
                            (u.Role == null || u.Role.Name != "Intern"))
                .Select(u => new 
                { 
                    u.Id, 
                    u.FirstName, 
                    u.LastName 
                })
                .ToListAsync();

            var activeMemberIds = activeMembers.Select(u => u.Id).ToList();

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

            var memberCapacities = activeMembers.Select(emp =>
            {
                decimal booked = hoursDataDict.TryGetValue(emp.Id, out var value) ? value : 0m;
                return new MemberCapacityDto
                {
                    UserId = emp.Id,
                    UserName = (emp.FirstName + " " + emp.LastName).Trim(),
                    BookedHours = Math.Round(booked, 2),
                    Percentage = targetHoursPerMember > 0
                        ? Math.Round((booked / targetHoursPerMember) * 100m, 2)
                        : 0m
                };
            }).OrderByDescending(x => x.BookedHours).ToList();

            var capacityTarget = new CapacityTargetDto
            {
                ActualBookedHours = Math.Round(totalBookedHours, 2),
                TargetHours = Math.Round(totalTargetHours, 2),
                RemainingHours = Math.Round(Math.Max(0m, totalTargetHours - totalBookedHours), 2)
            };

            var totalPriceTarget = totalTargetHours * query.HourlyRate;
            var totalBookedPrice = totalBookedHours * query.HourlyRate;

            var memberPrices = activeMembers.Select(emp =>
            {
                decimal booked = hoursDataDict.TryGetValue(emp.Id, out var value) ? value : 0m;
                decimal bookedPrice = booked * query.HourlyRate;
                return new MemberPriceDto
                {
                    UserId = emp.Id,
                    UserName = (emp.FirstName + " " + emp.LastName).Trim(),
                    BookedPrice = Math.Round(bookedPrice, 2),
                    Percentage = totalPriceTarget > 0
                        ? Math.Round((bookedPrice / (targetHoursPerMember * query.HourlyRate)) * 100m, 2)
                        : 0m
                };
            }).OrderByDescending(x => x.BookedPrice).ToList();

            var priceTarget = new PriceTargetDto
            {
                BookedPrice = Math.Round(totalBookedPrice, 2),
                TargetPrice = Math.Round(totalPriceTarget, 2),
                RemainingPrice = Math.Round(Math.Max(0, totalPriceTarget - totalBookedPrice), 2)
            };

            return new CapacityPriceDashboardDto
            {
                MemberCapacities = memberCapacities,
                CapacityTarget = capacityTarget,
                MemberPrices = memberPrices,
                PriceTarget = priceTarget
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
                .Where(u => u.IsActive &&
                    (u.MemberType == MemberType.intern || (u.Role != null && u.Role.Name == "Intern")))
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
                    RemainingHours = Math.Round(Math.Max(0m, totalTargetHours - totalBookedHours), 2)
                },
                PriceTarget = new PriceTargetDto
                {
                    BookedPrice = Math.Round(totalBookedPrice, 2),
                    TargetPrice = Math.Round(totalTargetPrice, 2),
                    RemainingPrice = Math.Round(Math.Max(0m, totalTargetPrice - totalBookedPrice), 2)
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
                .Where(u => u.IsActive &&
                            (u.MemberType == MemberType.Employee || u.MemberType == MemberType.Subcontractor));

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
    }
}
