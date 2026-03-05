using Microsoft.EntityFrameworkCore;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Persistence;
using PMHUB.Infrastructure.Repositories.Generique;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

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
            .Include(p => p.Members)
            .Include(p => p.KPIs)
            .Include(p => p.ProjectResources)
            .Include(p => p.SubProjects)
                .ThenInclude(sp => sp.Department)
            .Include(p => p.ParentProject)
            .Include(p => p.ProjectAllocations);

        public async Task<Project?> GetByNameAsync(string name)
        {
            return await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name == name);
        }

        public async Task<Project?> GetFullProjectByIdAsync(Guid id)
        {
            return await _context.Projects
                .Include(p => p.SubProjects)
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.AssignedUser)
                .Include(p => p.ProjectAllocations)
                    .ThenInclude(a => a.User)
                .Include(p => p.InternAllocations)
                    .ThenInclude(ia => ia.Intern)
                .Include(p => p.KPIs)
                .Include(p => p.ProjectResources)
                .Include(p => p.ProjectTechnologies)
                .Include(p => p.ProjectBusinessUnits)
                .Include(p => p.ProjectSolutionDomains)
                .Include(p => p.StrategicCriteria)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        

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
    }
}