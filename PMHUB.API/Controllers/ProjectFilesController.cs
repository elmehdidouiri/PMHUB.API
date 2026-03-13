using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.IServices;
using PMHUB.Domain.Enums;
 

namespace PMHUB.API.Controllers
{
    [ApiController]
    [Route("api/projects/{projectId:guid}/files")]
    public class ProjectFilesController : ControllerBase
    {
        private readonly IProjectFileService _service;
        private readonly ILogger<ProjectFilesController> _logger;

        public ProjectFilesController(IProjectFileService service, ILogger<ProjectFilesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET api/projects/{projectId}/files
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectFileDto>>>> GetAll(Guid projectId)
        {
            try
            {
                _logger.LogInformation("Récupération des fichiers pour ProjectId {ProjectId}", projectId);
                var files = await _service.GetByProjectIdAsync(projectId);
                return Ok(ApiResponse<IEnumerable<ProjectFileDto>>.Ok(files));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des fichiers pour ProjectId {ProjectId}", projectId);
                throw;
            }
        }

        // GET api/projects/{projectId}/files/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ApiResponse<ProjectFileDto>>> GetById(Guid projectId, Guid id)
        {
            try
            {
                _logger.LogInformation("Récupération du fichier Id {Id} pour ProjectId {ProjectId}", id, projectId);
                var file = await _service.GetByIdAsync(id);
                return Ok(ApiResponse<ProjectFileDto>.Ok(file));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du fichier Id {Id} pour ProjectId {ProjectId}", id, projectId);
                throw;
            }
        }

        // POST api/projects/{projectId}/files
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<ProjectFileDto>>> Upload(Guid projectId, [FromForm] UploadProjectFileDto dto)
        {
            try
            {
                _logger.LogInformation("Upload d'un fichier pour ProjectId {ProjectId} : {@Dto}", projectId, dto);
                var file = await _service.UploadAsync(projectId, dto);
                _logger.LogInformation("Fichier uploadé avec succès : {FileId}", file.Id);
                return CreatedAtAction(nameof(GetById),
                    new { projectId, id = file.Id },
                    ApiResponse<ProjectFileDto>.Ok(file, "Fichier uploadé avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'upload du fichier pour ProjectId {ProjectId}", projectId);
                throw;
            }
        }

        // GET api/projects/{projectId}/files/{id}/download
        [HttpGet("{id:guid}/download")]
        public async Task<IActionResult> Download(Guid projectId, Guid id)
        {
            try
            {
                _logger.LogInformation("Téléchargement du fichier Id {Id} pour ProjectId {ProjectId}", id, projectId);
                var (content, contentType, fileName) = await _service.DownloadAsync(id);
                return File(content, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du téléchargement du fichier Id {Id} pour ProjectId {ProjectId}", id, projectId);
                throw;
            }
        }

        // PUT api/projects/{projectId}/files/{id}
        [HttpPut("{id:guid}")]
        public async Task<ActionResult<ApiResponse<ProjectFileDto>>> Update(Guid projectId, Guid id, [FromBody] UpdateProjectFileDto dto)
        {
            try
            {
                _logger.LogInformation("Mise à jour du fichier Id {Id} pour ProjectId {ProjectId} : {@Dto}", id, projectId, dto);
                var file = await _service.UpdateAsync(id, dto);
                _logger.LogInformation("Fichier Id {Id} mis à jour avec succès", id);
                return Ok(ApiResponse<ProjectFileDto>.Ok(file, "Fichier mis à jour avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du fichier Id {Id} pour ProjectId {ProjectId}", id, projectId);
                throw;
            }
        }

        // DELETE api/projects/{projectId}/files/{id}
        [HttpDelete("{id:guid}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid projectId, Guid id)
        {
            try
            {
                _logger.LogInformation("Suppression du fichier Id {Id} pour ProjectId {ProjectId}", id, projectId);
                await _service.DeleteAsync(id);
                _logger.LogInformation("Fichier Id {Id} supprimé avec succès", id);
                return Ok(ApiResponse.Ok("Fichier supprimé avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du fichier Id {Id} pour ProjectId {ProjectId}", id, projectId);
                throw;
            }
        }

        // POST api/projects/{projectId}/files/bulk
        [HttpPost("bulk")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ProjectFileDto>>>> UploadBulk(Guid projectId, [FromForm] BulkUploadDto dto)
        {
            try
            {
                if (dto.Files.Count == 0)
                {
                    _logger.LogWarning("Aucun fichier reçu pour l'upload bulk ProjectId {ProjectId}", projectId);
                    return BadRequest(ApiResponse.Fail("Aucun fichier reçu."));
                }

                _logger.LogInformation("Upload bulk de {Count} fichiers pour ProjectId {ProjectId}", dto.Files.Count, projectId);

                var uploadDtos = dto.Files.Select((file, index) => new UploadProjectFileDto
                {
                    File = file,
                    FileType = (ProjectFileType)dto.FileTypes.ElementAtOrDefault(index),
                    Description = dto.Descriptions.ElementAtOrDefault(index)
                }).ToList();

                var result = await _service.UploadBulkAsync(projectId, uploadDtos);
                _logger.LogInformation("{Count} fichiers uploadés avec succès pour ProjectId {ProjectId}", result.Count(), projectId);

                return Ok(ApiResponse<IEnumerable<ProjectFileDto>>.Ok(result, "Fichiers uploadés avec succès."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'upload bulk des fichiers pour ProjectId {ProjectId}", projectId);
                throw;
            }
        }
    }
}