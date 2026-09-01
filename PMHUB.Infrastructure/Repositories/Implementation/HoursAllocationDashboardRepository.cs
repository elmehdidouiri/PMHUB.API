using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PMHUB.Application.DTOs;
using PMHUB.Application.IRepositories;
using PMHUB.Application.IServices;
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
        private readonly ITargetSettingsService _targetSettingsService;
        private CompanyStandards _standards;

        public HoursAllocationDashboardRepository(PMHubDbContext context, IOptions<CompanyStandards> standards, ITargetSettingsService targetSettingsService)
        {
            _context = context;
            _standards = standards.Value;
            _targetSettingsService = targetSettingsService;
        }

        public async Task<HoursAllocationFiltersDto> GetFiltersAsync()
        {
            await LoadTargetSettingsAsync();

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
            await LoadTargetSettingsAsync();

            var period = ResolveSelectedPeriod(query);
            var ytdPeriod = ResolveYtdPeriod(period.End);
            var selectedRows = await BuildHoursQuery(query, period.Start, period.End).ToListAsync();
            var ytdRows = await BuildHoursQuery(query, ytdPeriod.Start, ytdPeriod.End).ToListAsync();
            var capacityUsers = await BuildCapacityUsersQuery(query).ToListAsync();
            var projectCount = await BuildSummaryProjectsQuery(query, period.Start, period.End).CountAsync();
            var availableHoursPerUser = CalculateAvailableHours(period.Start, period.End);
            var totalAvailableHours = capacityUsers.Count * availableHoursPerUser;
            var totalHours = selectedRows.Sum(r => r.TotalHours);
            var ytdHours = ytdRows.Sum(r => r.TotalHours);
            var details = BuildDetails(selectedRows);
            var monthlyBreakdown = BuildMonthlyBreakdown(selectedRows, capacityUsers.Count, period.Start, period.End);
            var hoursByUser = BuildHoursByUser(selectedRows, capacityUsers, period.Start, period.End, availableHoursPerUser);
            var hoursByProject = BuildHoursByProject(selectedRows);
            var hoursByProjectUser = BuildHoursByProjectUser(selectedRows);
            var hoursByRole = BuildHoursByRole(selectedRows, totalHours);
            var hoursByTeam = BuildHoursByTeam(selectedRows);
            var pagination = ApplyPagination(query, ref details, ref hoursByUser, ref hoursByProject, ref hoursByProjectUser, ref hoursByRole, ref hoursByTeam);

            return new HoursAllocationDashboardDto
            {
                Summary = new HoursAllocationSummaryDto
                {
                    TotalHours = Round(totalHours),
                    Projects = projectCount,
                    Allocations = selectedRows.Count,
                    AverageUtilization = CalculatePercentage(totalHours, totalAvailableHours),
                    YearToDateHours = Round(ytdHours),
                    AverageMonthlyHours = CalculateAverageMonthlyHours(ytdHours, ytdPeriod.Start, ytdPeriod.End),
                    WorkedDays = selectedRows.Select(r => r.Date.Date).Distinct().Count()
                },
                Details = details,
                MonthlyBreakdown = monthlyBreakdown,
                HoursByUser = hoursByUser,
                HoursByProject = hoursByProject,
                HoursByProjectUser = hoursByProjectUser,
                HoursByRole = hoursByRole,
                HoursByTeam = hoursByTeam,
                Pagination = pagination
            };
        }

        public async Task<List<HoursAllocationReminderRecipientDto>> GetReminderRecipientsAsync(HoursAllocationDashboardQueryDto query)
        {
            await LoadTargetSettingsAsync();

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
                    h.Project != null &&
                    h.Project.ProjectMembers.Any(pm => pm.UserId == h.UserId && pm.RoleId == roleId));
            }

            var search = query.Search?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                hoursQuery = hoursQuery.Where(h =>
                    (h.User.FirstName + " " + h.User.LastName).Contains(search) ||
                    h.User.Email.Contains(search) ||
                    h.User.FirstName.Contains(search) ||
                    h.User.LastName.Contains(search) ||
                    h.Project != null && h.Project.Name.Contains(search) ||
                    h.User.Role != null && h.User.Role.Name.Contains(search));
            }

            return hoursQuery
                .Where(h => h.User.RoleId.HasValue)
                .OrderByDescending(h => h.Date)
                .Select(h => new HoursAllocationRow
                {
                    Date = h.Date,
                    UserId = h.UserId,
                    UserName = (h.User.FirstName + " " + h.User.LastName).Trim(),
                    RoleId = h.User.RoleId!.Value,
                    Role = h.User.Role != null ? h.User.Role.Name : string.Empty,
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
                    TotalHours = h.ExecutionHours + h.SupervisionHours + h.ProcessHours +
                        h.ManagementHours + h.RAndDHours + h.WorkshopHours + h.OtherHours +
                        h.InternManagementHours,
                    IsProjectManager = h.Project != null && h.Project.ProjectManagerId == h.UserId
                });
        }

        private IQueryable<Project> BuildSummaryProjectsQuery(
            HoursAllocationDashboardQueryDto query,
            DateTime startDate,
            DateTime endDate)
        {
            var projectsQuery = _context.Projects
                .AsNoTracking()
                .Where(p => p.StartDate <= endDate &&
                    (!p.EndDate.HasValue || p.EndDate.Value >= startDate));

            if (query.ProjectId.HasValue && query.ProjectId.Value != Guid.Empty)
                projectsQuery = projectsQuery.Where(p => p.Id == query.ProjectId.Value);

            if (query.UserId.HasValue && query.UserId.Value != Guid.Empty)
            {
                var userId = query.UserId.Value;
                projectsQuery = projectsQuery.Where(p =>
                    p.ProjectManagerId == userId ||
                    p.ProjectMembers.Any(pm => pm.UserId == userId));
            }

            if (query.MemberId.HasValue && query.MemberId.Value != Guid.Empty)
            {
                var memberId = query.MemberId.Value;
                projectsQuery = projectsQuery.Where(p => p.ProjectMembers.Any(pm => pm.UserId == memberId));
            }

            if (query.RoleId.HasValue && query.RoleId.Value != Guid.Empty)
            {
                var roleId = query.RoleId.Value;
                projectsQuery = projectsQuery.Where(p =>
                    p.ProjectMembers.Any(pm => pm.RoleId == roleId) ||
                    p.ProjectManager != null && p.ProjectManager.RoleId == roleId);
            }

            var search = query.Search?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                projectsQuery = projectsQuery.Where(p =>
                    p.Name.Contains(search) ||
                    p.ProjectManager != null &&
                    ((p.ProjectManager.FirstName + " " + p.ProjectManager.LastName).Contains(search) ||
                     p.ProjectManager.Email.Contains(search) ||
                     p.ProjectManager.Role != null && p.ProjectManager.Role.Name.Contains(search)) ||
                    p.ProjectMembers.Any(pm =>
                        (pm.User.FirstName + " " + pm.User.LastName).Contains(search) ||
                        pm.User.Email.Contains(search) ||
                        pm.Role.Name.Contains(search)));
            }

            return projectsQuery;
        }

        private IQueryable<CapacityUserRow> BuildCapacityUsersQuery(HoursAllocationDashboardQueryDto query)
        {
            var usersQuery = _context.Users
                .OfType<NormalUser>()
                .AsNoTracking()
                .Where(u => u.IsActive && u.IsApproved &&
                    u.RoleId.HasValue &&
                    u.Role != null &&
                    u.Role.Name.ToLower() != "intern" &&
                    u.Role.Name.ToLower() != "stagiaire");

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

            var search = query.Search?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                usersQuery = usersQuery.Where(u =>
                    (u.FirstName + " " + u.LastName).Contains(search) ||
                    u.Email.Contains(search) ||
                    u.FirstName.Contains(search) ||
                    u.LastName.Contains(search) ||
                    u.Role != null && u.Role.Name.Contains(search) ||
                    u.ProjectMembers.Any(pm => pm.Project.Name.Contains(search)) ||
                    _context.Projects.Any(p => p.ProjectManagerId == u.Id && p.Name.Contains(search)));
            }

            return usersQuery
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new CapacityUserRow
                {
                    UserId = u.Id,
                    UserName = (u.FirstName + " " + u.LastName).Trim(),
                    RoleId = u.RoleId!.Value,
                    Role = u.Role != null ? u.Role.Name : string.Empty
                });
        }

        private IQueryable<NormalUser> BuildReminderUsersQuery(HoursAllocationDashboardQueryDto query)
        {
            var usersQuery = _context.Users
                .OfType<NormalUser>()
                .Include(u => u.Role)
                .AsNoTracking()
                .Where(u => u.IsActive && u.IsApproved &&
                    u.RoleId.HasValue &&
                    u.Role != null &&
                    u.Role.Name.ToLower() != "intern" &&
                    u.Role.Name.ToLower() != "stagiaire");

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
            int capacityUserCount,
            DateTime startDate,
            DateTime endDate)
        {
            return BuildMonthStarts(startDate, endDate)
                .Select(monthStart =>
                {
                    var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
                    var effectiveStart = monthStart < startDate ? startDate : monthStart;
                    var effectiveEnd = monthEnd > endDate ? endDate : monthEnd;
                    var monthRows = rows
                        .Where(r => r.Date >= monthStart && r.Date <= monthEnd)
                        .ToList();
                    var totalHours = monthRows.Sum(r => r.TotalHours);
                    var availableHours = capacityUserCount * CalculateAvailableHours(effectiveStart, effectiveEnd);

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
            List<CapacityUserRow> users,
            DateTime startDate,
            DateTime endDate,
            decimal availableHoursPerUser)
        {
            var durationLabel = BuildDurationLabel(startDate, endDate);
            var rowsByUser = rows
                .GroupBy(r => r.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return users
                .Select(user =>
                {
                    var userRows = rowsByUser.GetValueOrDefault(user.UserId) ?? new List<HoursAllocationRow>();
                    var allocatedHours = userRows.Sum(x => x.TotalHours);
                    return new HoursAllocationByUserDto
                    {
                        UserId = user.UserId,
                        UserName = user.UserName,
                        Role = user.Role,
                        Department = userRows.GroupBy(x => x.Department).OrderByDescending(x => x.Count()).FirstOrDefault()?.Key ?? string.Empty,
                        AllocatedHours = Round(allocatedHours),
                        DurationLabel = durationLabel,
                        RemainingHours = Round(Math.Max(availableHoursPerUser - allocatedHours, 0m)),
                        AvailableHours = Round(availableHoursPerUser),
                        UtilizationPercentage = CalculatePercentage(allocatedHours, availableHoursPerUser),
                        ExecutionHours = Round(userRows.Sum(x => x.ExecutionHours)),
                        TechLeadHours = Round(userRows.Sum(x => x.TechLeadHours)),
                        ProcessHours = Round(userRows.Sum(x => x.ProcessHours)),
                        ProjectManagementHours = Round(userRows.Sum(x => x.ProjectManagementHours)),
                        ResearchAndDevHours = Round(userRows.Sum(x => x.ResearchAndDevHours)),
                        WorkshopHours = Round(userRows.Sum(x => x.WorkshopHours)),
                        ProjectCount = userRows.Where(x => x.ProjectId.HasValue).Select(x => x.ProjectId).Distinct().Count(),
                        AllocationCount = userRows.Count
                    };
                })
                .OrderByDescending(x => x.AllocatedHours)
                .ThenBy(x => x.UserName)
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

        private static List<HoursAllocationByProjectUserDto> BuildHoursByProjectUser(List<HoursAllocationRow> rows)
        {
            return rows
                .GroupBy(r => new
                {
                    r.ProjectId,
                    r.ProjectName,
                    r.UserId,
                    r.UserName,
                    r.Role,
                    r.IsProjectManager
                })
                .Select(g => new HoursAllocationByProjectUserDto
                {
                    ProjectId = g.Key.ProjectId,
                    ProjectName = g.Key.ProjectName,
                    UserId = g.Key.UserId,
                    UserName = g.Key.UserName,
                    Role = g.Key.Role,
                    TotalHours = Round(g.Sum(x => x.TotalHours)),
                    ExecutionHours = Round(g.Sum(x => x.ExecutionHours)),
                    TechLeadHours = Round(g.Sum(x => x.TechLeadHours)),
                    ProcessHours = Round(g.Sum(x => x.ProcessHours)),
                    ProjectManagementHours = Round(g.Sum(x => x.ProjectManagementHours)),
                    ResearchAndDevHours = Round(g.Sum(x => x.ResearchAndDevHours)),
                    WorkshopHours = Round(g.Sum(x => x.WorkshopHours)),
                    OtherHours = Round(g.Sum(x => x.OtherHours)),
                    WorkedDays = g.Select(x => x.Date.Date).Distinct().Count(),
                    AllocationCount = g.Count(),
                    IsProjectManager = g.Key.IsProjectManager
                })
                .OrderBy(x => x.ProjectName)
                .ThenByDescending(x => x.TotalHours)
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

        private static HoursAllocationPaginationDto ApplyPagination(
            HoursAllocationDashboardQueryDto query,
            ref List<HoursAllocationDetailDto> details,
            ref List<HoursAllocationByUserDto> hoursByUser,
            ref List<HoursAllocationByProjectDto> hoursByProject,
            ref List<HoursAllocationByProjectUserDto> hoursByProjectUser,
            ref List<HoursAllocationByRoleDto> hoursByRole,
            ref List<HoursAllocationByTeamDto> hoursByTeam)
        {
            var table = ResolvePaginatedTable(query.Analysis);

            return table switch
            {
                "resourcesCapacity" => PaginateTable(table, query, ref hoursByUser),
                "projects" => PaginateTable(table, query, ref hoursByProject),
                "projectUsers" => PaginateTable(table, query, ref hoursByProjectUser),
                "roles" => PaginateTable(table, query, ref hoursByRole),
                "team" => PaginateTable(table, query, ref hoursByTeam),
                _ => PaginateTable(table, query, ref details)
            };
        }

        private static string ResolvePaginatedTable(string? analysis)
        {
            return analysis?.Trim().ToLowerInvariant() switch
            {
                "resourcescapacity" or "resourcecapacity" or "resources" or "users" => "resourcesCapacity",
                "projects" or "project" => "projects",
                "projectusers" or "project-users" or "project_users" or "projectuser" or "project-user" or "byprojectuser" or "by-project-user" => "projectUsers",
                "roles" or "role" => "roles",
                "team" or "teams" or "members" => "team",
                "details" or "allocations" => "details",
                _ => "details"
            };
        }

        private static HoursAllocationPaginationDto PaginateTable<T>(
            string table,
            HoursAllocationDashboardQueryDto query,
            ref List<T> items)
        {
            var totalCount = items.Count;
            var pageSize = query.All ? totalCount : query.PageSize;

            if (!query.All)
            {
                items = items
                    .Skip((query.PageNumber - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToList();
            }

            return new HoursAllocationPaginationDto
            {
                Table = table,
                PageNumber = query.PageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
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
            return _standards.GetAvailableHours(startDate, endDate);
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

        private async Task LoadTargetSettingsAsync()
        {
            _standards = await _targetSettingsService.GetCompanyStandardsAsync();
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

        private class CapacityUserRow
        {
            public Guid UserId { get; set; }
            public string UserName { get; set; } = string.Empty;
            public Guid RoleId { get; set; }
            public string Role { get; set; } = string.Empty;
        }
    }
}
