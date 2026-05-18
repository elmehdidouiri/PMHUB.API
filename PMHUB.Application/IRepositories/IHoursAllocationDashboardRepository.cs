using PMHUB.Application.DTOs;

namespace PMHUB.Application.IRepositories
{
    public interface IHoursAllocationDashboardRepository
    {
        Task<HoursAllocationFiltersDto> GetFiltersAsync();
        Task<HoursAllocationDashboardDto> GetDashboardAsync(HoursAllocationDashboardQueryDto query);
        Task<List<HoursAllocationReminderRecipientDto>> GetReminderRecipientsAsync(HoursAllocationDashboardQueryDto query);
    }
}
