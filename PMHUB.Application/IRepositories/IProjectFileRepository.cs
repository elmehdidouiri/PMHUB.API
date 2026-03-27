using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.Interfaces
{
    public interface IProjectFileRepository : IRepository<ProjectFile>
    {
         Task<IEnumerable<ProjectFile>> GetByProjectIdAsync(Guid projectId);

         Task<ProjectFile?> GetByIdWithIncludesAsync(Guid id);

          Task<bool> ExistsByTypeAsync(Guid projectId, PMHUB.Domain.Enums.ProjectFileType fileType);
    }
}
