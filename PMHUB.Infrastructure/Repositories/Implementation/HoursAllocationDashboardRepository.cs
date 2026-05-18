using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PMHUB.Application.DTOs;
using PMHUB.Application.IRepositories;
using PMHUB.Domain.Entities;
using PMHUB.Domain.Enums;
using PMHUB.Infrastructure.Persistence;
using PMHUB.Shared.Helpers;
using PMHUB.Shared.Models;

namespace PMHUB.Infrastructure.Repositories.Implementation
{
    public class HoursAllocationDashboardRepository : IHoursAllocationDashboardRepository
    {
        private readonly PMHubDbContext _context;
        private readonly CompanyStandards _standards;

        public HoursAllocationDashboardRepository(PMHubDbContext context, IOptions<CompanyStandards> standards)
        {
            _context = context;
            _standards = standards.Value;
        }

        public async Task<HoursAllocationFiltersDto> GetFiltersAsync()
        {
            var fiscalYearStartMonth = GetFiscalYearStartMonth();
            var currentFiscalYear = CompanyYearHelper.GetCurrentCompanyYear(DateTime.UtcNow);
            var projectDates = await _context.Projects.AsNoTracking().Select(p => p.StartDate).ToListAsync();
            var hourDates = await _context.HourEntries.AsNoTracking().Select(h => h.Date).ToListAsync();

            return new HoursAllocationFiltersDto
            {
                Users = await _context.Users
                    .OfType<NormalUser>()
                    .AsNoTracking()
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Select(u => new HoursAllocationOptionDto<Guid>
                    {
                        Id = u.Id,
                        Label = (u.FirstName + " " + u.LastName).Trim()
                    })
                    .ToListAsync(),
                Projects = await _context.Projects
                    .AsNoTracking()
                    .OrderBy(p => p.Name)
                    .Select(p => new HoursAllocationOptionDto<Guid>
                    {
                        Id = p.Id,
                        Label = p.Name
                    })
                    .ToListAsync(),
                Roles = await _context.Roles
                    .AsNoTracking()
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Name)
                    .Select(r => new HoursAllocationOptionDto<Guid>
                    {
                        Id = r.Id,
                        Label = r.Name
                    })
                    .ToListAsync(),
                Members = await _context.ProjectMembers
                    .AsNoTracking()
                    .Where(pm => !pm.Project.ProjectManagerId.HasValue || pm.UserId != pm.Project.ProjectManagerId.Value)
                    .GroupBy(pm => new { pm.UserId, pm.User.FirstName, pm.User.LastName })
                    .OrderBy(g => g.Key.FirstName)
                    .ThenBy(g => g.Key.LastName)
                    .Select(g => new HoursAllocationOptionDto<Guid>
                    {
                        Id = g.Key.UserId,
                        Label = (g.Key.FirstName + " " + g.Key.LastName).Trim()
                    })
                    .ToListAsync(),
                FiscalYears = projectDates
                    .Concat(hourDates)
                    .Select(d => ResolveFiscalYear(d.Year, d.Month, fiscalYearStartMonth))
                    .Append(currentFiscalYear)
                    .Distinct()
                    .OrderBy(y => y)
                    .ToList(),
                Months = Enumerable.Range(0, 12)
                    .Select(offset => ((fiscalYearStartMonth - 1 + offset) % 12) + 1)
                    .Select(month => new HoursAllocationMonthOptionDto
                    {
                        Value = month,
                        Label = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month)
                    })
                    .ToList()
            };
        }

        public async Task<HoursAllocationDashboardDto> GetDashboardAsync(HoursAllocationDashboardQueryDto query)
        {
            var period = ResolveSelectedPeriod(query);
            var ytdPeriod = ResolveYtdPeriod(period.End);
            var selectedRows = await BuildHoursQuery(query, period.Start, period.End).ToListAsync();
            var ytdRows = await BuildHoursQuery(query, ytdPeriod.Start, ytdPeriod.End).ToListAsync();
            var availableHoursPerUser = CalculateAvailableHours(period.Start, period.End);
            var totalAvailableHours = selectedRows.Select(r => r.UserId).Distinct().Count() * availableHoursPerUser;
            var totalHours = selectedRows.Sum(r => r.TotalHours);
            var ytdHours = ytdRows.Sum(r => r.TotalHours);

            return new HoursAllocationDashboardDto
            {
                Summary = new HoursAllocationSummaryDto
                {
                    TotalHours = Round(totalHours),
                    ActiveUsers = selectedRows.Select(r => r.UserId).Distinct().Count(),
                    Projects = selectedRows.Where(r => r.ProjectId.HasValue).Select(r => r.ProjectId).Distinct().Count(),
                    Allocations = selectedRows.Count,
                    AverageUtilization = CalculatePercentage(totalHours, totalAvailableHours),
                    YearToDateHours = Round(ytdHours),
                    AverageMonthlyHours = CalculateAverageMonthlyHours(ytdHours, ytdPeriod.Start, ytdPeriod.End),
                    WorkedDays = selectedRows.Select(r => r.Date.Date).Distinct().Count()
                },
                Details = BuildDetails(selectedRows),
                MonthlyBreakdown = BuildMonthlyBreakdown(selectedRows, period.Start, period.End),
                HoursByUser = BuildHoursByUser(selectedRows, period.Start, period.End, availableHoursPerUser),
                HoursByProject = BuildHoursByProject(selectedRows),
                HoursByRole = BuildHoursByRole(selectedRows, totalHours),
                HoursByTeam = BuildHoursByTeam(selectedRows)
            };
        }

        public async Task<List<HoursAllocationReminderRecipientDto>> GetReminderRecipientsAsync(HoursAllocationDashboardQueryDto query)
        {
            var period = ResolveSelectedPeriod(query);
            var hours = await BuildHoursQuery(query, period.Start, period.End).ToListAsync();
            var totalsByUser = hours
                .GroupBy(h => h.UserId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalHours));

            var usersQuery = BuildReminderUsersQuery(query);
            var users = await usersQuery
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName
                })
                .ToListAsync();

            return users
                .Select(u => new HoursAllocationReminderRecipientDto
                {
                    UserId = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    TotalHours = totalsByUser.GetValueOrDefault(u.Id)
                })
                .ToList();
        }

        private IQueryable<HoursAllocationRow> BuildHoursQuery(
            HoursAllocationDashboardQueryDto query,
            DateTime startDate,
            DateTime endDate)
        {
            var hoursQuery = _context.HourEntries
                .AsNoTracking()
                .Where(h => h.Date >= startDate && h.Date <= endDate);

            if (query.UserId.HasValue && query.UserId.Value != Guid.Empty)
                hoursQuery = hoursQuery.Where(h => h.UserId == query.UserId.Value);

            if (query.MemberId.HasValue && query.MemberId.Value != Guid.Empty)
                hoursQuery = hoursQuery.Where(h => h.UserId == query.MemberId.Value);

            if (query.ProjectId.HasValue && query.ProjectId.Value != Guid.Empty)
                hoursQuery = hoursQuery.Where(h => h.ProjectId == query.ProjectId.Value);

            if (query.RoleId.HasValue && query.RoleId.Value != Guid.Empty)
            {
                var roleId = query.RoleId.Value;
                hoursQuery = hoursQuery.Where(h =>
                    h.User.RoleId == roleId ||
                    h.ProjectId.HasValue &&
                    h.Project.ProjectMembers.Any(pm => pm.UserId == h.UserId && pm.RoleId == roleId));
            }

            return hoursQuery
                .OrderByDescending(h => h.Date)
                .Select(h => new HoursAllocationRow
                {
                    Date = h.Date,
                    UserId = h.UserId,
                    UserName = (h.User.FirstName + " " + h.User.LastName).Trim(),
                    RoleId = h.User.RoleId,
                    Role = h.User.Role.Name,
                    ProjectId = h.ProjectId,
                    ProjectName = h.Project != null
                        ? h.Project.Name
                        : h.Category == CategoryWork.Workshop
                            ? "Workshop"
                            : h.Category == CategoryWork.Holiday
                                ? "Holiday"
                                : h.Category == CategoryWork.MonthlyMeeting
                                    ? "Monthly meeting"
                                    : "Other",
                    Department = h.Project != null && h.Project.Department != null ? h.Project.Department.Name : string.Empty,
                    Type = h.Category == CategoryWork.Project ? h.AllocationType.ToString() : h.Category.ToString(),
                    ExecutionHours = h.ExecutionHours,
                    TechLeadHours = h.SupervisionHours,
                    ProcessHours = h.ProcessHours,
                    ProjectManagementHours = h.ManagementHours,
                    ResearchAndDevHours = h.RAndDHours,
                    WorkshopHours = h.WorkshopHours,
                    OtherHours = h.OtherHours,
                    TotalHours = h.TotalHours,
                    IsProjectManager = h.Project != null && h.Project.ProjectManagerId == h.UserId
                });
        }

        private IQueryable<NormalUser> BuildReminderUsersQuery(HoursAllocationDashboardQueryDto query)
        {
            var usersQuery = _context.Users
                .OfType<NormalUser>()
                .AsNoTracking()
                .Where(u => u.IsActive && u.IsApproved);

            if (query.UserId.HasValue && query.UserId.Value != Guid.Empty)
                usersQuery = usersQuery.Where(u => u.Id == query.UserId.Value);

            if (query.MemberId.HasValue && query.MemberId.Value != Guid.Empty)
                usersQuery = usersQuery.Where(u => u.Id == query.MemberId.Value);

            if (query.RoleId.HasValue && query.RoleId.Value != Guid.Empty)
                usersQuery = usersQuery.Where(u => u.RoleId == query.RoleId.Value);

            if (query.ProjectId.HasValue && query.ProjectId.Value != Guid.Empty)
            {
                var projectId = query.ProjectId.Value;
                usersQuery = usersQuery.Where(u =>
                    u.ProjectMembers.Any(pm =>
                        pm.ProjectId == projectId &&
                        (!pm.Project.ProjectManagerId.HasValue || pm.UserId != pm.Project.ProjectManagerId.Value)) ||
                    _context.Projects.Any(p => p.Id == projectId && p.ProjectManagerId == u.Id));
            }

            return usersQuery.OrderBy(u => u.FirstName).ThenBy(u => u.LastName);
        }

        private List<HoursAllocationDetailDto> BuildDetails(List<HoursAllocationRow> rows)
        {
            return rows
                .Select(r => new HoursAllocationDetailDto
                {
                    Date = r.Date,
                    UserId = r.UserId,
                    UserName = r.UserName,
                    ProjectId = r.ProjectId,
                    ProjectName = r.ProjectName,
                    Type = r.Type,
                    ExecutionHours = Round(r.ExecutionHours),
                    TechLeadHours = Round(r.TechLeadHours),
                    ProcessHours = Round(r.ProcessHours),
                    ProjectManagementHours = Round(r.ProjectManagementHours),
                    ResearchAndDevHours = Round(r.ResearchAndDevHours),
                    WorkshopHours = Round(r.WorkshopHours),
                    OtherHours = Round(r.OtherHours),
                    TotalHours = Round(r.TotalHours),
                    IsProjectManager = r.IsProjectManager
                })
                .ToList();
        }

        private List<HoursAllocationMonthlyBreakdownDto> BuildMonthlyBreakdown(
            List<HoursAllocationRow> rows,
            DateTime startDate,
            DateTime endDate)
        {
            return BuildMonthStarts(startDate, endDate)
                .Select(monthStart =>
                {
                    var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
                    var monthRows = rows
                        .Where(r => r.Date >= monthStart && r.Date <= monthEnd)
                        .ToList();
                    var totalHours = monthRows.Sum(r => r.TotalHours);
                    var activeUsers = monthRows.Select(r => r.UserId).Distinct().Count();
                    var availableHours = activeUsers * CalculateAvailableHours(monthStart, monthEnd);

                    return new HoursAllocationMonthlyBreakdownDto
                    {
                        Year = monthStart.Year,
                        Month = monthStart.Month,
                        MonthName = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(monthStart.Month),
                        TotalHours = Round(totalHours),
                        UtilizationPercentage = CalculatePercentage(totalHours, availableHours)
                    };
                })
                .ToList();
        }

        private List<HoursAllocationByUserDto> BuildHoursByUser(
            List<HoursAllocationRow> rows,
            DateTime startDate,
            DateTime endDate,
            decimal availableHoursPerUser)
        {
            var durationLabel = BuildDurationLabel(startDate, endDate);

            return rows
                .GroupBy(r => new { r.UserId, r.UserName, r.Role })
                .Select(g =>
                {
                    var allocatedHours = g.Sum(x => x.TotalHours);
                    return new HoursAllocationByUserDto
                    {
                        UserId = g.Key.UserId,
                        UserName = g.Key.UserName,
                        Role = g.Key.Role,
                        Department = g.GroupBy(x => x.Department).OrderByDescending(x => x.Count()).FirstOrDefault()?.Key ?? string.Empty,
                        AllocatedHours = Round(allocatedHours),
                        DurationLabel = durationLabel,
                        RemainingHours = Round(Math.Max(availableHoursPerUser - allocatedHours, 0m)),
                        AvailableHours = Round(availableHoursPerUser),
                        UtilizationPercentage = CalculatePercentage(allocatedHours, availableHoursPerUser),
                        ExecutionHours = Round(g.Sum(x => x.ExecutionHours)),
                        TechLeadHours = Round(g.Sum(x => x.TechLeadHours)),
                        ProcessHours = Round(g.Sum(x => x.ProcessHours)),
                        ProjectManagementHours = Round(g.Sum(x => x.ProjectManagementHours)),
                        ResearchAndDevHours = Round(g.Sum(x => x.ResearchAndDevHours)),
                        WorkshopHours = Round(g.Sum(x => x.WorkshopHours)),
                        ProjectCount = g.Where(x => x.ProjectId.HasValue).Select(x => x.ProjectId).Distinct().Count(),
                        AllocationCount = g.Count()
                    };
                })
                .OrderByDescending(x => x.AllocatedHours)
                .ToList();
        }

        private static List<HoursAllocationByProjectDto> BuildHoursByProject(List<HoursAllocationRow> rows)
        {
            return rows
                .GroupBy(r => new { r.ProjectId, r.ProjectName })
                .Select(g => new HoursAllocationByProjectDto
                {
                    ProjectId = g.Key.ProjectId,
                    ProjectName = g.Key.ProjectName,
                    TotalHours = Round(g.Sum(x => x.TotalHours)),
                    ProjectManagerHours = Round(g.Where(x => x.IsProjectManager).Sum(x => x.TotalHours)),
                    TeamHours = Round(g.Where(x => !x.IsProjectManager).Sum(x => x.TotalHours)),
                    TeamMembers = g.Where(x => !x.IsProjectManager).Select(x => x.UserId).Distinct().Count(),
                    Allocations = g.Count()
                })
                .OrderByDescending(x => x.TotalHours)
                .ToList();
        }

        private static List<HoursAllocationByRoleDto> BuildHoursByRole(List<HoursAllocationRow> rows, decimal totalHours)
        {
            return rows
                .GroupBy(r => new { r.RoleId, r.Role })
                .Select(g => new HoursAllocationByRoleDto
                {
                    RoleId = g.Key.RoleId,
                    Role = g.Key.Role,
                    TotalHours = Round(g.Sum(x => x.TotalHours)),
                    TeamMembers = g.Where(x => !x.IsProjectManager).Select(x => x.UserId).Distinct().Count(),
                    Percentage = CalculatePercentage(g.Sum(x => x.TotalHours), totalHours)
                })
                .OrderByDescending(x => x.TotalHours)
                .ToList();
        }

        private static List<HoursAllocationByTeamDto> BuildHoursByTeam(List<HoursAllocationRow> rows)
        {
            return rows
                .Where(r => !r.IsProjectManager)
                .GroupBy(r => new { r.UserId, r.UserName, r.ProjectId, r.ProjectName })
                .Select(g => new HoursAllocationByTeamDto
                {
                    MemberId = g.Key.UserId,
                    MemberName = g.Key.UserName,
                    ProjectName = g.Key.ProjectName,
                    TotalHours = Round(g.Sum(x => x.TotalHours)),
                    WorkedDays = g.Select(x => x.Date.Date).Distinct().Count(),
                    AllocationCount = g.Count()
                })
                .OrderByDescending(x => x.TotalHours)
                .ToList();
        }

        private (DateTime Start, DateTime End) ResolveSelectedPeriod(HoursAllocationDashboardQueryDto query)
        {
            if (query.FromDate.HasValue || query.ToDate.HasValue)
            {
                var fallback = ResolveFiscalYearPeriod(query.Year);
                return (
                    query.FromDate?.Date ?? fallback.Start,
                    EndOfDay(query.ToDate?.Date ?? fallback.End));
            }

            var quickSelect = query.QuickSelect?.Trim().ToLowerInvariant();
            return quickSelect switch
            {
                "month" => ResolveMonthPeriod(query.Year, query.Month),
                "year" => ResolveFiscalYearPeriod(query.Year),
                "ytd" => ResolveYtdPeriod(DateTime.UtcNow.Date),
                _ => query.Month.HasValue
                    ? ResolveMonthPeriod(query.Year, query.Month)
                    : ResolveYtdPeriod(DateTime.UtcNow.Date)
            };
        }

        private (DateTime Start, DateTime End) ResolveYtdPeriod(DateTime selectedEnd)
        {
            var fiscalYearStartMonth = GetFiscalYearStartMonth();
            var fiscalYear = ResolveFiscalYear(selectedEnd.Year, selectedEnd.Month, fiscalYearStartMonth);
            var startYear = fiscalYear - (fiscalYearStartMonth == 1 ? 0 : 1);
            var start = new DateTime(startYear, fiscalYearStartMonth, 1);
            var fiscalEnd = EndOfDay(start.AddYears(1).AddDays(-1));
            var end = selectedEnd;

            if (end > fiscalEnd)
                end = fiscalEnd;

            return (start, EndOfDay(end.Date));
        }

        private (DateTime Start, DateTime End) ResolveFiscalYearPeriod(int? year)
        {
            var fiscalYear = ResolveCompanyYear(year, DateTime.UtcNow.Date);
            var fiscalYearStartMonth = GetFiscalYearStartMonth();
            var startYear = fiscalYear - (fiscalYearStartMonth == 1 ? 0 : 1);
            var start = new DateTime(startYear, fiscalYearStartMonth, 1);
            return (
                start,
                EndOfDay(start.AddYears(1).AddDays(-1)));
        }

        private (DateTime Start, DateTime End) ResolveMonthPeriod(int? year, int? month)
        {
            var current = DateTime.UtcNow.Date;
            var resolvedMonth = month is >= 1 and <= 12 ? month.Value : current.Month;
            var fiscalYear = ResolveCompanyYear(year, current);
            var calendarYear = resolvedMonth >= GetFiscalYearStartMonth() ? fiscalYear - 1 : fiscalYear;
            var start = new DateTime(calendarYear, resolvedMonth, 1);
            return (start, EndOfDay(start.AddMonths(1).AddDays(-1)));
        }

        private int GetFiscalYearStartMonth()
        {
            return _standards.FiscalYearStartMonth is >= 1 and <= 12
                ? _standards.FiscalYearStartMonth
                : 10;
        }

        private decimal CalculateAvailableHours(DateTime startDate, DateTime endDate)
        {
            var workingDays = 0;
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                    workingDays++;
            }

            return workingDays * _standards.HoursPerDay;
        }

        private static IEnumerable<DateTime> BuildMonthStarts(DateTime startDate, DateTime endDate)
        {
            var current = new DateTime(startDate.Year, startDate.Month, 1);
            var last = new DateTime(endDate.Year, endDate.Month, 1);

            while (current <= last)
            {
                yield return current;
                current = current.AddMonths(1);
            }
        }

        private static string BuildDurationLabel(DateTime startDate, DateTime endDate)
        {
            if (startDate.Year == endDate.Year && startDate.Month == endDate.Month)
                return CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(startDate.Month) + " " + startDate.Year;

            return $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";
        }

        private static decimal CalculateAverageMonthlyHours(decimal totalHours, DateTime startDate, DateTime endDate)
        {
            var months = ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month + 1;
            return months <= 0 ? 0m : Round(totalHours / months);
        }

        private static decimal CalculatePercentage(decimal numerator, decimal denominator)
        {
            return denominator <= 0 ? 0m : Round(Math.Min(numerator * 100m / denominator, 100m));
        }

        private static decimal Round(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static DateTime EndOfDay(DateTime date)
        {
            return date.Date.AddDays(1).AddTicks(-1);
        }

        private int ResolveCompanyYear(int? year, DateTime today)
        {
            var fiscalYearStartMonth = GetFiscalYearStartMonth();

            return year.HasValue && year.Value > 0
                ? year.Value
                : ResolveFiscalYear(today.Year, today.Month, fiscalYearStartMonth);
        }

        private static int ResolveFiscalYear(int year, int month, int fiscalYearStartMonth)
        {
            return month >= fiscalYearStartMonth
                ? year + (fiscalYearStartMonth == 1 ? 0 : 1)
                : year;
        }

        private class HoursAllocationRow
        {
            public DateTime Date { get; set; }
            public Guid UserId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public Guid RoleId { get; set; }
            public string Role { get; set; } = string.Empty;
            public Guid? ProjectId { get; set; }
            public string ProjectName { get; set; } = string.Empty;
            public string Department { get; set; } = string.Empty;
            public string Type { get; set; } = string.Empty;
            public decimal ExecutionHours { get; set; }
            public decimal TechLeadHours { get; set; }
            public decimal ProcessHours { get; set; }
            public decimal ProjectManagementHours { get; set; }
            public decimal ResearchAndDevHours { get; set; }
            public decimal WorkshopHours { get; set; }
            public decimal OtherHours { get; set; }
            public decimal TotalHours { get; set; }
            public bool IsProjectManager { get; set; }
        }
    }
}
