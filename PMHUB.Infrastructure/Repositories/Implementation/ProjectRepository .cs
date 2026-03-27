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
        private readonly PMHubDbContext _context;

        public ProjectRepository(PMHubDbContext context) : base(context)
        {
            _context = context;
        }

        private IQueryable<Project> WithIncludes() =>
            _context.Projects
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
                 .Include(p => p.ProjectManager)
                .Include(p => p.KPIs)
                .Include(p => p.ProjectResources)
                .Include(p => p.StrategicCriteria)
                .Include(p => p.SubProjects)
                    .ThenInclude(sp => sp.Department)
                .Include(p => p.ParentProject)
                 ;

        public async Task<Project?> GetByNameAsync(string name) =>
            await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name == name);

        public async Task<Project?> GetFullProjectByIdAsync(Guid id) =>
            await _context.Projects
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

        public async Task<IEnumerable<Project>> GetAllWithIncludesAsync() =>
            await WithIncludes()
                .AsNoTracking()
                .ToListAsync();

        public async Task<IEnumerable<Project>> FindWithIncludesAsync(
            Expression<Func<Project, bool>> predicate) =>
            await WithIncludes()
                .AsNoTracking()
                .Where(predicate)
                .ToListAsync();
    
    public async Task<(IEnumerable<Project> Items, int TotalCount)> GetPagedAsync(
    PaginationQueryDto query)
        {
            var queryable = WithIncludes().AsNoTracking();

             if (!string.IsNullOrWhiteSpace(query.Search))
                queryable = queryable.Where(p =>
                    p.Name.Contains(query.Search) ||
                    (p.Description != null && p.Description.Contains(query.Search)) ||
                    (p.Sponsor != null && p.Sponsor.Contains(query.Search)));

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

            var totalCount = await queryable.CountAsync();

            var items = await queryable
               .Skip((query.PageNumber - 1) * query.PageSize)
               .Take(query.PageSize)
               .ToListAsync();

            return (items, totalCount);
        }
    } 
}