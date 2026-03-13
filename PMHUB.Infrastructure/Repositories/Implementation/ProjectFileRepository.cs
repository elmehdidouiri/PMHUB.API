using Microsoft.EntityFrameworkCore;
using PMHUB.Application.Interfaces;
using PMHUB.Domain.Entities;
using PMHUB.Domain.Enums;
using PMHUB.Infrastructure.Persistence;
using PMHUB.Infrastructure.Repositories.Generique;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Infrastructure.Repositories
{
    public class ProjectFileRepository : Repository<ProjectFile>, IProjectFileRepository
    {
        private readonly PMHubDbContext _context;

        public ProjectFileRepository(PMHubDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ProjectFile>> GetByProjectIdAsync(Guid projectId) =>
            await _context.ProjectFiles
                .Where(f => f.ProjectId == projectId)
                .OrderBy(f => f.FileType)
                .AsNoTracking()
                .ToListAsync();

        public async Task<ProjectFile?> GetByIdWithIncludesAsync(Guid id) =>
            await _context.ProjectFiles
                .Include(f => f.Project)
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

        public async Task<bool> ExistsByTypeAsync(Guid projectId, ProjectFileType fileType) =>
            await _context.ProjectFiles
                .AnyAsync(f => f.ProjectId == projectId && f.FileType == fileType);
    }
}