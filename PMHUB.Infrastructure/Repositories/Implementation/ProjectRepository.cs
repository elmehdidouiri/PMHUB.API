using Microsoft.EntityFrameworkCore;
using PMHUB.Application.DTOs;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Persistence;
using PMHUB.Infrastructure.Repositories.Generique;
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
                .Include(p => p.Department)
                    .ThenInclude(d => d!.Plant);

        private IQueryable<Project> WithIncludes() =>
            _context.Projects
                .AsSplitQuery()
                .Include(p => p.Department)
                    .ThenInclude(d => d!.BusinessUnit)
                .Include(p => p.Department)
                    .ThenInclude(d => d!.Plant)
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

        // Tracked query for UPDATE scenarios.
        // Important: we intentionally do NOT include Department (or other heavy navigations)
        // to avoid duplicate tracking conflicts when validating related entities separately.
        private IQueryable<Project> WithUpdateIncludes() =>
            _context.Projects
                .AsSplitQuery()
                .Include(p => p.ProjectBusinessUnits)
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

        public async Task<(IEnumerable<Project> Items, int TotalCount)> GetPagedAsync(ProjectSearchDto query)
        {
            var queryable = WithSummaryIncludes().AsNoTracking();

            queryable = ApplyProjectFilters(queryable, query);

            var totalCount = await queryable.CountAsync();

            var items = await queryable
               .Skip((query.PageNumber - 1) * query.PageSize)
               .Take(query.PageSize)
               .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<Project>> GetFilteredAsync(ProjectSearchDto query)
        {
            var queryable = WithSummaryIncludes().AsNoTracking();
            queryable = ApplyProjectFilters(queryable, query);
            return await queryable.ToListAsync();
        }

        private IQueryable<Project> ApplyProjectFilters(IQueryable<Project> queryable, ProjectSearchDto query)
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                queryable = queryable.Where(p =>
                    p.Name.Contains(query.Search) ||
                    (p.Description != null && p.Description.Contains(query.Search)) ||
                    (p.Sponsor != null && p.Sponsor.Contains(query.Search)));
            }

            if (query.Status.HasValue)
                queryable = queryable.Where(p => p.Status == query.Status.Value);

            if (query.Phase.HasValue)
                queryable = queryable.Where(p => p.Phase == query.Phase.Value);

            if (query.ProjectType.HasValue)
                queryable = queryable.Where(p => p.ProjectType == query.ProjectType.Value);

            if (query.DepartmentId.HasValue)
                queryable = queryable.Where(p => p.DepartmentId == query.DepartmentId.Value);

            if (query.BusinessUnitId.HasValue)
                queryable = queryable.Where(p => p.ProjectBusinessUnits.Any(bu => bu.BusinessUnitId == query.BusinessUnitId.Value));

            if (query.ProjectManagerId.HasValue)
                queryable = queryable.Where(p => p.ProjectManagerId == query.ProjectManagerId.Value);

            if (query.UserId.HasValue)
            {
                queryable = queryable.Where(p => 
                    p.ProjectManagerId == query.UserId.Value || 
                    p.ProjectMembers.Any(m => m.UserId == query.UserId.Value));
            }

            if (query.InternId.HasValue)
            {
                queryable = queryable.Where(p => 
                    p.InternAllocations.Any(ia => ia.InternId == query.InternId.Value));
            }

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
    } 
}
