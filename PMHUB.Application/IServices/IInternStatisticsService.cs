using PMHUB.Application.DTOs;

namespace PMHUB.Application.IServices
{
    public interface IInternStatisticsService
    {
        // Statistiques individuelles
        Task<InternStatisticsDto> GetInternStatisticsAsync(Guid internId);
        Task<InternWorkVisualizationDto> GetInternWorkVisualizationAsync(Guid internId, DateTime? startDate = null, DateTime? endDate = null);
        Task<InternPeriodStatisticsDto> GetInternPeriodStatisticsAsync(Guid internId, int year, int? month = null);
        
        // Statistiques globales
        Task<InternsDashboardDto> GetInternsDashboardAsync();
        Task<InternsDashboardDto> GetSupervisorInternsStatisticsAsync(Guid supervisorId);
        
        // Suppression en masse
        Task DeleteInternAllDataAsync(Guid internId, Guid currentUserId);
        Task DeleteAllInternDataAsync(Guid currentUserId);
    }
}