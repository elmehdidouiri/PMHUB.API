using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PMHUB.Application.DTOs;
using PMHUB.Application.IRepositories;
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
                ["MonthlyWorkingHours"] = 161.5m
            };

        private readonly PMHubDbContext _context;
        private readonly CompanyStandards _standards;

        public AnalyticsRepository(PMHubDbContext context, IOptions<CompanyStandards> standards)
        {
            _context = context;
            _standards = standards.Value;
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
            var period = ResolvePeriod(query);
            var startDate = period.StartDate.ToDateTime(TimeOnly.MinValue);
            var endDate = period.EndDate.ToDateTime(TimeOnly.MaxValue);
            var projectsQuery = BuildProjectsQuery(query);
            var projectIds = await projectsQuery.Select(p => p.Id).ToListAsync();

            var selectedHoursQuery = BuildHoursQuery(projectIds, query.UserId, startDate, endDate);

            var selectedHours = await selectedHoursQuery.ToListAsync();
            var selectedKpis = await BuildKpiQuery(projectIds, startDate, endDate).ToListAsync();

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
                    ProgressPercentage = p.ProgressPercentage,
                    EndDate = p.EndDate,
                    EstimatedDueDate = p.EstimatedDueDate
                })
                .ToListAsync();

            var projectsWithData = projectIds
                .Where(id => selectedHours.Any(h => h.ProjectId == id) || selectedKpis.Any(k => k.ProjectId == id))
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
                    AverageEffectiveness = CalculateEffectiveness(projectMetricRows, selectedKpis),
                    AverageOtd = CalculateOtd(projectMetricRows, selectedKpis, endDate),
                    AverageCsat = CalculateNamedKpiAverage(selectedKpis, "CSA"),
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

        private IQueryable<KPI> BuildKpiQuery(IEnumerable<Guid> projectIds, DateTime startDate, DateTime endDate)
        {
            var ids = projectIds.ToList();
            return _context.KPIs
                .AsNoTracking()
                .Where(k => ids.Contains(k.ProjectId) && k.CreatedAt >= startDate && k.CreatedAt <= endDate);
        }

        private AnalyticsPeriodDto ResolvePeriod(AnalyticsQueryDto query)
        {
            var today = DateTime.UtcNow.Date;
            var month = query.Month is >= 1 and <= 12 ? query.Month.Value : today.Month;
            var fiscalYearStartMonth = GetFiscalYearStartMonth();
            var fiscalYear = query.FiscalYear.GetValueOrDefault(
                query.Year.GetValueOrDefault(ResolveFiscalYear(today.Year, today.Month, fiscalYearStartMonth)));
            var calendarYear = month >= fiscalYearStartMonth
                ? fiscalYear - (fiscalYearStartMonth == 1 ? 0 : 1)
                : fiscalYear;
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
                    var kpisUpToMonth = kpis
                        .Where(k => k.CreatedAt <= period.End)
                        .ToList();
                    var kpisCreatedInMonth = kpisUpToMonth
                        .Where(k => k.CreatedAt >= period.Start)
                        .ToList();
                    var monthProjectIds = hours
                        .Where(h => h.Date >= period.Start && h.Date <= period.End)
                        .Where(h => h.ProjectId.HasValue)
                        .Select(h => h.ProjectId!.Value)
                        .Concat(kpisCreatedInMonth.Select(k => k.ProjectId))
                        .Distinct()
                        .ToList();
                    var monthKpis = kpisUpToMonth
                        .Where(k => monthProjectIds.Contains(k.ProjectId))
                        .ToList();

                    return new AnalyticsKpiMonthlyTrendDto
                    {
                        Year = period.Start.Year,
                        Month = period.Start.Month,
                        MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(period.Start.Month),
                        Effectiveness = CalculateEffectiveness(projects.Where(p => monthProjectIds.Contains(p.Id)).ToList(), monthKpis),
                        Otd = CalculateOtd(projects.Where(p => monthProjectIds.Contains(p.Id)).ToList(), monthKpis, period.End),
                        Csat = CalculateNamedKpiAverage(monthKpis, "CSA"),
                        ProjectsWithData = monthProjectIds.Count
                    };
                })
                .ToList();
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

            return CalculateAverage(projects.Select(p => CalculateTargetScore(p.ProgressPercentage, "Effectiveness")));
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

        private static decimal CalculateOtd(List<ProjectMetricRow> projects, List<KPI> kpis, DateTime referenceDate)
        {
            var kpiAverage = TryCalculateNamedKpiAverage(kpis, "OTD");
            if (kpiAverage.HasValue)
                return kpiAverage.Value;

            var completedProjects = projects
                .Where(p => p.EstimatedDueDate.HasValue)
                .ToList();

            if (completedProjects.Count == 0)
                return 0m;

            var completedOnTime = completedProjects.Count(p =>
                p.Status == ProjectStatus.Done
                    ? p.EndDate.HasValue && p.EndDate.Value <= p.EstimatedDueDate!.Value
                    : p.EstimatedDueDate!.Value >= referenceDate);

            return Math.Round(completedOnTime * 100m / completedProjects.Count, 2);
        }

        private static decimal? CalculateKpiScore(KPI kpi, string name)
        {
            var rawValue = kpi.CalculatedValue ?? (kpi.CurrentValue > 0 ? kpi.CurrentValue : null);
            if (!rawValue.HasValue)
                return null;

            if (string.Equals(name, "OTD", StringComparison.OrdinalIgnoreCase))
                return NormalizePercentage(rawValue.Value);

            var target = kpi.TargetValue > 0
                ? kpi.TargetValue
                : GetDefaultKpiTarget(kpi.Name);

            if (target <= 0)
                return NormalizePercentage(rawValue.Value);

            return CalculateTargetScore(rawValue.Value, target);
        }

        private static decimal CalculateTargetScore(decimal rawValue, string targetName)
        {
            return CalculateTargetScore(rawValue, GetDefaultKpiTarget(targetName));
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

        private class ProjectMetricRow
        {
            public Guid Id { get; set; }
            public ProjectStatus Status { get; set; }
            public int ProgressPercentage { get; set; }
            public DateTime? EndDate { get; set; }
            public DateTime? EstimatedDueDate { get; set; }
        }
    }
}
