using PMHUB.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Application.IServices
{
    public interface IProjectFileService
    {
         Task<ProjectFileDto> UploadAsync(Guid projectId, UploadProjectFileDto dto);

         Task<IEnumerable<ProjectFileDto>> GetByProjectIdAsync(Guid projectId);

         Task<ProjectFileDto> GetByIdAsync(Guid id);

         Task<(byte[] Content, string ContentType, string FileName)> DownloadAsync(Guid id);

         Task<ProjectFileDto> UpdateAsync(Guid id, UpdateProjectFileDto dto);
         Task<ProjectFileDto> UploadNewVersionAsync(Guid id, UploadProjectFileVersionDto dto);
         Task<IEnumerable<ProjectFileVersionDto>> GetVersionsAsync(Guid id);

         Task DeleteAsync(Guid id);
        Task<IEnumerable<ProjectFileDto>> UploadBulkAsync(
    Guid projectId, IEnumerable<UploadProjectFileDto> dtos);

    }
}
