using Microsoft.EntityFrameworkCore;
using PMHUB.Application.DTOs;
using PMHUB.Domain.Entities;
using PMHUB.Domain.Enums;
using PMHUB.Infrastructure.Persistence;
using PMHUB.Infrastructure.Repositories.Generique;
using PMHUB.Shared.Helpers;
using System.Linq.Expressions;

namespace PMHUB.Infrastructure.Repositories
{
    public class ProjectRepository : Repository<Project>, IProjectRepository
    {
        public ProjectRepository(PMHubDbContext context) : base(context)
        {
        }

        private IQueryable<Project> WithMembersOnly() =>
            _context.Projects
                .Include(p => p.ProjectMembers);

        private IQueryable<Project> WithSummaryIncludes() =>
            _context.Projects
                .AsSplitQuery()
                .Include(p => p.Department)
                    .ThenInclude(d => d!.Plant)
                .Include(p => p.ProjectDepartments)
                    .ThenInclude(pd => pd.Department)
                        .ThenInclude(d => d.Plant)
                .Include(p => p.ProjectBusinessUnits)
                .Include(p => p.ProjectTechnologies)
                .Include(p => p.ProjectSolutionDomains)
                .Include(p => p.ProjectMembers)
                .Include(p => p.ProjectResources)
                .Include(p => p.StrategicCriteria)
                .Include(p => p.KPIs)
                .Include(p => p.HourEntries);

        private IQueryable<Project> WithExportIncludes() =>
            WithSummaryIncludes()
                .Include(p => p.InternAllocations)
                    .ThenInclude(ia => ia.Intern)
                .Include(p => p.InternAllocations)
                    .ThenInclude(ia => ia.InternHourEntries);

        private IQueryable<Project> WithIncludes() =>
            _context.Projects
                .AsSplitQuery()
                .Include(p => p.Department)
                    .ThenInclude(d => d!.BusinessUnit)
                .Include(p => p.Department)
                    .ThenInclude(d => d!.Plant)
                .Include(p => p.ProjectDepartments)
                    .ThenInclude(pd => pd.Department)
                        .ThenInclude(d => d.BusinessUnit)
                .Include(p => p.ProjectDepartments)
                    .ThenInclude(pd => pd.Department)
                        .ThenInclude(d => d.Plant)
                .Include(p => p.ProjectBusinessUnits)
                    .ThenInclude(pbu => pbu.BusinessUnit)
                .Include(p => p.ProjectTechnologies)
                    .ThenInclude(pt => pt.Technology)
                .Include(p => p.ProjectSolutionDomains)
                    .ThenInclude(psd => psd.SolutionDomain)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.User)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.Role)
                .Include(p => p.InternAllocations)
                    .ThenInclude(ia => ia.Intern)
                        .ThenInclude(i => i.Role)
                .Include(p => p.InternAllocations)
                    .ThenInclude(ia => ia.Intern)
                        .ThenInclude(i => i.Supervisor)
                .Include(p => p.InternAllocations)
                    .ThenInclude(ia => ia.InternHourEntries)
                        .ThenInclude(ihe => ihe.BookedByUser)
                .Include(p => p.HourEntries)
                 .Include(p => p.ProjectManager)
                .Include(p => p.KPIs)
                .Include(p => p.ProjectResources)
                .Include(p => p.StrategicCriteria)
                .Include(p => p.Deliverables)
                    .ThenInclude(d => d.Tasks)
                        .ThenInclude(t => t.ProjectMember)
                            .ThenInclude(pm => pm!.User)
                .Include(p => p.Deliverables)
                    .ThenInclude(d => d.Tasks)
                        .ThenInclude(t => t.InternAllocation)
                            .ThenInclude(ia => ia!.Intern)
                .Include(p => p.TimelineEntries)
                    .ThenInclude(t => t.SponsorUser)
                .Include(p => p.RoadblockEntries)
                .Include(p => p.ProjectFiles)
                    .ThenInclude(f => f.Versions)
                .Include(p => p.SubProjects)
                    .ThenInclude(sp => sp.Department)
                 .Include(p => p.ParentProject)
                 ;

      
        private IQueryable<Project> WithUpdateIncludes() =>
            _context.Projects
                .AsSplitQuery()
                .Include(p => p.ProjectBusinessUnits)
                .Include(p => p.ProjectDepartments)
                .Include(p => p.ProjectTechnologies)
                .Include(p => p.ProjectSolutionDomains)
                .Include(p => p.ProjectMembers)
                .Include(p => p.ProjectResources)
                .Include(p => p.KPIs)
                .Include(p => p.StrategicCriteria);

        public async Task<Project?> GetByNameAsync(string name) =>
            await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name == name);

        public async Task<Project?> GetFullProjectByIdAsync(Guid id) =>
            await _context.Projects
                .AsSplitQuery()
                .Include(p => p.SubProjects)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.AssignedUser)    
        .Include(p => p.ProjectMembers)           
            .ThenInclude(pm => pm.User)
                .Include(p => p.InternAllocations)
                    .ThenInclude(ia => ia.Intern)
                .Include(p => p.KPIs)
                .Include(p => p.ProjectResources)
                .Include(p => p.ProjectTechnologies)
                    .ThenInclude(pt => pt.Technology)
                .Include(p => p.ProjectBusinessUnits)
                    .ThenInclude(pbu => pbu.BusinessUnit)
                .Include(p => p.ProjectDepartments)
                    .ThenInclude(pd => pd.Department)
                        .ThenInclude(d => d.Plant)
                .Include(p => p.ProjectSolutionDomains)
                    .ThenInclude(psd => psd.SolutionDomain)
                .Include(p => p.StrategicCriteria)
                .Include(p => p.ProjectMembers)
                    .ThenInclude(pm => pm.User)
                 .Include(p => p.ProjectManager)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Project?> GetByIdWithIncludesAsync(Guid id) =>
            await WithIncludes()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Project?> GetByIdForUpdateAsync(Guid id) =>
            await WithUpdateIncludes()
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task<DeliverableTask?> GetDeliverableTaskWithIncludesAsync(Guid taskId) =>
            await _context.DeliverableTasks
                .Include(t => t.ProjectMember)
                    .ThenInclude(pm => pm!.User)
                .Include(t => t.InternAllocation)
                    .ThenInclude(ia => ia!.Intern)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId);

        public async Task<IEnumerable<Project>> GetAllSummariesAsync() =>
            await WithSummaryIncludes()
                .AsNoTracking()
                .ToListAsync();

        public async Task<IEnumerable<Project>> GetAllWithIncludesAsync() =>
            await WithIncludes()
                .AsNoTracking()
                .ToListAsync();

        public async Task<IEnumerable<Project>> FindSummariesAsync(
            Expression<Func<Project, bool>> predicate) =>
            await WithSummaryIncludes()
                .AsNoTracking()
                .Where(predicate)
                .ToListAsync();

        public async Task<IEnumerable<Project>> FindWithIncludesAsync(
            Expression<Func<Project, bool>> predicate) =>
            await WithIncludes()
                .AsNoTracking()
                .Where(predicate)
                .ToListAsync();

        public async Task<IEnumerable<Project>> FindForHeaderNotificationsAsync(
            Expression<Func<Project, bool>> predicate,
            CancellationToken cancellationToken = default) =>
            await _context.Projects
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.ProjectManager)
                .Include(p => p.RoadblockEntries)
                .Where(predicate)
                .ToListAsync(cancellationToken);

        public async Task<(IEnumerable<Project> Items, int TotalCount)> GetPagedAsync(ProjectSearchDto query)
        {
            var queryable = WithSummaryIncludes().AsNoTracking();

            queryable = ApplyProjectFilters(queryable, query);

            var totalCount = await queryable.CountAsync();

            if (!query.All)
            {
                queryable = queryable
                   .Skip((query.PageNumber - 1) * query.PageSize)
                   .Take(query.PageSize);
            }

            var items = await queryable.ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<Project>> GetFilteredAsync(ProjectSearchDto query)
        {
            var queryable = WithSummaryIncludes().AsNoTracking();
            queryable = ApplyProjectFilters(queryable, query);
            return await queryable.ToListAsync();
        }

        public async Task<IEnumerable<Project>> GetFilteredForExportAsync(ProjectSearchDto query)
        {
            var queryable = WithExportIncludes().AsNoTracking();
            queryable = ApplyProjectFilters(queryable, query);
            return await queryable.ToListAsync();
        }

        private IQueryable<Project> ApplyProjectFilters(IQueryable<Project> queryable, ProjectSearchDto query)
        {
            if (query.UserId.HasValue)
            {
                queryable = queryable.Where(p =>
                    p.ProjectManagerId == query.UserId.Value ||
                    p.ProjectMembers.Any(m => m.UserId == query.UserId.Value));
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                queryable = queryable.Where(p =>
                    p.Name.Contains(query.Search) ||
                    (p.Description != null && p.Description.Contains(query.Search)) ||
                    (p.Sponsor != null && p.Sponsor.Contains(query.Search)));
            }

            if (query.Status.HasValue)
                queryable = queryable.Where(p => p.Status == query.Status.Value);
            else if (TryParseProjectStatus(query.ProjectStatus, out var projectStatus))
                queryable = queryable.Where(p => p.Status == projectStatus);

            if (query.Phase.HasValue)
                queryable = queryable.Where(p => p.Phase == query.Phase.Value);
            else if (TryParseProjectPhase(query.ProjectPhase, out var projectPhase))
                queryable = queryable.Where(p => p.Phase == projectPhase);

            if (TryParseProcessStatus(query.ProcessStatus, out var processStatus))
                queryable = queryable.Where(p => p.ProcessStatus == processStatus);

            if (query.ProjectType.HasValue)
                queryable = queryable.Where(p => p.ProjectType == query.ProjectType.Value);

            if (TryParseProjectManagementType(query.ProjectManagementType, out var projectManagementType))
                queryable = queryable.Where(p => p.ProjectManagementType == projectManagementType);

            if (query.DepartmentId.HasValue)
                queryable = queryable.Where(p =>
                    p.DepartmentId == query.DepartmentId.Value ||
                    p.ProjectDepartments.Any(pd => pd.DepartmentId == query.DepartmentId.Value));

            if (query.BusinessUnitId.HasValue)
                queryable = queryable.Where(p => p.ProjectBusinessUnits.Any(bu => bu.BusinessUnitId == query.BusinessUnitId.Value));

            if (query.PlantId.HasValue)
                queryable = queryable.Where(p =>
                    (p.Department != null && p.Department.PlantId == query.PlantId.Value) ||
                    p.ProjectDepartments.Any(pd => pd.Department.PlantId == query.PlantId.Value));

            if (query.ProjectId.HasValue)
                queryable = queryable.Where(p => p.Id == query.ProjectId.Value);

            if (query.RoleId.HasValue)
                queryable = queryable.Where(p =>
                    p.ProjectMembers.Any(m => m.RoleId == query.RoleId.Value) ||
                    p.InternAllocations.Any(ia => ia.Intern.RoleId == query.RoleId.Value));

            if (query.ProjectManagerId.HasValue)
                queryable = queryable.Where(p => p.ProjectManagerId == query.ProjectManagerId.Value);

            if (query.InternId.HasValue)
            {
                queryable = queryable.Where(p => 
                    p.InternAllocations.Any(ia => ia.InternId == query.InternId.Value));
            }

            var period = ResolveProjectPeriod(query);
            // Do not surface projected portfolio statistics for periods that have not begun.
            if (period.Start.HasValue && period.Start.Value.Date > DateTime.UtcNow.Date)
                return queryable.Where(_ => false);

            if (period.Start.HasValue && period.EndExclusive.HasValue)
            {
                var start = period.Start.Value;
                var endExclusive = period.EndExclusive.Value;
                queryable = queryable.Where(p =>
                    (p.Status != ProjectStatus.Done &&
                        p.StartDate < endExclusive &&
                        (!p.EndDate.HasValue || p.EndDate.Value >= start)) ||
                    (p.Status == ProjectStatus.Done &&
                        p.EndDate.HasValue &&
                        p.EndDate.Value >= start &&
                        p.EndDate.Value < endExclusive));
            }
            else
            {
                if (period.Start.HasValue)
                    queryable = queryable.Where(p =>
                        p.Status != ProjectStatus.Done ||
                        (p.EndDate.HasValue && p.EndDate.Value >= period.Start.Value));

                if (period.EndExclusive.HasValue)
                    queryable = queryable.Where(p =>
                        p.Status != ProjectStatus.Done ||
                        (p.EndDate.HasValue && p.EndDate.Value < period.EndExclusive.Value));
            }

            if (query.DelayedOnly)
            {
                var today = DateTime.UtcNow.Date;
                queryable = queryable.Where(p =>
                    p.Status != ProjectStatus.Done &&
                    p.EstimatedDueDate.HasValue &&
                    p.EstimatedDueDate.Value.Date < today);
            }

            if (query.IncompleteOnly)
                queryable = queryable.Where(ProjectDataIncompletePredicate());

            queryable = query.SortBy?.ToLower() switch
            {
                "name" => query.SortDescending
                                ? queryable.OrderByDescending(p => p.Name)
                                : queryable.OrderBy(p => p.Name),
                "status" => query.SortDescending
                                ? queryable.OrderByDescending(p => p.Status)
                                : queryable.OrderBy(p => p.Status),
                "createdat" => query.SortDescending
                                ? queryable.OrderByDescending(p => p.CreatedAt)
                                : queryable.OrderBy(p => p.CreatedAt),
                "budget" => query.SortDescending
                                ? queryable.OrderByDescending(p => p.Budget)
                                : queryable.OrderBy(p => p.Budget),
                _ => queryable.OrderByDescending(p => p.CreatedAt)
            };

            return queryable;
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

        private static bool TryParseProjectManagementType(string? value, out Category type)
        {
            return Enum.TryParse(value, true, out type);
        }

        private static bool TrySet<T>(out T target, T value)
        {
            target = value;
            return true;
        }

        private static (DateTime? Start, DateTime? EndExclusive) ResolveProjectPeriod(ProjectSearchDto query)
        {
            var start = query.StartDate?.Date;
            var endExclusive = query.EndDate?.Date.AddDays(1);

            if (!start.HasValue && !endExclusive.HasValue)
            {
                if (query.Ytd)
                {
                    var calendarYear = query.Year.HasValue && query.Year.Value > 0
                        ? query.Year.Value
                        : DateTime.UtcNow.Year;

                    start = new DateTime(calendarYear, 1, 1);

                    if (query.Month is >= 1 and <= 12)
                    {
                        endExclusive = new DateTime(calendarYear, query.Month.Value, 1).AddMonths(1);
                    }
                    else
                    {
                        var calendarYearEndExclusive = start.Value.AddYears(1);
                        var todayEndExclusive = DateTime.UtcNow.Date.AddDays(1);
                        endExclusive = todayEndExclusive < calendarYearEndExclusive
                            ? todayEndExclusive
                            : calendarYearEndExclusive;
                    }
                }
                else if (query.Year.HasValue && query.Month is >= 1 and <= 12)
                {
                    start = new DateTime(query.Year.Value, query.Month.Value, 1);
                    endExclusive = start.Value.AddMonths(1);
                }
                else if (query.Year.HasValue)
                {
                    start = new DateTime(query.Year.Value, 1, 1);
                    endExclusive = start.Value.AddYears(1);
                }
            }

            return (start, endExclusive);
        }

        private static Expression<Func<Project, bool>> ProjectDataIncompletePredicate()
        {
            return p =>
                (p.Name == null || p.Name.Trim() == string.Empty || p.Name.Trim().ToUpper() == "VIDE") ||
                (p.Description == null || p.Description.Trim() == string.Empty || p.Description.Trim().ToUpper() == "VIDE") ||
                (p.DepartmentId == Guid.Empty && !p.ProjectDepartments.Any()) ||
                p.Budget <= 0 ||
                p.StartDate == default ||
                !p.EstimatedDueDate.HasValue ||
                (p.Status == PMHUB.Domain.Enums.ProjectStatus.Done && !p.EndDate.HasValue) ||
                !p.ProjectManagerId.HasValue ||
                (p.Sponsor == null || p.Sponsor.Trim() == string.Empty || p.Sponsor.Trim().ToUpper() == "VIDE") ||
                p.DigitalContribution <= 0 ||
                (p.CostCenter == null || p.CostCenter.Trim() == string.Empty || p.CostCenter.Trim().ToUpper() == "VIDE") ||
                p.CostSaving <= 0 ||
                (p.CodeSourceLink == null || p.CodeSourceLink.Trim() == string.Empty || p.CodeSourceLink.Trim().ToUpper() == "VIDE") ||
                (p.SolutionLink == null || p.SolutionLink.Trim() == string.Empty || p.SolutionLink.Trim().ToUpper() == "VIDE") ||
                (p.ServerHostName == null || p.ServerHostName.Trim() == string.Empty || p.ServerHostName.Trim().ToUpper() == "VIDE") ||
                (p.CurrentState == null || p.CurrentState.Trim() == string.Empty || p.CurrentState.Trim().ToUpper() == "VIDE") ||
                (p.NextSteps == null || p.NextSteps.Trim() == string.Empty || p.NextSteps.Trim().ToUpper() == "VIDE") ||
                (p.Enhancements == null || p.Enhancements.Trim() == string.Empty || p.Enhancements.Trim().ToUpper() == "VIDE") ||
                p.EstimatedHours <= 0 ||
                !p.ProjectBusinessUnits.Any() ||
                !p.ProjectTechnologies.Any() ||
                !p.ProjectSolutionDomains.Any() ||
                !p.ProjectMembers.Any() ||
                !p.ProjectResources.Any() ||
                !p.StrategicCriteria.Any() ||
                !p.KPIs.Any();
        }
    } 
}
