using Microsoft.Extensions.Logging;
using PMHUB.Application.DTOs;
using PMHUB.Application.Exceptions;
using PMHUB.Application.Interfaces;
using PMHUB.Application.IServices;
using PMHUB.Application.Mappings;
using PMHUB.Application.Validators;
using PMHUB.Domain.Entities;
using PMHUB.Infrastructure.Repositories;
using Task = System.Threading.Tasks.Task;

namespace PMHUB.Application.Services
{
    public class ProjectFileService : IProjectFileService
    {
        private readonly IProjectFileRepository _fileRepository;
        private readonly IRepository<Project> _projectRepository;
        private readonly IFileStorageService _storageService;
        private readonly ILogger<ProjectFileService> _logger;

        public ProjectFileService(
            IProjectFileRepository fileRepository,
            IRepository<Project> projectRepository,
            IFileStorageService storageService,
            ILogger<ProjectFileService> logger)
        {
            _fileRepository = fileRepository;
            _projectRepository = projectRepository;
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<ProjectFileDto> UploadAsync(Guid projectId, UploadProjectFileDto dto)
        {
            _logger.LogInformation("Upload fichier {FileName} pour le projet {ProjectId}", dto.File.FileName, projectId);

            await (_projectRepository.GetByIdAsync(projectId)
                ?? throw new NotFoundException("Project", projectId));

            ProjectFileValidator.Validate(dto);

            var folder = dto.FileType.ToString().ToLower();
            var filePath = await _storageService.SaveFileAsync(dto.File, projectId, folder);

            var projectFile = new ProjectFile
            {
                ProjectId = projectId,
                FileType = dto.FileType,
                OriginalFileName = dto.File.FileName,
                StoredFileName = Path.GetFileName(filePath),
                FilePath = filePath,
                ContentType = dto.File.ContentType,
                FileSize = dto.File.Length,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _fileRepository.AddAsync(projectFile);
            await _fileRepository.SaveChangesAsync();

            _logger.LogInformation("Fichier {FileId} enregistré pour le projet {ProjectId}", projectFile.Id, projectId);

            return ProjectFileMapper.ToDto(projectFile, _storageService);
        }

        public async Task<IEnumerable<ProjectFileDto>> UploadBulkAsync(Guid projectId, IEnumerable<UploadProjectFileDto> dtos)
        {
            _logger.LogInformation("Upload multiple fichiers pour le projet {ProjectId}", projectId);

            await (_projectRepository.GetByIdAsync(projectId)
                ?? throw new NotFoundException("Project", projectId));

            foreach (var dto in dtos)
                ProjectFileValidator.Validate(dto);

            var projectFiles = new List<ProjectFile>();

            foreach (var dto in dtos)
            {
                var folder = dto.FileType.ToString().ToLower();
                var filePath = await _storageService.SaveFileAsync(dto.File, projectId, folder);

                projectFiles.Add(new ProjectFile
                {
                    ProjectId = projectId,
                    FileType = dto.FileType,
                    OriginalFileName = dto.File.FileName,
                    StoredFileName = Path.GetFileName(filePath),
                    FilePath = filePath,
                    ContentType = dto.File.ContentType,
                    FileSize = dto.File.Length,
                    Description = dto.Description,
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var projectFile in projectFiles)
            {
                await _fileRepository.AddAsync(projectFile);
                _logger.LogInformation("Fichier {FileId} préparé pour le projet {ProjectId}", projectFile.Id, projectId);
            }

            await _fileRepository.SaveChangesAsync();
            _logger.LogInformation("Tous les fichiers ont été enregistrés pour le projet {ProjectId}", projectId);

            return projectFiles.Select(f => ProjectFileMapper.ToDto(f, _storageService)).ToList();
        }

        public async Task<IEnumerable<ProjectFileDto>> GetByProjectIdAsync(Guid projectId)
        {
            _logger.LogInformation("Récupération des fichiers pour le projet {ProjectId}", projectId);

            await (_projectRepository.GetByIdAsync(projectId)
                ?? throw new NotFoundException("Project", projectId));

            var files = await _fileRepository.GetByProjectIdAsync(projectId);
            return ProjectFileMapper.ToDtoList(files, _storageService);
        }

        public async Task<ProjectFileDto> GetByIdAsync(Guid id)
        {
            _logger.LogInformation("Récupération du fichier {FileId}", id);

            var file = await _fileRepository.GetByIdWithIncludesAsync(id)
                ?? throw new NotFoundException("ProjectFile", id);

            return ProjectFileMapper.ToDto(file, _storageService);
        }

        public async Task<(byte[] Content, string ContentType, string FileName)> DownloadAsync(Guid id)
        {
            _logger.LogInformation("Téléchargement du fichier {FileId}", id);

            var file = await _fileRepository.GetByIdWithIncludesAsync(id)
                ?? throw new NotFoundException("ProjectFile", id);

            var content = await _storageService.GetFileAsync(file.FilePath);

            _logger.LogInformation("Fichier {FileId} téléchargé avec succès", id);

            return (content, file.ContentType, file.OriginalFileName);
        }

        public async Task<ProjectFileDto> UpdateAsync(Guid id, UpdateProjectFileDto dto)
        {
            _logger.LogInformation("Mise à jour du fichier {FileId}", id);

            var file = await _fileRepository.GetByIdWithIncludesAsync(id)
                ?? throw new NotFoundException("ProjectFile", id);

            file.Description = dto.Description;
            file.UpdatedAt = DateTime.UtcNow;

            _fileRepository.Update(file);
            await _fileRepository.SaveChangesAsync();

            _logger.LogInformation("Fichier {FileId} mis à jour", id);

            return ProjectFileMapper.ToDto(file, _storageService);
        }

        public async Task DeleteAsync(Guid id)
        {
            _logger.LogInformation("Suppression du fichier {FileId}", id);

            var file = await _fileRepository.GetByIdWithIncludesAsync(id)
                ?? throw new NotFoundException("ProjectFile", id);

            await _storageService.DeleteFileAsync(file.FilePath);
            _fileRepository.Remove(file);
            await _fileRepository.SaveChangesAsync();

            _logger.LogInformation("Fichier {FileId} supprimé avec succès", id);
        }
    }
}