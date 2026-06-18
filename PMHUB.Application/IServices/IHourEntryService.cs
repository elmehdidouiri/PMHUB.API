using PMHUB.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PMHUB.Application.IServices
{
    public interface IHourEntryService
    {
         Task<HourEntryDto> CreateAsync(CreateHourEntryDto dto, Guid userId);
        Task<HourEntryDto> UpdateAsync(Guid id, UpdateHourEntryDto dto, Guid userId);
        Task DeleteAsync(Guid id, Guid userId);

        Task<IEnumerable<HourEntrySummaryDto>> GetMyEntriesAsync(Guid userId);
        Task<IEnumerable<HourEntrySummaryDto>> GetMyEntriesByDateAsync(Guid userId, DateTime date);
        Task<IEnumerable<HourEntrySummaryDto>> GetMyEntriesByMonthAsync(Guid userId, int year, int month);
        Task<IEnumerable<HourEntrySummaryDto>> GetMyEntriesByProjectAsync(Guid userId, Guid projectId);
        Task<IEnumerable<HourEntrySummaryDto>> GetByProjectAsync(Guid projectId);

         Task<MonthlyHoursDashboardDto> GetMonthlyDashboardAsync(Guid userId, int year, int month);
        Task<YtdDashboardDto> GetYtdDashboardAsync(Guid userId, int companyYear);

         Task<IEnumerable<HourEntryDto>> GetPendingPremiumAsync();
        Task ApprovePremiumAsync(ApprovePremiumDto dto);

         Task<IEnumerable<ProjectSummaryDto>> GetMyProjectsAsync(Guid userId);
         Task<IEnumerable<ProjectInternAllocationDto>> GetSupervisedInternsAsync(Guid userId);
    }
}
