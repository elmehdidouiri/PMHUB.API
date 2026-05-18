using PMHUB.Application.DTOs;

namespace PMHUB.Application.IServices
{
    public interface IHoursAllocationDashboardService
    {
        Task<HoursAllocationFiltersDto> GetFiltersAsync();
        Task<HoursAllocationDashboardDto> GetDashboardAsync(HoursAllocationDashboardQueryDto query);
        Task<HoursAllocationReminderResultDto> SendRemindersAsync(HoursAllocationReminderRequestDto request);
    }
}
